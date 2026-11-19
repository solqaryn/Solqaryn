using System.Security.Claims;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ArchitectureMigrationDomainRules;
using Worker = Solqaryn.Infrastructure.AutomationV5.V5ArchitectureMigrationBoundedReadWorker;
using Service = Solqaryn.Infrastructure.AutomationV5.V5ArchitectureMigrationSafeAuditService;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5ArchitectureMigrationSafeAuditServiceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private sealed class Sink : IV5ArchitectureMigrationAuditSink
    {
        public readonly List<V5ArchitectureMigrationAuditRecord> Records = new();
        public bool Fail { get; set; }
        public bool Throw { get; set; }
        public ValueTask<bool> WriteAsync(
            V5ArchitectureMigrationAuditRecord record, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (Throw) throw new InvalidOperationException("PRIVATE_DSN_SHOULD_NOT_LOG");
            Records.Add(record);
            return ValueTask.FromResult(!Fail);
        }
    }

    private sealed class Reader : IV5ArchitectureMigrationAuditReader
    {
        public int Calls { get; private set; }
        public bool Throws { get; set; }
        public IReadOnlyList<Worker.Item>? Items { get; set; }
        public TaskCompletionSource<bool>? Wait { get; set; }
        public async ValueTask<Worker.Review> ReadAsync(
            IReadOnlyList<Worker.Probe?>? probes, CancellationToken ct)
        {
            Calls++;
            if (Throws) throw new InvalidOperationException("PRIVATE_SECRET_DATA");
            if (Wait is not null) await Wait.Task.WaitAsync(ct);
            return new("MIGRATION_PREVIEW_EXTERNAL_EVIDENCE_REQUIRED",
                Items ?? new[] { new Worker.Item("attempt-001", 1) });
        }
    }

    private static V5ArchitectureMigrationAuthorizedPreflight.Context Context(
        bool member = true, string tenant = "tenant-A") =>
        new(new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, "actor-1"),
                new Claim("tenant_id", tenant)
            }, "synthetic-only")),
            "actor-1", "tenant-A",
            new HashSet<string>(StringComparer.Ordinal) { "architecture.migration.review" },
            member, true, true, Sha, Sha);

    private static Rules.TrustedBoundary Boundary() =>
        new(Sha, "tenant-A", true, true,
            new HashSet<string>(StringComparer.Ordinal) { "Render", "Vercel" });

    private static Rules.Candidate Candidate() =>
        new(Sha, "tenant-A", "Render", "Render", "dev",
            true, true, false, false, true, true, true, true, false);

    private static IV5ArchitectureMigrationAuditReader RealReader() =>
        new V5ArchitectureMigrationBoundedAuditReader(
            new Worker(new V5ArchitectureMigrationIsolatedReadPort(
                new V5ArchitectureMigrationIsolatedSnapshotAdapter(
                    Boundary(), new Rules.Candidate?[] { Candidate() }))));

    private static Worker.Probe Probe(string key = "attempt-001",
        string sha = Sha, string tenant = "tenant-A",
        V5ArchitectureMigrationAuthorizedPreflight.Context? context = null) =>
        new(key, sha, tenant, context ?? Context());

    private static ValueTask<Service.Result> Check(Service svc,
        IReadOnlyList<Worker.Probe?>? probes = null, string op = "operation-001",
        string sha = Sha, string tenant = "tenant-A",
        CancellationToken ct = default) =>
        svc.ReviewAsync(op, sha, tenant,
            probes ?? new Worker.Probe?[] { Probe() }, ct);

    [Fact]
    public async Task TrustedSyntheticReadIsAuditOnlyWithoutMigrationAuthorization()
    {
        var sink = new Sink();
        var response = await Check(new Service(RealReader(), sink));
        Assert.Equal("MIGRATION_AUDIT_EXTERNAL_EVIDENCE_REQUIRED", response.Code);
        Assert.Equal(1, response.ReviewedCount);
        Assert.False(response.AuthorizesDeployment);
        Assert.False(response.AuthorizesDataMigration);
        Assert.False(response.ReadsOrWritesMySql);
        Assert.False(response.AuthorizesBackupRestore);
        Assert.False(response.MutatesDnsOrSecrets);
        Assert.False(response.AuthorizesTenantOrRbac);
        Assert.False(response.StartsHostedWorker);
        Assert.False(response.PersistsAudit);
        Assert.False(response.CertifiesProduction);
        Assert.Single(sink.Records);
        Assert.DoesNotContain("tenant-A", sink.Records[0].ToString());
        Assert.DoesNotContain("operation-001", sink.Records[0].ToString());
        Assert.DoesNotContain("Render", sink.Records[0].ToString());
    }

    [Fact]
    public async Task ForeignTenantAndStaleShaAreDeniedBeforeAuditSuccess()
    {
        var sink = new Sink();
        var svc = new Service(RealReader(), sink);
        Assert.Equal("MIGRATION_AUDIT_MIXED_SCOPE_DENIED",
            (await Check(svc, new Worker.Probe?[] { Probe(tenant: "tenant-B") })).Code);
        Assert.Equal("MIGRATION_AUDIT_MIXED_SCOPE_DENIED",
            (await Check(svc, new Worker.Probe?[] { Probe(sha: OtherSha) })).Code);
    }

    [Fact]
    public async Task RevokedMembershipCannotBeAuditedAsReady()
    {
        var r = await Check(new Service(RealReader(), new Sink()),
            new Worker.Probe?[] { Probe(context: Context(member: false)) });
        Assert.Equal("MIGRATION_AUDIT_AUTHORITY_DENIED", r.Code);
        Assert.Equal(0, r.ReviewedCount);
    }

    [Fact]
    public async Task CrossActorContextAndForgedClaimsCannotBecomeReady()
    {
        var r = await Check(new Service(RealReader(), new Sink()),
            new Worker.Probe?[] { Probe(context: Context(tenant: "tenant-B")) });
        Assert.Equal("MIGRATION_AUDIT_AUTHORITY_DENIED", r.Code);
    }

    [Fact]
    public async Task RequestedMutationIsRejectedBeforeReader()
    {
        var reader = new Reader();
        var r = await Check(new Service(reader, new Sink()),
            new Worker.Probe?[] { Probe() with { RequestsExternalMutation = true } });
        Assert.Equal("MIGRATION_AUDIT_MIXED_SCOPE_DENIED", r.Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task DuplicateOperationKeyNeverTriggersSecondRead()
    {
        var reader = new Reader();
        var svc = new Service(reader, new Sink());
        Assert.Equal("MIGRATION_AUDIT_EXTERNAL_EVIDENCE_REQUIRED",
            (await Check(svc)).Code);
        Assert.Equal("MIGRATION_AUDIT_DUPLICATE_DENIED",
            (await Check(svc)).Code);
        Assert.Equal(1, reader.Calls);
    }

    [Fact]
    public async Task EmptyAndOversizeAreDenied()
    {
        var svc = new Service(new Reader(), new Sink());
        Assert.Equal("MIGRATION_AUDIT_CONTEXT_REQUIRED",
            (await svc.ReviewAsync("operation-001", Sha, "tenant-A",
                Array.Empty<Worker.Probe?>())).Code);
        Assert.Equal("MIGRATION_AUDIT_CONTEXT_REQUIRED",
            (await svc.ReviewAsync("operation-001", Sha, "tenant-A",
                Enumerable.Range(0, 13)
                    .Select(i => (Worker.Probe?)Probe($"attempt-{i:000}")).ToArray())).Code);
    }

    [Fact]
    public async Task MalformedOperationKeyAndShaAreDenied()
    {
        var svc = new Service(new Reader(), new Sink());
        Assert.Equal("MIGRATION_AUDIT_CONTEXT_REQUIRED",
            (await Check(svc, op: "bad/key")).Code);
        Assert.Equal("MIGRATION_AUDIT_CONTEXT_REQUIRED",
            (await Check(svc, sha: "short")).Code);
    }

    [Fact]
    public async Task MismatchedReaderAttemptKeyNeverBecomesSuccessfulAudit()
    {
        var reader = new Reader { Items = new[] { new Worker.Item("attempt-999", 1) } };
        var sink = new Sink();
        var result = await Check(new Service(reader, sink));
        Assert.Equal("MIGRATION_AUDIT_AUTHORITY_DENIED", result.Code);
        Assert.Equal(0, result.ReviewedCount);
        Assert.Equal(1, reader.Calls);
        Assert.Equal("MIGRATION_AUDIT_AUTHORITY_DENIED", Assert.Single(sink.Records).Code);
    }

    [Fact]
    public async Task DuplicateReaderKeysAreRejectedEvenWhenCountMatches()
    {
        var reader = new Reader { Items = new[] {
            new Worker.Item("attempt-001", 1),
            new Worker.Item("attempt-001", 1)
        } };
        var sink = new Sink();
        var result = await Check(new Service(reader, sink),
            new Worker.Probe?[] { Probe(), Probe("attempt-002") });
        Assert.Equal("MIGRATION_AUDIT_AUTHORITY_DENIED", result.Code);
        Assert.Equal(0, result.ReviewedCount);
    }

    [Fact]
    public async Task DuplicateOrUnsafeInputAttemptKeyNeverReachesReader()
    {
        var reader = new Reader();
        var svc = new Service(reader, new Sink());
        var duplicate = await Check(svc,
            new Worker.Probe?[] { Probe(), Probe() });
        Assert.Equal("MIGRATION_AUDIT_MIXED_SCOPE_DENIED", duplicate.Code);
        var unsafeKey = await Check(svc,
            new Worker.Probe?[] { Probe("attempt/secret") }, op: "operation-002");
        Assert.Equal("MIGRATION_AUDIT_MIXED_SCOPE_DENIED", unsafeKey.Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ReorderedReaderKeysAreAcceptedOnlyWhenSetMatchesExactly()
    {
        var reader = new Reader { Items = new[] {
            new Worker.Item("attempt-002", 1),
            new Worker.Item("attempt-001", 1)
        } };
        var result = await Check(new Service(reader, new Sink()),
            new Worker.Probe?[] { Probe(), Probe("attempt-002") });
        Assert.Equal("MIGRATION_AUDIT_EXTERNAL_EVIDENCE_REQUIRED", result.Code);
        Assert.Equal(2, result.ReviewedCount);
        Assert.False(result.CertifiesProduction);
    }

    [Fact]
    public async Task ReaderExceptionCannotExposeSecret()
    {
        var sink = new Sink();
        var svc = new Service(new Reader { Throws = true }, sink);
        Assert.Equal("MIGRATION_AUDIT_READER_UNAVAILABLE",
            (await Check(svc)).Code);
        Assert.DoesNotContain("PRIVATE_SECRET_DATA", sink.Records[0].ToString());
    }

    [Fact]
    public async Task AuditSinkFailureOrExceptionDeniesSuccess()
    {
        var sink = new Sink { Fail = true };
        var svc = new Service(new Reader(), sink);
        Assert.Equal("MIGRATION_AUDIT_SINK_UNAVAILABLE",
            (await Check(svc)).Code);
        sink.Fail = false;
        sink.Throw = true;
        Assert.Equal("MIGRATION_AUDIT_SINK_UNAVAILABLE",
            (await Check(svc, op: "operation-002")).Code);
    }

    [Fact]
    public async Task ConcurrentAuditCannotDoubleSelect()
    {
        var reader = new Reader {
            Wait = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var svc = new Service(reader, new Sink());
        var first = Check(svc).AsTask();
        Assert.Equal(1, reader.Calls);
        var second = await Check(svc, op: "operation-002");
        Assert.Equal("MIGRATION_AUDIT_CONCURRENT_DENIED", second.Code);
        Assert.Equal(1, reader.Calls);
        reader.Wait.SetResult(true);
        Assert.Equal("MIGRATION_AUDIT_EXTERNAL_EVIDENCE_REQUIRED",
            (await first).Code);
    }

    [Fact]
    public async Task CancellationNeverReturnsAuditSuccess()
    {
        var svc = new Service(new Reader(), new Sink());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Check(svc, ct: new CancellationToken(true)));
    }

    [Fact]
    public void RequiresBothReaderAndSink()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Service(null!, new Sink()));
        Assert.Throws<ArgumentNullException>(() =>
            new Service(new Reader(), null!));
    }
}
