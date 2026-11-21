using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.C0402ContractDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0402ContractApplicationService;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0402M0305ReadOnlyPreflightQueueTests
{
    private static Service.ClientIntent Intent(int tenant = 101) =>
        new(7001, tenant, 2, Rules.ContractOperation.Suspend,
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), "isolated test");

    private static Service.VerifiedServerActor Actor(bool revoked = false) =>
        new(new Rules.VerifiedActor(101, 911, true, true, true, revoked),
            TrustedServerSource: true, SignedJwtEstablished: true,
            RelationalRbacFresh: true, MembershipFresh: true);

    private static Service.VerifiedContractContext Context() =>
        new(new Rules.ContractSnapshot(101, 7001, Rules.ContractState.Active, 2,
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null),
            new Rules.CommercialEvidence(true, true, true, true, true, true, true),
            ServerTenantFiltered: true, ResourceOwnedByActorTenant: true,
            SnapshotVersionFresh: true, PolicyProvenanceVerified: true);

    [Fact]
    public async Task RejectsClientAuthorityOverflowAndNeverAuthorizesWrites()
    {
        var actors = new ActorSource(null);
        var contracts = new ContractSource(Context());
        var queue = new C0402ContractReadOnlyPreflightQueue(
            new Service(actors, contracts), capacity: 1);

        Assert.False(queue.TryEnqueue(null));
        Assert.False(queue.TryEnqueue(Intent() with { RequestsDirectMutation = true }));
        Assert.False(queue.TryEnqueue(Intent() with { RequestsPaymentOrRefund = true }));
        Assert.False(queue.TryEnqueue(Intent() with { SuppliesRoleOrPolicyAuthority = true }));
        Assert.False(queue.TryEnqueue(Intent() with { ExpectedVersion = -1 }));
        Assert.False(queue.TryEnqueue(Intent() with { Cause = "unsafe\nvalue" }));
        Assert.True(queue.TryEnqueue(Intent()));
        Assert.False(queue.TryEnqueue(Intent()));
        var result = Assert.Single(await queue.DrainAsync(1));
        Assert.Equal("LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED", result.Code);
        Assert.False(result.AuthorizesContractMutation);
        Assert.False(result.AuthorizesPaymentOrRefund);
        Assert.False(result.AuthorizesPrivateRead);
        Assert.False(result.PersistsData);
        Assert.Equal(1, actors.Reads);
        Assert.Equal(0, contracts.Reads);
        Assert.True(queue.TryEnqueue(Intent()));
    }

    [Fact]
    public async Task TrustedUnitFixtureYieldsPreviewNotMutationOrPayment()
    {
        // Synthetic fixtures are not a real signed-JWT/HTTP/MySQL certificate.
        var contracts = new ContractSource(Context());
        var queue = new C0402ContractReadOnlyPreflightQueue(
            new Service(new ActorSource(Actor()), contracts));
        Assert.True(queue.TryEnqueue(Intent()));
        var result = Assert.Single(await queue.DrainAsync(1));
        Assert.Equal("REAL_AUTHORIZED_ATOMIC_TRANSACTION_AND_POLICY_STILL_REQUIRED", result.Code);
        Assert.True(result.IsDomainCandidateOnly);
        Assert.False(result.AuthorizesContractMutation);
        Assert.False(result.AuthorizesPaymentOrRefund);
        Assert.False(result.CertifiesJwtHttpMysql);
        Assert.False(result.PersistsData);
        Assert.Equal(1, contracts.Reads);
    }

    [Theory]
    [InlineData(202, false)]
    [InlineData(101, true)]
    public async Task ForeignTenantOrRevokedActorNeverReadsContract(int tenant, bool revoked)
    {
        var contracts = new ContractSource(Context());
        var queue = new C0402ContractReadOnlyPreflightQueue(
            new Service(new ActorSource(Actor(revoked)), contracts));
        Assert.True(queue.TryEnqueue(Intent(tenant)));
        var result = Assert.Single(await queue.DrainAsync(1));
        Assert.Equal(revoked
            ? "LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED"
            : "CLIENT_TENANT_HINT_MISMATCH", result.Code);
        Assert.Equal(0, contracts.Reads);
        Assert.False(result.AuthorizesContractMutation);
    }

    [Fact]
    public async Task CancellationBeforeDequeuePreservesWorkAndChecksAgain()
    {
        var actors = new ActorSource(null);
        var queue = new C0402ContractReadOnlyPreflightQueue(
            new Service(actors, new ContractSource(null)));
        Assert.True(queue.TryEnqueue(Intent()));
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await queue.DrainAsync(1, cancel.Token));
        Assert.Equal(0, actors.Reads);
        Assert.Single(await queue.DrainAsync(1));
        Assert.Equal(1, actors.Reads);
    }

    [Fact]
    public async Task InvalidCapacityAndDrainBoundsRejected()
    {
        var service = new Service(new ActorSource(null), new ContractSource(null));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new C0402ContractReadOnlyPreflightQueue(service, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new C0402ContractReadOnlyPreflightQueue(service, 65));
        var queue = new C0402ContractReadOnlyPreflightQueue(service);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await queue.DrainAsync(0));
    }

    private sealed class ActorSource(Service.VerifiedServerActor? actor) :
        IC0402VerifiedContractActorSource
    {
        public int Reads { get; private set; }
        public ValueTask<Service.VerifiedServerActor?> ReadCurrentAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Reads++;
            return ValueTask.FromResult(actor);
        }
    }

    private sealed class ContractSource(Service.VerifiedContractContext? contract) :
        IC0402VerifiedContractSnapshotSource
    {
        public int Reads { get; private set; }
        public ValueTask<Service.VerifiedContractContext?> ReadOwnedAsync(
            int tenant, long contractId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Reads++;
            return ValueTask.FromResult(tenant == 101 && contractId == 7001 ? contract : null);
        }
    }
}
