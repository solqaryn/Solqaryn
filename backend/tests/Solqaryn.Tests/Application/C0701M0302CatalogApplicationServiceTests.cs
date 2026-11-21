using System.Threading;
using Solqaryn.Application.AutomationV5;
using Xunit;
using CatalogRules = Solqaryn.Domain.AutomationV5.C0701CatalogDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0701CatalogApplicationService;

namespace Solqaryn.Tests.Application;

public sealed class C0701M0302CatalogApplicationServiceTests
{
    private sealed class ActorReader : IC0701VerifiedCatalogActorReader
    {
        public Service.VerifiedActor? Value { get; set; } = Actor();
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }
        public ValueTask<Service.VerifiedActor?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Failure is not null) throw Failure;
            return ValueTask.FromResult(Value);
        }
    }

    private sealed class ProductReader : IC0701VerifiedCatalogResourceReader
    {
        public Service.VerifiedResource? Value { get; set; } = Resource();
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }
        public int LastTenantId { get; private set; }
        public int LastProductId { get; private set; }
        public ValueTask<Service.VerifiedResource?> ReadOwnedAsync(
            int tenantId, int productId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            LastTenantId = tenantId;
            LastProductId = productId;
            if (Failure is not null) throw Failure;
            return ValueTask.FromResult(Value);
        }
    }

    private static Service.VerifiedActor Actor() =>
        new(new CatalogRules.VerifiedServerScope(100, 17, 9, 11, true, true, true, true, false),
            true, true, true, true);
    private static Service.VerifiedResource Resource() =>
        new(new CatalogRules.ProductSnapshot(100, 42, 10, 11, 100, 100, true, false),
            true, true);
    private static CatalogRules.Mutation Mutation() =>
        new(42, 10, "PRODUCTO", "SKU-42", "Producto A",
            120m, 50m, 1500, true, true, true, true);
    private static Service.ClientIntent Intent() =>
        new(100, 42, 10, Mutation());
    private static Service Build(ActorReader a, ProductReader p) => new(a, p);

    [Fact]
    public async Task ValidUseCaseRequiresFutureAtomicBackendTransactionButCannotGrant()
    {
        var a = new ActorReader(); var p = new ProductReader();
        var result = await Build(a, p).PrepareAsync(Intent());
        Assert.Equal("LIVE_ATOMIC_AUTHORIZATION_AND_VERSION_RECHECK_REQUIRED", result.Code);
        Assert.True(result.RequiresLiveAuthorizedTransaction);
        Assert.False(result.AuthorizesReadOrWrite);
        Assert.False(result.PersistsData);
        Assert.False(result.PermitsCrossTenant);
        Assert.False(result.CertifiesJwtMysqlOrHttp);
        Assert.Equal(100, p.LastTenantId);
        Assert.Equal(42, p.LastProductId);
        Assert.Equal(1, a.Calls);
        Assert.Equal(1, p.Calls);
    }

    [Fact]
    public async Task InvalidClientRequestDoesNotEvenQueryTrustedAdapters()
    {
        var a = new ActorReader(); var p = new ProductReader();
        var svc = Build(a, p);
        foreach (var request in new Service.ClientIntent?[]
        {
            null, Intent() with { ProductoId = 0 },
            Intent() with { VersionEsperada = 0 },
            Intent() with { Propuesta = null },
            Intent() with { Propuesta = Mutation() with { ProductoId = 99 } },
            Intent() with { Propuesta = Mutation() with { ExpectedVersion = 9 } }
        })
            Assert.Equal("CATALOG_INTENT_INVALID_OR_STALE", (await svc.PrepareAsync(request)).Code);
        Assert.Equal(0, a.Calls);
        Assert.Equal(0, p.Calls);
    }

    [Fact]
    public async Task ClientGrantsAndDirectWritesAreNotAccepted()
    {
        var a = new ActorReader(); var p = new ProductReader();
        var svc = Build(a, p);
        Assert.Equal("UNTRUSTED_CLIENT_AUTHORITY_OR_DIRECT_WRITE_DENIED",
            (await svc.PrepareAsync(Intent() with { IncluyePermisoORolDelCliente = true })).Code);
        Assert.Equal("UNTRUSTED_CLIENT_AUTHORITY_OR_DIRECT_WRITE_DENIED",
            (await svc.PrepareAsync(Intent() with { SolicitaEscrituraDirecta = true })).Code);
        Assert.Equal(0, a.Calls);
    }

    [Fact]
    public async Task RevokedAndUnverifiedJwtTenantRoleNeverTouchPrivateResource()
    {
        foreach (var bad in new[]
        {
            Actor() with { JwtFirmadoVerificado = false },
            Actor() with { TenantResueltoEnServidor = false },
            Actor() with { MembresiaFresca = false },
            Actor() with { RbacRelacionalFresco = false },
            Actor() with { Scope = Actor().Scope with { PermissionRevoked = true } },
            Actor() with { Scope = Actor().Scope with { CatalogRbacGranted = false } }
        })
        {
            var a = new ActorReader { Value = bad }; var p = new ProductReader();
            Assert.Equal("LIVE_BACKEND_AUTHORITY_REQUIRED",
                (await Build(a, p).PrepareAsync(Intent())).Code);
            Assert.Equal(0, p.Calls);
        }
    }

    [Fact]
    public async Task TenantHintIsNotAuthorityAndCannotSelectOtherTenant()
    {
        var a = new ActorReader(); var p = new ProductReader();
        var result = await Build(a, p).PrepareAsync(Intent() with { EmpresaHint = 200 });
        Assert.Equal("CLIENT_TENANT_MISMATCH", result.Code);
        Assert.Equal(0, p.Calls);
    }

    [Fact]
    public async Task MissingOrForgedTenantResourceIsDeniedAfterTenantScopedQuery()
    {
        foreach (var bad in new Service.VerifiedResource?[]
        {
            null, Resource() with { ConsultaFiltradaPorTenant = false },
            Resource() with { PropiedadVerificadaEnServidor = false },
            Resource() with { Producto = Resource().Producto with { EmpresaId = 200 } },
            Resource() with { Producto = Resource().Producto with { EmpresaId = null } }
        })
        {
            var p = new ProductReader { Value = bad };
            Assert.Equal("TRUSTED_OWNED_RESOURCE_REQUIRED",
                (await Build(new ActorReader(), p).PrepareAsync(Intent())).Code);
            Assert.Equal(100, p.LastTenantId);
        }
    }

    [Fact]
    public async Task DomainDenialsRemainDenialsNeverTurnedIntoApplicationGrant()
    {
        var p = new ProductReader(); var svc = Build(new ActorReader(), p);
        var stale = await svc.PrepareAsync(Intent() with
        { Propuesta = Mutation() with { Precio = -1m } });
        Assert.Equal("PRICE_COST_OR_TAX_INVALID", stale.Code);
        Assert.False(stale.RequiresLiveAuthorizedTransaction);
        Assert.False(stale.AuthorizesReadOrWrite);
    }

    [Fact]
    public async Task BrokenActorAndCatalogProvidersFailClosedWithoutExceptionOrData()
    {
        var a = new ActorReader { Failure = new InvalidOperationException("internal auth") };
        var p = new ProductReader();
        var result = await Build(a, p).PrepareAsync(Intent());
        Assert.Equal("AUTHORITY_PROVIDER_UNAVAILABLE", result.Code);
        Assert.Equal(0, p.Calls);
        var broken = new ProductReader { Failure = new InvalidOperationException("db connection") };
        Assert.Equal("OWNED_RESOURCE_PROVIDER_UNAVAILABLE",
            (await Build(new ActorReader(), broken).PrepareAsync(Intent())).Code);
    }

    [Fact]
    public async Task CancellationPropagatesAndDoesNotQuerySources()
    {
        var a = new ActorReader(); var p = new ProductReader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Build(a, p).PrepareAsync(Intent(), cts.Token));
        Assert.Equal(0, a.Calls);
        Assert.Equal(0, p.Calls);
    }

    [Fact]
    public async Task UnknownTenantAndStaleResourceVersionRemainDenied()
    {
        var p = new ProductReader
        {
            Value = Resource() with { Producto = Resource().Producto with { Version = 11 } }
        };
        var result = await Build(new ActorReader(), p).PrepareAsync(Intent());
        Assert.Equal("CATALOG_STALE_VERSION", result.Code);
        Assert.False(result.RequiresLiveAuthorizedTransaction);
        var missingActor = new ActorReader { Value = null };
        Assert.Equal("LIVE_BACKEND_AUTHORITY_REQUIRED",
            (await Build(missingActor, new ProductReader()).PrepareAsync(Intent())).Code);
    }
}
