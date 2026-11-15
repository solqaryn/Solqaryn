using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5UxAuditIsolatedSnapshotAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static V5UxAuditAuthorizedApplicationService.TrustedAuthority Authority() =>
        new(21, "tenant-A", Sha, true, true, true, true, true,
            new HashSet<string>(StringComparer.Ordinal) { "portal", "builder" },
            new HashSet<string>(StringComparer.Ordinal) { "p95-latency" });

    private static V5UxAuditRules.Observation Observation(string surface = "portal",
        string tenant = "tenant-A", decimal observed = 90m) =>
        new(surface, "p95-latency", tenant, Sha, "ci:synthetic-example",
            observed, 150m, "MAX");

    private static V5UxAuditApplicationService.Request Request() =>
        new(Sha, Sha, "tenant-A", true,
            new HashSet<string>(StringComparer.Ordinal) { "portal" },
            new HashSet<string>(StringComparer.Ordinal) { "p95-latency" },
            new[] { Observation() });

    private static V5UxAuditIsolatedSnapshotAdapter Adapter() => new(Request());

    [Fact]
    public void CoherentReadOnlySnapshotNeverCertifiesExternalEvidence()
    {
        var adapter = Adapter();
        var result = adapter.PrepareReadOnly(Authority(), Sha, "tenant-A");
        Assert.Equal("EXTERNAL_EVIDENCE_AND_A5_REQUIRED", result.Code);
        Assert.Equal(1, result.ItemsReviewed);
        Assert.False(result.GrantsAccess);
        Assert.False(result.AllowsInstitutionalWrite);
        Assert.False(result.CertifiesExternalMeasurement);
        Assert.False(result.AuthorizesProduction);
        Assert.False(adapter.AllowsPersistence);
        Assert.False(adapter.AllowsInstitutionalWrite);
        Assert.False(adapter.CertifiesExternalMeasurement);
        Assert.False(adapter.AuthorizesProduction);
        Assert.False(adapter.GrantsAccess);
    }

    [Fact]
    public void MismatchedSourceShaAndTenantDenyBeforePreflight()
    {
        var adapter = Adapter();
        Assert.Equal("SOURCE_SHA_MISMATCH",
            adapter.PrepareReadOnly(Authority(), OtherSha, "tenant-A").Code);
        Assert.Equal("SOURCE_SHA_MISMATCH",
            adapter.PrepareReadOnly(Authority(), null, "tenant-A").Code);
        Assert.Equal("TENANT_SNAPSHOT_MISMATCH",
            adapter.PrepareReadOnly(Authority(), Sha, "tenant-B").Code);
        Assert.Equal("TENANT_SNAPSHOT_MISMATCH",
            adapter.PrepareReadOnly(Authority(), Sha, null).Code);
    }

    [Fact]
    public void UnauthenticatedAndUnapprovedAuthorityCannotPass()
    {
        var adapter = Adapter();
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            adapter.PrepareReadOnly(null, Sha, "tenant-A").Code);
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            adapter.PrepareReadOnly(Authority() with
            {
                JwtValidatedByServer = false
            }, Sha, "tenant-A").Code);
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            adapter.PrepareReadOnly(Authority() with
            {
                RequiredReviewPermissionVerified = false
            }, Sha, "tenant-A").Code);
    }

    [Fact]
    public void CrossTenantAuthorityAndRevokedMembershipAreDenied()
    {
        var adapter = Adapter();
        Assert.Equal("TRUSTED_TENANT_CONTEXT_MISMATCH",
            adapter.PrepareReadOnly(Authority() with
            {
                ServerTenantScope = "tenant-B"
            }, Sha, "tenant-A").Code);
        Assert.Equal("SERVER_VERIFIED_REVIEW_AUTHORITY_REQUIRED",
            adapter.PrepareReadOnly(Authority() with
            {
                CurrentMembershipVerified = false
            }, Sha, "tenant-A").Code);
    }

    [Fact]
    public void UnknownSurfaceAndFailedPerformanceBudgetAreDenied()
    {
        var request = Request();
        Assert.Equal("UNKNOWN_SURFACE",
            new V5UxAuditIsolatedSnapshotAdapter(request with
            {
                Observations = new[] { Observation(surface: "private-admin") }
            }).PrepareReadOnly(Authority(), Sha, "tenant-A").Code);
        Assert.Equal("BUDGET_EXCEEDED",
            new V5UxAuditIsolatedSnapshotAdapter(request with
            {
                Observations = new[] { Observation(observed: 151m) }
            }).PrepareReadOnly(Authority(), Sha, "tenant-A").Code);
    }

    [Fact]
    public void MutatingCallerCollectionsCannotExpandCapturedCatalog()
    {
        var surfaces = new HashSet<string> { "portal" };
        var criteria = new HashSet<string> { "p95-latency" };
        var observations = new List<V5UxAuditRules.Observation> { Observation() };
        var request = Request() with
        {
            AuthorizedSurfaces = surfaces,
            AuthorizedCriteria = criteria,
            Observations = observations
        };
        var adapter = new V5UxAuditIsolatedSnapshotAdapter(request);
        surfaces.Clear();
        criteria.Clear();
        observations.Clear();
        Assert.Equal("EXTERNAL_EVIDENCE_AND_A5_REQUIRED",
            adapter.PrepareReadOnly(Authority(), Sha, "tenant-A").Code);
    }

    [Fact]
    public void NullRequestAndCollectionsFailBeforeExternalEffects()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new V5UxAuditIsolatedSnapshotAdapter(null!));
        Assert.Throws<ArgumentNullException>(() =>
            new V5UxAuditIsolatedSnapshotAdapter(Request() with
            {
                Observations = null!
            }));
        Assert.Throws<ArgumentNullException>(() =>
            new V5UxAuditIsolatedSnapshotAdapter(Request() with
            {
                AuthorizedSurfaces = null!
            }));
    }
}
