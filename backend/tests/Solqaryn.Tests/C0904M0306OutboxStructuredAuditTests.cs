using Microsoft.Extensions.Logging;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

// C09-04.M03.06: auditoría estructurada segura, sin proveedor ni JWT/DB real.
public sealed class C0904M0306OutboxStructuredAuditTests
{
    private static readonly DateTime AhoraUtc = new(2026, 11, 17, 11, 15, 0, DateTimeKind.Utc);
    private const string Secreto = "SECRETO_NO_REGISTRAR_123";

    [Fact]
    public async Task TenantDenegado_EmiteCodigoOpacoSinClaimNiDespacho()
    {
        var repo = new Repositorio(Crear(7));
        var gate = new Gate { TenantPermitido = false };
        var envio = new Despachador();
        var log = new CapturaLogger();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CrearWorker(repo, gate, envio, log).ProcesarAsync(7, AhoraUtc));
        Assert.Equal(0, repo.Claims);
        Assert.Equal(0, repo.Stale);
        Assert.Equal(0, envio.Envios);
        Assert.Contains(log.Entradas, x => x.Nivel == LogLevel.Warning &&
            x.Texto.Contains("OUTBOX_TENANT_DENIED", StringComparison.Ordinal) &&
            x.Texto.Contains("EmpresaId=7", StringComparison.Ordinal));
        SinFiltraciones(log);
    }

    [Fact]
    public async Task Permitido_EmiteResumenDeConteosSinPayloadNiIdempotencia()
    {
        var repo = new Repositorio(Crear(7));
        var envio = new Despachador();
        var log = new CapturaLogger();
        var resultado = await CrearWorker(repo, new Gate(), envio, log).ProcesarAsync(7, AhoraUtc);
        Assert.Equal(1, resultado.Entregados);
        Assert.Equal(1, repo.Confirmaciones);
        Assert.Equal(1, envio.Envios);
        Assert.Contains(log.Entradas, x => x.Nivel == LogLevel.Information &&
            x.Texto.Contains("OUTBOX_BATCH_SUMMARY", StringComparison.Ordinal) &&
            x.Texto.Contains("Reclamados=1", StringComparison.Ordinal) &&
            x.Texto.Contains("Entregados=1", StringComparison.Ordinal));
        SinFiltraciones(log);
    }

    [Fact]
    public async Task RevocacionDeMensaje_NoDespachaYRegistraDenegacionOpaca()
    {
        var repo = new Repositorio(Crear(7));
        var envio = new Despachador();
        var log = new CapturaLogger();
        var resultado = await CrearWorker(repo, new Gate { MensajePermitido = false }, envio, log)
            .ProcesarAsync(7, AhoraUtc);
        Assert.Equal(1, resultado.Reprogramados);
        Assert.Equal(0, envio.Envios);
        Assert.Equal(0, repo.Confirmaciones);
        Assert.Equal("DELIVERY_FAILED", repo.ErrorSeguro);
        Assert.Contains(log.Entradas, x => x.Nivel == LogLevel.Warning &&
            x.Texto.Contains("OUTBOX_MESSAGE_DENIED", StringComparison.Ordinal));
        SinFiltraciones(log);
    }

    [Fact]
    public async Task LoteContaminadoAB_NoDespachaYRegistraErrorSinExcepcion()
    {
        var repo = new Repositorio(Crear(7), Crear(8));
        var envio = new Despachador();
        var log = new CapturaLogger();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CrearWorker(repo, new Gate(), envio, log).ProcesarAsync(7, AhoraUtc));
        Assert.Equal(0, envio.Envios);
        Assert.Contains(log.Entradas, x => x.Nivel == LogLevel.Error &&
            x.Texto.Contains("OUTBOX_PROCESS_FAILED", StringComparison.Ordinal));
        SinFiltraciones(log);
    }

    [Fact]
    public async Task ProveedorConErrorSensible_SeReprogramaSinFiltrarExcepcion()
    {
        var repo = new Repositorio(Crear(7));
        var envio = new Despachador { Fallar = true };
        var log = new CapturaLogger();
        var resultado = await CrearWorker(repo, new Gate(), envio, log).ProcesarAsync(7, AhoraUtc);
        Assert.Equal(1, resultado.Reprogramados);
        Assert.Equal("DELIVERY_FAILED", repo.ErrorSeguro);
        Assert.Contains(log.Entradas, x => x.Nivel == LogLevel.Information &&
            x.Texto.Contains("Reprogramados=1", StringComparison.Ordinal));
        SinFiltraciones(log);
    }

    private static void SinFiltraciones(CapturaLogger logger)
    {
        var texto = string.Join("|", logger.Entradas.Select(x => x.Texto));
        Assert.DoesNotContain(Secreto, texto, StringComparison.Ordinal);
        Assert.DoesNotContain("provider-password", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("PayloadJson", texto, StringComparison.Ordinal);
        Assert.DoesNotContain("ClaveIdempotencia", texto, StringComparison.Ordinal);
    }

    private static OutboxTenantDeliveryWorker CrearWorker(
        Repositorio repo, Gate gate, Despachador envio, CapturaLogger log) =>
        new(new OutboxRetryProcessor(repo, new OutboxRetryProcessorOptions(JitterRatio: 0), () => 0.5),
            gate, envio, log);

    private static MensajeOutbox Crear(int empresaId)
    {
        var mensaje = MensajeOutbox.Crear(empresaId, "correo.notificacion",
            "{\"secreto\":\"" + Secreto + "\"}", "clave-" + Secreto,
            correlationId: "corr-" + Secreto, creadoEnUtc: AhoraUtc.AddMinutes(-1));
        mensaje.MarcarProcesando(AhoraUtc);
        return mensaje;
    }

    private sealed class CapturaLogger : ILogger<OutboxTenantDeliveryWorker>
    {
        public List<(LogLevel Nivel, string Texto)> Entradas { get; } = new();
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => Alcance.Instance;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entradas.Add((logLevel, formatter(state, exception)));
        private sealed class Alcance : IDisposable
        {
            public static readonly Alcance Instance = new();
            public void Dispose() { }
        }
    }

    private sealed class Gate : IOutboxTenantDeliveryGate
    {
        public bool TenantPermitido { get; init; } = true;
        public bool MensajePermitido { get; init; } = true;
        public Task<bool> AutorizarTenantAsync(int empresaId, CancellationToken ct = default) =>
            Task.FromResult(TenantPermitido);
        public Task<bool> AutorizarMensajeAsync(int empresaId, Guid eventoId, string tipoEvento,
            CancellationToken ct = default) => Task.FromResult(MensajePermitido);
    }

    private sealed class Despachador : IOutboxEffectDispatcher
    {
        public int Envios { get; private set; }
        public bool Fallar { get; init; }
        public Task DispatchAsync(MensajeOutbox mensaje, CancellationToken ct = default)
        {
            Envios++;
            return Fallar
                ? Task.FromException(new InvalidOperationException("provider-password=" + Secreto))
                : Task.CompletedTask;
        }
    }

    private sealed class Repositorio : IMensajeOutboxRepository
    {
        private readonly IReadOnlyList<MensajeOutbox> _lote;
        public Repositorio(params MensajeOutbox[] lote) => _lote = lote;
        public int Claims { get; private set; }
        public int Stale { get; private set; }
        public int Confirmaciones { get; private set; }
        public string? ErrorSeguro { get; private set; }
        public Task AddAsync(MensajeOutbox mensaje, CancellationToken ct = default) => Task.CompletedTask;
        public Task<MensajeOutbox?> GetByEventoIdAsync(int empresaId, Guid eventoId,
            CancellationToken ct = default) => Task.FromResult<MensajeOutbox?>(null);
        public Task<bool> ExisteClaveIdempotenciaAsync(int empresaId, string clave,
            CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> RecuperarProcesandoStaleAsync(int empresaId, DateTime stale, DateTime ahora,
            int maximo, CancellationToken ct = default) { Stale++; return Task.FromResult(0); }
        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(int empresaId, DateTime ahora,
            int maximo, CancellationToken ct = default) { Claims++; return Task.FromResult(_lote); }
        public Task<bool> MarcarEntregadoAsync(int empresaId, int mensajeId, int intento,
            DateTime ahora, CancellationToken ct = default)
        { Confirmaciones++; return Task.FromResult(true); }
        public Task<bool> RegistrarFalloAsync(int empresaId, int mensajeId, int intento,
            string errorSeguro, DateTime ahora, DateTime disponible, bool dead,
            CancellationToken ct = default)
        { ErrorSeguro = errorSeguro; return Task.FromResult(true); }
    }
}
