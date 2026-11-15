using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class V5UxAuditApplicationServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5UxAuditApplicationService Service = new();

    private static V5UxAuditRules.Observation Good(
        string surface = "portal", string tenant = "tenant-A") =>
        new(surface, "p95-latency", tenant, Sha, "ci:external-evidence-ref",
            90m, 150m, "MAX");

    private static V5UxAuditApplicationService.Request Request(
        params V5UxAuditRules.Observation[] values) =>
        new(Sha, Sha, "tenant-A", true,
            new HashSet<string>(StringComparer.Ordinal) { "portal", "builder" },
            new HashSet<string>(StringComparer.Ordinal)
            { "p95-latency", "contrast-ratio" },
            values.Length > 0 ? values : new[] { Good() });

    [Fact]
    public void ValidObservationsStillNeedExternalEvidenceAndA5()
    {
        var x = Service.Prepare(Request(Good(), Good("builder")));
        Assert.Equal("EXTERNAL_EVIDENCE_AND_A5_REQUIRED", x.Code);
        Assert.Equal(2, x.ItemsReviewed);
        Assert.False(x.AllowsInstitutionalWrite);
        Assert.False(x.CertifiesExternalMeasurement);
        Assert.False(x.GrantsUserAccess);
    }

    [Fact]
    public void TenantBAndNullContextFailClosed()
    {
        Assert.Equal("TENANT_SCOPE_MISMATCH",
            Service.Prepare(Request(Good(tenant: "tenant-B"))).Code);
        Assert.Equal("TENANT_SCOPE_MISMATCH",
            Service.Prepare(Request(Good(tenant: ""))).Code);
        Assert.Equal("VERIFIED_TENANT_CONTEXT_REQUIRED",
            Service.Prepare(Request(Good()) with
                { TenantContextVerifiedByServer = false }).Code);
        Assert.Equal("VERIFIED_TENANT_CONTEXT_REQUIRED",
            Service.Prepare(Request(Good()) with
                { ServerResolvedTenantScope = null }).Code);
    }

    [Fact]
    public void InvalidExpectedOrObservedShaIsRejected()
    {
        Assert.Equal("EXACT_SHA_MISMATCH",
            Service.Prepare(Request(Good()) with
                { ExpectedSourceSha = "short" }).Code);
        Assert.Equal("EXACT_SHA_MISMATCH",
            Service.Prepare(Request(Good()) with
                { ObservedSourceSha = new string('f', 40) }).Code);
    }

    [Fact]
    public void UnknownSurfaceAndCriterionCannotEnterAudit()
    {
        Assert.Equal("UNKNOWN_SURFACE",
            Service.Prepare(Request(Good("unapproved"))).Code);
        Assert.Equal("UNKNOWN_CRITERION",
            Service.Prepare(Request(Good() with
                { CriterionKey = "unknown" })).Code);
    }

    [Fact]
    public void BudgetFailureAndUnsafeEvidenceDoNotBecomePass()
    {
        Assert.Equal("BUDGET_EXCEEDED",
            Service.Prepare(Request(Good() with
                { ObservedValue = 151m })).Code);
        Assert.Equal("EVIDENCE_REQUIRED_OR_UNSAFE",
            Service.Prepare(Request(Good() with
                { EvidenceReference = "credential\nunsafe" })).Code);
    }

    [Fact]
    public void CapacityAndEmptyOrUntrustedCatalogsAreDenied()
    {
        Assert.Equal("OBSERVATION_CAPACITY_EXCEEDED",
            Service.Prepare(Request(Good(), Good("builder")) with
                { MaxObservations = 1 }).Code);
        Assert.Equal("OBSERVATION_CAPACITY_EXCEEDED",
            Service.Prepare(Request(Good()) with
                { MaxObservations = 501 }).Code);
        Assert.Equal("VERIFIED_CATALOG_REQUIRED",
            Service.Prepare(Request(Good()) with
                { AuthorizedSurfaces = new HashSet<string>() }).Code);
    }

    [Fact]
    public void EarlyFailureCountsOnlyVisitedObservations()
    {
        var x = Service.Prepare(Request(
            Good(), Good("unapproved"), Good("builder")));
        Assert.Equal("UNKNOWN_SURFACE", x.Code);
        Assert.Equal(2, x.ItemsReviewed);
        Assert.False(x.AllowsInstitutionalWrite);
    }
}
