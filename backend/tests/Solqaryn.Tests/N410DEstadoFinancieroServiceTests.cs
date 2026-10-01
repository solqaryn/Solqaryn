using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N410DEstadoFinancieroServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IPeriodoContableRepository> _periodos = new();
    private readonly Mock<IMovimientoFinancieroRepository> _movimientos = new();
    private readonly EstadoFinancieroService _service;

    public N410DEstadoFinancieroServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new EstadoFinancieroService(_context, _periodos.Object, _movimientos.Object);
    }

    [Fact]
    public async Task GenerarAsync_SoportaLosSeisTipos()
    {
        var fecha = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        await SeedAsientoAsync(fecha);

        _movimientos.Setup(x => x.GetFilteredAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<MovimientoFinanciero>
            {
                new() { Fecha = fecha, Tipo = TipoMovimientoFinanciero.Ingreso, Monto = 50m, Estado = EstadoMovimientoFinanciero.Pagado },
                new() { Fecha = fecha, Tipo = TipoMovimientoFinanciero.Egreso, Monto = 10m, Estado = EstadoMovimientoFinanciero.Pagado }
            });

        var filtro = new EstadoFinancieroFiltroDto
        {
            FechaDesde = fecha.AddDays(-1),
            FechaHasta = fecha.AddDays(1)
        };

        foreach (var tipo in Enum.GetValues<TipoEstadoFinanciero>())
        {
            var resultado = await _service.GenerarAsync(tipo, filtro);
            Assert.False(string.IsNullOrWhiteSpace(resultado.Nombre));
            Assert.Equal(filtro.FechaDesde, resultado.FechaInicio);
            Assert.Equal(filtro.FechaHasta, resultado.FechaFin);
        }
    }

    [Fact]
    public async Task BalanceGeneral_EsAcumuladoHastaFechaFin()
    {
        var anterior = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        var actual = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        await SeedAsientoAsync(anterior);

        var resultado = await _service.GenerarAsync(
            TipoEstadoFinanciero.BalanceGeneral,
            new EstadoFinancieroFiltroDto { FechaDesde = actual, FechaHasta = actual.AddDays(1) });

        Assert.Contains(resultado.Lineas, l => l.CuentaCodigo == "1.1" && l.Saldo == 80m);
    }

    [Fact]
    public async Task FlujoEfectivo_ExcluyeMovimientosAnulados()
    {
        var desde = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var hasta = desde.AddDays(1);
        _movimientos.Setup(x => x.GetFilteredAsync(desde, hasta))
            .ReturnsAsync(new List<MovimientoFinanciero>
            {
                new() { Tipo = TipoMovimientoFinanciero.Ingreso, Monto = 100m, Estado = EstadoMovimientoFinanciero.Pagado },
                new() { Tipo = TipoMovimientoFinanciero.Egreso, Monto = 25m, Estado = EstadoMovimientoFinanciero.Pagado },
                new() { Tipo = TipoMovimientoFinanciero.Ingreso, Monto = 999m, Estado = EstadoMovimientoFinanciero.Anulado }
            });

        var resultado = await _service.GenerarAsync(
            TipoEstadoFinanciero.FlujoEfectivo,
            new EstadoFinancieroFiltroDto { FechaDesde = desde, FechaHasta = hasta });

        Assert.Contains(resultado.Totales, t => t.Etiqueta == "Flujo Neto" && t.Valor == 75m);
    }

    [Fact]
    public async Task GenerarAsync_RechazaFiltroVacio()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _service.GenerarAsync(TipoEstadoFinanciero.BalanceGeneral, new EstadoFinancieroFiltroDto()));
    }

    private async Task SeedAsientoAsync(DateTime fecha)
    {
        var activo = new CuentaContable { Codigo = "1.1", Nombre = "Caja", Tipo = TipoCuentaContable.Activo };
        var ingreso = new CuentaContable { Codigo = "4.1", Nombre = "Ventas", Tipo = TipoCuentaContable.Ingreso };
        var gasto = new CuentaContable { Codigo = "5.1", Nombre = "Gasto", Tipo = TipoCuentaContable.Gasto };
        _context.Set<CuentaContable>().AddRange(activo, ingreso, gasto);
        await _context.SaveChangesAsync();

        var asiento = new AsientoContable { Fecha = fecha, Concepto = "Prueba", Numero = "A-1" };
        asiento.AgregarDetalle(new AsientoDetalle(activo.Id, 100m, 0m, null));
        asiento.AgregarDetalle(new AsientoDetalle(ingreso.Id, 0m, 100m, null));
        asiento.AgregarDetalle(new AsientoDetalle(gasto.Id, 20m, 0m, null));
        asiento.AgregarDetalle(new AsientoDetalle(activo.Id, 0m, 20m, null));
        _context.AsientosContables.Add(asiento);
        await _context.SaveChangesAsync();
    }

    public void Dispose() => _context.Dispose();
}
