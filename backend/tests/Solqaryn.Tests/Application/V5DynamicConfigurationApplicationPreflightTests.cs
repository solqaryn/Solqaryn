using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5DynamicConfigurationDomainRules;
using Gate = Solqaryn.Application.AutomationV5.V5DynamicConfigurationApplicationPreflight;

namespace Solqaryn.Tests.Application;

public sealed class V5DynamicConfigurationApplicationPreflightTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static Rules.TrustedAuthority TrustedAuthority() =>
        new(11, true, true, true, true, false, false, false);

    private static Gate.ActorEvidence CurrentActor() =>
        new(7, 11, Sha, true, true, TrustedAuthority());

    private static Gate.CatalogEvidence CurrentCatalog() =>
        new(11, Sha, true, true, 3,
            new Rules.CatalogKey("ui.compactMode", Rules.ValueKind.Boolean,
                Rules.ConfigScope.Tenant, false, 120, 0, 100),
            new HashSet<string>(StringComparer.Ordinal) { "ui.compactMode" });

    private static Gate.ClientIntent Intent(string value = "true") =>
        new("ui.compactMode", Rules.ValueKind.Boolean, Rules.ConfigScope.Tenant,
            11, value, 3, Sha);

    private sealed class Actors : IV5DynamicConfigurationCurrentActorReader
    {
        public Gate.ActorEvidence? Value { get; set; } = CurrentActor();
        public int Calls { get; private set; }
        public bool Throws { get; set; }
        public ValueTask<Gate.ActorEvidence?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Throws) throw new InvalidOperationException("private actor source");
            return ValueTask.FromResult(Value);
        }
    }

    private sealed class Catalogs : IV5DynamicConfigurationServerCatalogReader
    {
        public Gate.CatalogEvidence? Value { get; set; } = CurrentCatalog();
        public int? SeenTenant { get; private set; }
        public string? SeenKey { get; private set; }
        public int Calls { get; private set; }
        public bool Throws { get; set; }
        public ValueTask<Gate.CatalogEvidence?> ReadAsync(
            int authenticatedTenantId, string exactKey, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            SeenTenant = authenticatedTenantId;
            SeenKey = exactKey;
            if (Throws) throw new InvalidOperationException("private catalog source");
            return ValueTask.FromResult(Value);
        }
    }

    [Fact]
    public async Task ValidServerEvidenceStillNeverAuthorizesMutation()
    {
        var actor = new Actors();
        var catalog = new Catalogs();
        var result = await new Gate(actor, catalog).ReviewAsync(Intent());
        Assert.Equal("AUTHENTICATED_DURABLE_CONFIG_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresAuthenticatedCasAdapter);
        Assert.False(result.GrantsWrite);
        Assert.False(result.GrantsPlatformOverride);
        Assert.False(result.GrantsCrossTenantRead);
        Assert.False(result.CertifiesDurableCas);
        Assert.Equal(11, catalog.SeenTenant);
        Assert.Equal("ui.compactMode", catalog.SeenKey);
    }

    [Fact]
    public async Task InvalidInputsStopBeforeAnyBackendRead()
    {
        var actors = new Actors();
        var catalog = new Catalogs();
        var gate = new Gate(actors, catalog);
        Assert.Equal("VALID_CONFIG_INTENT_AND_SOURCE_REQUIRED",
            (await gate.ReviewAsync(null)).Code);
        Assert.Equal("VALID_CONFIG_INTENT_AND_SOURCE_REQUIRED",
            (await gate.ReviewAsync(Intent() with { ExpectedSourceSha = "bad" })).Code);
        Assert.Equal("VALID_CONFIG_INTENT_AND_SOURCE_REQUIRED",
            (await gate.ReviewAsync(Intent() with { ExpectedVersion = 0 })).Code);
        Assert.Equal("SEPARATE_PLATFORM_AUTHORITY_REQUIRED",
            (await gate.ReviewAsync(Intent() with {
                RequestedScope = Rules.ConfigScope.Platform })).Code);
        Assert.Equal("CLIENT_AUTHORITY_AND_BYPASS_DENIED",
            (await gate.ReviewAsync(Intent() with {
                ClientSuppliesAuthority = true })).Code);
        Assert.Equal(0, actors.Calls);
        Assert.Equal(0, catalog.Calls);
    }

    [Fact]
    public async Task RevokedOrStaleBackendActorCannotQueryTenantCatalog()
    {
        var actor = new Actors();
        var catalog = new Catalogs();
        var gate = new Gate(actor, catalog);
        actor.Value = CurrentActor() with { MembershipAndGrantsFresh = false };
        Assert.Equal("CURRENT_BACKEND_IDENTITY_REQUIRED",
            (await gate.ReviewAsync(Intent())).Code);
        actor.Value = CurrentActor() with { SourceSha = OtherSha };
        Assert.Equal("CURRENT_BACKEND_IDENTITY_REQUIRED",
            (await gate.ReviewAsync(Intent())).Code);
        actor.Value = CurrentActor() with { Authority = TrustedAuthority() with {
            AuthenticatedTenantId = 22 } };
        Assert.Equal("CURRENT_BACKEND_IDENTITY_REQUIRED",
            (await gate.ReviewAsync(Intent())).Code);
        Assert.Equal(0, catalog.Calls);
    }

    [Fact]
    public async Task ClientTenantSpoofingCannotSelectTheCatalogTenant()
    {
        var catalog = new Catalogs();
        var result = await new Gate(new Actors(), catalog)
            .ReviewAsync(Intent() with { TenantAssertion = 22 });
        Assert.Equal("CLIENT_TENANT_MISMATCH", result.Code);
        Assert.Equal(0, catalog.Calls);
    }

    [Fact]
    public async Task ForeignOrUntrustedCatalogIsDenied()
    {
        var catalog = new Catalogs();
        var gate = new Gate(new Actors(), catalog);
        catalog.Value = CurrentCatalog() with { OwnerTenantId = 22 };
        Assert.Equal("PERSISTED_TENANT_CATALOG_EVIDENCE_REQUIRED",
            (await gate.ReviewAsync(Intent())).Code);
        catalog.Value = CurrentCatalog() with { OwnershipVerified = false };
        Assert.Equal("PERSISTED_TENANT_CATALOG_EVIDENCE_REQUIRED",
            (await gate.ReviewAsync(Intent())).Code);
        catalog.Value = CurrentCatalog() with { SourceSha = OtherSha };
        Assert.Equal("PERSISTED_TENANT_CATALOG_EVIDENCE_REQUIRED",
            (await gate.ReviewAsync(Intent())).Code);
    }

    [Fact]
    public async Task StaleVersionsAndUnsafeValuesStayDenied()
    {
        var catalog = new Catalogs();
        var gate = new Gate(new Actors(), catalog);
        Assert.Equal("STALE_CONFIG_VERSION_DENIED",
            (await gate.ReviewAsync(Intent() with { ExpectedVersion = 2 })).Code);
        Assert.Equal("CONFIG_VALUE_INVALID_OR_UNSAFE",
            (await gate.ReviewAsync(Intent("<script>alert(1)</script>"))).Code);
        catalog.Value = CurrentCatalog() with {
            ApprovedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "ui.compactMode" } };
        Assert.Equal("CATALOG_KEY_NOT_ALLOWED",
            (await gate.ReviewAsync(Intent() with { Key = "UI.compactMode" })).Code);
    }

    [Fact]
    public async Task DomainStillRejectsRevocationAndMissingRbac()
    {
        var actor = new Actors();
        var gate = new Gate(actor, new Catalogs());
        actor.Value = CurrentActor() with {
            Authority = TrustedAuthority() with { PermissionRevoked = true } };
        Assert.Equal("SERVER_MEMBERSHIP_AND_RBAC_REQUIRED",
            (await gate.ReviewAsync(Intent())).Code);
        actor.Value = CurrentActor() with {
            Authority = TrustedAuthority() with { RbacConfigWritePermission = false } };
        Assert.Equal("SERVER_MEMBERSHIP_AND_RBAC_REQUIRED",
            (await gate.ReviewAsync(Intent())).Code);
    }

    [Fact]
    public async Task SourceErrorsAreOpaqueAndCancellationPropagates()
    {
        var actors = new Actors { Throws = true };
        var catalog = new Catalogs();
        var gate = new Gate(actors, catalog);
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_UNAVAILABLE",
            (await gate.ReviewAsync(Intent())).Code);
        Assert.Equal(0, catalog.Calls);
        actors.Throws = false;
        catalog.Throws = true;
        Assert.Equal("TRUSTED_CONFIG_CATALOG_UNAVAILABLE",
            (await gate.ReviewAsync(Intent())).Code);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => {
            await gate.ReviewAsync(Intent(), cts.Token);
        });
    }

    [Fact]
    public void MissingSourcesCannotConstructService()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Gate(null!, new Catalogs()));
        Assert.Throws<ArgumentNullException>(() =>
            new Gate(new Actors(), null!));
    }
}
