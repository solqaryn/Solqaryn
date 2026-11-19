using Rules = Solqaryn.Domain.AutomationV5.C1103OutboxEventDomainRules;
using Solqaryn.Application.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C1103M0302OutboxApplicationServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 19, 20, 0, 0, DateTimeKind.Utc);
    private static readonly Guid EventId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly string Hash = new('a', 64);

    private static Rules.VerifiedServerContext Context() =>
        new(101, true, true, false,
            new HashSet<string>(StringComparer.Ordinal) { "ventas.factura.emitida" }, Now);
    private static Rules.EventDraft Draft() =>
        new(101, EventId, "opaque-unique-key-123456789", "ventas.factura.emitida",
            Hash, Now.AddMinutes(-1), "correlation", 1, 0, 3);
    private static Rules.ExistingIntent Existing() =>
        new(101, EventId, Draft().IdempotencyKey!, "ventas.factura.emitida",
            Hash, 1, false, false);
    private static C1103OutboxApplicationService.TrustedEvidence Evidence() =>
        new(Context(), null, 101, true, true, true, true, true);

    private sealed class FakeReader : IC1103OutboxEvidenceReader
    {
        public C1103OutboxApplicationService.TrustedEvidence? Data { get; set; } = Evidence();
        public Exception? Error { get; set; }
        public Action? OnRead { get; set; }
        public int Calls { get; private set; }
        public int LastTenant { get; private set; }
        public ValueTask<C1103OutboxApplicationService.TrustedEvidence?> ReadAsync(
            int requestedTenant, Guid eventId, string opaqueKey, CancellationToken cancellationToken)
        {
            Calls++;
            LastTenant = requestedTenant;
            OnRead?.Invoke();
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Data);
        }
    }

    [Fact]
    public async Task PositiveDomainPreflightNeverAuthorizesWriteOrDelivery()
    {
        var reader = new FakeReader();
        var plan = await new C1103OutboxApplicationService(reader).PrepareEnqueueAsync(Draft());
        Assert.Equal("ATOMIC_RESERVATION_AND_RUNTIME_AUTH_REQUIRED", plan.Code);
        Assert.True(plan.RequiresAtomicReservation);
        Assert.False(plan.AuthorizesDispatch);
        Assert.False(plan.AuthorizesPersistence);
        Assert.False(plan.CertifiesSignedJwtHttpOrMySql);
        Assert.Equal(1, reader.Calls);
        Assert.Equal(101, reader.LastTenant);
    }

    [Fact]
    public async Task NullDraftRejectedWithoutReader()
    {
        var reader = new FakeReader();
        Assert.Equal("INVALID_UNTRUSTED_REQUEST",
            (await new C1103OutboxApplicationService(reader).PrepareEnqueueAsync(null)).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task EmptyEventIdRejectedWithoutReader()
    {
        var reader = new FakeReader();
        Assert.Equal("INVALID_UNTRUSTED_REQUEST",
            (await new C1103OutboxApplicationService(reader)
                .PrepareEnqueueAsync(Draft() with { EventoId = Guid.Empty })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ZeroTenantRejectedWithoutReader()
    {
        var reader = new FakeReader();
        Assert.Equal("INVALID_UNTRUSTED_REQUEST",
            (await new C1103OutboxApplicationService(reader)
                .PrepareEnqueueAsync(Draft() with { EmpresaId = 0 })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task MissingOpaqueKeyRejectedBeforeReader()
    {
        var reader = new FakeReader();
        Assert.Equal("INVALID_UNTRUSTED_REQUEST",
            (await new C1103OutboxApplicationService(reader)
                .PrepareEnqueueAsync(Draft() with { IdempotencyKey = null })).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ForgedTenantEvidenceIsDenied()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader
            { Data = Evidence() with { AuthenticatedTenantId = 202 } }).PrepareEnqueueAsync(Draft());
        Assert.Equal("AUTHENTICATED_TENANT_MISMATCH", plan.Code);
    }

    [Fact]
    public async Task BackendContextForOtherTenantIsDenied()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader
            { Data = Evidence() with { Context = Context() with { EmpresaId = 202 } } })
            .PrepareEnqueueAsync(Draft());
        Assert.Equal("AUTHENTICATED_TENANT_MISMATCH", plan.Code);
    }

    [Theory]
    [InlineData("jwt")]
    [InlineData("membership")]
    [InlineData("rbac")]
    [InlineData("audit")]
    [InlineData("trust")]
    public async Task StaleOrForgedBackendEvidenceFailsClosed(string missing)
    {
        var data = Evidence();
        data = missing switch
        {
            "jwt" => data with { CurrentJwtBound = false },
            "membership" => data with { CurrentMembershipRechecked = false },
            "rbac" => data with { CurrentExplicitEventGrantRechecked = false },
            "audit" => data with { AuditReady = false },
            _ => data with { FromTrustedBackend = false }
        };
        var plan = await new C1103OutboxApplicationService(new FakeReader { Data = data })
            .PrepareEnqueueAsync(Draft());
        Assert.Equal("FRESH_BACKEND_AUTHORITY_AND_AUDIT_REQUIRED", plan.Code);
        Assert.False(plan.AuthorizesDispatch);
    }

    [Fact]
    public async Task RevokedDomainGrantDeniedEvenWithTrustedReader()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader
            { Data = Evidence() with { Context = Context() with { PermissionRevoked = true } } })
            .PrepareEnqueueAsync(Draft());
        Assert.Equal("MEMBERSHIP_OR_EVENT_GRANT_DENIED", plan.Code);
    }

    [Fact]
    public async Task UnlistedEventDenied()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader())
            .PrepareEnqueueAsync(Draft() with { EventType = "sys.admin" });
        Assert.Equal("EVENT_ID_OR_TYPE_DENIED", plan.Code);
    }

    [Fact]
    public async Task ReplayedIntentCannotDispatchAgain()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader
            { Data = Evidence() with { Existing = Existing() } }).PrepareEnqueueAsync(Draft());
        Assert.Equal("EXACT_REPLAY_NO_NEW_DISPATCH", plan.Code);
        Assert.False(plan.RequiresAtomicReservation);
    }

    [Fact]
    public async Task ExistingIntentFromAnotherTenantCannotAuthorizeReplay()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader
            { Data = Evidence() with { Existing = Existing() with { EmpresaId = 202 } } })
            .PrepareEnqueueAsync(Draft());
        Assert.Equal("EXISTING_INTENT_TENANT_MISMATCH", plan.Code);
    }

    [Fact]
    public async Task SameIdempotencyKeyButDifferentHashIsConflict()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader
            { Data = Evidence() with { Existing = Existing() with { PayloadSha256 = new string('b', 64) } } })
            .PrepareEnqueueAsync(Draft());
        Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT_409", plan.Code);
    }

    [Fact]
    public async Task DeadLetterIsTerminal()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader
            { Data = Evidence() with { Existing = Existing() with { DeadLetter = true } } })
            .PrepareEnqueueAsync(Draft());
        Assert.Equal("DEAD_LETTER_TERMINAL", plan.Code);
    }

    [Fact]
    public async Task MissingEvidenceFailsClosed()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader { Data = null })
            .PrepareEnqueueAsync(Draft());
        Assert.Equal("FRESH_BACKEND_AUTHORITY_AND_AUDIT_REQUIRED", plan.Code);
    }

    [Fact]
    public async Task AdapterFailureIsNotAWriteOrDataLeak()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader
            { Error = new InvalidOperationException("secret internal provider connection") })
            .PrepareEnqueueAsync(Draft());
        Assert.Equal("TRUSTED_EVIDENCE_READER_UNAVAILABLE", plan.Code);
        Assert.DoesNotContain("secret", plan.Code);
        Assert.False(plan.AuthorizesPersistence);
    }

    [Fact]
    public async Task FutureEventTimeDenied()
    {
        var plan = await new C1103OutboxApplicationService(new FakeReader())
            .PrepareEnqueueAsync(Draft() with { OccurredAtUtc = Now.AddDays(1) });
        Assert.Equal("SCHEMA_OR_TRUSTED_UTC_INVALID", plan.Code);
    }

    [Fact]
    public async Task CancellationBeforeEvidenceReadNeverCallsAdapter()
    {
        var reader = new FakeReader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => { await new C1103OutboxApplicationService(reader)
                .PrepareEnqueueAsync(Draft(), cts.Token); });
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task CancellationInsideAdapterIsPropagated()
    {
        using var cts = new CancellationTokenSource();
        var reader = new FakeReader
        {
            OnRead = () => cts.Cancel(),
            Error = new OperationCanceledException(cts.Token)
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => { await new C1103OutboxApplicationService(reader)
                .PrepareEnqueueAsync(Draft(), cts.Token); });
        Assert.Equal(1, reader.Calls);
    }
}
