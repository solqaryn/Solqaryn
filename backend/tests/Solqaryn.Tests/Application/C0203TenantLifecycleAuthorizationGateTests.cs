using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5TenantLifecycleDomainRules;
using Gate = Solqaryn.Application.AutomationV5.C0203TenantLifecycleAuthorizationGate;

namespace Solqaryn.Tests.Application;

public sealed class C0203TenantLifecycleAuthorizationGateTests
{
    private sealed class Reader : IC0203VerifiedOperatorEvidenceReader
    {
        public Gate.TrustedOperatorSnapshot? Value { get; set; }
        public Exception? Error { get; set; }
        public int Calls { get; private set; }
        public Rules.Command? LastRequestedCommand { get; private set; }
        public ValueTask<Gate.TrustedOperatorSnapshot?> ReadAsync(
            string tenant, Rules.Command command, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            LastRequestedCommand = command;
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Value);
        }
    }

    private static Gate.Intent Request() =>
        new("company-A", Rules.Command.Suspend);

    private static Gate.TrustedOperatorSnapshot Trusted() => new()
    {
        VerifiedActorId = "platform-admin-01", VerifiedTenantId = "company-A",
        VerifiedCommand = Rules.Command.Suspend,
        ServerControlledEvidence = true, BackendJwtSignatureVerified = true,
        CurrentPlatformGrantVerified = true, CurrentOperatorRoleVerified = true,
        MembershipAndSessionFresh = true, TargetOwnershipVerifiedServerSide = true,
        PolicyRevisionFresh = true, LifecycleFeatureEnabled = true,
        CommandEnabledByTrustedConfig = true, TenantFencedEvidenceRead = true
    };

    [Fact]
    public async Task VerifiedSnapshotOnlyPermitsReadOnlyPreflight_NotMutation()
    {
        var reader = new Reader { Value = Trusted() };
        var decision = await new Gate(reader).InspectAsync(Request());
        Assert.Equal("VERIFIED_READONLY_PREFLIGHT_ONLY", decision.Code);
        Assert.True(decision.MayProceedToReadOnlyApplicationPreflight);
        Assert.False(decision.AuthorizesTenantMutation);
        Assert.False(decision.GrantsPlatformPermission);
        Assert.False(decision.WritesToDatabase);
        Assert.False(decision.AuthorizesProductionCutover);
        Assert.Equal(1, reader.Calls);
        Assert.Equal(Rules.Command.Suspend, reader.LastRequestedCommand);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("a")]
    [InlineData(" company-A")]
    [InlineData("company/A")]
    [InlineData("company-\u200B")]
    public async Task InvalidTenantIdsNeverReachTrustedReader(string? tenantId)
    {
        var reader = new Reader { Value = Trusted() };
        var decision = await new Gate(reader).InspectAsync(Request() with { TargetTenantId = tenantId });
        Assert.Equal("LIFECYCLE_ACCESS_DENIED", decision.Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Boundary129AndUnknownCommandAreDeniedWithoutReading()
    {
        var reader = new Reader { Value = Trusted() };
        var gate = new Gate(reader);
        Assert.Equal("LIFECYCLE_ACCESS_DENIED",
            (await gate.InspectAsync(Request() with { TargetTenantId = new string('a', 129) })).Code);
        Assert.Equal("LIFECYCLE_ACCESS_DENIED",
            (await gate.InspectAsync(Request() with { Operation = (Rules.Command)999 })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ClientCannotSupplyJwtGrantStorageOrIrreversibleAuthority()
    {
        var reader = new Reader { Value = Trusted() };
        var gate = new Gate(reader);
        Assert.Equal("LIFECYCLE_ACCESS_DENIED",
            (await gate.InspectAsync(Request() with { ClientClaimsPlatformGrant = true })).Code);
        Assert.Equal("LIFECYCLE_ACCESS_DENIED",
            (await gate.InspectAsync(Request() with { ClientSuppliesActorOrJwt = true })).Code);
        Assert.Equal("LIFECYCLE_ACCESS_DENIED",
            (await gate.InspectAsync(Request() with { ClientSuppliesStorageRouting = true })).Code);
        Assert.Equal("LIFECYCLE_ACCESS_DENIED",
            (await gate.InspectAsync(Request() with { RequestsPhysicalDeletion = true })).Code);
        Assert.Equal("LIFECYCLE_ACCESS_DENIED",
            (await gate.InspectAsync(Request() with { RequestsAutomaticGrantOrRevival = true })).Code);
        Assert.Equal("LIFECYCLE_ACCESS_DENIED",
            (await gate.InspectAsync(Request() with { RequestsLiveCutover = true })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ForeignTenantRevocationAndUnverifiedJwtAlwaysDeny()
    {
        var reader = new Reader { Value = Trusted() };
        var gate = new Gate(reader);
        var cases = new[]
        {
            Trusted() with { VerifiedTenantId = "company-B" },
            Trusted() with { VerifiedActorId = "invalid actor" },
            Trusted() with { ServerControlledEvidence = false },
            Trusted() with { BackendJwtSignatureVerified = false },
            Trusted() with { CurrentPlatformGrantVerified = false },
            Trusted() with { CurrentOperatorRoleVerified = false },
            Trusted() with { MembershipAndSessionFresh = false },
            Trusted() with { ActorOrGrantRevoked = true },
            Trusted() with { TargetOwnershipVerifiedServerSide = false },
            Trusted() with { PolicyRevisionFresh = false },
            Trusted() with { LifecycleFeatureEnabled = false },
            Trusted() with { CommandEnabledByTrustedConfig = false },
            Trusted() with { TenantFencedEvidenceRead = false }
        };
        foreach (var testCase in cases)
        {
            reader.Value = testCase;
            var decision = await gate.InspectAsync(Request());
            Assert.Equal("LIFECYCLE_ACCESS_DENIED", decision.Code);
            Assert.False(decision.MayProceedToReadOnlyApplicationPreflight);
            Assert.False(decision.AuthorizesTenantMutation);
        }
        Assert.Equal(cases.Length, reader.Calls);
    }

    [Theory]
    [InlineData(Rules.Command.Reactivate)]
    [InlineData(Rules.Command.Archive)]
    [InlineData(Rules.Command.LogicalDelete)]
    public async Task PreviouslyVerifiedDifferentCommandCannotReauthorizeCurrentOperation(
        Rules.Command requestedCommand)
    {
        // A stale reader decision for Suspend must never authorize Reactivate,
        // Archive, or LogicalDelete even when every other trusted flag is true.
        var reader = new Reader { Value = Trusted() };
        var decision = await new Gate(reader).InspectAsync(
            Request() with { Operation = requestedCommand });
        Assert.Equal(requestedCommand, reader.LastRequestedCommand);
        Assert.Equal("LIFECYCLE_ACCESS_DENIED", decision.Code);
        Assert.False(decision.MayProceedToReadOnlyApplicationPreflight);
        Assert.False(decision.AuthorizesTenantMutation);
    }

    [Fact]
    public async Task UnknownOperationInTrustedSnapshotAlwaysDenies()
    {
        var reader = new Reader
        {
            Value = Trusted() with { VerifiedCommand = (Rules.Command)999 }
        };
        var decision = await new Gate(reader).InspectAsync(Request());
        Assert.Equal("LIFECYCLE_ACCESS_DENIED", decision.Code);
        Assert.False(decision.AuthorizesTenantMutation);
    }

    [Fact]
    public async Task NullReadbackAndProviderFailureNeverLeakDetails()
    {
        var reader = new Reader();
        var gate = new Gate(reader);
        Assert.Equal("LIFECYCLE_ACCESS_DENIED", (await gate.InspectAsync(Request())).Code);
        reader.Error = new InvalidOperationException("mysql://private?jwt=TOP_SECRET");
        var outcome = await gate.InspectAsync(Request());
        Assert.Equal("LIFECYCLE_ACCESS_DENIED", outcome.Code);
        Assert.DoesNotContain("TOP_SECRET", outcome.Code);
        Assert.False(outcome.WritesToDatabase);
    }

    private sealed class NonCooperativeCancellingReader(CancellationTokenSource cancellation)
        : IC0203VerifiedOperatorEvidenceReader
    {
        public async ValueTask<Gate.TrustedOperatorSnapshot?> ReadAsync(
            string tenant, Rules.Command command, CancellationToken ignored)
        {
            // Simulates an async database/provider that disregards cancellation and
            // hands back a previously trusted positive snapshot after the request dies.
            await Task.Yield();
            cancellation.Cancel();
            return Trusted();
        }
    }

    [Fact]
    public async Task CancellationWhileNonCooperativeProviderIsReadingNeverAuthorizes()
    {
        using var cts = new CancellationTokenSource();
        var gate = new Gate(new NonCooperativeCancellingReader(cts));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await gate.InspectAsync(Request(), cts.Token));
        Assert.True(cts.IsCancellationRequested);
    }

    [Fact]
    public async Task CancellationIsPropagatedWithoutTouchingProvider()
    {
        var reader = new Reader { Value = Trusted() };
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Gate(reader).InspectAsync(Request(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
