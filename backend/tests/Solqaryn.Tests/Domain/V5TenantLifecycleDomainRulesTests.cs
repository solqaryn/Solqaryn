using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5TenantLifecycleDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class V5TenantLifecycleDomainRulesTests
{
    private const string Fingerprint = "A1B2C3D4E5F60708";

    private static Rules.TrustedPlatformEvidence Trusted(
        Rules.State state = Rules.State.Active) => new()
    {
        ActorId = "platform-operator-A",
        VerifiedTargetTenantId = "company-A",
        BackendVerifiedSignedJwt = true,
        CurrentExplicitPlatformGrantVerified = true,
        CurrentOperatorRoleVerified = true,
        TargetOwnershipVerifiedServerSide = true,
        FreshPolicyRevisionVerified = true,
        TrustedLifecycleConfigurationEnabled = true,
        RequestedCommandEnabledByTrustedConfiguration = true,
        PersistedState = state,
        PersistedRevision = 7,
        RevocationPlanVerified = true,
        MembershipAndGrantsRevalidated = true,
        RetentionPolicyVerified = true,
        RestorationEvidenceVerified = true,
        DurableRecoveryCheckpointVerified = true,
        AuditAndFencingAvailable = true
    };

    private static Rules.ProposedLifecycleCommand Request(
        Rules.Command op = Rules.Command.Suspend,
        Rules.State state = Rules.State.Active) => new()
    {
        Operation = op,
        RequestedTenantId = "company-A",
        ExpectedSourceState = state,
        ExpectedRevision = 7,
        IdempotencyKey = "request-007",
        PayloadFingerprint = Fingerprint,
        PreservesDataAndAuditHistory = true
    };

    private static string Code(Rules.TrustedPlatformEvidence? trusted = null,
        Rules.ProposedLifecycleCommand? request = null,
        Rules.ExistingDurableReceipt? receipt = null) =>
        Rules.Evaluate(trusted ?? Trusted(), request ?? Request(), receipt).Code;

    [Theory]
    [InlineData(Rules.State.Requested, Rules.Command.BeginProvisioning)]
    [InlineData(Rules.State.Provisioning, Rules.Command.CompleteProvisioning)]
    [InlineData(Rules.State.Active, Rules.Command.Suspend)]
    [InlineData(Rules.State.Suspended, Rules.Command.Reactivate)]
    [InlineData(Rules.State.Suspended, Rules.Command.Archive)]
    [InlineData(Rules.State.Archived, Rules.Command.LogicalDelete)]
    [InlineData(Rules.State.Provisioning, Rules.Command.MarkRepairableFailure)]
    [InlineData(Rules.State.RepairableFailure, Rules.Command.ResumeProvisioning)]
    public void AllowedLifecycleTransitionsRequireAdditionalApplicationCas(
        Rules.State state, Rules.Command operation)
    {
        var outcome = Rules.Evaluate(Trusted(state), Request(operation, state));
        Assert.Equal("VERIFIED_PRECONDITIONS_REQUIRE_APPLICATION_CAS", outcome.Code);
        Assert.True(outcome.RequiresApplicationEnforcement);
        Assert.False(outcome.AuthorizesTenantMutation);
        Assert.False(outcome.AuthorizesGlobalPermission);
        Assert.False(outcome.AuthorizesPhysicalDeletion);
        Assert.False(outcome.AuthorizesProductionCutover);
        Assert.False(outcome.WritesToDatabase);
    }

    [Fact]
    public void MissingRevokedOrTenantOnlyActorNeverBecomesPlatformOperator()
    {
        Assert.Equal("TRUSTED_PLATFORM_OPERATOR_REQUIRED", Rules.Evaluate(null, Request()).Code);
        Assert.Equal("TRUSTED_PLATFORM_OPERATOR_REQUIRED", Rules.Evaluate(Trusted(), null).Code);
        Assert.Equal("TRUSTED_PLATFORM_OPERATOR_REQUIRED",
            Code(Trusted() with { BackendVerifiedSignedJwt = false }));
        Assert.Equal("TRUSTED_PLATFORM_OPERATOR_REQUIRED",
            Code(Trusted() with { CurrentExplicitPlatformGrantVerified = false }));
        Assert.Equal("TRUSTED_PLATFORM_OPERATOR_REQUIRED",
            Code(Trusted() with { ActorOrGrantRevoked = true }));
        Assert.Equal("TRUSTED_PLATFORM_OPERATOR_REQUIRED",
            Code(Trusted() with { CurrentOperatorRoleVerified = false }));
    }

    [Fact]
    public void TargetBAndUnverifiedPersistenceCannotBorrowTargetAAuthority()
    {
        Assert.Equal("CROSS_TENANT_LIFECYCLE_DENIED",
            Code(request: Request() with { RequestedTenantId = "company-B" }));
        Assert.Equal("CROSS_TENANT_LIFECYCLE_DENIED",
            Code(Trusted() with { TargetOwnershipVerifiedServerSide = false }));
        Assert.Equal("CROSS_TENANT_LIFECYCLE_DENIED",
            Code(Trusted() with { VerifiedTargetTenantId = "company-B" }));
    }

    [Fact]
    public void FeatureDisabledStalePolicyOrAbsentAuditFenceAlwaysDenies()
    {
        Assert.Equal("TRUSTED_POLICY_FENCE_OR_FEATURE_DISABLED",
            Code(Trusted() with { TrustedLifecycleConfigurationEnabled = false }));
        Assert.Equal("TRUSTED_POLICY_FENCE_OR_FEATURE_DISABLED",
            Code(Trusted() with { RequestedCommandEnabledByTrustedConfiguration = false }));
        Assert.Equal("TRUSTED_POLICY_FENCE_OR_FEATURE_DISABLED",
            Code(Trusted() with { FreshPolicyRevisionVerified = false }));
        Assert.Equal("TRUSTED_POLICY_FENCE_OR_FEATURE_DISABLED",
            Code(Trusted() with { AuditAndFencingAvailable = false }));
    }

    [Fact]
    public void RevisionAndSourceStateMustMatchTheTrustedSnapshot()
    {
        Assert.Equal("STALE_LIFECYCLE_STATE_OR_REVISION",
            Code(request: Request() with { ExpectedRevision = 6 }));
        Assert.Equal("STALE_LIFECYCLE_STATE_OR_REVISION",
            Code(request: Request() with { ExpectedRevision = 8 }));
        Assert.Equal("STALE_LIFECYCLE_STATE_OR_REVISION",
            Code(request: Request() with { ExpectedRevision = 0 }));
        Assert.Equal("STALE_LIFECYCLE_STATE_OR_REVISION",
            Code(request: Request() with { ExpectedSourceState = Rules.State.Suspended }));
    }

    [Fact]
    public void MalformedKeyAndFingerprintCannotActAsIdempotentProof()
    {
        Assert.Equal("INVALID_IDEMPOTENCY_OR_PAYLOAD_FINGERPRINT",
            Code(request: Request() with { IdempotencyKey = "" }));
        Assert.Equal("INVALID_IDEMPOTENCY_OR_PAYLOAD_FINGERPRINT",
            Code(request: Request() with { IdempotencyKey = new string('x', 129) }));
        Assert.Equal("INVALID_IDEMPOTENCY_OR_PAYLOAD_FINGERPRINT",
            Code(request: Request() with { PayloadFingerprint = "short" }));
        Assert.Equal("INVALID_IDEMPOTENCY_OR_PAYLOAD_FINGERPRINT",
            Code(request: Request() with { PayloadFingerprint = "sensitive:plaintext" }));
    }

    [Fact]
    public void ClientSuppliedTenantDsnOrProductionMutationNeverOverridesServer()
    {
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_IRREVERSIBLE_ACTION",
            Code(request: Request() with { ClientSuppliesAuthorityOrTenantContext = true }));
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_IRREVERSIBLE_ACTION",
            Code(request: Request() with { ClientSuppliesHostDsnOrStorageRouting = true }));
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_IRREVERSIBLE_ACTION",
            Code(request: Request() with { RequestsPhysicalDeletionOrSchemaMutation = true }));
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_IRREVERSIBLE_ACTION",
            Code(request: Request() with { RequestsAutomaticGrantsOrSessionRestoration = true }));
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_IRREVERSIBLE_ACTION",
            Code(request: Request() with { RequestsLiveCutoverOrProductionSideEffects = true }));
        Assert.Equal("UNAUTHORIZED_CLIENT_OVERRIDE_OR_IRREVERSIBLE_ACTION",
            Code(request: Request() with { PreservesDataAndAuditHistory = false }));
    }

    [Fact]
    public void UnknownStatesAndForbiddenTransitionsAreExplicitlyRejected()
    {
        Assert.Equal("UNKNOWN_LIFECYCLE_STATE_OR_COMMAND",
            Code(Trusted() with { PersistedState = (Rules.State)99 },
                Request() with { ExpectedSourceState = (Rules.State)99 }));
        Assert.Equal("UNKNOWN_LIFECYCLE_STATE_OR_COMMAND",
            Code(request: Request() with { Operation = (Rules.Command)99 }));
        Assert.Equal("ILLEGAL_LIFECYCLE_TRANSITION",
            Code(request: Request(Rules.Command.Reactivate)));
        Assert.Equal("ILLEGAL_LIFECYCLE_TRANSITION",
            Code(Trusted(Rules.State.LogicalDeleted),
                Request(Rules.Command.Reactivate, Rules.State.LogicalDeleted)));
    }

    [Fact]
    public void SuspensionRequiresRevocationPlanAndReactivationFreshGrants()
    {
        Assert.Equal("SUSPENSION_REVOCATION_GATE_REQUIRED",
            Code(Trusted() with { RevocationPlanVerified = false }));
        Assert.Equal("REACTIVATION_FRESH_MEMBERSHIP_REQUIRED",
            Code(Trusted(Rules.State.Suspended) with { MembershipAndGrantsRevalidated = false },
                Request(Rules.Command.Reactivate, Rules.State.Suspended)));
        Assert.Equal("REACTIVATION_FRESH_MEMBERSHIP_REQUIRED",
            Code(Trusted(Rules.State.Suspended) with { RevocationPlanVerified = false },
                Request(Rules.Command.Reactivate, Rules.State.Suspended)));
    }

    [Fact]
    public void ArchiveAndLogicalDeleteRequireRetentionAndRestoration()
    {
        Assert.Equal("RETENTION_AND_RESTORATION_REQUIRED",
            Code(Trusted(Rules.State.Suspended) with { RetentionPolicyVerified = false },
                Request(Rules.Command.Archive, Rules.State.Suspended)));
        Assert.Equal("RETENTION_AND_RESTORATION_REQUIRED",
            Code(Trusted(Rules.State.Archived) with { RestorationEvidenceVerified = false },
                Request(Rules.Command.LogicalDelete, Rules.State.Archived)));
    }

    [Fact]
    public void InterruptedProvisioningCannotResumeWithoutDurableCheckpoint()
    {
        Assert.Equal("DURABLE_RECOVERY_CHECKPOINT_REQUIRED",
            Code(Trusted(Rules.State.Provisioning) with { DurableRecoveryCheckpointVerified = false },
                Request(Rules.Command.MarkRepairableFailure, Rules.State.Provisioning)));
        Assert.Equal("DURABLE_RECOVERY_CHECKPOINT_REQUIRED",
            Code(Trusted(Rules.State.RepairableFailure) with { DurableRecoveryCheckpointVerified = false },
                Request(Rules.Command.ResumeProvisioning, Rules.State.RepairableFailure)));
    }

    [Fact]
    public void CommittedOrInterruptedReplayAfterStateAdvanceRequiresDurableReadbackNotFreshCas()
    {
        var previous = new Rules.ExistingDurableReceipt
        {
            ActorId = "platform-operator-A",
            TenantId = "company-A",
            Operation = Rules.Command.Suspend,
            IdempotencyKey = "request-007",
            PayloadFingerprint = Fingerprint,
            CompletedAndCommitted = true
        };
        // Original request was Active@v7, after a committed suspension the
        // authoritative snapshot is Suspended@v8. Re-running the same request
        // must never become a new mutation or an incorrectly stale idempotent
        // receipt: the server must read the existing durable outcome.
        var now = Trusted(Rules.State.Suspended) with { PersistedRevision = 8 };
        var first = Rules.Evaluate(now, Request(), previous);
        Assert.Equal("DURABLE_IDEMPOTENCY_READBACK_REQUIRED", first.Code);
        Assert.False(first.AuthorizesTenantMutation);
        Assert.False(first.WritesToDatabase);

        var incomplete = Rules.Evaluate(now, Request(),
            previous with { CompletedAndCommitted = false });
        Assert.Equal("INCOMPLETE_REPLAY_REQUIRES_CHECKPOINT_RECOVERY", incomplete.Code);
        Assert.False(incomplete.AuthorizesTenantMutation);

        Assert.Equal("IDEMPOTENCY_REUSE_CONFLICT",
            Rules.Evaluate(now, Request(),
                previous with { PayloadFingerprint = "BBBBBBBBBBBBBBBB" }).Code);
    }

    [Fact]
    public void SameKeyCannotBeReusedForDifferentPayloadActorOrTenant()
    {
        var receipt = new Rules.ExistingDurableReceipt
        {
            ActorId = "platform-operator-A",
            TenantId = "company-A",
            Operation = Rules.Command.Suspend,
            IdempotencyKey = "request-007",
            PayloadFingerprint = Fingerprint,
            CompletedAndCommitted = true
        };
        Assert.Equal("IDEMPOTENCY_REUSE_CONFLICT",
            Code(receipt: receipt with { PayloadFingerprint = "BBBBBBBBBBBBBBBB" }));
        Assert.Equal("IDEMPOTENCY_REUSE_CONFLICT",
            Code(receipt: receipt with { TenantId = "company-B" }));
        Assert.Equal("IDEMPOTENCY_REUSE_CONFLICT",
            Code(receipt: receipt with { ActorId = "platform-operator-B" }));
        Assert.Equal("IDEMPOTENCY_REUSE_CONFLICT",
            Code(receipt: receipt with { Operation = Rules.Command.Reactivate }));
    }

    [Fact]
    public void IdenticalReplayNeverCausesAnotherMutationWithoutDurableReadback()
    {
        var receipt = new Rules.ExistingDurableReceipt
        {
            ActorId = "platform-operator-A",
            TenantId = "company-A",
            Operation = Rules.Command.Suspend,
            IdempotencyKey = "request-007",
            PayloadFingerprint = Fingerprint,
            CompletedAndCommitted = true
        };
        var done = Rules.Evaluate(Trusted(), Request(), receipt);
        Assert.Equal("DURABLE_IDEMPOTENCY_READBACK_REQUIRED", done.Code);
        Assert.True(done.RequiresApplicationEnforcement);
        Assert.False(done.AuthorizesTenantMutation);

        var interrupted = Rules.Evaluate(Trusted(), Request(),
            receipt with { CompletedAndCommitted = false });
        Assert.Equal("INCOMPLETE_REPLAY_REQUIRES_CHECKPOINT_RECOVERY", interrupted.Code);
        Assert.False(interrupted.AuthorizesTenantMutation);
        Assert.False(interrupted.WritesToDatabase);
    }
}
