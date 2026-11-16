using Microsoft.Extensions.Logging;
using Rules = Solqaryn.Domain.AutomationV5.V5CommercialDocumentDomainRules;
using Solqaryn.Infrastructure.AutomationV5;
using Xunit;
using Pipeline = Solqaryn.Infrastructure.AutomationV5.V5CommercialDocumentSafeAuditPipeline;
using Batch = Solqaryn.Infrastructure.AutomationV5.V5CommercialDocumentIsolatedReviewBatch;
using Adapter = Solqaryn.Infrastructure.AutomationV5.V5CommercialDocumentIsolatedReadAdapter;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5CommercialDocumentSafeAuditPipelineTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string Digest = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";

    private static Pipeline.AuditEvidence Valid() =>
        new(7, 11, 11, true, true, true, false, true, "safe-corr-12345", Sha);

    private sealed class Authority : IC0704CurrentCommercialAuditSource
    {
        public Pipeline.AuditEvidence? State = Valid();
        public Exception? Error;
        public int Reads;
        public ValueTask<Pipeline.AuditEvidence?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            if (Error is not null) throw Error;
            return ValueTask.FromResult(State);
        }
    }
    private sealed class Principals : IV5CommercialTrustedPrincipalSource
    {
        public Adapter.PrincipalSnapshot? State = new(
            7, 11, Sha, "trusted-proof", true, true, true, true, false,
            new HashSet<Rules.Operation> { Rules.Operation.ConfirmOrder }, false, false);
        public int Reads;
        public ValueTask<Adapter.PrincipalSnapshot?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            return ValueTask.FromResult(State);
        }
    }
    private sealed class Documents : IV5CommercialTenantDocumentSource
    {
        public Adapter.DocumentSnapshot? State = new(
            12, 11, 11, 11, "BORRADOR", "HNL", 12.50m, 5, Sha, true, false);
        public int Reads;
        public ValueTask<Adapter.DocumentSnapshot?> ReadAsync(int tenant, int id, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            return ValueTask.FromResult(State);
        }
    }
    private sealed class Capture : ILogger<Pipeline>
    {
        private sealed class Scope : IDisposable { public void Dispose() { } }
        public List<string> Messages = new();
        public bool ThrowOnLog;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Scope();
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (ThrowOnLog) throw new InvalidOperationException("private sink token");
            Messages.Add(formatter(state, exception));
        }
    }

    private static (Pipeline Service, Authority Source, Principals Principals,
        Documents Docs, Capture Logger) Setup()
    {
        var authority = new Authority();
        var principals = new Principals();
        var documents = new Documents();
        var logger = new Capture();
        var batch = new Batch(new Adapter(principals, documents));
        return (new Pipeline(authority, batch, logger),
            authority, principals, documents, logger);
    }
    private static Rules.Command Command() =>
        new(Rules.Operation.ConfirmOrder, 5, 12.50m, "HNL", "private-idem-key", Digest);
    private static Batch.Ticket Ticket() => new(12, Command(), Sha);

    [Fact]
    public async Task NominalReviewLogsOnlyTrustedCorrelationWithoutGrant()
    {
        var s = Setup();
        var r = await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket() }, 11);
        Assert.Equal("AUDIT_REVIEW_ONLY_NO_LIVE_EFFECT", r.Code);
        Assert.True(r.LoggerAccepted);
        Assert.Equal(1, r.ReceiptCount);
        Assert.False(r.GrantsTenantOrResourceAccess);
        Assert.False(r.GrantsPaymentOrFiscalIssuance);
        Assert.False(r.StartsHostedWorkerOrTransaction);
        Assert.False(r.CertifiesDurableAudit);
        Assert.Single(s.Logger.Messages);
        Assert.Contains("safe-corr-12345", s.Logger.Messages[0]);
        Assert.Contains("REVIEW_ONLY", s.Logger.Messages[0]);
        Assert.DoesNotContain("private-idem-key", s.Logger.Messages[0]);
        Assert.DoesNotContain("12.50", s.Logger.Messages[0]);
        Assert.DoesNotContain(Sha, s.Logger.Messages[0]);
        Assert.Equal(1, s.Principals.Reads);
        Assert.Equal(1, s.Docs.Reads);
    }
    [Fact]
    public async Task NullEmptyTooLargeAndInvalidTenantAreRejectedBeforeSource()
    {
        var s = Setup();
        Assert.Equal("AUDIT_BATCH_INVALID", (await s.Service.ReviewAndAuditAsync(null, 11)).Code);
        Assert.Equal("AUDIT_BATCH_INVALID", (await s.Service.ReviewAndAuditAsync(Array.Empty<Batch.Ticket?>(), 11)).Code);
        Assert.Equal("AUDIT_BATCH_INVALID", (await s.Service.ReviewAndAuditAsync(
            Enumerable.Repeat<Batch.Ticket?>(Ticket(), 17).ToArray(), 11)).Code);
        Assert.Equal("AUDIT_BATCH_INVALID", (await s.Service.ReviewAndAuditAsync(
            new Batch.Ticket?[] { Ticket() }, 0)).Code);
        Assert.Equal(0, s.Source.Reads);
        Assert.Empty(s.Logger.Messages);
    }
    [Fact]
    public async Task MalformedTicketOrShaNeverReadsAuthority()
    {
        var s = Setup();
        foreach (var batch in new Batch.Ticket?[][] {
            new Batch.Ticket?[] { null },
            new Batch.Ticket?[] { Ticket() with { DocumentId = 0 } },
            new Batch.Ticket?[] { Ticket() with { SourceSha = "bad" } },
            new Batch.Ticket?[] { Ticket() with { Command = null } }
        })
            Assert.Equal("AUDIT_TICKET_INVALID", (await s.Service.ReviewAndAuditAsync(batch, 11)).Code);
        Assert.Equal(0, s.Source.Reads);
        Assert.Empty(s.Logger.Messages);
    }
    [Fact]
    public async Task CrossTenantAndSpoofedSourceShaFailClosed()
    {
        var s = Setup();
        s.Source.State = Valid() with { ResourceTenantId = 22 };
        Assert.Equal("CURRENT_VERIFIED_COMMERCIAL_AUDIT_SCOPE_REQUIRED",
            (await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket() }, 11)).Code);
        s.Source.State = Valid();
        Assert.Equal("CURRENT_VERIFIED_COMMERCIAL_AUDIT_SCOPE_REQUIRED",
            (await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket() }, 22)).Code);
        s.Source.State = Valid() with { SourceSha = new string('a', 40) };
        Assert.Equal("CURRENT_VERIFIED_COMMERCIAL_AUDIT_SCOPE_REQUIRED",
            (await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket() }, 11)).Code);
        Assert.Equal(0, s.Principals.Reads);
        Assert.Empty(s.Logger.Messages);
    }
    [Fact]
    public async Task MembershipRoleGrantsAndTrustedSourceRevocationFailClosed()
    {
        var s = Setup();
        foreach (var state in new[] {
            Valid() with { MembershipActive = false },
            Valid() with { CurrentRoleVerified = false },
            Valid() with { GrantRevoked = true },
            Valid() with { BackendIdentityVerified = false },
            Valid() with { SourceAuthenticated = false },
            Valid() with { ActorId = 0 },
            Valid() with { TenantId = 0 }
        })
        {
            s.Source.State = state;
            Assert.Equal("CURRENT_VERIFIED_COMMERCIAL_AUDIT_SCOPE_REQUIRED",
                (await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket() }, 11)).Code);
        }
        Assert.Equal(0, s.Principals.Reads);
        Assert.Empty(s.Logger.Messages);
    }
    [Fact]
    public async Task CorrelationInjectionOrSensitiveTextCannotBeLogged()
    {
        var s = Setup();
        foreach (var key in new[] { "short", "secret\nbearer=123", "tenant:11", new string('x', 97) })
        {
            s.Source.State = Valid() with { InternalCorrelationKey = key };
            Assert.Equal("CURRENT_VERIFIED_COMMERCIAL_AUDIT_SCOPE_REQUIRED",
                (await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket() }, 11)).Code);
        }
        Assert.Empty(s.Logger.Messages);
        Assert.Equal(0, s.Principals.Reads);
    }
    [Fact]
    public async Task DuplicateReplayAndFiscalCommandCannotAuthorize()
    {
        var s = Setup();
        var duplicate = await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket(), Ticket() }, 11);
        Assert.Equal("AUDIT_REVIEW_DENIED_NO_LIVE_EFFECT", duplicate.Code);
        Assert.False(duplicate.GrantsPaymentOrFiscalIssuance);
        Assert.False(duplicate.CertifiesDurableAudit);
        var fiscal = await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] {
            Ticket() with { Command = Command() with { RequestedOperation = Rules.Operation.IssueInvoice } }
        }, 11);
        Assert.Equal("AUDIT_REVIEW_DENIED_NO_LIVE_EFFECT", fiscal.Code);
        Assert.All(s.Logger.Messages, msg => Assert.DoesNotContain("private-idem-key", msg));
    }
    [Fact]
    public async Task UnderlyingReaderRevocationCannotPass()
    {
        var s = Setup();
        s.Principals.State = s.Principals.State! with { GrantsRevoked = true };
        var r = await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket() }, 11);
        Assert.Equal("AUDIT_REVIEW_DENIED_NO_LIVE_EFFECT", r.Code);
        Assert.False(r.GrantsTenantOrResourceAccess);
        Assert.Equal(0, s.Docs.Reads);
    }
    [Fact]
    public async Task AuthorityFailureIsOpaqueAndNoExceptionLeak()
    {
        var s = Setup();
        s.Source.Error = new InvalidOperationException("secret=value");
        var r = await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket() }, 11);
        Assert.Equal("AUDIT_AUTHORITY_UNAVAILABLE", r.Code);
        Assert.False(r.LoggerAccepted);
        Assert.Single(s.Logger.Messages);
        Assert.DoesNotContain("secret=value", s.Logger.Messages[0]);
        Assert.Equal(0, s.Principals.Reads);
    }
    [Fact]
    public async Task LoggingFailureNeverReturnsAudited()
    {
        var s = Setup();
        s.Logger.ThrowOnLog = true;
        var r = await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket() }, 11);
        Assert.Equal("AUDIT_LOG_SINK_UNAVAILABLE", r.Code);
        Assert.False(r.LoggerAccepted);
        Assert.False(r.CertifiesDurableAudit);
    }
    [Fact]
    public async Task CancellationStopsBeforeReadingOrLogging()
    {
        var s = Setup();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await s.Service.ReviewAndAuditAsync(new Batch.Ticket?[] { Ticket() }, 11, cts.Token));
        Assert.Equal(0, s.Source.Reads);
        Assert.Empty(s.Logger.Messages);
    }
    [Fact]
    public async Task SixteenAtBoundaryProduceOnlyReadOnlyReviews()
    {
        var s = Setup();
        var tickets = Enumerable.Range(1, 16).Select(i =>
            new Batch.Ticket(12, Command() with { IdempotencyKey = "key-" + i }, Sha))
            .Cast<Batch.Ticket?>().ToArray();
        var r = await s.Service.ReviewAndAuditAsync(tickets, 11);
        Assert.Equal("AUDIT_REVIEW_ONLY_NO_LIVE_EFFECT", r.Code);
        Assert.Equal(16, r.ReceiptCount);
        Assert.False(r.StartsHostedWorkerOrTransaction);
    }
}
