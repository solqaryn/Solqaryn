using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Tests;

public class MetodoPagoElegibilidadRepositoryTests
{
    [Fact]
    public async Task Resolvers_ParaNuevasOperaciones_SoloAceptanCatalogosActivosNoEliminados()
    {
        await using var context = CrearContexto();
        context.Set<CatalogoMetodoPago>().AddRange(
            CrearCatalogo(901, "ACTIVO", "Activo", activo: true),
            CrearCatalogo(902, "INACTIVO", "Inactivo", activo: false),
            CrearCatalogo(903, "ELIMINADO", "Eliminado", activo: true, eliminado: true));
        await context.SaveChangesAsync();

        var scope = CrearScopeAdmin();
        var current = new Mock<ICurrentUserService>();
        var venta = new VentaRepository(context, current.Object, scope.Object);
        var factura = new FacturaRepository(context, scope.Object);
        var financiero = new MovimientoFinancieroRepository(context, scope.Object);

        Assert.NotNull(await venta.GetMetodoPagoPorCodigoONombreAsync("ACTIVO"));
        Assert.NotNull(await factura.GetMetodoPagoPorCodigoONombreAsync("ACTIVO"));
        Assert.NotNull(await financiero.GetMetodoPagoPorCodigoONombreAsync("ACTIVO"));

        Assert.Null(await venta.GetMetodoPagoPorCodigoONombreAsync("INACTIVO"));
        Assert.Null(await factura.GetMetodoPagoPorCodigoONombreAsync("INACTIVO"));
        Assert.Null(await financiero.GetMetodoPagoPorCodigoONombreAsync("INACTIVO"));

        Assert.Null(await venta.GetMetodoPagoPorCodigoONombreAsync("ELIMINADO"));
        Assert.Null(await factura.GetMetodoPagoPorCodigoONombreAsync("ELIMINADO"));
        Assert.Null(await financiero.GetMetodoPagoPorCodigoONombreAsync("ELIMINADO"));
    }

    [Fact]
    public async Task LecturaHistorica_ConservaMetodoInactivoYaRelacionado()
    {
        await using var context = CrearContexto();
        var inactivo = CrearCatalogo(910, "HISTORICO", "Método histórico", activo: false);
        context.Set<CatalogoMetodoPago>().Add(inactivo);
        context.MovimientosFinancieros.Add(new MovimientoFinanciero
        {
            Id = 77,
            Tipo = TipoMovimientoFinanciero.Ingreso,
            Categoria = CategoriaMovimientoFinanciero.Venta,
            Concepto = "Movimiento histórico",
            Monto = 100m,
            Estado = EstadoMovimientoFinanciero.Pagado,
            MetodoPagoId = inactivo.Id,
            MetodoPagoCatalogo = inactivo,
            EsAutomatico = true,
            ModuloOrigen = "Venta",
            CreadoPorUsuarioId = 1
        });
        await context.SaveChangesAsync();

        var repo = new MovimientoFinancieroRepository(context, CrearScopeAdmin().Object);
        var historico = await repo.GetByIdAsync(77);

        Assert.NotNull(historico);
        Assert.NotNull(historico!.MetodoPagoCatalogo);
        Assert.False(historico.MetodoPagoCatalogo!.Activo);
        Assert.Equal("Método histórico", historico.MetodoPagoCatalogo.Nombre);
    }

    [Fact]
    public async Task AddAsync_NuevaOperacionConCatalogoInactivo_FallaCerrado()
    {
        await using var context = CrearContexto();
        var inactivo = CrearCatalogo(920, "INACTIVO", "Inactivo", activo: false);
        context.Set<CatalogoMetodoPago>().Add(inactivo);
        await context.SaveChangesAsync();
        var repo = new MovimientoFinancieroRepository(context, CrearScopeAdmin().Object);

        await Assert.ThrowsAsync<Solqaryn.Application.Exceptions.BusinessRuleException>(() => repo.AddAsync(new MovimientoFinanciero
        {
            Tipo = TipoMovimientoFinanciero.Egreso,
            Categoria = CategoriaMovimientoFinanciero.GastoOperativo,
            Concepto = "No permitido",
            Monto = 50m,
            Estado = EstadoMovimientoFinanciero.Pagado,
            MetodoPagoId = inactivo.Id,
            MetodoPagoCatalogo = inactivo,
            EsAutomatico = false,
            ModuloOrigen = "Manual",
            CreadoPorUsuarioId = 1
        }));
    }

    private static AppDbContext CrearContexto() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"metodo-elegibilidad-{Guid.NewGuid():N}").Options);

    private static Mock<IUsuarioScopeService> CrearScopeAdmin()
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Administrador", true));
        return scope;
    }

    private static CatalogoMetodoPago CrearCatalogo(int id, string codigo, string nombre, bool activo, bool eliminado = false) => new()
    {
        Id = id,
        Codigo = codigo,
        Nombre = nombre,
        Tipo = "Otro",
        Activo = activo,
        Eliminado = eliminado,
        Orden = id
    };
}
