using Solqaryn.Domain.Security;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C10-04.M03.03: barrera de autorización fail-closed sin efectos externos.
/// La fuente debe verificar desde backend autenticación, tenant, owner, RBAC y política.
/// No reemplaza JWT/HTTP, CAS persistente ni autorización de un proveedor real.
/// </summary>
public sealed class C1004CacheJobsAuthorizationGate
{
    private readonly IC1004VerifiedWorkEvidenceSource _source;
    public C1004CacheJobsAuthorizationGate(IC1004VerifiedWorkEvidenceSource source) =>
        _source = source ?? throw new ArgumentNullException(nameof(source));

    public readonly record struct Decision(string Code)
    {
        public bool CanEnqueue => false;
        public bool CanWrite => false;
        public bool CanAcquireLease => false;
        public bool CanGrantProviderAccess => false;
        public bool CanReadPrivateArtifact => false;
        public bool CertifiesRuntimeSecurity => false;
    }

    public async Task<Decision> CheckAsync(
        C1004CacheJobsApplicationService.Request? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || !SafeId(request.WorkId) || !SafeOperation(request.Operation) ||
            string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
            string.IsNullOrWhiteSpace(request.PayloadSha256) || request.ExpectedVersion <= 0)
            return Deny("REQUEST_INVALID");

        cancellationToken.ThrowIfCancellationRequested();
        C1004CacheJobsApplicationService.VerifiedSnapshot? snapshot;
        try
        {
            snapshot = await _source.ReadVerifiedAsync(request.WorkId!, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("VERIFIED_SOURCE_UNAVAILABLE");
        }

        if (snapshot?.Context is null || snapshot.AllowedOperations is null ||
            snapshot.VerifiedActorId <= 0 || snapshot.PersistedResourceEmpresaId <= 0 ||
            snapshot.Context.RolId <= 0)
            return Deny("VERIFIED_BACKEND_CONTEXT_REQUIRED");

        if (snapshot.Context.UsuarioId != snapshot.VerifiedActorId ||
            snapshot.Context.EmpresaId != snapshot.PersistedResourceEmpresaId)
            return Deny("ACTOR_OR_TENANT_MISMATCH");

        if (!snapshot.ActiveMembership || snapshot.PermissionRevoked ||
            !snapshot.ExplicitOperationPermission ||
            (snapshot.IsServiceActor && !snapshot.ServiceGrantVerified))
            return Deny("PERMISSION_OR_MEMBERSHIP_DENIED");

        // El tenant que manda el cliente es como maximo una comprobacion de consistencia.
        if (request.ClientTenantHint.HasValue &&
            request.ClientTenantHint.Value != snapshot.PersistedResourceEmpresaId)
            return Deny("CLIENT_TENANT_OVERRIDE_DENIED");

        if (!snapshot.AllowedOperations.Contains(request.Operation!))
            return Deny("OPERATION_NOT_ALLOWLISTED");

        if (request.ExpectedVersion != snapshot.PersistedVersion)
            return Deny("PERSISTED_VERSION_STALE");

        try
        {
            C1004CacheJobsDomainRules.ValidarSolicitudTrabajo(
                snapshot.Context, snapshot.VerifiedActorId, snapshot.PersistedResourceEmpresaId,
                request.Operation!, request.IdempotencyKey!, request.PayloadSha256!,
                request.ExpectedVersion, snapshot.RetryAttempt, snapshot.RetryLimit,
                snapshot.DeadlineUtc, snapshot.ServerNowUtc);
        }
        catch (ArgumentException)
        {
            return Deny("REQUEST_PARAMETERS_INVALID");
        }
        catch (InvalidOperationException)
        {
            return Deny("TENANT_OR_POLICY_DENIED");
        }

        // Ninguna comprobacion de forma concede un permiso de ejecucion.
        return new Decision("PRECONDITIONS_ONLY_EXTERNAL_CAS_REQUIRED");
    }

    private static Decision Deny(string code) => new(code);
    private static bool SafeId(string? value) =>
        value is { Length: >= 1 and <= 128 } && value.All(c =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
            (c >= '0' && c <= '9') || c is '-' or '_');
    private static bool SafeOperation(string? value) =>
        value is { Length: >= 3 and <= 64 } && value.All(c =>
            (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
            c is '.' or '_' or '-');
}
