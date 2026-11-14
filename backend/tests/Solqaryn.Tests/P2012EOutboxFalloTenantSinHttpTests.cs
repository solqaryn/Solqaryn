using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-012.E: en un worker sin HttpContext, el error de proveedor al despachar
/// un efecto de A no reprograma ni confirma el mensaje de B, aun con la misma
/// clave idempotente. Prueba sintética, no HTTP ni MySQL.
/// </summary>
public sealed class P2012EOutboxFalloTenantSinHttpTests
{
    private static readonly DateTime AhoraUtc =
        new(2026, 11, 7, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FalloA_NoReprogramaB_YEntregaBConClaveCompartida()
    {
        const int empresaA = 101;
        const int empresaB = 202;
        const string mismaClave = "p2012e-clave-idempotente";

        var a = MensajeOutbox.Crear(empresaA, "pedido.confirmado",
            "{\"origen\":\"A\"}", mismaClave, creadoEnUtc: AhoraUtc.AddMinutes(-1));
        var b = MensajeOutbox.Crear(empresaB, "pedido.confirmado",
            "{\"origen\":\"B\"}", mismaClave, creadoEnUtc: AhoraUtc.AddMinutes(-1));
        a.MarcarProcesando(AhoraUtc);
        b.MarcarProcesando(AhoraUtc);

        var repo = new RepositorioPorEmpresa(a, b);
        var dispatcher = new DespachadorConFalloA(empresaA);
        var procesador = new OutboxRetryProcessor(
            repo, new OutboxRetryProcessorOptions(JitterRatio: 0), () => 0.5);

        var resultadoA = await procesador.ProcesarLoteAsync(empresaA, AhoraUtc, dispatcher);
        Assert.Equal(0, resultadoA.Entregados);
        Assert.Equal(1, resultadoA.Reprogramados);
        Assert.Equal(0, resultadoA.DeadLetter);
        Assert.Equal(new[] { empresaA }, repo.Fallos);
        Assert.Empty(repo.Confirmados);
        Assert.Equal("DELIVERY_FAILED", repo.ErrorSeguro);
        Assert.DoesNotContain("credencial-del-proveedor", repo.ErrorSeguro!,
            StringComparison.OrdinalIgnoreCase);

        var resultadoB = await procesador.ProcesarLoteAsync(empresaB, AhoraUtc, dispatcher);
        Assert.Equal(1, resultadoB.Entregados);
        Assert.Equal(0, resultadoB.Reprogramados);
        Assert.Equal(new[] { empresaA, empresaB }, repo.Reclamadas);
        Assert.Equal(new[] { empresaA, empresaB }, dispatcher.EmpresasIntentadas);
        Assert.Equal(new[] { empresaA }, repo.Fallos);
        Assert.Equal(new[] { empresaB }, repo.Confirmados);
        Assert.Equal(mismaClave, a.ClaveIdempotencia);
        Assert.Equal(mismaClave, b.ClaveIdempotencia);
    }

    private sealed class DespachadorConFalloA : IOutboxEffectDispatcher
    {
        private readonly int _empresaConFallo;
        public List<int> EmpresasIntentadas { get; } = new();

        public DespachadorConFalloA(int empresaConFallo) =>
            _empresaConFallo = empresaConFallo;

        public Task DispatchAsync(MensajeOutbox mensaje,
            CancellationToken cancellationToken = default)
        {
            EmpresasIntentadas.Add(mensaje.EmpresaId);
            return mensaje.EmpresaId == _empresaConFallo
                ? Task.FromException(new InvalidOperationException("credencial-del-proveedor"))
                : Task.CompletedTask;
        }
    }

    private sealed class RepositorioPorEmpresa : IMensajeOutboxRepository
    {
        private readonly IReadOnlyList<MensajeOutbox> _mensajes;
        public RepositorioPorEmpresa(params MensajeOutbox[] mensajes) => _mensajes = mensajes;
        public List<int> Reclamadas { get; } = new();
        public List<int> Confirmados { get; } = new();
        public List<int> Fallos { get; } = new();
        public string? ErrorSeguro { get; private set; }

        public Task AddAsync(MensajeOutbox mensaje,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<MensajeOutbox?> GetByEventoIdAsync(int empresaId, Guid eventoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MensajeOutbox?>(null);

        public Task<bool> ExisteClaveIdempotenciaAsync(int empresaId, string clave,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<int> RecuperarProcesandoStaleAsync(int empresaId,
            DateTime staleAntesUtc, DateTime ahoraUtc, int maximoMensajes,
            CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(int empresaId,
            DateTime ahoraUtc, int maximoMensajes,
            CancellationToken cancellationToken = default)
        {
            Reclamadas.Add(empresaId);
            IReadOnlyList<MensajeOutbox> lote = _mensajes
                .Where(m => m.EmpresaId == empresaId)
                .Take(maximoMensajes)
                .ToArray();
            return Task.FromResult(lote);
        }

        public Task<bool> MarcarEntregadoAsync(int empresaId, int mensajeId,
            int intentoEsperado, DateTime ahoraUtc,
            CancellationToken cancellationToken = default)
        {
            Confirmados.Add(empresaId);
            return Task.FromResult(true);
        }

        public Task<bool> RegistrarFalloAsync(int empresaId, int mensajeId,
            int intentoEsperado, string errorSeguro, DateTime ahoraUtc,
            DateTime disponibleDesdeUtc, bool deadLetter,
            CancellationToken cancellationToken = default)
        {
            Fallos.Add(empresaId);
            ErrorSeguro = errorSeguro;
            return Task.FromResult(true);
        }
    }
}
