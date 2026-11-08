using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-012.D: incluso si el repositorio devuelve un lote corrupto con un
/// mensaje de otra empresa, el worker debe rechazar TODO antes de despachar.
/// Prueba sintética sin HTTP, red ni MySQL.
/// </summary>
public sealed class P2012DOutboxLoteCruzadoTests
{
    private static readonly DateTime AhoraUtc =
        new(2026, 11, 7, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoteAConMensajeDeB_RechazaTodoSinEntregasParciales(
        bool mensajeAjenoAlFinal)
    {
        const int empresaA = 101;
        const int empresaB = 202;

        var mensajeA = MensajeOutbox.Crear(
            empresaA, "venta.notificada", "{\"empresa\":\"A\"}",
            "p2012d-clave-compartida", creadoEnUtc: AhoraUtc.AddMinutes(-1));
        var mensajeB = MensajeOutbox.Crear(
            empresaB, "venta.notificada", "{\"empresa\":\"B\"}",
            "p2012d-clave-compartida", creadoEnUtc: AhoraUtc.AddMinutes(-1));

        mensajeA.MarcarProcesando(AhoraUtc);
        mensajeB.MarcarProcesando(AhoraUtc);

        var lote = mensajeAjenoAlFinal
            ? new[] { mensajeA, mensajeB }
            : new[] { mensajeB, mensajeA };
        var repositorio = new RepositorioLoteContaminado(lote);
        var despachador = new DespachadorCaptura();
        var procesador = new OutboxRetryProcessor(
            repositorio, new OutboxRetryProcessorOptions(JitterRatio: 0));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            procesador.ProcesarLoteAsync(empresaA, AhoraUtc, despachador));

        Assert.Equal(1, repositorio.RecuperacionesStale);
        Assert.Equal(new[] { empresaA }, repositorio.EmpresasReclamadas);
        Assert.Empty(despachador.Empresas);
        Assert.Empty(repositorio.EmpresasConfirmadas);
        Assert.Empty(repositorio.EmpresasReprogramadas);
        Assert.Equal(mensajeA.ClaveIdempotencia, mensajeB.ClaveIdempotencia);
    }

    private sealed class DespachadorCaptura : IOutboxEffectDispatcher
    {
        public List<int> Empresas { get; } = new();

        public Task DispatchAsync(
            MensajeOutbox mensaje,
            CancellationToken cancellationToken = default)
        {
            Empresas.Add(mensaje.EmpresaId);
            return Task.CompletedTask;
        }
    }

    private sealed class RepositorioLoteContaminado : IMensajeOutboxRepository
    {
        private readonly IReadOnlyList<MensajeOutbox> _lote;

        public RepositorioLoteContaminado(IReadOnlyList<MensajeOutbox> lote) =>
            _lote = lote;

        public int RecuperacionesStale { get; private set; }
        public List<int> EmpresasReclamadas { get; } = new();
        public List<int> EmpresasConfirmadas { get; } = new();
        public List<int> EmpresasReprogramadas { get; } = new();

        public Task AddAsync(
            MensajeOutbox mensaje,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<MensajeOutbox?> GetByEventoIdAsync(
            int empresaId, Guid eventoId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MensajeOutbox?>(null);

        public Task<bool> ExisteClaveIdempotenciaAsync(
            int empresaId, string claveIdempotencia,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> RecuperarProcesandoStaleAsync(
            int empresaId, DateTime staleAntesUtc, DateTime ahoraUtc,
            int maximoMensajes, CancellationToken cancellationToken = default)
        {
            RecuperacionesStale++;
            return Task.FromResult(0);
        }

        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(
            int empresaId, DateTime ahoraUtc,
            int maximoMensajes, CancellationToken cancellationToken = default)
        {
            EmpresasReclamadas.Add(empresaId);
            return Task.FromResult(_lote);
        }

        public Task<bool> MarcarEntregadoAsync(
            int empresaId, int mensajeId, int intentoEsperado,
            DateTime ahoraUtc, CancellationToken cancellationToken = default)
        {
            EmpresasConfirmadas.Add(empresaId);
            return Task.FromResult(true);
        }

        public Task<bool> RegistrarFalloAsync(
            int empresaId, int mensajeId, int intentoEsperado,
            string errorSeguro, DateTime ahoraUtc,
            DateTime disponibleDesdeUtc, bool deadLetter,
            CancellationToken cancellationToken = default)
        {
            EmpresasReprogramadas.Add(empresaId);
            return Task.FromResult(true);
        }
    }
}
