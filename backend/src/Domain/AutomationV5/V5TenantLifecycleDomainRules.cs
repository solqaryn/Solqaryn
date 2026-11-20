using System;
using System.Linq;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C02-03.M03.01: pure, non-authorizing server-side preflight for a future
/// tenant lifecycle. Inputs must come from independently verified backend
/// snapshots. This class never changes Empresa.Activa, writes EF/SQL, issues
/// grants, revives sessions, emits events or authorizes deployments.
/// </summary>
public static class V5TenantLifecycleDomainRules
{
    public enum State
    {
        Requested = 1, Provisioning = 2, Active = 3, Suspended = 4,
        Archived = 5, LogicalDeleted = 6, RepairableFailure = 7
    }

    public enum Command
    {
        BeginProvisioning = 1, CompleteProvisioning = 2, Suspend = 3,
        Reactivate = 4, Archive = 5, LogicalDelete = 6,
        MarkRepairableFailure = 7, ResumeProvisioning = 8
    }

    public sealed record TrustedPlatformEvidence
    {
        public string? ActorId { get; init; }
        public string? VerifiedTargetTenantId { get; init; }
        public bool BackendVerifiedSignedJwt { get; init; }
        public bool CurrentExplicitPlatformGrantVerified { get; init; }
        public bool CurrentOperatorRoleVerified { get; init; }
        public bool ActorOrGrantRevoked { get; init; }
        public bool TargetOwnershipVerifiedServerSide { get; init; }
        public bool FreshPolicyRevisionVerified { get; init; }
        public bool TrustedLifecycleConfigurationEnabled { get; init; }
        public bool RequestedCommandEnabledByTrustedConfiguration { get; init; }
        public State PersistedState { get; init; }
        public long PersistedRevision { get; init; }
        public bool RevocationPlanVerified { get; init; }
        public bool MembershipAndGrantsRevalidated { get; init; }
        public bool RetentionPolicyVerified { get; init; }
        public bool RestorationEvidenceVerified { get; init; }
        public bool DurableRecoveryCheckpointVerified { get; init; }
        public bool AuditAndFencingAvailable { get; init; }
    }

    public sealed record ProposedLifecycleCommand
    {
        public Command Operation { get; init; }
        public string? RequestedTenantId { get; init; }
        public State ExpectedSourceState { get; init; }
        public long ExpectedRevision { get; init; }
        public string? IdempotencyKey { get; init; }
        public string? PayloadFingerprint { get; init; }
        public bool ClientSuppliesAuthorityOrTenantContext { get; init; }
        public bool ClientSuppliesHostDsnOrStorageRouting { get; init; }
        public bool RequestsPhysicalDeletionOrSchemaMutation { get; init; }
        public bool RequestsAutomaticGrantsOrSessionRestoration { get; init; }
        public bool RequestsLiveCutoverOrProductionSideEffects { get; init; }
        public bool PreservesDataAndAuditHistory { get; init; }
    }

    public sealed record ExistingDurableReceipt
    {
        public string? ActorId { get; init; }
        public string? TenantId { get; init; }
        public Command Operation { get; init; }
        public string? IdempotencyKey { get; init; }
        public string? PayloadFingerprint { get; init; }
        public bool CompletedAndCommitted { get; init; }
    }

    public readonly record struct Verdict(string Code)
    {
        public bool RequiresApplicationEnforcement =>
            Code == "VERIFIED_PRECONDITIONS_REQUIRE_APPLICATION_CAS" ||
            Code == "DURABLE_IDEMPOTENCY_READBACK_REQUIRED" ||
            Code == "INCOMPLETE_REPLAY_REQUIRES_CHECKPOINT_RECOVERY";

        public bool AuthorizesTenantMutation => false;
        public bool AuthorizesGlobalPermission => false;
        public bool AuthorizesPhysicalDeletion => false;
        public bool AuthorizesProductionCutover => false;
        public bool WritesToDatabase => false;
    }

    private static bool SafeOpaqueId(string? value) =>
        value is { Length: >= 2 and <= 128 } &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_');

    // SQL-backed Empresa.Id and Usuario.Id may be a single positive digit.
    // Keep opaque idempotency keys subject to the stricter length >= 2 rule.
    private static bool SafeBusinessId(string? value) =>
        value is { Length: >= 1 and <= 128 } &&
        (value.Length > 1 || value[0] is >= '1' and <= '9') &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or
                               >= '0' and <= '9' or '-' or '_');

    private static bool SafeFingerprint(string? value) =>
        value is { Length: >= 16 and <= 128 } &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9');

    private static bool Allowed(State state, Command command) =>
        command switch
        {
            Command.BeginProvisioning => state == State.Requested,
            Command.CompleteProvisioning => state == State.Provisioning,
            Command.Suspend => state == State.Active,
            Command.Reactivate => state == State.Suspended,
            Command.Archive => state == State.Suspended,
            Command.LogicalDelete => state == State.Archived,
            Command.MarkRepairableFailure => state == State.Provisioning,
            Command.ResumeProvisioning => state == State.RepairableFailure,
            _ => false
        };

    public static Verdict Evaluate(
        TrustedPlatformEvidence? trusted,
        ProposedLifecycleCommand? command,
        ExistingDurableReceipt? existingReceipt = null)
    {
        if (trusted is null || command is null ||
            !SafeBusinessId(trusted.ActorId) ||
            !trusted.BackendVerifiedSignedJwt ||
            !trusted.CurrentExplicitPlatformGrantVerified ||
            !trusted.CurrentOperatorRoleVerified ||
            trusted.ActorOrGrantRevoked)
            return new("TRUSTED_PLATFORM_OPERATOR_REQUIRED");

        if (!Enum.IsDefined(trusted.PersistedState) ||
            !Enum.IsDefined(command.ExpectedSourceState) ||
            !Enum.IsDefined(command.Operation))
            return new("UNKNOWN_LIFECYCLE_STATE_OR_COMMAND");

        if (!SafeBusinessId(trusted.VerifiedTargetTenantId) ||
            !SafeBusinessId(command.RequestedTenantId) ||
            !trusted.TargetOwnershipVerifiedServerSide ||
            !string.Equals(trusted.VerifiedTargetTenantId,
                command.RequestedTenantId, StringComparison.Ordinal))
            return new("CROSS_TENANT_LIFECYCLE_DENIED");

        if (!trusted.FreshPolicyRevisionVerified ||
            !trusted.TrustedLifecycleConfigurationEnabled ||
            !trusted.RequestedCommandEnabledByTrustedConfiguration ||
            !trusted.AuditAndFencingAvailable)
            return new("TRUSTED_POLICY_FENCE_OR_FEATURE_DISABLED");

        if (command.ClientSuppliesAuthorityOrTenantContext ||
            command.ClientSuppliesHostDsnOrStorageRouting ||
            command.RequestsPhysicalDeletionOrSchemaMutation ||
            command.RequestsAutomaticGrantsOrSessionRestoration ||
            command.RequestsLiveCutoverOrProductionSideEffects ||
            !command.PreservesDataAndAuditHistory)
            return new("UNAUTHORIZED_CLIENT_OVERRIDE_OR_IRREVERSIBLE_ACTION");

        if (!SafeOpaqueId(command.IdempotencyKey) ||
            !SafeFingerprint(command.PayloadFingerprint))
            return new("INVALID_IDEMPOTENCY_OR_PAYLOAD_FINGERPRINT");

        // Idempotent retries must be evaluated BEFORE current-state/version
        // checks: a legitimate first execution may have already advanced
        // PersistedState and PersistedRevision. This never replays a write;
        // Application must read the exact durable receipt and auth anew.
        if (existingReceipt is not null)
        {
            if (!SafeBusinessId(existingReceipt.ActorId) ||
                !SafeBusinessId(existingReceipt.TenantId) ||
                !string.Equals(existingReceipt.ActorId, trusted.ActorId, StringComparison.Ordinal) ||
                !string.Equals(existingReceipt.TenantId, trusted.VerifiedTargetTenantId, StringComparison.Ordinal) ||
                existingReceipt.Operation != command.Operation ||
                !string.Equals(existingReceipt.IdempotencyKey, command.IdempotencyKey, StringComparison.Ordinal) ||
                !string.Equals(existingReceipt.PayloadFingerprint, command.PayloadFingerprint, StringComparison.Ordinal))
                return new("IDEMPOTENCY_REUSE_CONFLICT");

            return existingReceipt.CompletedAndCommitted
                ? new("DURABLE_IDEMPOTENCY_READBACK_REQUIRED")
                : new("INCOMPLETE_REPLAY_REQUIRES_CHECKPOINT_RECOVERY");
        }

        if (trusted.PersistedRevision <= 0 ||
            command.ExpectedRevision != trusted.PersistedRevision ||
            command.ExpectedSourceState != trusted.PersistedState)
            return new("STALE_LIFECYCLE_STATE_OR_REVISION");

        if (!Allowed(trusted.PersistedState, command.Operation))
            return new("ILLEGAL_LIFECYCLE_TRANSITION");

        if (command.Operation == Command.Suspend && !trusted.RevocationPlanVerified)
            return new("SUSPENSION_REVOCATION_GATE_REQUIRED");

        if (command.Operation == Command.Reactivate &&
            (!trusted.MembershipAndGrantsRevalidated ||
             !trusted.RevocationPlanVerified))
            return new("REACTIVATION_FRESH_MEMBERSHIP_REQUIRED");

        if (command.Operation is Command.Archive or Command.LogicalDelete &&
            (!trusted.RetentionPolicyVerified || !trusted.RestorationEvidenceVerified))
            return new("RETENTION_AND_RESTORATION_REQUIRED");

        if (command.Operation is Command.MarkRepairableFailure or Command.ResumeProvisioning &&
            !trusted.DurableRecoveryCheckpointVerified)
            return new("DURABLE_RECOVERY_CHECKPOINT_REQUIRED");


        // A positive domain verdict is NEVER authorization or CAS execution:
        // Application must re-check server-side JWT/RBAC, tenant, revision,
        // durable idempotency, migration/retention safety, atomic transaction,
        // revocation, checkpoint and audit against the same committed version.
        return new("VERIFIED_PRECONDITIONS_REQUIRE_APPLICATION_CAS");
    }
}
