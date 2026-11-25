using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5UniversalCaseFileDomainRules;
using Gate = Solqaryn.Application.AutomationV5.V5UniversalCaseFileAuthorizationGate;

namespace Solqaryn.Tests.Application;

public sealed class V5UniversalCaseFileAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private sealed class Reader : IV5UniversalCaseFileVerifiedEvidenceReader
    {
        public Gate.VerifiedEvidence? Evidence { get; set; } = ValidEvidence();
        public int Calls { get; private set; }
        public bool BindRequestedAction { get; set; } = true;

        public ValueTask<Gate.VerifiedEvidence?> ReadAsync(
            int caseFileId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(Evidence);
        }

        public async ValueTask<Gate.VerifiedEvidence?> ReadForActionAsync(
            int caseFileId, Rules.CaseAction action, CancellationToken cancellationToken)
        {
            var evidence = await ReadAsync(caseFileId, cancellationToken);
            return BindRequestedAction && evidence is not null
                ? evidence with { VerifiedAction = action } : evidence;
        }
    }

    private sealed class LegacyReader : IV5UniversalCaseFileVerifiedEvidenceReader
    {
        public int Calls { get; private set; }
        public ValueTask<Gate.VerifiedEvidence?> ReadAsync(int caseFileId, CancellationToken ct)
        {
            Calls++;
            return ValueTask.FromResult<Gate.VerifiedEvidence?>(ValidEvidence());
        }
    }

    private static Rules.ServerAuthority Authority() =>
        new(11, 100, 5, true, true, true, true);

    private static Rules.StoredCase Stored() =>
        new(100, 5, 3, true, false, false);

    private static Gate.VerifiedEvidence ValidEvidence() => new(
        Sha, 41, 11, 100, true, true, true, false, true,
        Authority(), Stored(), false, false, false);

    private static Gate.Request Read() => new(
        41, Rules.CaseAction.ReadTimeline, Sha, null,
        false, false, null, null, null);

    private static Gate.Request Write() => Read() with
    {
        Action = Rules.CaseAction.AddObservation,
        ExpectedVersion = 3,
        IdempotencyKey = "case-41-v3",
        ActorReason = "verified operation"
    };

    [Fact]
    public async Task VerifiedReadRequiresAnotherAuthenticatedAdapterAndGrantsNothing()
    {
        var reader = new Reader();
        var result = await new Gate(reader).EvaluateAsync(Read());
        Assert.Equal("TRUSTED_CASEFILE_EXECUTION_REQUIRED", result.Code);
        Assert.True(result.RequiresSeparateAuthenticatedAdapter);
        Assert.Equal(1, reader.Calls);
        Assert.False(result.GrantsTimelineRead);
        Assert.False(result.GrantsCaseWrite);
        Assert.False(result.GrantsAttachment);
        Assert.False(result.GrantsInstitutionalWrite);
        Assert.False(result.AllowsTenantFilterBypass);
    }

    [Fact]
    public async Task ValidWriteIntentStillDoesNotAuthorizePersistence()
    {
        var result = await new Gate(new Reader()).EvaluateAsync(Write());
        Assert.Equal("TRUSTED_CASEFILE_EXECUTION_REQUIRED", result.Code);
        Assert.False(result.GrantsCaseWrite);
    }

    [Fact]
    public async Task NullInvalidAndStaleShaRejectWithoutReadingEvidence()
    {
        var reader = new Reader();
        var gate = new Gate(reader);
        Assert.Equal("CASEFILE_AUTHORIZATION_REQUEST_REQUIRED",
            (await gate.EvaluateAsync(null)).Code);
        Assert.Equal("CASEFILE_ID_OR_ACTION_INVALID",
            (await gate.EvaluateAsync(Read() with { CaseFileId = 0 })).Code);
        Assert.Equal("CASEFILE_ID_OR_ACTION_INVALID",
            (await gate.EvaluateAsync(Read() with { Action = (Rules.CaseAction)99 })).Code);
        Assert.Equal("CASEFILE_EXACT_SOURCE_SHA_REQUIRED",
            (await gate.EvaluateAsync(Read() with { ExpectedSourceSha = "bad" })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task BypassAndInstitutionalWriteRejectBeforeProvider()
    {
        var reader = new Reader();
        var gate = new Gate(reader);
        Assert.Equal("CASEFILE_TENANT_FILTER_BYPASS_DENIED",
            (await gate.EvaluateAsync(Read() with { AttemptsTenantFilterBypass = true })).Code);
        Assert.Equal("CASEFILE_INSTITUTIONAL_WRITE_DENIED",
            (await gate.EvaluateAsync(Read() with { AttemptsInstitutionalWrite = true })).Code);
        Assert.Equal("CASEFILE_CLIENT_TENANT_INVALID",
            (await gate.EvaluateAsync(Read() with { ClientSelectedTenantId = 0 })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task MissingOrStaleEvidenceCannotBeUsed()
    {
        var reader = new Reader { Evidence = null };
        var gate = new Gate(reader);
        Assert.Equal("CASEFILE_TRUSTED_EVIDENCE_MISSING_OR_STALE",
            (await gate.EvaluateAsync(Read())).Code);
        reader.Evidence = ValidEvidence() with { ObservedSourceSha = OtherSha };
        Assert.Equal("CASEFILE_TRUSTED_EVIDENCE_MISSING_OR_STALE",
            (await gate.EvaluateAsync(Read())).Code);
    }

    [Fact]
    public async Task RevocationMembershipAndTrustedOriginAreMandatory()
    {
        var reader = new Reader();
        var gate = new Gate(reader);
        reader.Evidence = ValidEvidence() with { PermissionRevoked = true };
        Assert.Equal("CASEFILE_CURRENT_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Read())).Code);
        reader.Evidence = ValidEvidence() with { MembershipReadFromTrustedBackend = false };
        Assert.Equal("CASEFILE_CURRENT_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Read())).Code);
        reader.Evidence = ValidEvidence() with { PermissionReadFromTrustedBackend = false };
        Assert.Equal("CASEFILE_CURRENT_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Read())).Code);
        reader.Evidence = ValidEvidence() with { ServerAuthenticationVerified = false };
        Assert.Equal("CASEFILE_CURRENT_AUTHORIZATION_REQUIRED",
            (await gate.EvaluateAsync(Read())).Code);
    }

    [Fact]
    public async Task ForeignTenantActorAndPersistedOwnerMismatchAlwaysDeny()
    {
        var reader = new Reader();
        var gate = new Gate(reader);
        reader.Evidence = ValidEvidence() with { VerifiedTenantId = 200 };
        Assert.Equal("CASEFILE_PERSISTED_OWNER_OR_IDENTITY_MISMATCH",
            (await gate.EvaluateAsync(Read())).Code);
        reader.Evidence = ValidEvidence() with { VerifiedActorId = 15 };
        Assert.Equal("CASEFILE_PERSISTED_OWNER_OR_IDENTITY_MISMATCH",
            (await gate.EvaluateAsync(Read())).Code);
        reader.Evidence = ValidEvidence() with { VerifiedCaseFileId = 42 };
        Assert.Equal("CASEFILE_PERSISTED_OWNER_OR_IDENTITY_MISMATCH",
            (await gate.EvaluateAsync(Read())).Code);
        reader.Evidence = ValidEvidence() with { CaseOwnershipReadFromPersistence = false };
        Assert.Equal("CASEFILE_PERSISTED_OWNER_OR_IDENTITY_MISMATCH",
            (await gate.EvaluateAsync(Read())).Code);
        reader.Evidence = ValidEvidence() with
        {
            Stored = Stored() with { OwnershipVerifiedFromPersistence = false }
        };
        Assert.Equal("CASEFILE_PERSISTED_OWNER_OR_IDENTITY_MISMATCH",
            (await gate.EvaluateAsync(Read())).Code);
    }

    [Fact]
    public async Task ClientTenantOverrideAndBranchMismatchFailClosed()
    {
        var reader = new Reader();
        var gate = new Gate(reader);
        Assert.Equal("CASEFILE_CLIENT_TENANT_OVERRIDE_DENIED",
            (await gate.EvaluateAsync(Read() with { ClientSelectedTenantId = 200 })).Code);
        reader.Evidence = ValidEvidence() with { Stored = Stored() with { OwnerBranchId = 10 } };
        Assert.Equal("CASEFILE_BRANCH_MISMATCH",
            (await gate.EvaluateAsync(Read())).Code);
    }

    [Fact]
    public async Task ArchivedWriteAndStaleVersionAreDenied()
    {
        var reader = new Reader();
        var gate = new Gate(reader);
        reader.Evidence = ValidEvidence() with { Stored = Stored() with { Archived = true } };
        Assert.Equal("ARCHIVED_CASEFILE_IS_READ_ONLY",
            (await gate.EvaluateAsync(Write())).Code);
        reader.Evidence = ValidEvidence();
        Assert.Equal("CASEFILE_VERSION_CONFLICT",
            (await gate.EvaluateAsync(Write() with { ExpectedVersion = 2 })).Code);
        Assert.Equal("SCOPED_IDEMPOTENCY_KEY_REQUIRED",
            (await gate.EvaluateAsync(Write() with { IdempotencyKey = null })).Code);
    }

    [Fact]
    public async Task IndependentDecisionReviewAndAttachmentOwnershipCannotBeClientGranted()
    {
        var reader = new Reader();
        var gate = new Gate(reader);
        var decision = Write() with { Action = Rules.CaseAction.RecordDecision };
        Assert.Equal("DECISION_REVIEW_REQUIRED",
            (await gate.EvaluateAsync(decision)).Code);
        reader.Evidence = ValidEvidence() with { IndependentDecisionReviewVerified = true };
        Assert.Equal("TRUSTED_CASEFILE_EXECUTION_REQUIRED",
            (await gate.EvaluateAsync(decision)).Code);
        var attachment = Write() with { Action = Rules.CaseAction.LinkAttachment };
        Assert.Equal("ATTACHMENT_ACCESS_DENIED",
            (await gate.EvaluateAsync(attachment)).Code);
        reader.Evidence = ValidEvidence() with { AttachmentOwnershipVerified = true };
        Assert.Equal("TRUSTED_CASEFILE_EXECUTION_REQUIRED",
            (await gate.EvaluateAsync(attachment)).Code);
        reader.Evidence = ValidEvidence() with
        {
            AttachmentOwnershipVerified = true, AttachmentLinkExpired = true
        };
        Assert.Equal("ATTACHMENT_ACCESS_DENIED",
            (await gate.EvaluateAsync(attachment)).Code);
    }

    [Fact]
    public async Task ReadGrantCannotBeReusedForDifferentOperation()
    {
        var reader = new Reader { BindRequestedAction = false,
            Evidence = ValidEvidence() with { VerifiedAction = Rules.CaseAction.ReadTimeline } };
        var gate = new Gate(reader);
        Assert.Equal("TRUSTED_CASEFILE_EXECUTION_REQUIRED", (await gate.EvaluateAsync(Read())).Code);
        Assert.Equal("CASEFILE_ACTION_GRANT_MISMATCH", (await gate.EvaluateAsync(Write())).Code);
        reader.Evidence = ValidEvidence();
        Assert.Equal("CASEFILE_ACTION_GRANT_MISMATCH", (await gate.EvaluateAsync(Read())).Code);
    }

    [Fact]
    public async Task LegacyReaderWithoutOperationBindingFailsClosed()
    {
        var reader = new LegacyReader();
        Assert.Equal("CASEFILE_TRUSTED_EVIDENCE_MISSING_OR_STALE",
            (await new Gate(reader).EvaluateAsync(Read())).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task CancellationAndMissingReaderFailClosed()
    {
        Assert.Throws<ArgumentNullException>(() => new Gate(null!));
        var reader = new Reader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Gate(reader).EvaluateAsync(Read(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
