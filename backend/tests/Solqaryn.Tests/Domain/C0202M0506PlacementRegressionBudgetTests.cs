using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;

namespace Solqaryn.Tests.Domain;

/// <summary>
/// C02-02.M05.06: bounded, deterministic R2 regression of inert tenant placement.
/// No HTTP/JWT integration, MySQL, live router or production authorization is inferred.
/// </summary>
public sealed class C0202M0506PlacementRegressionBudgetTests
{
    private static Rules.TrustedPlacementSnapshot Trusted(string tenant) => new()
    {
        VerifiedTenantId = tenant,
        AuthenticatedActorId = "reviewer-A",
        JwtValidatedByBackend = true,
        CurrentMembershipVerified = true,
        CurrentRoleCanReviewPlacement = true,
        TrustedConfigurationAvailable = true,
        AssignmentOwnerTenantId = tenant,
        CurrentAssignmentRevision = 17,
        CurrentBindingAlias = "shared-A",
        BindingFromTrustedCatalog = true,
        UniqueActiveAssignmentVerified = true
    };

    private static Rules.PlacementCandidate Inspect(string tenant) => new()
    {
        Intent = Rules.Intent.InspectExistingAssignment,
        RequestedTenantId = tenant,
        TargetMode = Rules.PlacementMode.Shared,
        ExpectedAssignmentRevision = 17
    };

    private static void NoStorageAuthority(Rules.Verdict verdict)
    {
        Assert.False(verdict.AuthorizesConnection);
        Assert.False(verdict.AuthorizesMigration);
    }

    [Fact]
    public void ConcurrentTenantsCannotContaminateReadOnlyPlacementDecisions()
    {
        var mismatches = new ConcurrentQueue<string>();
        Parallel.For(0, 128, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            var tenant = "tenant_" + i.ToString("D4");
            var other = "tenant_" + ((i + 1) % 128).ToString("D4");
            var snapshot = Trusted(tenant);
            var request = Inspect(tenant);
            for (var repetition = 0; repetition < 16; repetition++)
            {
                var same = Rules.Evaluate(snapshot, request);
                var cross = Rules.Evaluate(snapshot, request with { RequestedTenantId = other });
                if (same.Code != "TRUSTED_PLACEMENT_RUNTIME_VERIFICATION_REQUIRED" ||
                    cross.Code != "CROSS_TENANT_PLACEMENT_DENIED" ||
                    same.AuthorizesConnection || same.AuthorizesMigration ||
                    cross.AuthorizesConnection || cross.AuthorizesMigration)
                    mismatches.Enqueue(tenant + " / " + other);
            }
        });
        Assert.Empty(mismatches);
    }

    [Fact]
    public void RegressionBudgetKeepsPositiveNegativeRevokedRoleAndRevisionGatesDistinct()
    {
        var evidence = Trusted("tenant_A");
        var candidate = Inspect("tenant_A");
        var stopwatch = Stopwatch.StartNew();
        for (var iteration = 0; iteration < 2048; iteration++)
        {
            var allowedToInspect = Rules.Evaluate(evidence, candidate);
            var foreign = Rules.Evaluate(evidence, candidate with { RequestedTenantId = "tenant_B" });
            var revoked = Rules.Evaluate(evidence with { TenantSuspendedOrRevoked = true }, candidate);
            var noRole = Rules.Evaluate(evidence with { CurrentRoleCanReviewPlacement = false }, candidate);
            var stale = Rules.Evaluate(evidence, candidate with { ExpectedAssignmentRevision = 18 });
            var malicious = Rules.Evaluate(evidence, candidate with { ClientSuppliedConnectionOrDsn = true });

            Assert.Equal("TRUSTED_PLACEMENT_RUNTIME_VERIFICATION_REQUIRED", allowedToInspect.Code);
            Assert.Equal("CROSS_TENANT_PLACEMENT_DENIED", foreign.Code);
            Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED", revoked.Code);
            Assert.Equal("PLACEMENT_REVIEW_ROLE_REQUIRED", noRole.Code);
            Assert.Equal("STALE_PLACEMENT_REVISION", stale.Code);
            Assert.Equal("UNAUTHORIZED_STORAGE_MUTATION_DENIED", malicious.Code);
            NoStorageAuthority(allowedToInspect);
            NoStorageAuthority(foreign);
            NoStorageAuthority(revoked);
            NoStorageAuthority(noRole);
            NoStorageAuthority(stale);
            NoStorageAuthority(malicious);
        }
        stopwatch.Stop();
        // Deliberately loose GitHub-hosted smoke budget, not an SLO or production benchmark.
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(15),
            $"Inert 2048x6 placement evaluations exceeded the 15s CI smoke budget: {stopwatch.Elapsed}.");
    }
}
