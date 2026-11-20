using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C1103OutboxEventDomainRules;
using Service = Solqaryn.Application.AutomationV5.C1103OutboxApplicationService;
using ReaderInterface = Solqaryn.Application.AutomationV5.IC1103OutboxEvidenceReader;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C11-03.M05.01: deterministic isolated unit acceptance of outbox application.
/// The fixture does not sign JWTs or assert real HTTP/MySQL/worker integrations.
/// </summary>
public sealed class C1103M0501OutboxApplicationUnitTests
{
    private static readonly Guid EventId = Guid.Parse("11111111-2222-4333-8444-555555555555");
    private static readonly DateTime Now = new(2026, 11, 20, 18, 0, 0, DateTimeKind.Utc);
    private static readonly string Digest = new('b', 64);
    private const string Key = "outbox-safe-key-1234567890";
    private sealed class Reader : ReaderInterface
    {
        public Service.TrustedEvidence? Evidence { get; init; }
        public Exception? Failure { get; init; }
        public int Calls { get; private set; }
        public int LastRequestedTenant { get; private set; }
        public ValueTask<Service.TrustedEvidence?> ReadAsync(int requestedTenant, Guid eventId,
            string opaqueKey, CancellationToken cancellationToken)
        {
            Calls++;
            LastRequestedTenant = requestedTenant;
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null) throw Failure;
            return ValueTask.FromResult(Evidence);
        }
    }
    private static Rules.VerifiedServerContext Context() =>
        new(101, true, true, false, new HashSet<string>(StringComparer.Ordinal) { "ventas.factura.emitida" }, Now);
    private static Rules.EventDraft Draft() =>
        new(101, EventId, Key, "ventas.factura.emitida", Digest, Now.AddMinutes(-1),
            "opaque-correlation", 1, 0, 3);
    private static Service.TrustedEvidence Trusted() =>
        new(Context(), null, 101, true, true, true, true, true);
    private static void NoEffects(Service.Plan plan)
    {
        Assert.False(plan.AuthorizesDispatch);
        Assert.False(plan.AuthorizesPersistence);
        Assert.False(plan.CertifiesSignedJwtHttpOrMySql);
    }
    private static async Task<Service.Plan> Decide(Service.TrustedEvidence? e, Rules.EventDraft? d = null)
    {
        var r = new Reader { Evidence = e };
        return await new Service(r).PrepareEnqueueAsync(d ?? Draft());
    }

    [Fact] public async Task VerifiedFixtureOnlyRequestsReservationNotDispatch()
    {
        var reader = new Reader { Evidence = Trusted() };
        var p = await new Service(reader).PrepareEnqueueAsync(Draft());
        Assert.Equal("ATOMIC_RESERVATION_AND_RUNTIME_AUTH_REQUIRED", p.Code);
        Assert.True(p.RequiresAtomicReservation);
        Assert.Equal(101, reader.LastRequestedTenant);
        Assert.Equal(1, reader.Calls);
        NoEffects(p);
    }

    [Fact] public async Task InvalidInputFailsBeforeReadingBackendEvidence()
    {
        var reader = new Reader { Evidence = Trusted() };
        var sut = new Service(reader);
        Assert.Equal("INVALID_UNTRUSTED_REQUEST", (await sut.PrepareEnqueueAsync(null)).Code);
        Assert.Equal("INVALID_UNTRUSTED_REQUEST",
            (await sut.PrepareEnqueueAsync(Draft() with { IdempotencyKey = "short" })).Code);
        Assert.Equal("INVALID_UNTRUSTED_REQUEST",
            (await sut.PrepareEnqueueAsync(Draft() with { EventoId = Guid.Empty })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact] public async Task NullOrUntrustedEvidenceFailsClosed()
    {
        Assert.Equal("FRESH_BACKEND_AUTHORITY_AND_AUDIT_REQUIRED", (await Decide(null)).Code);
        Assert.Equal("FRESH_BACKEND_AUTHORITY_AND_AUDIT_REQUIRED",
            (await Decide(Trusted() with { FromTrustedBackend = false })).Code);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public async Task EveryStaleAuthorityFlagRejects(bool jwt, bool member, bool grant, bool audit)
    {
        var p = await Decide(Trusted() with {
            CurrentJwtBound = jwt, CurrentMembershipRechecked = member,
            CurrentExplicitEventGrantRechecked = grant, AuditReady = audit
        });
        Assert.Equal("FRESH_BACKEND_AUTHORITY_AND_AUDIT_REQUIRED", p.Code);
        NoEffects(p);
    }

    [Fact] public async Task ForgedTenantAndUnmatchedContextDenied()
    {
        Assert.Equal("AUTHENTICATED_TENANT_MISMATCH",
            (await Decide(Trusted() with { AuthenticatedTenantId = 202 })).Code);
        Assert.Equal("AUTHENTICATED_TENANT_MISMATCH",
            (await Decide(Trusted() with { Context = Context() with { EmpresaId = 202 } })).Code);
    }

    [Fact] public async Task RevocationAndUnknownEventCannotSucceed()
    {
        Assert.Equal("MEMBERSHIP_OR_EVENT_GRANT_DENIED",
            (await Decide(Trusted() with { Context = Context() with { PermissionRevoked = true } })).Code);
        Assert.Equal("EVENT_ID_OR_TYPE_DENIED",
            (await Decide(Trusted(), Draft() with { EventType = "root.superuser" })).Code);
    }

    [Fact] public async Task FutureClockAndCorruptDigestAreRejected()
    {
        Assert.Equal("SCHEMA_OR_TRUSTED_UTC_INVALID",
            (await Decide(Trusted(), Draft() with { OccurredAtUtc = Now.AddDays(1) })).Code);
        Assert.Equal("IDEMPOTENCY_OR_PAYLOAD_DIGEST_INVALID",
            (await Decide(Trusted(), Draft() with { PayloadSha256 = "malformed" })).Code);
    }

    [Fact] public async Task ExactReplayDoesNotScheduleAnotherDelivery()
    {
        var old = new Rules.ExistingIntent(101, EventId, Key, "ventas.factura.emitida", Digest, 1, false, false);
        var p = await Decide(Trusted() with { Existing = old });
        Assert.Equal("EXACT_REPLAY_NO_NEW_DISPATCH", p.Code);
        NoEffects(p);
    }

    [Fact] public async Task ReusedKeyWithChangedPayloadRejectsAsConflict()
    {
        var old = new Rules.ExistingIntent(101, EventId, Key, "ventas.factura.emitida", new string('a', 64), 1, false, false);
        var p = await Decide(Trusted() with { Existing = old });
        Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT_409", p.Code);
        NoEffects(p);
    }

    [Fact] public async Task CrossTenantExistingRecordDoesNotBecomeReplayAuthority()
    {
        var old = new Rules.ExistingIntent(202, EventId, Key, "ventas.factura.emitida", Digest, 1, false, false);
        var p = await Decide(Trusted() with { Existing = old });
        Assert.Equal("EXISTING_INTENT_TENANT_MISMATCH", p.Code);
        NoEffects(p);
    }

    [Fact] public async Task ReaderOutageReturnsOpaqueSafeError()
    {
        var reader = new Reader { Failure = new InvalidOperationException("sensitive-provider-key:1234") };
        var p = await new Service(reader).PrepareEnqueueAsync(Draft());
        Assert.Equal("TRUSTED_EVIDENCE_READER_UNAVAILABLE", p.Code);
        Assert.DoesNotContain("sensitive", p.Code);
        NoEffects(p);
    }

    [Fact] public async Task CancellationPropagatesRatherThanDispatching()
    {
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        var reader = new Reader { Evidence = Trusted() };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Service(reader).PrepareEnqueueAsync(Draft(), cancel.Token));
        Assert.Equal(0, reader.Calls);
    }

    [Fact] public async Task ReplayTerminalDeadLetterStillFails()
    {
        var old = new Rules.ExistingIntent(101, EventId, Key, "ventas.factura.emitida", Digest, 1, false, true);
        var p = await Decide(Trusted() with { Existing = old });
        Assert.Equal("DEAD_LETTER_TERMINAL", p.Code);
        NoEffects(p);
    }
}
