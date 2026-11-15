using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class V5UniversalCaseFileDomainRulesTests
{
    private static V5UniversalCaseFileDomainRules.ServerAuthority Authority() =>
        new(10, 100, 5, true, true, true, true);

    private static V5UniversalCaseFileDomainRules.StoredCase Stored() =>
        new(100, 5, 3, true, false, false);

    private static V5UniversalCaseFileDomainRules.Command Read() =>
        new(V5UniversalCaseFileDomainRules.CaseAction.ReadTimeline,
            null, null, null, null, false, false, false);

    private static V5UniversalCaseFileDomainRules.Command Write() =>
        new(V5UniversalCaseFileDomainRules.CaseAction.AddObservation,
            null, 3, "case-100-v3", "verified-update", false, false, false);

    private static string Check(
        V5UniversalCaseFileDomainRules.ServerAuthority? auth = null,
        V5UniversalCaseFileDomainRules.StoredCase? stored = null,
        V5UniversalCaseFileDomainRules.Command? cmd = null) =>
        V5UniversalCaseFileDomainRules.Evaluate(
            auth ?? Authority(), stored ?? Stored(), cmd ?? Read()).Code;

    [Fact]
    public void AuthorizedSnapshotOnlyRequestsRealApplicationEnforcement()
    {
        var v = V5UniversalCaseFileDomainRules.Evaluate(Authority(), Stored(), Read());
        Assert.True(v.RequiresTrustedApplicationEnforcement);
        Assert.False(v.AuthorizesPersistence);
        Assert.False(v.AuthorizesExternalMediaAccess);
        Assert.Equal("READY_FOR_TRUSTED_CASEFILE_ENFORCEMENT", Check(cmd: Write()));
    }

    [Fact]
    public void MissingOrRevokedIdentityAlwaysDenied()
    {
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Check(auth: Authority() with { Authenticated = false }));
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Check(auth: Authority() with { MembershipActive = false }));
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Check(auth: Authority() with { ExplicitPermissionCurrent = false }));
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Check(auth: Authority() with { ScopeResolvedOnServer = false }));
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            Check(auth: Authority() with { TenantId = null }));
    }

    [Fact]
    public void KnownForeignIdAndUnattributedLegacyCannotLeak()
    {
        Assert.Equal("CASEFILE_OWNER_OR_LEGACY_SCOPE_DENIED",
            Check(stored: Stored() with { OwnerTenantId = 200 }));
        Assert.Equal("CASEFILE_OWNER_OR_LEGACY_SCOPE_DENIED",
            Check(stored: Stored() with { OwnerTenantId = null }));
        Assert.Equal("CASEFILE_OWNER_OR_LEGACY_SCOPE_DENIED",
            Check(stored: Stored() with { UnattributedLegacy = true }));
        Assert.Equal("CASEFILE_OWNER_OR_LEGACY_SCOPE_DENIED",
            Check(stored: Stored() with { OwnershipVerifiedFromPersistence = false }));
        Assert.Equal("CLIENT_TENANT_OVERRIDE_DENIED",
            Check(cmd: Read() with { ClientSelectedTenantId = 200 }));
    }

    [Fact]
    public void BranchAndInvalidActionAreFailClosed()
    {
        Assert.Equal("CASEFILE_BRANCH_MISMATCH",
            Check(auth: Authority() with { BranchId = 9 }));
        Assert.Equal("CASEFILE_BRANCH_MISMATCH",
            Check(stored: Stored() with { OwnerBranchId = 0 }));
        Assert.Equal("UNKNOWN_CASEFILE_ACTION",
            Check(cmd: Read() with { Action = (V5UniversalCaseFileDomainRules.CaseAction)99 }));
    }

    [Fact]
    public void WritesRequireVersionAndScopedDedupeKey()
    {
        Assert.Equal("CASEFILE_VERSION_CONFLICT",
            Check(cmd: Write() with { ExpectedVersion = 2 }));
        Assert.Equal("CASEFILE_VERSION_CONFLICT",
            Check(cmd: Write() with { ExpectedVersion = null }));
        Assert.Equal("SCOPED_IDEMPOTENCY_KEY_REQUIRED",
            Check(cmd: Write() with { IdempotencyKey = "" }));
        Assert.Equal("SCOPED_IDEMPOTENCY_KEY_REQUIRED",
            Check(cmd: Write() with { IdempotencyKey = "bad\nkey" }));
        Assert.Equal("AUDIT_REASON_REQUIRED",
            Check(cmd: Write() with { ActorReason = "" }));
    }

    [Fact]
    public void ArchivedCaseRemainsReadOnly()
    {
        Assert.Equal("READY_FOR_TRUSTED_CASEFILE_ENFORCEMENT",
            Check(stored: Stored() with { Archived = true }));
        Assert.Equal("ARCHIVED_CASEFILE_IS_READ_ONLY",
            Check(stored: Stored() with { Archived = true }, cmd: Write()));
    }

    [Fact]
    public void DecisionMustCarryIndependentEvidence()
    {
        var decision = Write() with
        {
            Action = V5UniversalCaseFileDomainRules.CaseAction.RecordDecision
        };
        Assert.Equal("DECISION_REVIEW_REQUIRED", Check(cmd: decision));
        Assert.Equal("READY_FOR_TRUSTED_CASEFILE_ENFORCEMENT",
            Check(cmd: decision with { IndependentDecisionReviewVerified = true }));
    }

    [Fact]
    public void PrivateAttachmentRequiresOwnershipAndFreshLink()
    {
        var link = Write() with
        {
            Action = V5UniversalCaseFileDomainRules.CaseAction.LinkAttachment
        };
        Assert.Equal("ATTACHMENT_ACCESS_DENIED", Check(cmd: link));
        Assert.Equal("ATTACHMENT_ACCESS_DENIED",
            Check(cmd: link with { AttachmentOwnershipVerified = true, AttachmentLinkExpired = true }));
        Assert.Equal("READY_FOR_TRUSTED_CASEFILE_ENFORCEMENT",
            Check(cmd: link with { AttachmentOwnershipVerified = true }));
    }

    [Fact]
    public void NullInputsAreDenied()
    {
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            V5UniversalCaseFileDomainRules.Evaluate(null, Stored(), Read()).Code);
        Assert.Equal("SERVER_AUTHORITY_REQUIRED",
            V5UniversalCaseFileDomainRules.Evaluate(Authority(), null, Read()).Code);
    }
}
