using App = Solqaryn.Application.AutomationV5.C0501DesignTokenApplicationService;
using Rules = Solqaryn.Domain.AutomationV5.C0501DesignTokenDomainRules;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0501DesignTokenIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";
    private const string Idempotency = "2c85f621-314a-4f81-88c4-8054e61f812d";

    private static App.ActorSnapshot Actor() =>
        new(new Rules.TrustedContext(7, 11, true, true, true, false, 7, 9),
            true, true, true, true);

    private static App.CatalogSnapshot Catalog() =>
        new(11, "primary-color", 7,
            new HashSet<string>(StringComparer.Ordinal) { "primary-color" },
            true, true);

    private static App.ClientIntent Intent() =>
        new(11, 9, 7, "primary-color", "COLOR", "#123AbC", Idempotency);

    private static C0501DesignTokenIsolatedSnapshotAdapter Adapter(
        App.ActorSnapshot? actor = null, App.CatalogSnapshot? catalog = null) =>
        new(actor ?? Actor(), catalog ?? Catalog(), Sha);

    private static ValueTask<App.Result> Check(
        C0501DesignTokenIsolatedSnapshotAdapter adapter,
        App.ClientIntent? intent = null, int actorId = 7, int tenant = 11,
        long revision = 7, int epoch = 9, string? sha = Sha,
        CancellationToken ct = default) =>
        adapter.InspectReadOnlyAsync(intent ?? Intent(), actorId, tenant,
            revision, epoch, sha, ct);

    [Fact]
    public async Task ApprovedSyntheticIntentRequiresSeparateAuthenticatedAdapter()
    {
        var adapter = Adapter();
        var result = await Check(adapter);
        Assert.Equal("AUTHENTICATED_THEME_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsAuthenticatedAdapter);
        Assert.False(result.AuthorizesTenantAccess);
        Assert.False(result.AuthorizesThemeMutation);
        Assert.False(result.PersistsAnything);
        Assert.False(result.CertifiesAccessibility);
        Assert.False(adapter.IsLivePersistenceProvider);
        Assert.False(adapter.ReadsOrWritesMysql);
        Assert.False(adapter.AuthenticatesJwt);
        Assert.False(adapter.GrantsTenantAccess);
        Assert.False(adapter.GrantsRbac);
        Assert.False(adapter.WritesTheme);
        Assert.False(adapter.InstallsTenantContext);
        Assert.False(adapter.CertifiesAccessibility);
    }

    [Fact]
    public async Task NullIntentIsRejectedBeforeAnyReader()
    {
        var result = await Adapter().InspectReadOnlyAsync(null, 7, 11, 7, 9, Sha);
        Assert.Equal("ISOLATED_THEME_INTENT_REQUIRED", result.Code);
    }

    [Fact]
    public async Task ObservedActorAndTenantMismatchFailClosed()
    {
        Assert.Equal("ISOLATED_THEME_ACTOR_OR_TENANT_MISMATCH",
            (await Check(Adapter(), actorId: 99)).Code);
        Assert.Equal("ISOLATED_THEME_ACTOR_OR_TENANT_MISMATCH",
            (await Check(Adapter(), tenant: 22)).Code);
    }

    [Fact]
    public async Task ClientTenantBFailsBeforeApplicationEvaluation()
    {
        var result = await Check(Adapter(),
            Intent() with { ClaimedTenantId = 22 });
        Assert.Equal("ISOLATED_THEME_CLIENT_TENANT_DENIED", result.Code);
        Assert.False(result.AuthorizesTenantAccess);
    }

    [Fact]
    public async Task StaleRevisionAndSessionEpochAreDenied()
    {
        Assert.Equal("ISOLATED_THEME_REVISION_MISMATCH",
            (await Check(Adapter(), revision: 8)).Code);
        Assert.Equal("ISOLATED_THEME_SESSION_EPOCH_MISMATCH",
            (await Check(Adapter(), epoch: 8)).Code);
    }

    [Fact]
    public async Task WrongOrMissingSourceShaNeverPasses()
    {
        Assert.Equal("ISOLATED_THEME_SOURCE_SHA_MISMATCH",
            (await Check(Adapter(), sha: OtherSha)).Code);
        Assert.Equal("ISOLATED_THEME_SOURCE_SHA_MISMATCH",
            (await Check(Adapter(), sha: null)).Code);
    }

    [Fact]
    public async Task RevokedRoleAndMembershipNeverPass()
    {
        var role = Actor() with { Context = Actor().Context with { RoleRevoked = true } };
        var member = Actor() with
        { Context = Actor().Context with { MembershipCurrent = false } };
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await Check(Adapter(actor: role))).Code);
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await Check(Adapter(actor: member))).Code);
    }

    [Fact]
    public async Task UntrustedOrStaleJwtAndRbacFailClosed()
    {
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await Check(Adapter(actor: Actor() with { JwtFresh = false }))).Code);
        Assert.Equal("CURRENT_BACKEND_AUTHORITY_REQUIRED",
            (await Check(Adapter(actor: Actor() with { RbacFresh = false }))).Code);
    }

    [Fact]
    public async Task ForeignOrUnownedCatalogIsRejected()
    {
        var foreign = Catalog() with { TenantId = 22 };
        Assert.Equal("ISOLATED_THEME_CATALOG_TENANT_MISMATCH",
            (await Check(Adapter(catalog: foreign))).Code);
        var unowned = Catalog() with { OwnerVerified = false };
        Assert.Equal("TENANT_CATALOG_EVIDENCE_REQUIRED",
            (await Check(Adapter(catalog: unowned))).Code);
    }

    [Fact]
    public async Task StaleCatalogAndWrongOrdinalKeyFailClosed()
    {
        Assert.Equal("STALE_CATALOG_SNAPSHOT_RETRY_REQUIRED",
            (await Check(Adapter(catalog: Catalog() with { StoredRevision = 8 }))).Code);
        var wrongKey = Catalog() with { ExactKey = "Primary-Color" };
        Assert.Equal("TENANT_CATALOG_EVIDENCE_REQUIRED",
            (await Check(Adapter(catalog: wrongKey))).Code);
    }

    [Fact]
    public async Task CallerMutationCannotReplaceCapturedApprovedKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal) { "primary-color" };
        var adapter = Adapter(catalog: Catalog() with { ApprovedKeys = keys });
        keys.Clear();
        keys.Add("malicious");
        Assert.Equal("AUTHENTICATED_THEME_ADAPTER_REQUIRED",
            (await Check(adapter)).Code);
        Assert.Equal("UNKNOWN_OR_UNSAFE_TOKEN",
            (await Check(Adapter(catalog: Catalog() with
            { ApprovedKeys = new HashSet<string>(StringComparer.Ordinal) { "malicious" } }))).Code);
    }

    [Fact]
    public async Task InvalidCssAndGlobalMutationRemainDenied()
    {
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE",
            (await Check(Adapter(), Intent() with
            { TokenValue = "url(javascript:alert(1))" })).Code);
        Assert.Equal("UNTRUSTED_MUTATION_DENIED",
            (await Check(Adapter(), Intent() with { DirectWrite = true })).Code);
        Assert.Equal("UNTRUSTED_MUTATION_DENIED",
            (await Check(Adapter(), Intent() with { PlatformCatalogMutation = true })).Code);
    }

    [Fact]
    public async Task CaseInsensitiveInputSetDoesNotAuthorizeCaseVariant()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PRIMARY-COLOR" };
        var adapter = Adapter(catalog: Catalog() with { ApprovedKeys = keys });
        Assert.Equal("UNKNOWN_OR_UNSAFE_TOKEN", (await Check(adapter)).Code);
    }

    [Fact]
    public async Task CancellationPropagatesBeforeAnyInspection()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Check(Adapter(), ct: new CancellationToken(true)));
    }

    [Fact]
    public void InvalidConstructionNeverCreatesLiveProvider()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new C0501DesignTokenIsolatedSnapshotAdapter(null!, Catalog(), Sha));
        Assert.Throws<ArgumentNullException>(() =>
            new C0501DesignTokenIsolatedSnapshotAdapter(Actor(), null!, Sha));
        Assert.Throws<ArgumentException>(() =>
            new C0501DesignTokenIsolatedSnapshotAdapter(Actor(), Catalog(), "short"));
        Assert.Throws<ArgumentException>(() =>
            new C0501DesignTokenIsolatedSnapshotAdapter(Actor(), Catalog(), Sha.ToUpperInvariant()));
        Assert.Throws<ArgumentException>(() =>
            new C0501DesignTokenIsolatedSnapshotAdapter(
                Actor() with { Context = Actor().Context with { ActorId = 0 } },
                Catalog(), Sha));
    }
}
