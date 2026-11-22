using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Adversarial repository-contract tests for C11-03.M06.06.
/// The existing MySQL 8.4 tests cover genuine persistence; these tests inject
/// a defective claim adapter to verify pre-delivery fail-closed behavior.
/// </summary>
public sealed class C1103M0606ClaimBatchPreflightTests
{
    [Fact]
    public async Task ForeignTenantInSecondClaimNeverDeliversFirstTenant()
    {
        var now = new DateTime(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);
        var own = MensajeOutbox.Crear(17, "outbox.event", "{\"tenant\":\"A\"}",
            "c1103-preflight-A", creadoEnUtc: now.AddMinutes(-2));
        var foreign = MensajeOutbox.Crear(18, "outbox.event", "{\"tenant\":\"B\"}",
            "c1103-preflight-B", creadoEnUtc: now.AddMinutes(-2));
        own.MarcarProcesando(now);
        foreign.MarcarProcesando(now);

        var repository = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        repository.Setup(x => x.ClaimDisponiblesAsync(
                17, now, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { own, foreign });
        var transport = new Mock<IC1103OutboxIsolatedTransport>(MockBehavior.Strict);
        var worker = new C1103OutboxIsolatedRelayWorker(
            repository.Object, transport.Object);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            worker.RunTenantAsync(17, now, 2, 3, TimeSpan.FromMinutes(1)));
        Assert.Equal("OUTBOX_CROSS_TENANT_OR_INVALID_CLAIM", error.Message);
        transport.VerifyNoOtherCalls();
        repository.Verify(x => x.MarcarEntregadoAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.RegistrarFalloAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OversizedRepositoryClaimFailsBeforeAnyDelivery()
    {
        var now = new DateTime(2026, 11, 22, 0, 0, 0, DateTimeKind.Utc);
        var first = MensajeOutbox.Crear(17, "outbox.event", "{}",
            "c1103-batch-1", creadoEnUtc: now.AddMinutes(-2));
        var second = MensajeOutbox.Crear(17, "outbox.event", "{}",
            "c1103-batch-2", creadoEnUtc: now.AddMinutes(-2));
        first.MarcarProcesando(now);
        second.MarcarProcesando(now);

        var repository = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        repository.Setup(x => x.ClaimDisponiblesAsync(
                17, now, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { first, second });
        var transport = new Mock<IC1103OutboxIsolatedTransport>(MockBehavior.Strict);
        var worker = new C1103OutboxIsolatedRelayWorker(
            repository.Object, transport.Object);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            worker.RunTenantAsync(17, now, 1, 3, TimeSpan.FromMinutes(1)));
        Assert.Equal("OUTBOX_CLAIM_INVALID_BATCH", error.Message);
        transport.VerifyNoOtherCalls();
    }
}
