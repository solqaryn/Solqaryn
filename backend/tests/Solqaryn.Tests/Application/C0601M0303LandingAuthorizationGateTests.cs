using Solqaryn.Application.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0601M0303LandingAuthorizationGateTests
{
    private static readonly C0601LandingAuthorizationGate.VerifiedActor Valid = new(
        new C0601LandingAuthorizationGate.VerifiedScope(101, 7, 2),
        SignedJwtEstablished: true, MembershipCurrent: true, RoleAndGrantCurrent: true);

    private sealed class Reader(Func<CancellationToken,
        ValueTask<C0601LandingAuthorizationGate.VerifiedActor?>> read)
        : IC0601LandingVerifiedActorReader
    {
        public ValueTask<C0601LandingAuthorizationGate.VerifiedActor?> ReadCurrentAsync(
            CancellationToken cancellationToken) => read(cancellationToken);
    }

    private static C0601LandingAuthorizationGate Gate(
        C0601LandingAuthorizationGate.VerifiedActor? actor) =>
        new(new Reader(_ => ValueTask.FromResult(actor)));

    [Fact]
    public async Task VerifiedScopeOnlyAllowsInertInspection()
    {
        var decision = await Gate(Valid).InspectAsync(new C0601LandingAuthorizationGate.Request(101));
        Assert.True(decision.MayInspectInertCandidate);
        Assert.False(decision.AuthorizesPublication);
        Assert.False(decision.GrantsPrivateOrCrossTenantRead);
        Assert.False(decision.AuthorizesPayment);
        Assert.False(decision.IssuesJwt);
        Assert.False(decision.CertifiesPublicRuntime);
    }

    [Theory]
    [InlineData(0, false, false, false, false, false)]
    [InlineData(102, false, false, false, false, false)]
    [InlineData(101, true, false, false, false, false)]
    [InlineData(101, false, true, false, false, false)]
    [InlineData(101, false, false, true, false, false)]
    [InlineData(101, false, false, false, true, false)]
    [InlineData(101, false, false, false, false, true)]
    public async Task ClientAuthorityOrCrossTenantDenied(
        int tenant, bool role, bool host, bool publish, bool privateAccess, bool payment)
    {
        var decision = await Gate(Valid).InspectAsync(
            new C0601LandingAuthorizationGate.Request(tenant, role, host, publish, privateAccess, payment));
        Assert.Equal("LANDING_AUTHORIZATION_DENIED", decision.Code);
        Assert.False(decision.AuthorizesPublication);
    }

    [Fact]
    public async Task MissingRevokedOrForgedEvidenceFailsClosed()
    {
        var request = new C0601LandingAuthorizationGate.Request(101);
        Assert.Equal("LANDING_AUTHORIZATION_DENIED", (await Gate(null).InspectAsync(request)).Code);
        Assert.Equal("LANDING_AUTHORIZATION_DENIED", (await Gate(Valid with { MembershipCurrent = false }).InspectAsync(request)).Code);
        Assert.Equal("LANDING_AUTHORIZATION_DENIED", (await Gate(Valid with { RoleAndGrantCurrent = false }).InspectAsync(request)).Code);
        Assert.Equal("LANDING_AUTHORIZATION_DENIED", (await Gate(Valid with { SignedJwtEstablished = false }).InspectAsync(request)).Code);
    }

    [Fact]
    public async Task ReaderCancellationAfterAwaitNeverCreatesDecision()
    {
        using var cts = new CancellationTokenSource();
        var gate = new C0601LandingAuthorizationGate(new Reader(_ =>
        {
            cts.Cancel();
            return ValueTask.FromResult<C0601LandingAuthorizationGate.VerifiedActor?>(Valid);
        }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await gate.InspectAsync(new C0601LandingAuthorizationGate.Request(101), cts.Token));
    }

    [Fact]
    public async Task ProviderFaultIsDeniedAndPreCancellationPropagates()
    {
        var gate = new C0601LandingAuthorizationGate(new Reader(_ =>
            throw new InvalidOperationException("unavailable")));
        var request = new C0601LandingAuthorizationGate.Request(101);
        Assert.Equal("LANDING_AUTHORIZATION_DENIED", (await gate.InspectAsync(request)).Code);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await gate.InspectAsync(request, new CancellationToken(true)));
    }
}
