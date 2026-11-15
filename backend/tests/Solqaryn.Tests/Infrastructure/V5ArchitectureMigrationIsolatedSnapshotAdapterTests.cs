using System.Security.Claims;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ArchitectureMigrationDomainRules;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5ArchitectureMigrationIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static Rules.TrustedBoundary Boundary(IReadOnlySet<string>? providers = null) =>
        new(Sha, "tenant-A", true, true,
            providers ?? new HashSet<string>(StringComparer.Ordinal) { "Render", "Vercel" });

    private static Rules.Candidate Candidate() =>
        new(Sha, "tenant-A", "Render", "Render", "dev",
            true, true, false, false, true, true, true, true, false);

    private static V5ArchitectureMigrationAuthorizedPreflight.Context Context(
        string user = "user-A", string tenant = "tenant-A") =>
        new(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user),
                new Claim("tenant_id", tenant)
            }, "synthetic-only")),
            "user-A", "tenant-A",
            new HashSet<string>(StringComparer.Ordinal) { "architecture.migration.review" },
            true, true, true, Sha, Sha);

    private static V5ArchitectureMigrationIsolatedSnapshotAdapter Adapter(
        Rules.Candidate? item = null) =>
        new(Boundary(), new Rules.Candidate?[] { item ?? Candidate() });

    [Fact]
    public void CoherentSnapshotStillRequiresRealAuthenticatedEvidence()
    {
        var adapter = Adapter();
        var result = adapter.PrepareReadOnly(Context(), Sha, "tenant-A");
        Assert.Equal("AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED", result.Code);
        Assert.Equal(1, result.ItemsReviewed);
        Assert.False(result.AuthorizesDeployment);
        Assert.False(result.AuthorizesDataMigration);
        Assert.False(result.AuthorizesSecretsOrDnsMutation);
        Assert.False(result.GrantsA5OrEnvironmentPromotion);
        Assert.False(adapter.AllowsPersistence);
        Assert.False(adapter.AllowsDatabaseMigration);
        Assert.False(adapter.AllowsExternalProviderWrite);
        Assert.False(adapter.AllowsEnvironmentPromotion);
        Assert.False(adapter.CanIssueIndependentA5);
    }

    [Fact]
    public void StaleShaAndCrossTenantSnapshotAreRejected()
    {
        var a = Adapter();
        Assert.Equal("SOURCE_HEAD_SHA_MISMATCH",
            a.PrepareReadOnly(Context(), OtherSha, "tenant-A").Code);
        Assert.Equal("SOURCE_HEAD_SHA_MISMATCH",
            a.PrepareReadOnly(Context(), null, "tenant-A").Code);
        Assert.Equal("SOURCE_TENANT_MISMATCH",
            a.PrepareReadOnly(Context(), Sha, "tenant-B").Code);
        Assert.Equal("SOURCE_TENANT_MISMATCH",
            a.PrepareReadOnly(Context(), Sha, null).Code);
    }

    [Fact]
    public void UnauthenticatedOrUnauthorizedSnapshotIsDenied()
    {
        var a = Adapter();
        Assert.Equal("BACKEND_AUTHENTICATION_AND_MEMBERSHIP_REQUIRED",
            a.PrepareReadOnly(null, Sha, "tenant-A").Code);
        Assert.Equal("BACKEND_AUTHENTICATION_AND_MEMBERSHIP_REQUIRED",
            a.PrepareReadOnly(Context() with { BackendMembershipVerified = false },
                Sha, "tenant-A").Code);
        Assert.Equal("VERIFIED_RBAC_REVIEW_PERMISSION_REQUIRED",
            a.PrepareReadOnly(Context() with
            {
                BackendEffectivePermissions = new HashSet<string>()
            }, Sha, "tenant-A").Code);
    }

    [Fact]
    public void SpoofedSubjectOrTenantIsDenied()
    {
        var a = Adapter();
        Assert.Equal("TENANT_OR_SUBJECT_MISMATCH",
            a.PrepareReadOnly(Context(user: "intruder"), Sha, "tenant-A").Code);
        Assert.Equal("TENANT_OR_SUBJECT_MISMATCH",
            a.PrepareReadOnly(Context(tenant: "tenant-B"), Sha, "tenant-A").Code);
    }

    [Fact]
    public void UnsafeDeploymentOrRestoreStateIsDenied()
    {
        Assert.Equal("PRODUCTION_OR_QA_NOT_AUTHORIZED",
            Adapter(Candidate() with { Environment = "qa" })
                .PrepareReadOnly(Context(), Sha, "tenant-A").Code);
        Assert.Equal("RUNTIME_MUTATION_OUT_OF_SCOPE",
            Adapter(Candidate() with { RequiresLiveDnsOrSecretMutation = true })
                .PrepareReadOnly(Context(), Sha, "tenant-A").Code);
        Assert.Equal("RECOVERY_EVIDENCE_REQUIRED",
            Adapter(Candidate() with { SandboxRestoreVerified = false })
                .PrepareReadOnly(Context(), Sha, "tenant-A").Code);
    }

    [Fact]
    public void CallerCollectionChangesCannotChangeCapturedAuthority()
    {
        var catalog = new HashSet<string> { "Render" };
        var items = new List<Rules.Candidate?> { Candidate() };
        var a = new V5ArchitectureMigrationIsolatedSnapshotAdapter(
            Boundary(catalog), items);
        catalog.Clear();
        items.Clear();
        Assert.Equal("AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED",
            a.PrepareReadOnly(Context(), Sha, "tenant-A").Code);
    }

    [Fact]
    public void InvalidCollectionBoundsFailBeforeExternalEffects()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new V5ArchitectureMigrationIsolatedSnapshotAdapter(null!,
                new Rules.Candidate?[] { Candidate() }));
        Assert.Throws<ArgumentNullException>(() =>
            new V5ArchitectureMigrationIsolatedSnapshotAdapter(Boundary(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new V5ArchitectureMigrationIsolatedSnapshotAdapter(
                Boundary(), Array.Empty<Rules.Candidate?>()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new V5ArchitectureMigrationIsolatedSnapshotAdapter(
                Boundary(), Enumerable.Repeat<Rules.Candidate?>(Candidate(), 65)));
    }
}
