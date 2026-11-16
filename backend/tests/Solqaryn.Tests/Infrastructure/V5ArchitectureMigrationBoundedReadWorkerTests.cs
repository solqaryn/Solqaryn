using System.Security.Claims;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5ArchitectureMigrationDomainRules;
using Worker = Solqaryn.Infrastructure.AutomationV5.V5ArchitectureMigrationBoundedReadWorker;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5ArchitectureMigrationBoundedReadWorkerTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static Rules.TrustedBoundary Boundary() =>
        new(Sha, "tenant-A", true, true,
            new HashSet<string>(StringComparer.Ordinal) { "Render", "Vercel" });

    private static Rules.Candidate Candidate() =>
        new(Sha, "tenant-A", "Render", "Render", "dev",
            true, true, false, false, true, true, true, true, false);

    private static V5ArchitectureMigrationAuthorizedPreflight.Context Context(
        bool membership = true, string tenant = "tenant-A") =>
        new(new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, "user-A"),
                new Claim("tenant_id", tenant)
            }, "synthetic-only")),
            "user-A", "tenant-A",
            new HashSet<string>(StringComparer.Ordinal) {
                "architecture.migration.review" },
            membership, true, true, Sha, Sha);

    private static Worker Create(
        IV5ArchitectureMigrationReadReviewPort? port = null) =>
        new(port ?? new V5ArchitectureMigrationIsolatedReadPort(
            new V5ArchitectureMigrationIsolatedSnapshotAdapter(
                Boundary(), new Rules.Candidate?[] { Candidate() })));

    private static Worker.Probe Probe(string key = "attempt-001",
        string sha = Sha, string tenant = "tenant-A",
        V5ArchitectureMigrationAuthorizedPreflight.Context? ctx = null) =>
        new(key, sha, tenant, ctx ?? Context());

    private sealed class CountingPort : IV5ArchitectureMigrationReadReviewPort
    {
        public int Calls { get; private set; }
        public bool Throws { get; set; }
        public V5ArchitectureMigrationAuthorizedPreflight.Result Result { get; set; } =
            new("AUTHENTICATED_ADAPTER_EVIDENCE_REQUIRED", 1);
        public ValueTask<V5ArchitectureMigrationAuthorizedPreflight.Result>
            InspectAsync(V5ArchitectureMigrationAuthorizedPreflight.Context? context,
                string? sha, string? tenant, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            if (Throws) throw new InvalidOperationException("synthetic read failure");
            return ValueTask.FromResult(Result);
        }
    }

    [Fact]
    public async Task ValidSnapshotReturnsEvidenceRequiredNotDeployment()
    {
        var result = await Create().PreviewAsync(
            new Worker.Probe?[] { Probe("attempt-001"), Probe("attempt-002") });
        Assert.Equal("MIGRATION_PREVIEW_EXTERNAL_EVIDENCE_REQUIRED", result.Code);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, row => Assert.Equal(1, row.ItemsReviewed));
        Assert.False(result.AuthorizesDeployment);
        Assert.False(result.AuthorizesMySqlMigration);
        Assert.False(result.AuthorizesDnsOrSecretMutation);
        Assert.False(result.StartsHostedWorker);
        Assert.False(result.WritesAuditOrInstitutionalData);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.GrantsA5);
        Assert.False(result.CertifiesRealBackupRestore);
    }

    [Fact]
    public async Task CrossTenantAndForgedShaFailClosed()
    {
        Assert.Equal("MIGRATION_PREVIEW_AUTHORITY_REJECTED",
            (await Create().PreviewAsync(new Worker.Probe?[] {
                Probe(tenant: "tenant-B")
            })).Code);
        Assert.Equal("MIGRATION_PREVIEW_AUTHORITY_REJECTED",
            (await Create().PreviewAsync(new Worker.Probe?[] {
                Probe(sha: OtherSha)
            })).Code);
        Assert.Equal("MIGRATION_PREVIEW_AUTHORITY_REJECTED",
            (await Create().PreviewAsync(new Worker.Probe?[] {
                Probe(ctx: Context(tenant: "tenant-B"))
            })).Code);
    }

    [Fact]
    public async Task RevokedMembershipAndMissingPermissionsRejectWholeBatch()
    {
        var response = await Create().PreviewAsync(new Worker.Probe?[] {
            Probe("attempt-001"), Probe("attempt-002", ctx: Context(membership: false))
        });
        Assert.Equal("MIGRATION_PREVIEW_AUTHORITY_REJECTED", response.Code);
        Assert.Empty(response.Items);
    }

    [Fact]
    public async Task DuplicateKeysAreOrdinalAndRejectedBeforeAnyRead()
    {
        var port = new CountingPort();
        var result = await Create(port).PreviewAsync(new Worker.Probe?[] {
            Probe("attempt-001"), Probe("attempt-001")
        });
        Assert.Equal("MIGRATION_PREVIEW_DUPLICATE_KEY", result.Code);
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task MixedTenantAndShaBatchesRejectBeforeRead()
    {
        var port = new CountingPort();
        Assert.Equal("MIGRATION_PREVIEW_MIXED_SCOPE",
            (await Create(port).PreviewAsync(new Worker.Probe?[] {
                Probe("attempt-001"), Probe("attempt-002", tenant: "tenant-B")
            })).Code);
        Assert.Equal("MIGRATION_PREVIEW_MIXED_SCOPE",
            (await Create(port).PreviewAsync(new Worker.Probe?[] {
                Probe("attempt-001"), Probe("attempt-002", sha: OtherSha)
            })).Code);
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task LiveMutationAndMalformedInputRejectedBeforePort()
    {
        var port = new CountingPort();
        var result = await Create(port).PreviewAsync(new Worker.Probe?[] {
            Probe("attempt-001"),
            Probe("attempt-002") with { RequestsExternalMutation = true }
        });
        Assert.Equal("MIGRATION_PREVIEW_UNTRUSTED_REQUEST", result.Code);
        Assert.Equal(0, port.Calls);
        Assert.Equal("MIGRATION_PREVIEW_UNTRUSTED_REQUEST",
            (await Create(port).PreviewAsync(new Worker.Probe?[] {
                Probe("bad/key")
            })).Code);
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task NullAndOversizedBatchRejectWithoutRead()
    {
        var port = new CountingPort();
        Assert.Equal("MIGRATION_PREVIEW_BATCH_REQUIRED_OR_TOO_LARGE",
            (await Create(port).PreviewAsync(null)).Code);
        Assert.Equal("MIGRATION_PREVIEW_BATCH_REQUIRED_OR_TOO_LARGE",
            (await Create(port).PreviewAsync(Array.Empty<Worker.Probe?>())).Code);
        Assert.Equal("MIGRATION_PREVIEW_BATCH_REQUIRED_OR_TOO_LARGE",
            (await Create(port).PreviewAsync(Enumerable.Range(0, 13)
                .Select(i => (Worker.Probe?)Probe($"attempt-{i:000}")).ToArray())).Code);
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public async Task FailedSecondReadNeverYieldsPartialApproval()
    {
        var port = new CountingPort() { Result = new("PERMISSION_DENIED", 0) };
        var result = await Create(port).PreviewAsync(new Worker.Probe?[] {
            Probe("attempt-001"), Probe("attempt-002")
        });
        Assert.Equal("MIGRATION_PREVIEW_AUTHORITY_REJECTED", result.Code);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SourceExceptionReturnsSafeErrorWithoutPartialResults()
    {
        var port = new CountingPort { Throws = true };
        var result = await Create(port).PreviewAsync(new Worker.Probe?[] {
            Probe()
        });
        Assert.Equal("MIGRATION_PREVIEW_SOURCE_UNAVAILABLE", result.Code);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task CancellationPreventsPortReadAndNoSnapshot()
    {
        var port = new CountingPort();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await Create(port).PreviewAsync(new Worker.Probe?[] {
                Probe()
            }, new CancellationToken(true)));
        Assert.Equal(0, port.Calls);
    }

    [Fact]
    public void InvalidPortFailsBeforeCreatingWorker()
    {
        Assert.Throws<ArgumentNullException>(() => new Worker(null!));
        Assert.Throws<ArgumentNullException>(() =>
            new V5ArchitectureMigrationIsolatedReadPort(null!));
    }
}
