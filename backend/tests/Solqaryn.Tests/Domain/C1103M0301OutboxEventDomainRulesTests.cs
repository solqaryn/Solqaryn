using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C1103OutboxEventDomainRules;

namespace Solqaryn.Tests.Domain;

public sealed class C1103M0301OutboxEventDomainRulesTests
{
    private static readonly Guid EventId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly DateTime Now = new(2026, 11, 19, 20, 0, 0, DateTimeKind.Utc);
    private static readonly string Digest = new('a', 64);

    private static Rules.VerifiedServerContext Trusted() =>
        new(101, true, true, false,
            new HashSet<string>(StringComparer.Ordinal) { "ventas.factura.emitida" }, Now);

    private static Rules.EventDraft Draft() =>
        new(101, EventId, "safe-opaque-key-123456789", "ventas.factura.emitida",
            Digest, Now.AddMinutes(-1), "opaque-correlation", 1, 0, 3);

    private static Rules.ExistingIntent Stored() =>
        new(101, EventId, Draft().IdempotencyKey!, Draft().EventType!, Digest, 2, false, false);

    [Fact]
    public void VerifiedInputOnlyPassesDomainPreflightNotAuthorization()
    {
        var decision = Rules.CheckEnqueue(Trusted(), Draft());
        Assert.Equal("DOMAIN_PRECONDITIONS_ONLY", decision.Code);
        Assert.True(decision.SatisfiesDomainPreflight);
        Assert.False(decision.AuthorizesDispatch);
        Assert.False(decision.AuthorizesPersistence);
        Assert.False(decision.CertifiesJwtOrMySqlIntegration);
    }

    [Fact]
    public void MissingOrInvalidTrustedContextFailsClosed()
    {
        Assert.Equal("TRUSTED_CONTEXT_REQUIRED", Rules.CheckEnqueue(null, Draft()).Code);
        Assert.Equal("TRUSTED_CONTEXT_REQUIRED", Rules.CheckEnqueue(Trusted(), null).Code);
        Assert.Equal("TRUSTED_CONTEXT_REQUIRED",
            Rules.CheckEnqueue(Trusted() with { EmpresaId = 0 }, Draft()).Code);
        Assert.Equal("TRUSTED_CONTEXT_REQUIRED",
            Rules.CheckEnqueue(Trusted() with { TrustedNowUtc = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified) },
                Draft()).Code);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void RevocationOrMissingGrantDenies(bool active, bool granted, bool revoked)
    {
        var context = Trusted() with {
            MembershipActive = active, ExplicitEventGrant = granted, PermissionRevoked = revoked
        };
        Assert.Equal("MEMBERSHIP_OR_EVENT_GRANT_DENIED",
            Rules.CheckEnqueue(context, Draft()).Code);
    }

    [Fact]
    public void CrossTenantIsDeniedEvenWithIdenticalIdempotencyKey()
    {
        Assert.Equal("TENANT_MISMATCH",
            Rules.CheckEnqueue(Trusted(), Draft() with { EmpresaId = 202 }).Code);
        Assert.Equal("EXISTING_INTENT_TENANT_MISMATCH",
            Rules.CheckEnqueue(Trusted(), Draft(), Stored() with { EmpresaId = 202 }).Code);
    }

    [Fact]
    public void UnknownTypeAndForgedEventIdFailBeforeAnyEffect()
    {
        Assert.Equal("EVENT_ID_OR_TYPE_DENIED",
            Rules.CheckEnqueue(Trusted(), Draft() with { EventoId = Guid.Empty }).Code);
        Assert.Equal("EVENT_ID_OR_TYPE_DENIED",
            Rules.CheckEnqueue(Trusted(), Draft() with { EventType = "secret.admin" }).Code);
        Assert.Equal("EVENT_ID_OR_TYPE_DENIED",
            Rules.CheckEnqueue(Trusted(), Draft() with { EventType = "../evil" }).Code);
    }

    [Fact]
    public void InvalidKeyAndTamperedPayloadHashAreRejected()
    {
        Assert.Equal("IDEMPOTENCY_OR_PAYLOAD_DIGEST_INVALID",
            Rules.CheckEnqueue(Trusted(), Draft() with { IdempotencyKey = "short" }).Code);
        Assert.Equal("IDEMPOTENCY_OR_PAYLOAD_DIGEST_INVALID",
            Rules.CheckEnqueue(Trusted(), Draft() with { PayloadSha256 = "not-sha256" }).Code);
        Assert.Equal("IDEMPOTENCY_OR_PAYLOAD_DIGEST_INVALID",
            Rules.CheckEnqueue(Trusted(), Draft() with { PayloadSha256 = new string('z', 64) }).Code);
    }

    [Fact]
    public void SchemaFutureClockAndNonUtcTimestampsAreDenied()
    {
        Assert.Equal("SCHEMA_OR_TRUSTED_UTC_INVALID",
            Rules.CheckEnqueue(Trusted(), Draft() with { SchemaVersion = 42 }).Code);
        Assert.Equal("SCHEMA_OR_TRUSTED_UTC_INVALID",
            Rules.CheckEnqueue(Trusted(), Draft() with { OccurredAtUtc = Now.AddMinutes(1) }).Code);
        Assert.Equal("SCHEMA_OR_TRUSTED_UTC_INVALID",
            Rules.CheckEnqueue(Trusted(), Draft() with {
                OccurredAtUtc = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified)
            }).Code);
    }

    [Fact]
    public void RetryBudgetAndControlCharacterCorrelationAreDenied()
    {
        Assert.Equal("CORRELATION_OR_RETRY_LIMIT_INVALID",
            Rules.CheckEnqueue(Trusted(), Draft() with { Attempt = 3 }).Code);
        Assert.Equal("CORRELATION_OR_RETRY_LIMIT_INVALID",
            Rules.CheckEnqueue(Trusted(), Draft() with { MaxAttempts = 0 }).Code);
        Assert.Equal("CORRELATION_OR_RETRY_LIMIT_INVALID",
            Rules.CheckEnqueue(Trusted(), Draft() with { CorrelationId = "a\nb" }).Code);
    }

    [Fact]
    public void CorrelationIsOnlyTraceMetadataNotPermission()
    {
        var denied = Rules.CheckEnqueue(
            Trusted() with { ExplicitEventGrant = false },
            Draft() with { CorrelationId = "tenant-202-admin" });
        Assert.Equal("MEMBERSHIP_OR_EVENT_GRANT_DENIED", denied.Code);
    }

    [Fact]
    public void ExactReplayNeverAuthorizesSecondDispatch()
    {
        var decision = Rules.CheckEnqueue(Trusted(), Draft(), Stored());
        Assert.Equal("EXACT_REPLAY_NO_NEW_DISPATCH", decision.Code);
        Assert.False(decision.AuthorizesDispatch);
    }

    [Fact]
    public void ReusedKeyWithDifferentPayloadOrIdentityIsConflict()
    {
        Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT_409",
            Rules.CheckEnqueue(Trusted(), Draft(), Stored() with { PayloadSha256 = new string('b', 64) }).Code);
        Assert.Equal("IDEMPOTENCY_PAYLOAD_CONFLICT_409",
            Rules.CheckEnqueue(Trusted(), Draft(), Stored() with { EventoId = Guid.NewGuid() }).Code);
    }

    [Fact]
    public void ExistingWrongKeyAndDeadLetterAreDenied()
    {
        Assert.Equal("EXISTING_IDEMPOTENCY_KEY_MISMATCH",
            Rules.CheckEnqueue(Trusted(), Draft(), Stored() with { IdempotencyKey = "other-opaque-key-987654321" }).Code);
        Assert.Equal("DEAD_LETTER_TERMINAL",
            Rules.CheckEnqueue(Trusted(), Draft(), Stored() with { DeadLetter = true }).Code);
    }

    [Fact]
    public void StaleAttemptAndProviderTimeoutNeverBecomeAck()
    {
        Assert.Equal("STALE_ACK_ATTEMPT",
            Rules.CheckDurableAck(Trusted(), Stored(), 101, EventId, 1, true).Code);
        Assert.Equal("DURABLE_ACK_REQUIRED",
            Rules.CheckDurableAck(Trusted(), Stored(), 101, EventId, 2, false).Code);
    }

    [Fact]
    public void AckPreflightCannotPersistOrCertifyProvider()
    {
        var decision = Rules.CheckDurableAck(Trusted(), Stored(), 101, EventId, 2, true);
        Assert.Equal("ACK_PRECONDITIONS_ONLY", decision.Code);
        Assert.False(decision.AuthorizesPersistence);
        Assert.False(decision.AuthorizesDispatch);
        Assert.False(decision.CertifiesJwtOrMySqlIntegration);
    }

    [Fact]
    public void TenantForgedAckAndTerminalAckAreDenied()
    {
        Assert.Equal("ACK_TENANT_OR_EVENT_MISMATCH",
            Rules.CheckDurableAck(Trusted(), Stored(), 202, EventId, 2, true).Code);
        Assert.Equal("ACK_TERMINAL_STATE",
            Rules.CheckDurableAck(Trusted(), Stored() with { DeadLetter = true }, 101, EventId, 2, true).Code);
        Assert.Equal("ACK_TERMINAL_STATE",
            Rules.CheckDurableAck(Trusted(), Stored() with { Delivered = true }, 101, EventId, 2, true).Code);
    }
}
