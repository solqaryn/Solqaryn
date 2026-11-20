using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5TenantLifecycleDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0203TenantLifecycleApplicationService;

namespace Solqaryn.Tests.Application;

public sealed class C0203TenantLifecycleApplicationServiceTests
{
    private sealed class Reader : IC0203LifecycleEvidenceReader
    {
        public Service.TrustedReadback? Value { get; set; }
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }
        public Rules.Command? LastRequestedOperation { get; private set; }
        public ValueTask<Service.TrustedReadback?> ReadAsync(string tenant, Rules.Command operation, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            LastRequestedOperation = operation;
            if (Failure is not null) throw Failure;
            return ValueTask.FromResult(Value);
        }
    }
    private const string Fingerprint = "A1B2C3D4E5F60708";
    private static Service.Intent Request() => new(
        "company-A", Rules.Command.Suspend, Rules.State.Active, 7,
        "request-007", Fingerprint);
    private static Rules.TrustedPlatformEvidence Trusted(
        Rules.State state = Rules.State.Active, long revision = 7) => new()
    {
        ActorId = "platform-operator-A", VerifiedTargetTenantId = "company-A",
        BackendVerifiedSignedJwt = true, CurrentExplicitPlatformGrantVerified = true,
        CurrentOperatorRoleVerified = true, TargetOwnershipVerifiedServerSide = true,
        FreshPolicyRevisionVerified = true, TrustedLifecycleConfigurationEnabled = true,
        RequestedCommandEnabledByTrustedConfiguration = true, PersistedState = state,
        PersistedRevision = revision, RevocationPlanVerified = true,
        MembershipAndGrantsRevalidated = true, RetentionPolicyVerified = true,
        RestorationEvidenceVerified = true, DurableRecoveryCheckpointVerified = true,
        AuditAndFencingAvailable = true
    };
    private static Service.TrustedReadback Evidence(
        Rules.TrustedPlatformEvidence? trusted = null,
        Rules.ExistingDurableReceipt? receipt = null,
        Rules.Command verifiedOperation = Rules.Command.Suspend) =>
        new(trusted ?? Trusted(), receipt, true, true, true, verifiedOperation);

    [Fact]
    public async Task PositivePreflightAlwaysRequiresSeparateDurableAdapter()
    {
        var reader = new Reader { Value = Evidence() };
        var outcome = await new Service(reader).InspectAsync(Request());
        Assert.Equal("DURABLE_LIFECYCLE_CAS_ADAPTER_REQUIRED", outcome.Code);
        Assert.True(outcome.RequiresDurableApplicationAdapter);
        Assert.False(outcome.AuthorizesTenantMutation);
        Assert.False(outcome.GrantsGlobalPermission);
        Assert.False(outcome.WritesToDatabase);
        Assert.False(outcome.AuthorizesProductionCutover);
        Assert.Equal(1, reader.Calls);
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("7", "9")]
    [InlineData("9", "3")]
    public async Task SqlBusinessIdsWithSinglePositiveDigitReachOnlyReadOnlyPreflight(
        string tenantId, string actorId)
    {
        var trusted = Trusted() with { ActorId = actorId, VerifiedTargetTenantId = tenantId };
        var reader = new Reader { Value = Evidence(trusted) };
        var outcome = await new Service(reader).InspectAsync(Request() with { TenantId = tenantId });
        Assert.Equal("DURABLE_LIFECYCLE_CAS_ADAPTER_REQUIRED", outcome.Code);
        Assert.False(outcome.AuthorizesTenantMutation);
        Assert.False(outcome.WritesToDatabase);
        Assert.Equal(1, reader.Calls);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("a")]
    public async Task InvalidSingleCharacterTenantDoesNotReachEvidenceReader(string tenantId)
    {
        var reader = new Reader { Value = Evidence() };
        var outcome = await new Service(reader).InspectAsync(Request() with { TenantId = tenantId });
        Assert.Equal("INVALID_LIFECYCLE_INTENT", outcome.Code);
        Assert.Equal(0, reader.Calls);
    }

    private sealed class LatePositiveReader(CancellationTokenSource source)
        : IC0203LifecycleEvidenceReader
    {
        public async ValueTask<Service.TrustedReadback?> ReadAsync(
            string tenant, Rules.Command operation, CancellationToken ignored)
        {
            await Task.Yield();
            source.Cancel();
            return Evidence();
        }
    }

    [Fact]
    public async Task CancelledWhileNonCooperativeEvidenceReaderIsAwaitedNeverReturnsPositive()
    {
        using var source = new CancellationTokenSource();
        var service = new Service(new LatePositiveReader(source));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await service.InspectAsync(Request(), source.Token));
        Assert.True(source.IsCancellationRequested);
    }


    [Theory]
    [InlineData(Rules.Command.Reactivate, Rules.State.Suspended)]
    [InlineData(Rules.Command.Archive, Rules.State.Suspended)]
    [InlineData(Rules.Command.LogicalDelete, Rules.State.Archived)]
    public async Task SuspendEvidenceCannotBeReusedForAnotherLifecycleOperation(
        Rules.Command requested, Rules.State persisted)
    {
        var reader = new Reader { Value = Evidence(Trusted(persisted)) };
        var intent = Request() with { Operation = requested, ExpectedSourceState = persisted };
        var outcome = await new Service(reader).InspectAsync(intent);
        Assert.Equal("TRUSTED_LIFECYCLE_EVIDENCE_REQUIRED", outcome.Code);
        Assert.Equal(requested, reader.LastRequestedOperation);
        Assert.False(outcome.AuthorizesTenantMutation);
        Assert.False(outcome.WritesToDatabase);
    }

    [Fact]
    public async Task MatchingServerBoundOperationRemainsReadOnlyAndRequiresDurableCas()
    {
        var reader = new Reader
        {
            Value = Evidence(Trusted(Rules.State.Suspended),
                verifiedOperation: Rules.Command.Reactivate)
        };
        var intent = Request() with
        {
            Operation = Rules.Command.Reactivate,
            ExpectedSourceState = Rules.State.Suspended
        };
        var outcome = await new Service(reader).InspectAsync(intent);
        Assert.Equal("DURABLE_LIFECYCLE_CAS_ADAPTER_REQUIRED", outcome.Code);
        Assert.Equal(Rules.Command.Reactivate, reader.LastRequestedOperation);
        Assert.False(outcome.AuthorizesTenantMutation);
        Assert.False(outcome.WritesToDatabase);
    }

    [Fact]
    public async Task UnknownOperationReturnedByTrustedReaderIsRejectedBeforeDomain()
    {
        var reader = new Reader
        {
            Value = Evidence(verifiedOperation: (Rules.Command)999)
        };
        var outcome = await new Service(reader).InspectAsync(Request());
        Assert.Equal("TRUSTED_LIFECYCLE_EVIDENCE_REQUIRED", outcome.Code);
        Assert.Equal(Rules.Command.Suspend, reader.LastRequestedOperation);
        Assert.False(outcome.WritesToDatabase);
    }

    [Fact]
    public async Task InvalidAndClientForgedIntentNeverCallsEvidenceReader()
    {
        var reader = new Reader { Value = Evidence() };
        var service = new Service(reader);
        Assert.Equal("INVALID_LIFECYCLE_INTENT", (await service.InspectAsync(null)).Code);
        Assert.Equal("INVALID_LIFECYCLE_INTENT",
            (await service.InspectAsync(Request() with { TenantId = " company-A " })).Code);
        Assert.Equal("INVALID_LIFECYCLE_INTENT",
            (await service.InspectAsync(Request() with { ExpectedRevision = 0 })).Code);
        Assert.Equal("INVALID_LIFECYCLE_INTENT",
            (await service.InspectAsync(Request() with { Operation = (Rules.Command)999 })).Code);
        Assert.Equal("UNTRUSTED_LIFECYCLE_REQUEST",
            (await service.InspectAsync(Request() with { ClientClaimsAuthority = true })).Code);
        Assert.Equal("UNTRUSTED_LIFECYCLE_REQUEST",
            (await service.InspectAsync(Request() with { ClientSuppliesDsn = true })).Code);
        Assert.Equal("UNTRUSTED_LIFECYCLE_REQUEST",
            (await service.InspectAsync(Request() with { RequestsProductionCutover = true })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task MissingStaleOrForeignTenantReadbackAlwaysDenies()
    {
        var reader = new Reader();
        var service = new Service(reader);
        Assert.Equal("TRUSTED_LIFECYCLE_EVIDENCE_REQUIRED",
            (await service.InspectAsync(Request())).Code);
        reader.Value = Evidence() with { ServerControlledReader = false };
        Assert.Equal("TRUSTED_LIFECYCLE_EVIDENCE_REQUIRED",
            (await service.InspectAsync(Request())).Code);
        reader.Value = Evidence() with { FreshSessionAndMembership = false };
        Assert.Equal("TRUSTED_LIFECYCLE_EVIDENCE_REQUIRED",
            (await service.InspectAsync(Request())).Code);
        reader.Value = Evidence() with { TenantFencedReceiptLookup = false };
        Assert.Equal("TRUSTED_LIFECYCLE_EVIDENCE_REQUIRED",
            (await service.InspectAsync(Request())).Code);
        reader.Value = Evidence(Trusted() with { VerifiedTargetTenantId = "company-B" });
        Assert.Equal("TRUSTED_LIFECYCLE_EVIDENCE_REQUIRED",
            (await service.InspectAsync(Request())).Code);
    }

    [Fact]
    public async Task RevokedJwtAndTenantBDoNotAcquireAuthority()
    {
        var reader = new Reader { Value = Evidence(Trusted() with { ActorOrGrantRevoked = true }) };
        var service = new Service(reader);
        Assert.Equal("TRUSTED_PLATFORM_OPERATOR_REQUIRED",
            (await service.InspectAsync(Request())).Code);
        reader.Value = Evidence(Trusted() with { BackendVerifiedSignedJwt = false });
        Assert.Equal("TRUSTED_PLATFORM_OPERATOR_REQUIRED",
            (await service.InspectAsync(Request())).Code);
        reader.Value = Evidence();
        Assert.Equal("TRUSTED_LIFECYCLE_EVIDENCE_REQUIRED",
            (await service.InspectAsync(Request() with { TenantId = "company-B" })).Code);
    }

    [Fact]
    public async Task PostCommitReplayNeverMutatesAndRejectsTenantCollision()
    {
        var receipt = new Rules.ExistingDurableReceipt
        {
            ActorId = "platform-operator-A", TenantId = "company-A",
            Operation = Rules.Command.Suspend, IdempotencyKey = "request-007",
            PayloadFingerprint = Fingerprint, CompletedAndCommitted = true
        };
        var reader = new Reader { Value = Evidence(Trusted(Rules.State.Suspended, 8), receipt) };
        var service = new Service(reader);
        var committed = await service.InspectAsync(Request());
        Assert.Equal("DURABLE_IDEMPOTENCY_READBACK_REQUIRED", committed.Code);
        Assert.True(committed.RequiresDurableApplicationAdapter);
        Assert.False(committed.WritesToDatabase);
        reader.Value = Evidence(Trusted(Rules.State.Suspended, 8),
            receipt with { CompletedAndCommitted = false });
        Assert.Equal("INCOMPLETE_REPLAY_REQUIRES_CHECKPOINT_RECOVERY",
            (await service.InspectAsync(Request())).Code);
        reader.Value = Evidence(Trusted(Rules.State.Suspended, 8),
            receipt with { TenantId = "company-B" });
        Assert.Equal("IDEMPOTENCY_REUSE_CONFLICT",
            (await service.InspectAsync(Request())).Code);
    }

    [Fact]
    public async Task StaleRevisionAndInvalidFingerprintFailClosed()
    {
        var reader = new Reader { Value = Evidence() };
        var service = new Service(reader);
        Assert.Equal("STALE_LIFECYCLE_STATE_OR_REVISION",
            (await service.InspectAsync(Request() with { ExpectedRevision = 6 })).Code);
        Assert.Equal("INVALID_IDEMPOTENCY_OR_PAYLOAD_FINGERPRINT",
            (await service.InspectAsync(Request() with { PayloadFingerprint = "short" })).Code);
    }

    [Fact]
    public async Task ProviderExceptionIsSanitized()
    {
        var reader = new Reader
        {
            Failure = new InvalidOperationException("mysql://secret.host?token=TOP_SECRET")
        };
        var outcome = await new Service(reader).InspectAsync(Request());
        Assert.Equal("LIFECYCLE_EVIDENCE_SOURCE_UNAVAILABLE", outcome.Code);
        Assert.DoesNotContain("TOP_SECRET", outcome.Code);
        Assert.False(outcome.WritesToDatabase);
    }

    [Fact]
    public async Task CancelledRequestDoesNotTouchProvider()
    {
        var reader = new Reader { Value = Evidence() };
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await new Service(reader).InspectAsync(Request(), cts.Token);
        });
        Assert.Equal(0, reader.Calls);
    }
}
