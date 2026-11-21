using Rules = Solqaryn.Domain.AutomationV5.C0701CatalogDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Fuente futura de autoridad autenticada real: implementacion ajena a M03.02.
/// Un usuario o DTO no puede fabricar un VerifiedActor.
/// </summary>
public interface IC0701VerifiedCatalogActorReader
{
    ValueTask<C0701CatalogApplicationService.VerifiedActor?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>
/// Adaptador futuro de consulta filtrada por tenant verificado en servidor.
/// No conecta Aiven ni hace llamadas desde esta microtarea.
/// </summary>
public interface IC0701VerifiedCatalogResourceReader
{
    ValueTask<C0701CatalogApplicationService.VerifiedResource?> ReadOwnedAsync(
        int authenticatedTenantId, int productoId, CancellationToken cancellationToken);
}

/// <summary>
/// C07-01.M03.02: caso de uso REAL y verificable de preflight de catalogo.
/// Application no se registra en DI ni expone un API; NO concede lectura/escritura,
/// cobra, persiste ni certifica JWT/MySQL. Los adapters concretos deben acreditar
/// JWT firmado, RBAC relacional, tenant, propietario y version al operar.
/// </summary>
public sealed class C0701CatalogApplicationService
{
    private readonly IC0701VerifiedCatalogActorReader _actors;
    private readonly IC0701VerifiedCatalogResourceReader _resources;

    public C0701CatalogApplicationService(
        IC0701VerifiedCatalogActorReader actors,
        IC0701VerifiedCatalogResourceReader resources)
    {
        _actors = actors ?? throw new ArgumentNullException(nameof(actors));
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public sealed record ClientIntent(
        int? EmpresaHint,
        int ProductoId,
        long VersionEsperada,
        Rules.Mutation? Propuesta,
        bool IncluyePermisoORolDelCliente = false,
        bool SolicitaEscrituraDirecta = false);

    public sealed record VerifiedActor(
        Rules.VerifiedServerScope Scope,
        bool JwtFirmadoVerificado,
        bool TenantResueltoEnServidor,
        bool MembresiaFresca,
        bool RbacRelacionalFresco);

    public sealed record VerifiedResource(
        Rules.ProductSnapshot Producto,
        bool ConsultaFiltradaPorTenant,
        bool PropiedadVerificadaEnServidor);

    public readonly record struct Result(string Code, bool RequiresLiveAuthorizedTransaction)
    {
        public bool AuthorizesReadOrWrite => false;
        public bool PersistsData => false;
        public bool PermitsCrossTenant => false;
        public bool CertifiesJwtMysqlOrHttp => false;
    }

    public async ValueTask<Result> PrepareAsync(
        ClientIntent? request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || request.ProductoId <= 0 ||
            request.VersionEsperada <= 0 || request.Propuesta is null ||
            request.Propuesta.ProductoId != request.ProductoId ||
            request.Propuesta.ExpectedVersion != request.VersionEsperada)
            return Deny("CATALOG_INTENT_INVALID_OR_STALE");

        if (request.IncluyePermisoORolDelCliente || request.SolicitaEscrituraDirecta)
            return Deny("UNTRUSTED_CLIENT_AUTHORITY_OR_DIRECT_WRITE_DENIED");

        VerifiedActor? actor;
        try { actor = await _actors.ReadCurrentAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return Deny("AUTHORITY_PROVIDER_UNAVAILABLE"); }

        var scope = actor?.Scope;
        if (scope is null || scope.EmpresaId <= 0 || scope.ActorId <= 0 ||
            scope.RoleId <= 0 || !actor!.JwtFirmadoVerificado ||
            !actor.TenantResueltoEnServidor || !actor.MembresiaFresca ||
            !actor.RbacRelacionalFresco || !scope.TenantActive ||
            !scope.MembershipActive || !scope.RoleActive ||
            scope.PermissionRevoked || !scope.CatalogRbacGranted)
            return Deny("LIVE_BACKEND_AUTHORITY_REQUIRED");

        // La empresa del cliente es solo una comprobacion, nunca autoridad.
        if (request.EmpresaHint.HasValue &&
            request.EmpresaHint.Value != scope.EmpresaId)
            return Deny("CLIENT_TENANT_MISMATCH");

        cancellationToken.ThrowIfCancellationRequested();
        VerifiedResource? resource;
        try
        {
            resource = await _resources.ReadOwnedAsync(
                scope.EmpresaId, request.ProductoId, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        { return Deny("OWNED_RESOURCE_PROVIDER_UNAVAILABLE"); }

        if (resource is null || resource.Producto is null ||
            !resource.ConsultaFiltradaPorTenant ||
            !resource.PropiedadVerificadaEnServidor ||
            resource.Producto.EmpresaId != scope.EmpresaId ||
            resource.Producto.ProductoId != request.ProductoId)
            return Deny("TRUSTED_OWNED_RESOURCE_REQUIRED");

        Rules.Decision domain = Rules.Evaluate(
            scope, resource.Producto, request.Propuesta);

        // Una comprobacion positiva de Domain es solo preparatoria.
        // JWT/RBAC/propiedad/Version se deben REVERIFICAR atomicamente en el
        // endpoint real antes de cualquier transaccion de escritura MySQL.
        return domain.Code == "DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED"
            ? new("LIVE_ATOMIC_AUTHORIZATION_AND_VERSION_RECHECK_REQUIRED", true)
            : Deny(domain.Code);
    }

    private static Result Deny(string code) => new(code, false);
}
