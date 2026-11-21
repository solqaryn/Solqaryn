namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C04-02.M03.01: deterministic contractual state and validation rules only.
/// This type does not authenticate a principal, authorize a tenant, persist a
/// contract or approve any commercial policy. Application/API must independently
/// verify JWT, membership, RBAC, resource ownership and approved D01-D08 policy.
/// </summary>
public static class C0402ContractDomainRules
{
    public enum ContractState
    {
        Draft = 1,
        PendingAcceptance = 2,
        Active = 3,
        Suspended = 4,
        Cancelled = 5,
        Expired = 6
    }

    public enum ContractOperation
    {
        SubmitForAcceptance = 1,
        Activate = 2,
        Suspend = 3,
        Reactivate = 4,
        Cancel = 5,
        Expire = 6,
        ProposeRenewal = 7
    }

    // These snapshots MUST be constructed from validated server-side sources.
    // Boolean evidence on its own is never an authorization or a legal approval.
    public sealed record VerifiedActor(
        int EmpresaId, int ActorId, bool JwtVerified, bool MembershipActive,
        bool ExplicitContractRbac, bool PermissionRevoked);

    public sealed record ContractSnapshot(
        int EmpresaId, long ContractId, ContractState State, long Version,
        DateTime StartUtc, DateTime? EndUtc);

    public sealed record CommercialEvidence(
        bool D01RepresentationApproved, bool D02TermsApproved,
        bool D04RenewalPolicyApproved, bool D05SuspensionPolicyApproved,
        bool D05ReconciliationVerified, bool D07CancellationPolicyApproved,
        bool D08RetentionPolicyApproved);

    public sealed record TransitionRequest(
        long ContractId, int EmpresaId, long ExpectedVersion,
        ContractOperation Operation, DateTime EffectiveUtc, string Cause);

    public sealed record Decision(string Code)
    {
        // Deliberately false in every outcome, even if the state rule matches:
        // approval and side effects require a separate authenticated backend.
        public bool AuthorizesMutation => false;
        public bool AuthorizesPaymentOrRefund => false;
        public bool CertifiesJwtHttpOrMysql => false;
    }

    public static Decision Evaluate(
        VerifiedActor? actor, ContractSnapshot? contract,
        TransitionRequest? request, CommercialEvidence? evidence)
    {
        if (actor is null || contract is null || request is null || evidence is null ||
            actor.ActorId <= 0 || actor.EmpresaId <= 0 ||
            contract.ContractId <= 0 || contract.EmpresaId <= 0 ||
            request.ContractId <= 0 || request.EmpresaId <= 0)
            return new("VERIFIED_SERVER_CONTEXT_REQUIRED");

        if (!actor.JwtVerified || !actor.MembershipActive ||
            !actor.ExplicitContractRbac || actor.PermissionRevoked)
            return new("JWT_RBAC_MEMBERSHIP_DENIED");

        if (actor.EmpresaId != contract.EmpresaId ||
            actor.EmpresaId != request.EmpresaId || contract.ContractId != request.ContractId)
            return new("TENANT_OR_RESOURCE_OWNERSHIP_DENIED");

        if (!Enum.IsDefined(contract.State) || !Enum.IsDefined(request.Operation))
            return new("UNKNOWN_STATE_OR_OPERATION_DENIED");

        if (contract.Version <= 0 || request.ExpectedVersion != contract.Version)
            return new("STALE_OR_INVALID_CONTRACT_VERSION");

        if (contract.StartUtc.Kind != DateTimeKind.Utc ||
            (contract.EndUtc.HasValue && contract.EndUtc.Value.Kind != DateTimeKind.Utc) ||
            request.EffectiveUtc.Kind != DateTimeKind.Utc ||
            (contract.EndUtc.HasValue && contract.EndUtc.Value <= contract.StartUtc))
            return new("UTC_PERIOD_EVIDENCE_INVALID");

        if (string.IsNullOrWhiteSpace(request.Cause) || request.Cause.Length > 160 ||
            request.Cause.Any(char.IsControl))
            return new("SAFE_CAUSE_REQUIRED");

        if (request.EffectiveUtc < contract.StartUtc)
            return new("RETROACTIVE_TRANSITION_DENIED");

        // Historical states remain terminal. No reactivation or rewrite.
        if (contract.State is ContractState.Cancelled or ContractState.Expired)
            return new("TERMINAL_CONTRACT_DENIED");

        return request.Operation switch
        {
            ContractOperation.SubmitForAcceptance =>
                contract.State != ContractState.Draft
                    ? new("INVALID_STATE_TRANSITION")
                    : !evidence.D01RepresentationApproved || !evidence.D02TermsApproved
                        ? new("D01_D02_APPROVAL_REQUIRED")
                        : new("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED"),

            ContractOperation.Activate =>
                contract.State != ContractState.PendingAcceptance
                    ? new("INVALID_STATE_TRANSITION")
                    : !evidence.D01RepresentationApproved || !evidence.D02TermsApproved
                        ? new("D01_D02_APPROVAL_REQUIRED")
                        : new("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED"),

            ContractOperation.Suspend =>
                contract.State != ContractState.Active
                    ? new("INVALID_STATE_TRANSITION")
                    : !evidence.D05SuspensionPolicyApproved || !evidence.D05ReconciliationVerified
                        ? new("MOROSITY_OR_TIMEOUT_NOT_VERIFIED")
                        : new("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED"),

            ContractOperation.Reactivate =>
                contract.State != ContractState.Suspended
                    ? new("INVALID_STATE_TRANSITION")
                    : !evidence.D05SuspensionPolicyApproved || !evidence.D05ReconciliationVerified
                        ? new("REACTIVATION_RECONCILIATION_REQUIRED")
                        : new("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED"),

            ContractOperation.Cancel =>
                contract.State is not (ContractState.Active or ContractState.Suspended or ContractState.PendingAcceptance)
                    ? new("INVALID_STATE_TRANSITION")
                    : !evidence.D07CancellationPolicyApproved || !evidence.D08RetentionPolicyApproved
                        ? new("D07_D08_APPROVAL_REQUIRED")
                        : new("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED"),

            ContractOperation.Expire =>
                contract.State != ContractState.Active
                    ? new("INVALID_STATE_TRANSITION")
                    : !contract.EndUtc.HasValue || request.EffectiveUtc < contract.EndUtc.Value
                        ? new("EXPIRY_NOT_SUPPORTED_BY_PERIOD")
                        : new("DOMAIN_PRECONDITIONS_ONLY_BACKEND_GATE_REQUIRED"),

            ContractOperation.ProposeRenewal =>
                contract.State != ContractState.Active
                    ? new("INVALID_STATE_TRANSITION")
                    : !evidence.D04RenewalPolicyApproved ||
                      !contract.EndUtc.HasValue ||
                      request.EffectiveUtc < contract.EndUtc.Value
                        ? new("D04_RENEWAL_POLICY_OR_FUTURE_PERIOD_REQUIRED")
                        : new("RENEWAL_PROPOSAL_ONLY_NO_EFFECT"),

            _ => new("UNKNOWN_STATE_OR_OPERATION_DENIED")
        };
    }
}
