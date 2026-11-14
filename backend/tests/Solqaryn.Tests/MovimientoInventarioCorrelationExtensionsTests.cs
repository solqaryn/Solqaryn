using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class MovimientoInventarioCorrelationExtensionsTests
{
    [Fact]
    public async Task AddConOrigenTipadoCorrelacionadoAsync_NormalizaCorrelationId()
    {
        var repository = new RepositorySpy();

        await repository.AddConOrigenTipadoCorrelacionadoAsync(
            new MovimientoInventario(),
            OrigenMovimientoInventario.DesdeCompra(7),
            "  compra:7:confirmar  ");

        Assert.NotNull(repository.UltimoMovimiento);
        Assert.Equal("compra:7:confirmar", repository.UltimoMovimiento!.CorrelationId);
        Assert.NotNull(repository.UltimoOrigen);
        Assert.Equal(7, repository.UltimoOrigen!.DocumentoId);
    }

    [Fact]
    public async Task AddConOrigenTipadoCorrelacionadoAsync_CorrelationIdVacio_FallaCerrado()
    {
        var repository = new RepositorySpy();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.AddConOrigenTipadoCorrelacionadoAsync(
                new MovimientoInventario(),
                OrigenMovimientoInventario.DesdeCompra(1),
                "   "));

        Assert.Null(repository.UltimoMovimiento);
    }

    [Theory]
    [InlineData("compra:1 con espacio")]
    [InlineData("compra/1")]
    [InlineData("compra#1")]
    public async Task AddConOrigenTipadoCorrelacionadoAsync_CorrelationIdInseguro_FallaCerrado(string correlationId)
    {
        var repository = new RepositorySpy();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.AddConOrigenTipadoCorrelacionadoAsync(
                new MovimientoInventario(),
                OrigenMovimientoInventario.DesdeCompra(1),
                correlationId));

        Assert.Null(repository.UltimoMovimiento);
    }

    [Fact]
    public async Task AddConOrigenTipadoCorrelacionadoAsync_CorrelationIdExcesivo_FallaCerrado()
    {
        var repository = new RepositorySpy();
        var correlationId = new string('x', ContextoFisicoMovimientoInventario.MaxCorrelationIdLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.AddConOrigenTipadoCorrelacionadoAsync(
                new MovimientoInventario(),
                OrigenMovimientoInventario.DesdeCompra(1),
                correlationId));

        Assert.Null(repository.UltimoMovimiento);
    }

    private sealed class RepositorySpy : IMovimientoInventarioRepository
    {
        public MovimientoInventario? UltimoMovimiento { get; private set; }
        public OrigenMovimientoInventario? UltimoOrigen { get; private set; }

        public Task AddAsync(MovimientoInventario movimiento)
        {
            UltimoMovimiento = movimiento;
            return Task.CompletedTask;
        }

        public Task AddConOrigenTipadoAsync(MovimientoInventario movimiento, OrigenMovimientoInventario origen)
        {
            UltimoMovimiento = movimiento;
            UltimoOrigen = origen;
            return Task.CompletedTask;
        }

        public Task<List<MovimientoInventario>> GetByProductoAsync(int productoId) =>
            Task.FromResult(new List<MovimientoInventario>());

        public Task<List<MovimientoInventario>> GetFilteredAsync(
            int? productoId,
            string? tipo,
            DateTime? desde,
            DateTime? hasta) =>
            Task.FromResult(new List<MovimientoInventario>());

        public Task<(List<MovimientoInventario> Items, int TotalCount)> GetPagedAsync(
            MovimientoInventarioQueryDto query) =>
            Task.FromResult((new List<MovimientoInventario>(), 0));

        public Task<IReadOnlyDictionary<int, MovimientoInventarioOrigenPersistido>> GetOrigenesTipadosAsync(
            IReadOnlyCollection<int> movimientoIds) =>
            Task.FromResult<IReadOnlyDictionary<int, MovimientoInventarioOrigenPersistido>>(
                new Dictionary<int, MovimientoInventarioOrigenPersistido>());

        public Task<int?> GetUltimoMovimientoOriginalCompraIdAsync(int compraId) =>
            Task.FromResult<int?>(null);

        public Task<bool> ExisteMovimientoPosteriorAsync(
            int ultimoMovimientoOriginalId,
            IReadOnlyCollection<int> productoIds) =>
            Task.FromResult(false);

        public Task<bool> ExisteMovimientoPosteriorRecepcionAsync(int recepcionCompraId) =>
            Task.FromResult(false);
    }
}
