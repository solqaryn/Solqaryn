using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-012.C: el worker Outbox no dispone de HttpContext y debe
/// delimitar cada despacho por EmpresaId durable, no por cabeceras HTTP.
/// Prueba sintética sin proveedor externo ni MySQL.
/// </summary>
public sealed class P2012COutboxSinHttpTenantTests
{
    private static readonly DateTime AhoraUtc =
        new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task WorkerSinHttp_DespachaPorTenantAunqueLaClaveIdempotenteSeaIgual()
    {
        const int empresaA = 101;
        const int empresaB = 202;
        const string claveCompartida = "p2012c-misma-clave";

        var a = MensajeOutbox.Crear(empresaA, "venta.notificada", "{\"origen\":\"A\"}",
            claveCompartida, creadoEnUtc: AhoraUtc.AddMinutes(-1));
        var b = MensajeOutbox.Crear(empresaB, "venta.notificada", "{\"origen\":\"B\"}",
            claveCompartida, creadoEnUtc: AhoraUtc.AddMinutes(-1));
        a.MarcarProcesando(AhoraUtc);
        b.MarcarProcesando(AhoraUtc);

        var repositorio = new RepositorioSinHttp(a, b);
        var despachador = new DespachadorCaptura();
        var procesador = new OutboxRetryProcessor(
            repositorio, new OutboxRetryProcessorOptions(JitterRatio: 0), () => 0.5);

        var resultadoA = await procesador.ProcesarLoteAsync(empresaA, AhoraUtc, despachador);
        Assert.Equal(1, resultadoA.Entregados);
        Assert.Equal(new[] { empresaA }, despachador.Empresas);
        Assert.Equal(new[] { empresaA }, repositorio.EmpresasConfirmadas);

        var resultadoB = await procesador.ProcesarLoteAsync(empresaB, AhoraUtc, despachador);
        Assert.Equal(1, resultadoB.Entregados);
        Assert.Equal(new[] { empresaA, empresaB }, despachador.Empresas);
        Assert.Equal(new[] { empresaA, empresaB }, repositorio.EmpresasReclamadas);
        Assert.Equal(new[] { empresaA, empresaB }, repositorio.EmpresasConfirmadas);
        Assert.Equal(claveCompartida, a.ClaveIdempotencia);
        Assert.Equal(claveCompartida, b.ClaveIdempotencia);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task WorkerSinTenantValido_NoConsultaRepositorioNiDespacha(int empresaInvalida)
    {
        var repositorio = new RepositorioSinHttp();
        var despachador = new DespachadorCaptura();
        var procesador = new OutboxRetryProcessor(repositorio, new OutboxRetryProcessorOptions());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            procesador.ProcesarLoteAsync(empresaInvalida, AhoraUtc, despachador));

        Assert.Empty(repositorio.EmpresasReclamadas);
        Assert.Empty(despachador.Empresas);
    }

    private sealed class DespachadorCaptura : IOutboxEffectDispatcher
    {
        public List<int> Empresas { get; } = new();

        public Task DispatchAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default)
        {
            Empresas.Add(mensaje.EmpresaId);
            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioSinHttp : IMensajeOutboxRepository
    {
        private readonly IReadOnlyList<MensajeOutbox> _mensajes;

        public RepositorioSinHttp(params MensajeOutbox[] mensajes) => _mensajes = mensajes;

        public List<int> EmpresasReclamadas { get; } = new();
        public List<int> EmpresasConfirmadas { get; } = new();

        public Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<MensajeOutbox?> GetByEventoIdAsync(
            int empresaId, Guid eventoId, CancellationToken cancellationToken = default) =>
            Task.FromResult<MensajeOutbox?>(null);

        public Task<bool> ExisteClaveIdempotenciaAsync(
            int empresaId, string claveIdempotencia, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> RecuperarProcesandoStaleAsync(
            int empresaId, DateTime staleAntesUtc, DateTime ahoraUtc,
            int maximoMensajes, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(
            int empresaId, DateTime ahoraUtc,
            int maximoMensajes, CancellationToken cancellationToken = default)
        {
            EmpresasReclamadas.Add(empresaId);
            IReadOnlyList<MensajeOutbox> lote = _mensajes
                .Where(m => m.EmpresaId == empresaId)
                .Take(maximoMensajes)
                .ToArray();
            return Task.FromResult(lote);
        }

        public Task<bool> MarcarEntregadoAsync(
            int empresaId, int mensajeId, int intentoEsperado,
            DateTime ahoraUtc, CancellationToken cancellationToken = default)
        {
            EmpresasConfirmadas.Add(empresaId);
            return Task.FromResult(true);
        }
    }
}
