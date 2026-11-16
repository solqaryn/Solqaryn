using Solqaryn.Infrastructure.AutomationV5;
using Gate = Solqaryn.Application.AutomationV5.V17IntelligentTenantLoginAuthorization;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0201TenantPreferenceSafeAuditServiceTests
{
    private const int Actor = 42;
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private sealed class Sink : IC0201TenantAuditSink
    {
        public readonly List<C0201TenantAuditRecord> Records = new();
        public bool Fail { get; set; }
        public bool Throw { get; set; }
        public ValueTask<bool> WriteAsync(C0201TenantAuditRecord record,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Throw) throw new InvalidOperationException("private-cookie-value");
            Records.Add(record);
            return ValueTask.FromResult(!Fail);
        }
    }

    private sealed class Reader : IC0201TenantAuditPreviewReader
    {
        public int Calls { get; private set; }
        public bool Throw { get; set; }
        public TaskCompletionSource<bool>? Wait { get; set; }
        public async ValueTask<C0201TenantPreferenceBoundedRevalidationWorker.Review>
            ReadAsync(
            IReadOnlyList<C0201TenantPreferenceBoundedRevalidationWorker.Input>? inputs,
            int actor, long revision, string? sha, CancellationToken ct)
        {
            Calls++;
            if (Throw) throw new InvalidOperationException("private-request-value");
            if (Wait is not null) await Wait.Task.WaitAsync(ct);
            return new("PREVIEW_ONLY_AUTHENTICATED_BACKEND_REISSUE_REQUIRED",
                new[] { new C0201TenantPreferenceBoundedRevalidationWorker.Item(
                    "attempt-001", "ONE_TENANT_REISSUE_REQUIRED", 11, 0, false) });
        }
    }

    private static Gate.Membership Member(int tenant, bool revoked = false) =>
        new(Actor, tenant, "Private tenant PII", true, true, true, true, revoked);

    private static Gate.TrustedSnapshot Snapshot(
        bool sessionValid = false, int? sessionTenant = null,
        params Gate.Membership[] members) =>
        new(Actor, true, true, true, false, sessionValid, sessionTenant,
            null, null, false, false, members);

    private static IC0201TenantAuditPreviewReader RealReader(
        Gate.TrustedSnapshot snapshot) =>
        new C0201TenantAuditPreviewReader(
            new C0201TenantPreferenceBoundedRevalidationWorker(
                new C0201TenantPreferenceIsolatedSnapshotAdapter(
                    Actor, 7, Sha, snapshot)));

    private static C0201TenantPreferenceBoundedRevalidationWorker.Input Input(
        string key = "attempt-001", Gate.Request? request = null) =>
        new(key, request ?? new Gate.Request());

    [Fact]
    public async Task OneTenantSelectionAuditsOnlySafeCodeNotAccess()
    {
        var sink = new Sink();
        var service = new C0201TenantPreferenceSafeAuditService(
            RealReader(Snapshot(members: new[] { Member(11) })), sink);
        var result = await service.ReviewAsync("operation-001",
            new[] { Input() }, Actor, 7, Sha);
        Assert.Equal("AUDIT_SELECTION_REISSUE_PREVIEW", result.Code);
        Assert.Equal(1, result.ReviewedCount);
        Assert.False(result.IssuesJwt);
        Assert.False(result.AuthorizesTenantAccess);
        Assert.False(result.SwitchesTenantSession);
        Assert.False(result.WritesPreference);
        Assert.False(result.PersistsAudit);
        Assert.False(result.ReadsOrWritesMySql);
        Assert.False(result.CertifiesProduction);
        Assert.Single(sink.Records);
        Assert.DoesNotContain("Private", sink.Records[0].ToString());
        Assert.DoesNotContain("operation-001", sink.Records[0].ToString());
        Assert.DoesNotContain("11", sink.Records[0].ToString());
    }

    [Fact]
    public async Task MultipleMembershipsRequireChoiceWithoutNamesInAudit()
    {
        var sink = new Sink();
        var service = new C0201TenantPreferenceSafeAuditService(
            RealReader(Snapshot(members: new[] { Member(11), Member(22) })), sink);
        var result = await service.ReviewAsync("operation-001",
            new[] { Input() }, Actor, 7, Sha);
        Assert.Equal("AUDIT_SELECTION_CHOICE_PREVIEW", result.Code);
        Assert.Equal(1, sink.Records[0].ReviewedCount);
    }

    [Fact]
    public async Task ExplicitVerifiedSwitchIsOnlyAReissuePreview()
    {
        var sink = new Sink();
        var service = new C0201TenantPreferenceSafeAuditService(
            RealReader(Snapshot(true, 11, Member(11), Member(22))), sink);
        var result = await service.ReviewAsync("operation-001",
            new[] { Input(request: new Gate.Request(SelectedTenantId: 22,
                SwitchingEnterprise: true)) }, Actor, 7, Sha);
        Assert.Equal("AUDIT_SWITCH_REISSUE_PREVIEW", result.Code);
        Assert.False(result.SwitchesTenantSession);
    }

    [Fact]
    public async Task RevokedMembershipIsDeniedAndAuditedWithoutTenantDetails()
    {
        var sink = new Sink();
        var service = new C0201TenantPreferenceSafeAuditService(
            RealReader(Snapshot(members: new[] { Member(11, revoked: true) })), sink);
        var result = await service.ReviewAsync("operation-001",
            new[] { Input() }, Actor, 7, Sha);
        Assert.Equal("AUDIT_AUTHORITY_DENIED", result.Code);
        Assert.Equal(0, result.ReviewedCount);
        Assert.Equal(0, sink.Records[0].ReviewedCount);
    }

    [Fact]
    public async Task ForeignTenantCannotBecomeAuditSuccess()
    {
        var service = new C0201TenantPreferenceSafeAuditService(
            RealReader(Snapshot(members: new[] { Member(11) })), new Sink());
        var result = await service.ReviewAsync("operation-001",
            new[] { Input(request: new Gate.Request(SelectedTenantId: 22)) },
            Actor, 7, Sha);
        Assert.Equal("AUDIT_AUTHORITY_DENIED", result.Code);
    }

    [Fact]
    public async Task ForeignActorRevisionOrShaFailClosed()
    {
        var service = new C0201TenantPreferenceSafeAuditService(
            RealReader(Snapshot(members: new[] { Member(11) })), new Sink());
        Assert.Equal("AUDIT_AUTHORITY_DENIED",
            (await service.ReviewAsync("operation-001",
                new[] { Input() }, 99, 7, Sha)).Code);
        Assert.Equal("AUDIT_AUTHORITY_DENIED",
            (await service.ReviewAsync("operation-002",
                new[] { Input() }, Actor, 8, Sha)).Code);
        Assert.Equal("AUDIT_AUTHORITY_DENIED",
            (await service.ReviewAsync("operation-003",
                new[] { Input() }, Actor, 7, OtherSha)).Code);
    }

    [Fact]
    public async Task BrowserRoleAndTenantClaimsAreDenied()
    {
        var service = new C0201TenantPreferenceSafeAuditService(
            RealReader(Snapshot(members: new[] { Member(11) })), new Sink());
        var result = await service.ReviewAsync("operation-001",
            new[] { Input(request: new Gate.Request(ClientClaimsMembership: true)) },
            Actor, 7, Sha);
        Assert.Equal("AUDIT_AUTHORITY_DENIED", result.Code);
    }

    [Fact]
    public async Task ReplayedOperationKeyIsDeniedBeforeSecondRead()
    {
        var reader = new Reader();
        var service = new C0201TenantPreferenceSafeAuditService(reader, new Sink());
        Assert.Equal("AUDIT_SELECTION_REISSUE_PREVIEW",
            (await service.ReviewAsync("operation-001",
                new[] { Input() }, Actor, 7, Sha)).Code);
        Assert.Equal("AUDIT_DUPLICATE_SELECTION_DENIED",
            (await service.ReviewAsync("operation-001",
                new[] { Input() }, Actor, 7, Sha)).Code);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task MixedTenantBatchRejectsWholeRequest()
    {
        var service = new C0201TenantPreferenceSafeAuditService(
            RealReader(Snapshot(members: new[] { Member(11), Member(22) })), new Sink());
        var result = await service.ReviewAsync("operation-001",
            new[] {
                Input("attempt-001", new Gate.Request(SelectedTenantId: 11)),
                Input("attempt-002", new Gate.Request(SelectedTenantId: 22))
            }, Actor, 7, Sha);
        Assert.Equal("AUDIT_AUTHORITY_DENIED", result.Code);
        Assert.Equal(0, result.ReviewedCount);
    }

    [Fact]
    public async Task FailingOrThrowingAuditSinkDeniesSuccess()
    {
        var sink = new Sink { Fail = true };
        var service = new C0201TenantPreferenceSafeAuditService(new Reader(), sink);
        Assert.Equal("AUDIT_SINK_UNAVAILABLE",
            (await service.ReviewAsync("operation-001",
                new[] { Input() }, Actor, 7, Sha)).Code);
        sink.Fail = false;
        sink.Throw = true;
        Assert.Equal("AUDIT_SINK_UNAVAILABLE",
            (await service.ReviewAsync("operation-002",
                new[] { Input() }, Actor, 7, Sha)).Code);
    }

    [Fact]
    public async Task ThrowingAuthorityReaderDeniesWithoutErrorDetails()
    {
        var reader = new Reader { Throw = true };
        var sink = new Sink();
        var service = new C0201TenantPreferenceSafeAuditService(reader, sink);
        var result = await service.ReviewAsync("operation-001",
            new[] { Input() }, Actor, 7, Sha);
        Assert.Equal("AUDIT_AUTHORITY_READER_UNAVAILABLE", result.Code);
        Assert.DoesNotContain("private", sink.Records[0].ToString());
    }

    [Fact]
    public async Task ConcurrentSelectionIsDeniedWithoutSecondRead()
    {
        var reader = new Reader {
            Wait = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var service = new C0201TenantPreferenceSafeAuditService(reader, new Sink());
        var first = service.ReviewAsync("operation-001",
            new[] { Input() }, Actor, 7, Sha).AsTask();
        Assert.Equal(1, reader.Calls);
        var second = await service.ReviewAsync("operation-002",
            new[] { Input() }, Actor, 7, Sha);
        Assert.Equal("AUDIT_CONCURRENT_SELECTION_DENIED", second.Code);
        Assert.Equal(1, reader.Calls);
        reader.Wait.SetResult(true);
        Assert.Equal("AUDIT_SELECTION_REISSUE_PREVIEW", (await first).Code);
    }

    [Fact]
    public async Task InvalidAttemptAndObservedContextFailClosed()
    {
        var service = new C0201TenantPreferenceSafeAuditService(new Reader(), new Sink());
        Assert.Equal("AUDIT_INVALID_TRUSTED_CONTEXT",
            (await service.ReviewAsync("bad cookie", new[] { Input() },
                Actor, 7, Sha)).Code);
        Assert.Equal("AUDIT_INVALID_TRUSTED_CONTEXT",
            (await service.ReviewAsync("operation-001", new[] { Input() },
                Actor, 7, "short")).Code);
    }

    [Fact]
    public async Task CancellationDoesNotReportSuccess()
    {
        var service = new C0201TenantPreferenceSafeAuditService(new Reader(), new Sink());
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await service.ReviewAsync("operation-001",
                new[] { Input() }, Actor, 7, Sha, new CancellationToken(true)));
    }

    [Fact]
    public void ConstructorRequiresBothReaderAndSink()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new C0201TenantPreferenceSafeAuditService(null!, new Sink()));
        Assert.Throws<ArgumentNullException>(() =>
            new C0201TenantPreferenceSafeAuditService(new Reader(), null!));
    }
}
