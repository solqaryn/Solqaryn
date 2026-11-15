using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5DynamicConfigurationDomainRules;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0103DynamicConfigurationAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static Rules.TrustedAuthority Authority() =>
        new(11, true, true, true, true, false, false, false);

    private static C0103DynamicConfigurationAuthorizationGate.ActorSnapshot Actor() =>
        new(7, 11, Sha, true, true, true, true, false, Authority());

    private static C0103DynamicConfigurationAuthorizationGate.CatalogSnapshot Catalog() =>
        new(11, Sha, 3, true, true,
            new("ui.compactMode", Rules.ValueKind.Boolean, Rules.ConfigScope.Tenant,
                false, 0, null, null),
            new HashSet<string>(StringComparer.Ordinal) { "ui.compactMode" });

    private static V5DynamicConfigurationApplicationPreflight.ClientIntent Intent() =>
        new("ui.compactMode", Rules.ValueKind.Boolean, Rules.ConfigScope.Tenant,
            11, "true", 3, Sha);

    private sealed class Actors : IC0103TrustedConfigurationActorSource
    {
        public C0103DynamicConfigurationAuthorizationGate.ActorSnapshot? Snapshot = Actor();
        public Exception? Error;
        public int Calls;
        public ValueTask<C0103DynamicConfigurationAuthorizationGate.ActorSnapshot?> ReadCurrentAsync(
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Snapshot);
        }
    }

    private sealed class Catalogs : IC0103TenantConfigurationCatalogSource
    {
        public C0103DynamicConfigurationAuthorizationGate.CatalogSnapshot? Snapshot = Catalog();
        public Exception? Error;
        public int Calls;
        public int LastTenant;
        public string? LastKey;
        public ValueTask<C0103DynamicConfigurationAuthorizationGate.CatalogSnapshot?> ReadAsync(
            int tenant, string key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            LastTenant = tenant;
            LastKey = key;
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Snapshot);
        }
    }

    private static C0103DynamicConfigurationAuthorizationGate Gate(
        Actors? a = null, Catalogs? c = null) => new(a ?? new(), c ?? new());

    [Fact]
    public async Task NominalReviewAlwaysRequiresFutureAuthenticatedDurableCas()
    {
        var c = new Catalogs();
        var x = await Gate(c: c).InspectAsync(Intent());
        Assert.Equal("AUTHENTICATED_CONFIGURATION_TRANSACTION_AND_A5_REQUIRED", x.Code);
        Assert.True(x.NeedsAuthenticatedDurableCasAdapter);
        Assert.False(x.GrantsConfigurationWrite);
        Assert.False(x.AllowsPlatformOverride);
        Assert.False(x.AllowsCrossTenantRead);
        Assert.False(x.CertifiesDurableCas);
        Assert.False(x.AuthorizesQaOrProduction);
        Assert.Equal(11, c.LastTenant);
        Assert.Equal("ui.compactMode", c.LastKey);
    }

    [Fact]
    public async Task InvalidIntentDoesNotCallAnyTrustedReader()
    {
        var a = new Actors();
        var c = new Catalogs();
        var g = Gate(a, c);
        Assert.Equal("VALID_CONFIG_KEY_VERSION_SHA_REQUIRED",
            (await g.InspectAsync(null)).Code);
        Assert.Equal("VALID_CONFIG_KEY_VERSION_SHA_REQUIRED",
            (await g.InspectAsync(Intent() with { Key = "" })).Code);
        Assert.Equal("VALID_CONFIG_KEY_VERSION_SHA_REQUIRED",
            (await g.InspectAsync(Intent() with { ExpectedSourceSha = "wrong" })).Code);
        Assert.Equal("VALID_CONFIG_KEY_VERSION_SHA_REQUIRED",
            (await g.InspectAsync(Intent() with { ExpectedVersion = 0 })).Code);
        Assert.Equal(0, a.Calls);
        Assert.Equal(0, c.Calls);
    }

    [Fact]
    public async Task PlatformScopeAndClientPrivilegeOverrideCannotReadBackend()
    {
        var a = new Actors();
        var g = Gate(a);
        Assert.Equal("INDEPENDENT_PLATFORM_AUTHORIZATION_REQUIRED",
            (await g.InspectAsync(Intent() with { RequestedScope = Rules.ConfigScope.Platform })).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_SECURITY_BYPASS_DENIED",
            (await g.InspectAsync(Intent() with { ClientSuppliesAuthority = true })).Code);
        Assert.Equal("CLIENT_AUTHORITY_OR_SECURITY_BYPASS_DENIED",
            (await g.InspectAsync(Intent() with { RequestsSecurityBypass = true })).Code);
        Assert.Equal(0, a.Calls);
    }

    [Fact]
    public async Task MissingActorAndRevokedIdentityCannotReadCatalog()
    {
        var a = new Actors { Snapshot = null };
        var c = new Catalogs();
        var g = Gate(a, c);
        Assert.Equal("CURRENT_TRUSTED_JWT_MEMBERSHIP_RBAC_AND_TENANT_REQUIRED",
            (await g.InspectAsync(Intent())).Code);
        foreach (var actor in new[] {
            Actor() with { CurrentBackendJwtVerified = false },
            Actor() with { MembershipFresh = false },
            Actor() with { RelationalRbacGrantFresh = false },
            Actor() with { EntitlementFresh = false },
            Actor() with { Revoked = true },
            Actor() with { Authority = Authority() with { PermissionRevoked = true } },
            Actor() with { TenantId = 22 },
            Actor() with { SourceSha = new string('f', 40) }
        })
        {
            a.Snapshot = actor;
            Assert.Equal("CURRENT_TRUSTED_JWT_MEMBERSHIP_RBAC_AND_TENANT_REQUIRED",
                (await g.InspectAsync(Intent())).Code);
        }
        Assert.Equal(0, c.Calls);
    }

    [Fact]
    public async Task ClientTenantBIsNeverAuthoritativeTenantForDatabaseRead()
    {
        var c = new Catalogs();
        Assert.Equal("CROSS_TENANT_CLIENT_ASSERTION_DENIED",
            (await Gate(c: c).InspectAsync(Intent() with { TenantAssertion = 22 })).Code);
        Assert.Equal(0, c.Calls);
    }

    [Fact]
    public async Task MissingUnownedOrStaleCatalogFailsClosed()
    {
        var c = new Catalogs();
        var g = Gate(c: c);
        foreach (var catalog in new C0103DynamicConfigurationAuthorizationGate.CatalogSnapshot?[] {
            null, Catalog() with { OwnerTenantId = 22 },
            Catalog() with { BackendReadVerified = false },
            Catalog() with { PersistedOwnerVerified = false },
            Catalog() with { Version = 0 },
            Catalog() with { SourceSha = new string('e', 40) },
            Catalog() with { Definition = null }
        })
        {
            c.Snapshot = catalog;
            Assert.Equal("PERSISTED_TENANT_CATALOG_AND_CANONICAL_KEY_REQUIRED",
                (await g.InspectAsync(Intent())).Code);
        }
    }

    [Fact]
    public async Task CaseInsensitiveCatalogComparerCannotBroadenExactAllowlist()
    {
        var c = new Catalogs { Snapshot = Catalog() with
        {
            Definition = Catalog().Definition! with { CanonicalKey = "UI.compactMode" },
            ApprovedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ui.compactMode" }
        }};
        Assert.Equal("CATALOG_KEY_NOT_ALLOWED",
            (await Gate(c: c).InspectAsync(Intent() with { Key = "UI.compactMode" })).Code);
        c.Snapshot = Catalog();
        Assert.True((await Gate(c: c).InspectAsync(Intent())).NeedsAuthenticatedDurableCasAdapter);
    }

    [Fact]
    public async Task StaleVersionInvalidTypeOrUnsafeValueCannotPass()
    {
        Assert.Equal("STALE_CONFIG_VERSION_DENIED",
            (await Gate().InspectAsync(Intent() with { ExpectedVersion = 2 })).Code);
        Assert.Equal("CONFIG_TYPE_OR_SCOPE_MISMATCH",
            (await Gate().InspectAsync(Intent() with { DeclaredKind = Rules.ValueKind.SafeString })).Code);
        Assert.Equal("CONFIG_VALUE_INVALID_OR_UNSAFE",
            (await Gate().InspectAsync(Intent() with { Value = "not-bool" })).Code);
    }

    [Fact]
    public async Task ReservedSecurityKeyAndSecretDefinitionDoNotGrant()
    {
        var catalog = new Catalogs { Snapshot = Catalog() with
        {
            Definition = Catalog().Definition! with { CanonicalKey = "jwt.force" },
            ApprovedKeys = new HashSet<string>(StringComparer.Ordinal) { "jwt.force" }
        }};
        Assert.Equal("CATALOG_KEY_NOT_ALLOWED",
            (await Gate(c: catalog).InspectAsync(Intent() with { Key = "jwt.force" })).Code);
        catalog.Snapshot = Catalog() with
        { Definition = Catalog().Definition! with { ContainsSecret = true } };
        Assert.Equal("SECRET_CONFIGURATION_NOT_EDITABLE",
            (await Gate(c: catalog).InspectAsync(Intent())).Code);
    }

    [Fact]
    public async Task BackendReaderErrorsAreOpaqueAndDoNotGrant()
    {
        var a = new Actors { Error = new InvalidOperationException("private jwt") };
        var c = new Catalogs();
        Assert.Equal("CURRENT_ACTOR_BACKEND_SOURCE_UNAVAILABLE",
            (await Gate(a, c).InspectAsync(Intent())).Code);
        Assert.Equal(0, c.Calls);
        a.Error = null;
        c.Error = new InvalidOperationException("secret tenant config");
        var result = await Gate(a, c).InspectAsync(Intent());
        Assert.Equal("CURRENT_TENANT_CATALOG_SOURCE_UNAVAILABLE", result.Code);
        Assert.False(result.GrantsConfigurationWrite);
    }

    [Fact]
    public async Task CancellationPropagatesWithoutReaderActivity()
    {
        var a = new Actors();
        var c = new Catalogs();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await Gate(a, c).InspectAsync(Intent(), cts.Token));
        Assert.Equal(0, a.Calls);
        Assert.Equal(0, c.Calls);
    }

    [Fact]
    public void MissingDependencyIsRejectedAtConstruction()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new C0103DynamicConfigurationAuthorizationGate(null!, new Catalogs()));
        Assert.Throws<ArgumentNullException>(() =>
            new C0103DynamicConfigurationAuthorizationGate(new Actors(), null!));
    }
}
