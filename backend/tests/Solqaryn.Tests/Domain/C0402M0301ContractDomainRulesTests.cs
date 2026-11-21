using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0402ContractDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class C0402M0301ContractDomainRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 21, 12, 0, 0, DateTimeKind.Utc);
    private static Rules.VerifiedActor Actor() => new(100, 17, true, true, true, false);
    private static Rules.ContractSnapshot Contract(
        Rules.ContractState state = Rules.ContractState.Draft) =>
        new(100, 42, state, 3, Now.AddDays(-20), Now.AddDays(10));
    // Test-only evidence: NOT an approval of real D01-D08 policies.
    private static Rules.CommercialEvidence Evidence() =>
        new(true, true, true, true, true, true, true);
    private static Rules.TransitionRequest Request(
        Rules.ContractOperation op = Rules.ContractOperation.SubmitForAcceptance) =>
        new(42, 100, 3, op, Now, "server-reviewed-reason");

    private static Rules.Decision Eval(
        Rules.VerifiedActor? actor = null, Rules.ContractSnapshot? contract = null,
        Rules.TransitionRequest? request = null, Rules.CommercialEvidence? evidence = null) =>
        Rules.Evaluate(actor ?? Actor(), contract ?? Contract(),
            request ?? Request(), evidence ?? Evidence());

    [Fact]
    public void ValidDraftCanOnlyPassDomainPreconditionsNeverGrantEffects()
    {
        var decision = Eval();
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED", decision.Code);
        Assert.False(decision.AuthorizesMutation);
        Assert.False(decision.AuthorizesPaymentOrRefund);
        Assert.False(decision.CertifiesJwtHttpOrMysql);
    }

    [Fact]
    public void NullAndInvalidServerEvidenceAlwaysFailsClosed()
    {
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED",
            Rules.Evaluate(null, Contract(), Request(), Evidence()).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED",
            Rules.Evaluate(Actor(), null, Request(), Evidence()).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED",
            Rules.Evaluate(Actor(), Contract(), null, Evidence()).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED",
            Rules.Evaluate(Actor(), Contract(), Request(), null).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED",
            Eval(actor: Actor() with { ActorId = 0 }).Code);
        Assert.Equal("VERIFIED_SERVER_CONTEXT_REQUIRED",
            Eval(contract: Contract() with { ContractId = 0 }).Code);
    }

    [Fact]
    public void RevokedExpiredOrUnprivilegedActorDeniedBeforeStateEvaluation()
    {
        foreach (var bad in new[]
        {
            Actor() with { JwtVerified = false },
            Actor() with { MembershipActive = false },
            Actor() with { ExplicitContractRbac = false },
            Actor() with { PermissionRevoked = true }
        })
            Assert.Equal("JWT_RBAC_MEMBERSHIP_DENIED", Eval(actor: bad).Code);
    }

    [Fact]
    public void ForgedTenantAndCrossTenantResourcesAreDeniedWithoutDisclosure()
    {
        Assert.Equal("TENANT_OR_RESOURCE_OWNERSHIP_DENIED",
            Eval(actor: Actor() with { EmpresaId = 200 }).Code);
        Assert.Equal("TENANT_OR_RESOURCE_OWNERSHIP_DENIED",
            Eval(contract: Contract() with { EmpresaId = 200 }).Code);
        Assert.Equal("TENANT_OR_RESOURCE_OWNERSHIP_DENIED",
            Eval(request: Request() with { EmpresaId = 200 }).Code);
        Assert.Equal("TENANT_OR_RESOURCE_OWNERSHIP_DENIED",
            Eval(request: Request() with { ContractId = 43 }).Code);
    }

    [Fact]
    public void UnknownOperationAndStateFailClosed()
    {
        Assert.Equal("UNKNOWN_STATE_OR_OPERATION_DENIED",
            Eval(contract: Contract((Rules.ContractState)99)).Code);
        Assert.Equal("UNKNOWN_STATE_OR_OPERATION_DENIED",
            Eval(request: Request((Rules.ContractOperation)99)).Code);
    }

    [Fact]
    public void OptimisticVersionMismatchAndInvalidVersionAreRejected()
    {
        Assert.Equal("STALE_OR_INVALID_CONTRACT_VERSION",
            Eval(request: Request() with { ExpectedVersion = 2 }).Code);
        Assert.Equal("STALE_OR_INVALID_CONTRACT_VERSION",
            Eval(contract: Contract() with { Version = 0 }).Code);
    }

    [Fact]
    public void NonUtcInvertedPeriodsAndRetroactiveRequestsAreDenied()
    {
        Assert.Equal("UTC_PERIOD_EVIDENCE_INVALID",
            Eval(contract: Contract() with { StartUtc = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified) }).Code);
        Assert.Equal("UTC_PERIOD_EVIDENCE_INVALID",
            Eval(contract: Contract() with { EndUtc = Now.AddDays(-21) }).Code);
        Assert.Equal("UTC_PERIOD_EVIDENCE_INVALID",
            Eval(request: Request() with { EffectiveUtc = DateTime.SpecifyKind(Now, DateTimeKind.Local) }).Code);
        Assert.Equal("RETROACTIVE_TRANSITION_DENIED",
            Eval(request: Request() with { EffectiveUtc = Now.AddDays(-21) }).Code);
    }

    [Fact]
    public void UnsafeMissingAndOverlongCauseCannotEnterAuditEnvelope()
    {
        Assert.Equal("SAFE_CAUSE_REQUIRED", Eval(request: Request() with { Cause = " " }).Code);
        Assert.Equal("SAFE_CAUSE_REQUIRED", Eval(request: Request() with { Cause = new string('x', 161) }).Code);
        Assert.Equal("SAFE_CAUSE_REQUIRED", Eval(request: Request() with { Cause = "bad\nreason" }).Code);
    }

    [Fact]
    public void SubmissionAndActivationRequireExplicitD01D02Evidence()
    {
        Assert.Equal("D01_D02_APPROVAL_REQUIRED",
            Eval(evidence: Evidence() with { D01RepresentationApproved = false }).Code);
        Assert.Equal("D01_D02_APPROVAL_REQUIRED",
            Eval(evidence: Evidence() with { D02TermsApproved = false }).Code);
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED",
            Eval(contract: Contract(Rules.ContractState.PendingAcceptance),
                request: Request(Rules.ContractOperation.Activate)).Code);
        Assert.Equal("INVALID_STATE_TRANSITION",
            Eval(request: Request(Rules.ContractOperation.Activate)).Code);
    }

    [Fact]
    public void SuspensionAndReactivationNeverFollowTimeoutWithoutReconciliation()
    {
        Assert.Equal("MOROSITY_OR_TIMEOUT_NOT_VERIFIED",
            Eval(contract: Contract(Rules.ContractState.Active),
                request: Request(Rules.ContractOperation.Suspend),
                evidence: Evidence() with { D05ReconciliationVerified = false }).Code);
        Assert.Equal("REACTIVATION_RECONCILIATION_REQUIRED",
            Eval(contract: Contract(Rules.ContractState.Suspended),
                request: Request(Rules.ContractOperation.Reactivate),
                evidence: Evidence() with { D05SuspensionPolicyApproved = false }).Code);
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED",
            Eval(contract: Contract(Rules.ContractState.Suspended),
                request: Request(Rules.ContractOperation.Reactivate)).Code);
    }

    [Fact]
    public void CancellationRequiresPolicyAndRetentionNoImplicitRefund()
    {
        var r = Eval(contract: Contract(Rules.ContractState.Active),
            request: Request(Rules.ContractOperation.Cancel),
            evidence: Evidence() with { D07CancellationPolicyApproved = false });
        Assert.Equal("D07_D08_APPROVAL_REQUIRED", r.Code);
        Assert.False(r.AuthorizesPaymentOrRefund);
        Assert.Equal("D07_D08_APPROVAL_REQUIRED",
            Eval(contract: Contract(Rules.ContractState.Active),
                request: Request(Rules.ContractOperation.Cancel),
                evidence: Evidence() with { D08RetentionPolicyApproved = false }).Code);
    }

    [Fact]
    public void TerminalContractsCannotBeReactivatedRenewedOrRewritten()
    {
        foreach (var state in new[] { Rules.ContractState.Cancelled, Rules.ContractState.Expired })
        {
            Assert.Equal("TERMINAL_CONTRACT_DENIED",
                Eval(contract: Contract(state),
                    request: Request(Rules.ContractOperation.Reactivate)).Code);
            Assert.Equal("TERMINAL_CONTRACT_DENIED",
                Eval(contract: Contract(state),
                    request: Request(Rules.ContractOperation.ProposeRenewal)).Code);
        }
    }

    [Fact]
    public void ExpirationRequiresEndOfPeriodAndValidSourceState()
    {
        Assert.Equal("EXPIRY_NOT_SUPPORTED_BY_PERIOD",
            Eval(contract: Contract(Rules.ContractState.Active),
                request: Request(Rules.ContractOperation.Expire)).Code);
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED",
            Eval(contract: Contract(Rules.ContractState.Active),
                request: Request(Rules.ContractOperation.Expire) with { EffectiveUtc = Now.AddDays(10) }).Code);
    }

    [Fact]
    public void RenewalIsProposalOnlyNoNewPeriodNoCharge()
    {
        Assert.Equal("D04_RENEWAL_POLICY_OR_FUTURE_PERIOD_REQUIRED",
            Eval(contract: Contract(Rules.ContractState.Active),
                request: Request(Rules.ContractOperation.ProposeRenewal)).Code);
        var decision = Eval(contract: Contract(Rules.ContractState.Active),
            request: Request(Rules.ContractOperation.ProposeRenewal) with { EffectiveUtc = Now.AddDays(10) });
        Assert.Equal("RENEWAL_PROPOSAL_ONLY_NO_EFFECT", decision.Code);
        Assert.False(decision.AuthorizesMutation);
        Assert.False(decision.AuthorizesPaymentOrRefund);
    }
}
