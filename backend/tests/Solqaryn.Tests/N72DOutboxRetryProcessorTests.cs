using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N72DOutboxRetryProcessorTests
{
    private static readonly DateTime AhoraUtc = new(2026, 9, 13, 10, 45, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Procesa_Stale_Antes_De_Claim_Y_Confirma_Entrega()
    {
        var mensaje = CrearClaimed();
        var repo = new FakeRepository(mensaje) { StaleResult = 1 };
        var processor = new OutboxRetryProcessor(
            repo,
            new OutboxRetryProcessorOptions(StaleClaimAfter: TimeSpan.FromMinutes(7)),
            () => 0.5);

        var result = await processor.ProcesarLoteAsync(7, AhoraUtc, new SuccessDispatcher());

        Assert.Equal(new[] { "recover", "claim", "delivered" }, repo.Calls);
        Assert.Equal(1, result.StaleRecuperados);
        Assert.Equal(1, result.Reclamados);
        Assert.Equal(1, result.Entregados);
        Assert.Equal(AhoraUtc.AddMinutes(-7), repo.StaleBefore);
    }

    [Fact]
    public async Task Fallo_No_Persiste_Mensaje_De_Excepcion_Y_Reprograma_Con_Backoff()
    {
        var mensaje = CrearClaimed();
        var repo = new FakeRepository(mensaje);
        var processor = new OutboxRetryProcessor(
            repo,
            new OutboxRetryProcessorOptions(JitterRatio: 0),
            () => 0.5);

        var result = await processor.ProcesarLoteAsync(
            7,
            AhoraUtc,
            new ThrowingDispatcher("provider-secret-payload"));

        Assert.Equal("DELIVERY_FAILED", repo.LastSafeError);
        Assert.DoesNotContain("provider-secret", repo.LastSafeError, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(AhoraUtc.AddSeconds(5), repo.LastAvailableAt);
        Assert.False(repo.LastDeadLetter);
        Assert.Equal(1, result.Reprogramados);
    }

    [Fact]
    public async Task MaximoIntentos_Confirma_DeadLetter()
    {
        var mensaje = CrearClaimed();
        var repo = new FakeRepository(mensaje);
        var processor = new OutboxRetryProcessor(
            repo,
            new OutboxRetryProcessorOptions(MaxAttempts: 1),
            () => 0.5);

        var result = await processor.ProcesarLoteAsync(7, AhoraUtc, new ThrowingDispatcher("boom"));

        Assert.True(repo.LastDeadLetter);
        Assert.Equal(AhoraUtc, repo.LastAvailableAt);
        Assert.Equal(1, result.DeadLetter);
        Assert.Equal(0, result.Reprogramados);
    }

    [Fact]
    public async Task Claim_Supersedido_No_Se_Contabiliza_Como_Entrega()
    {
        var mensaje = CrearClaimed();
        var repo = new FakeRepository(mensaje) { ConfirmOutcome = false };
        var processor = new OutboxRetryProcessor(repo, new OutboxRetryProcessorOptions(), () => 0.5);

        var result = await processor.ProcesarLoteAsync(7, AhoraUtc, new SuccessDispatcher());

        Assert.Equal(0, result.Entregados);
        Assert.Equal(1, result.Supersedidos);
    }

    [Fact]
    public void Configuracion_Invalida_Falla_Cerrado()
    {
        var repo = new FakeRepository();

        Assert.Throws<InvalidOperationException>(() =>
            new OutboxRetryProcessor(repo, new OutboxRetryProcessorOptions(BatchSize: 0)));
        Assert.Throws<InvalidOperationException>(() =>
            new OutboxRetryProcessor(repo, new OutboxRetryProcessorOptions(MaxAttempts: 0)));
        Assert.Throws<InvalidOperationException>(() =>
            new OutboxRetryProcessor(repo, new OutboxRetryProcessorOptions(StaleClaimAfter: TimeSpan.Zero)));
        Assert.Throws<InvalidOperationException>(() =>
            new OutboxRetryProcessor(repo, new OutboxRetryProcessorOptions(JitterRatio: 1.1)));
    }

    [Fact]
    public async Task Cancelacion_No_Se_Convierte_En_Fallo()
    {
        var mensaje = CrearClaimed();
        var repo = new FakeRepository(mensaje);
        var processor = new OutboxRetryProcessor(repo, new OutboxRetryProcessorOptions(), () => 0.5);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            processor.ProcesarLoteAsync(7, AhoraUtc, new SuccessDispatcher(), cts.Token));

        Assert.Null(repo.LastSafeError);
    }

    private static MensajeOutbox CrearClaimed()
    {
        var mensaje = MensajeOutbox.Crear(
            7,
            "inventario.cambiado",
            "{\"safe\":true}",
            "idem-1",
            creadoEnUtc: AhoraUtc.AddMinutes(-1));
        mensaje.MarcarProcesando(AhoraUtc);
        return mensaje;
    }

    private sealed class SuccessDispatcher : IOutboxEffectDispatcher
    {
        public Task DispatchAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ThrowingDispatcher : IOutboxEffectDispatcher
    {
        private readonly string _message;
        public ThrowingDispatcher(string message) => _message = message;
        public Task DispatchAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) =>
            Task.FromException(new InvalidOperationException(_message));
    }

    private sealed class FakeRepository : IMensajeOutboxRepository
    {
        private readonly IReadOnlyList<MensajeOutbox> _claimed;

        public FakeRepository(params MensajeOutbox[] claimed) => _claimed = claimed;

        public List<string> Calls { get; } = new();
        public int StaleResult { get; init; }
        public DateTime? StaleBefore { get; private set; }
        public bool ConfirmOutcome { get; init; } = true;
        public string? LastSafeError { get; private set; }
        public DateTime? LastAvailableAt { get; private set; }
        public bool LastDeadLetter { get; private set; }

        public Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<MensajeOutbox?> GetByEventoIdAsync(int empresaId, Guid eventoId, CancellationToken cancellationToken = default) => Task.FromResult<MensajeOutbox?>(null);
        public Task<bool> ExisteClaveIdempotenciaAsync(int empresaId, string claveIdempotencia, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(int empresaId, DateTime ahoraUtc, int maximoMensajes, CancellationToken cancellationToken = default)
        {
            Calls.Add("claim");
            return Task.FromResult(_claimed);
        }

        public Task<int> RecuperarProcesandoStaleAsync(int empresaId, DateTime staleAntesUtc, DateTime ahoraUtc, int maximoMensajes, CancellationToken cancellationToken = default)
        {
            Calls.Add("recover");
            StaleBefore = staleAntesUtc;
            return Task.FromResult(StaleResult);
        }

        public Task<bool> MarcarEntregadoAsync(int empresaId, int mensajeId, int intentoEsperado, DateTime ahoraUtc, CancellationToken cancellationToken = default)
        {
            Calls.Add("delivered");
            return Task.FromResult(ConfirmOutcome);
        }

        public Task<bool> RegistrarFalloAsync(int empresaId, int mensajeId, int intentoEsperado, string errorSeguro, DateTime ahoraUtc, DateTime disponibleDesdeUtc, bool deadLetter, CancellationToken cancellationToken = default)
        {
            Calls.Add("failed");
            LastSafeError = errorSeguro;
            LastAvailableAt = disponibleDesdeUtc;
            LastDeadLetter = deadLetter;
            return Task.FromResult(ConfirmOutcome);
        }
    }
}
