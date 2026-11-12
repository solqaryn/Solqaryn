using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-012.A: el procesador Outbox no obtiene autoridad de HttpContext.
/// El llamador suministra EmpresaId durable y reloj UTC antes de consultar
/// repositorio o ejecutar efectos externos. Pruebas sintéticas, sin DB/red.
/// </summary>
public sealed class P2012AOutboxSinHttpArranqueTests
{
    private static readonly DateTime AhoraUtc =
        new(2026, 11, 12, 17, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public async Task ArranqueConRelojNoUtc_RechazaAntesDeClaimsYDispatch(DateTimeKind tipo)
    {
        var repo = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        var dispatcher = new Mock<IOutboxEffectDispatcher>(MockBehavior.Strict);
        var procesador = new OutboxRetryProcessor(
            repo.Object, new OutboxRetryProcessorOptions());
        var fecha = DateTime.SpecifyKind(AhoraUtc, tipo);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            procesador.ProcesarLoteAsync(101, fecha, dispatcher.Object));
        repo.VerifyNoOtherCalls();
        dispatcher.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(101)]
    [InlineData(202)]
    public async Task ArranqueSinHttp_PropagaTenantExplicitoAStaleYClaim(int empresaId)
    {
        var repo = new Mock<IMensajeOutboxRepository>(MockBehavior.Strict);
        var dispatcher = new Mock<IOutboxEffectDispatcher>(MockBehavior.Strict);
        repo.Setup(x => x.RecuperarProcesandoStaleAsync(
                empresaId, It.IsAny<DateTime>(), AhoraUtc,
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        repo.Setup(x => x.ClaimDisponiblesAsync(
                empresaId, AhoraUtc, It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MensajeOutbox>)Array.Empty<MensajeOutbox>());

        var procesador = new OutboxRetryProcessor(
            repo.Object, new OutboxRetryProcessorOptions(JitterRatio: 0));
        var resultado = await procesador.ProcesarLoteAsync(
            empresaId, AhoraUtc, dispatcher.Object);

        Assert.Equal(0, resultado.Reclamados);
        Assert.Equal(0, resultado.Entregados);
        repo.Verify(x => x.RecuperarProcesandoStaleAsync(
            empresaId, It.IsAny<DateTime>(), AhoraUtc,
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(x => x.ClaimDisponiblesAsync(
            empresaId, AhoraUtc, It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
        repo.VerifyNoOtherCalls();
        dispatcher.VerifyNoOtherCalls();
    }
}
