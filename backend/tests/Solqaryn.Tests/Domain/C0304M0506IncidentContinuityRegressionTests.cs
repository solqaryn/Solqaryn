using System.Collections.Concurrent;
using System.Diagnostics;
using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ContinuityIncidentDomainRules;

namespace Solqaryn.Tests.Domain;

/// <summary>
/// C03-04.M05.06: deterministic, in-process regression and bounded-cost checks.
/// These tests do not impersonate signed JWT, real HTTP, production restores or UI.
/// </summary>
public sealed class C0304M0506IncidentContinuityRegressionTests
{
    private static Rules.ServerAuthority Authority(int tenant) =>
        new(101, tenant, 2, true, true, true, true, true);

    private static Rules.PersistedIncident Incident(
        int tenant, Rules.IncidentState state = Rules.IncidentState.Open) =>
        new(tenant, 2, 4, state, true, false, true);

    private static Rules.Request Read() =>
        new(Rules.IncidentAction.ReadEvidence, null, null, null, null, false, false);

    private static Rules.Request Contain() =>
        new(Rules.IncidentAction.Contain, null, 4,
            "incident-v4", "sanitized evidence", false, false);

    private static Rules.Request Resolve() =>
        Contain() with
        {
            Action = Rules.IncidentAction.Resolve,
            MitigationEvidenceVerified = true
        };

    private static void NoEffects(Rules.Verdict verdict)
    {
        Assert.False(verdict.AuthorizesIncidentMutation);
        Assert.False(verdict.AuthorizesBackupRestore);
    }

    [Theory]
    [InlineData(11)]
    [InlineData(22)]
    public void RepeatedValidReadNeverAuthorizesEffectsOrOtherTenants(int tenant)
    {
        var a = Authority(tenant);
        var own = Incident(tenant);
        var foreign = Incident(tenant == 11 ? 22 : 11);
        for (var i = 0; i < 512; ++i)
        {
            var read = Rules.Evaluate(a, own, Read());
            Assert.Equal("READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT", read.Code);
            Assert.True(read.RequiresTrustedServiceEnforcement);
            NoEffects(read);
            var cross = Rules.Evaluate(a, foreign, Read());
            Assert.Equal("CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED", cross.Code);
            Assert.False(cross.RequiresTrustedServiceEnforcement);
            NoEffects(cross);
        }
    }

    [Fact]
    public void ContainmentResolutionAndStaleReplayRemainFailClosed()
    {
        for (var i = 0; i < 512; ++i)
        {
            var a = Authority(11);
            var open = Incident(11);
            var contained = Incident(11, Rules.IncidentState.Contained);
            var validContain = Rules.Evaluate(a, open, Contain());
            var validResolve = Rules.Evaluate(a, contained, Resolve());
            Assert.Equal("READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT", validContain.Code);
            Assert.Equal("READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT", validResolve.Code);
            NoEffects(validContain);
            NoEffects(validResolve);

            var stale = Rules.Evaluate(a, open, Contain() with { ExpectedVersion = 3 });
            Assert.Equal("INCIDENT_VERSION_CONFLICT", stale.Code);
            Assert.False(stale.RequiresTrustedServiceEnforcement);
            NoEffects(stale);

            var replay = Rules.Evaluate(a, contained, Contain());
            Assert.Equal("INVALID_CONTAINMENT_TRANSITION", replay.Code);
            NoEffects(replay);
        }
    }

    [Fact]
    public void RevocationUnsanitizedEvidenceAndForeignBranchStayDenied()
    {
        var a = Authority(11);
        var incident = Incident(11);
        foreach (var sample in new[]
        {
            (Incident: incident with { IncidentRevoked = true }, Expected: "INCIDENT_ACCESS_REVOKED"),
            (Incident: incident with { EvidenceSanitized = false }, Expected: "SANITIZED_EVIDENCE_REQUIRED"),
            (Incident: incident with { OwnerBranchId = 3 }, Expected: "INCIDENT_BRANCH_MISMATCH")
        })
        {
            for (var i = 0; i < 128; ++i)
            {
                var verdict = Rules.Evaluate(a, sample.Incident, Read());
                Assert.Equal(sample.Expected, verdict.Code);
                Assert.False(verdict.RequiresTrustedServiceEnforcement);
                NoEffects(verdict);
            }
        }
    }

    [Fact]
    public void ConcurrentTenantEvaluationNeverSharesServerAuthority()
    {
        var errors = new ConcurrentBag<string>();
        Parallel.For(0, 256, i =>
        {
            var tenant = i % 2 == 0 ? 11 : 22;
            var other = tenant == 11 ? 22 : 11;
            var denied = Rules.Evaluate(Authority(tenant), Incident(other), Read());
            var allowedPreflight = Rules.Evaluate(Authority(tenant), Incident(tenant), Read());
            if (denied.Code != "CROSS_TENANT_OR_UNOWNED_INCIDENT_DENIED" ||
                denied.RequiresTrustedServiceEnforcement ||
                allowedPreflight.Code != "READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT" ||
                denied.AuthorizesIncidentMutation || allowedPreflight.AuthorizesBackupRestore)
                errors.Add($"iteration:{i}");
        });
        Assert.Empty(errors);
    }

    [Fact]
    public void ATrustedFlagNeverAuthorizesSandboxRestore()
    {
        var request = Read() with
        {
            Action = Rules.IncidentAction.RequestSandboxRestore,
            RecoveryApprovalFromTrustedSource = true
        };
        for (var i = 0; i < 512; ++i)
        {
            var verdict = Rules.Evaluate(Authority(11), Incident(11), request);
            Assert.Equal("RESTORE_REQUIRES_SEPARATE_AUTHORIZED_RUNNER", verdict.Code);
            Assert.False(verdict.RequiresTrustedServiceEnforcement);
            NoEffects(verdict);
        }
    }

    [Fact]
    public void DomainPreflightBatchFitsWideSmokeBudgetWithoutSideEffects()
    {
        var a = Authority(11);
        var incident = Incident(11);
        var request = Read();
        var timer = Stopwatch.StartNew();
        for (var i = 0; i < 20_000; ++i)
        {
            var verdict = Rules.Evaluate(a, incident, request);
            Assert.Equal("READY_FOR_TRUSTED_INCIDENT_ENFORCEMENT", verdict.Code);
            NoEffects(verdict);
        }
        timer.Stop();
        // Diagnostic smoke guard, not a latency SLO or provider performance claim.
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(15),
            $"In-process incident preflight unexpectedly slow: {timer.Elapsed}");
    }
}
