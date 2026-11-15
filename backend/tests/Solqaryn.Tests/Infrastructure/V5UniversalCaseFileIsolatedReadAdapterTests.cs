using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5UniversalCaseFileDomainRules;
using Adapter = Solqaryn.Infrastructure.AutomationV5.V5UniversalCaseFileIsolatedReadAdapter;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5UniversalCaseFileIsolatedReadAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private sealed class Actors : IV5CaseFileTrustedActorSnapshotSource
    {
        public Adapter.ActorSnapshot? Snapshot { get; set; } = ValidActor();
        public int Calls { get; private set; }
        public ValueTask<Adapter.ActorSnapshot?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(Snapshot);
        }
    }

    private sealed class Cases : IV5CaseFileTenantScopedSnapshotSource
    {
        public Adapter.CaseSnapshot? Snapshot { get; set; } = ValidCase();
        public int Calls { get; private set; }
        public int? SeenTenant { get; private set; }
        public int? SeenCase { get; private set; }
        public ValueTask<Adapter.CaseSnapshot?> ReadAsync(
            int tenantId, int caseFileId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            SeenTenant = tenantId;
            SeenCase = caseFileId;
            return ValueTask.FromResult(Snapshot);
        }
    }

    private static Adapter.ActorSnapshot ValidActor() =>
        new(11, 100, 5, Sha, true, true, true, false);

    private static Adapter.CaseSnapshot ValidCase() =>
        new(41, 100, 5, 3, Sha, true, false, false, false, false, false);

    [Fact]
    public async Task TenantScopedLookupUsesServerActorAndDoesNotGrantAnything()
    {
        var actor = new Actors();
        var cases = new Cases();
        var bridge = new Adapter(actor, cases);
        var evidence = await bridge.ReadAsync(41, CancellationToken.None);
        Assert.NotNull(evidence);
        Assert.Equal(100, cases.SeenTenant);
        Assert.Equal(41, cases.SeenCase);
        Assert.Equal(11, evidence.VerifiedActorId);
        Assert.Equal(100, evidence.VerifiedTenantId);
        var gate = new V5UniversalCaseFileAuthorizationGate(bridge);
        var request = new V5UniversalCaseFileAuthorizationGate.Request(
            41, Rules.CaseAction.ReadTimeline, Sha, null,
            false, false, null, null, null);
        var result = await gate.EvaluateAsync(request);
        Assert.Equal("TRUSTED_CASEFILE_EXECUTION_REQUIRED", result.Code);
        Assert.False(result.GrantsTimelineRead);
        Assert.False(result.GrantsCaseWrite);
        Assert.False(result.GrantsAttachment);
        Assert.False(result.AllowsTenantFilterBypass);
        Assert.False(result.GrantsInstitutionalWrite);
    }

    [Fact]
    public async Task InvalidIdDoesNotCallAnySource()
    {
        var actors = new Actors();
        var cases = new Cases();
        Assert.Null(await new Adapter(actors, cases).ReadAsync(0, CancellationToken.None));
        Assert.Equal(0, actors.Calls);
        Assert.Equal(0, cases.Calls);
    }

    [Theory]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, false, false)]
    [InlineData(true, true, true, true)]
    public async Task UnverifiedOrRevokedActorNeverQueriesCase(
        bool authentication, bool membership, bool permission, bool revoked)
    {
        var actors = new Actors
        {
            Snapshot = ValidActor() with {
                ServerAuthenticationVerified = authentication,
                MembershipReadFromTrustedBackend = membership,
                PermissionReadFromTrustedBackend = permission,
                PermissionRevoked = revoked
            }
        };
        var cases = new Cases();
        Assert.Null(await new Adapter(actors, cases).ReadAsync(41, CancellationToken.None));
        Assert.Equal(0, cases.Calls);
    }

    [Fact]
    public async Task NullActorInvalidTenantBranchOrShaFailsBeforeCaseLookup()
    {
        var actors = new Actors();
        var cases = new Cases();
        var adapter = new Adapter(actors, cases);
        actors.Snapshot = null;
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
        actors.Snapshot = ValidActor() with { TenantId = 0 };
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
        actors.Snapshot = ValidActor() with { BranchId = 0 };
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
        actors.Snapshot = ValidActor() with { SourceSha = "bad" };
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
        Assert.Equal(0, cases.Calls);
    }

    [Fact]
    public async Task CrossTenantCaseOrWrongResourceFailsClosed()
    {
        var cases = new Cases { Snapshot = ValidCase() with { OwnerTenantId = 200 } };
        var adapter = new Adapter(new Actors(), cases);
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
        Assert.Equal(100, cases.SeenTenant);
        cases.Snapshot = ValidCase() with { CaseFileId = 42 };
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
    }

    [Fact]
    public async Task StaleVersionShaAndUnattributedLegacyFailClosed()
    {
        var cases = new Cases();
        var adapter = new Adapter(new Actors(), cases);
        cases.Snapshot = ValidCase() with { SourceSha = OtherSha };
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
        cases.Snapshot = ValidCase() with { Version = -1 };
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
        cases.Snapshot = ValidCase() with { UnattributedLegacy = true };
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
        cases.Snapshot = ValidCase() with { OwnershipVerifiedFromPersistence = false };
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
    }

    [Fact]
    public async Task ForeignBranchAndInvalidStoredBranchFailClosed()
    {
        var cases = new Cases();
        var adapter = new Adapter(new Actors(), cases);
        cases.Snapshot = ValidCase() with { OwnerBranchId = 6 };
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
        cases.Snapshot = ValidCase() with { OwnerBranchId = 0 };
        Assert.Null(await adapter.ReadAsync(41, CancellationToken.None));
    }

    [Fact]
    public async Task MissingCaseOrCancellationNeverReturnsEvidence()
    {
        var cases = new Cases { Snapshot = null };
        var bridge = new Adapter(new Actors(), cases);
        Assert.Null(await bridge.ReadAsync(41, CancellationToken.None));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await bridge.ReadAsync(41, cts.Token));
    }

    [Fact]
    public void NullSourcesAreRejectedAtConstruction()
    {
        Assert.Throws<ArgumentNullException>(() => new Adapter(null!, new Cases()));
        Assert.Throws<ArgumentNullException>(() => new Adapter(new Actors(), null!));
    }
}
