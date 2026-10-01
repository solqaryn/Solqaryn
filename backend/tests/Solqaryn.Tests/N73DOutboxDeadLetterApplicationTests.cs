using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N73DOutboxDeadLetterApplicationTests
{
    private static readonly DateTime AhoraUtc = new(2026, 9, 13, 17, 25, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Agotar_MaximoIntentos_Confirma_DeadLetter_TenantBound()
    {
        var mensaje = CrearClaimed(empresaId: 7);
        var repository = new DeadLetterRepository(mensaje);
        var processor = new OutboxRetryProcessor(
            repository,
            new OutboxRetryProcessorOptions(MaxAttempts: 1, JitterRatio: 0),
            () => 0.5);

        var result = await processor.ProcesarLoteAsync(
            7,
            AhoraUtc,
            new ThrowingDispatcher(),
            CancellationToken.None);

        Assert.Equal(1, result.DeadLetter);
        Assert.Equal(0, result.Reprogramados);
        Assert.Equal(0, result.Supersedidos);
        Assert.Equal(7, repository.EmpresaConfirmada);
        Assert.Equal(mensaje.Id, repository.MensajeConfirmado);
        Assert.Equal(mensaje.Intentos, repository.IntentoConfirmado);
        Assert.Equal("DELIVERY_FAILED", repository.ErrorSeguro);
        Assert.Equal(AhoraUtc, repository.DisponibleDesdeUtc);
        Assert.True(repository.DeadLetterSolicitado);
    }

    [Fact]
    public async Task Claim_Supersedido_No_Se_Contabiliza_Como_DeadLetter_Confirmado()
    {
        var mensaje = CrearClaimed(empresaId: 7);
        var repository = new DeadLetterRepository(mensaje)
        {
            ConfirmarFallo = false
        };
        var processor = new OutboxRetryProcessor(
            repository,
            new OutboxRetryProcessorOptions(MaxAttempts: 1, JitterRatio: 0),
            () => 0.5);

        var result = await processor.ProcesarLoteAsync(
            7,
            AhoraUtc,
            new ThrowingDispatcher(),
            CancellationToken.None);

        Assert.Equal(0, result.DeadLetter);
        Assert.Equal(1, result.Supersedidos);
        Assert.True(repository.DeadLetterSolicitado);
    }

    private static MensajeOutbox CrearClaimed(int empresaId)
    {
        var mensaje = MensajeOutbox.Crear(
            empresaId,
            "inventory.dead-letter.certificate",
            "{\"safe\":true}",
            "n73d-idem-1",
            creadoEnUtc: AhoraUtc.AddMinutes(-1));
        mensaje.MarcarProcesando(AhoraUtc);
        return mensaje;
    }

    private sealed class ThrowingDispatcher : IOutboxEffectDispatcher
    {
        public Task DispatchAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException("provider-detail-must-not-be-persisted"));
    }

    private sealed class DeadLetterRepository : IMensajeOutboxRepository
    {
        private readonly IReadOnlyList<MensajeOutbox> _claimed;

        public DeadLetterRepository(params MensajeOutbox[] claimed)
        {
            _claimed = claimed;
        }

        public bool ConfirmarFallo { get; init; } = true;
        public int? EmpresaConfirmada { get; private set; }
        public int? MensajeConfirmado { get; private set; }
        public int? IntentoConfirmado { get; private set; }
        public string? ErrorSeguro { get; private set; }
        public DateTime? DisponibleDesdeUtc { get; private set; }
        public bool DeadLetterSolicitado { get; private set; }

        public Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<MensajeOutbox?> GetByEventoIdAsync(
            int empresaId,
            Guid eventoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MensajeOutbox?>(null);

        public Task<bool> ExisteClaveIdempotenciaAsync(
            int empresaId,
            string claveIdempotencia,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(
            int empresaId,
            DateTime ahoraUtc,
            int maximoMensajes,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_claimed);

        public Task<int> RecuperarProcesandoStaleAsync(
            int empresaId,
            DateTime staleAntesUtc,
            DateTime ahoraUtc,
            int maximoMensajes,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<bool> MarcarEntregadoAsync(
            int empresaId,
            int mensajeId,
            int intentoEsperado,
            DateTime ahoraUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> RegistrarFalloAsync(
            int empresaId,
            int mensajeId,
            int intentoEsperado,
            string errorSeguro,
            DateTime ahoraUtc,
            DateTime disponibleDesdeUtc,
            bool deadLetter,
            CancellationToken cancellationToken = default)
        {
            EmpresaConfirmada = empresaId;
            MensajeConfirmado = mensajeId;
            IntentoConfirmado = intentoEsperado;
            ErrorSeguro = errorSeguro;
            DisponibleDesdeUtc = disponibleDesdeUtc;
            DeadLetterSolicitado = deadLetter;
            return Task.FromResult(ConfirmarFallo);
        }
    }
}
