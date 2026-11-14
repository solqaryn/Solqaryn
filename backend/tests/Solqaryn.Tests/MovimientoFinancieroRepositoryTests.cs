using Solqaryn.Application.Exceptions;
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

public class MovimientoFinancieroRepositoryTests
{
    [Fact]
    public async Task AddAsync_ReversionCompraPendiente_AnulaOriginalSinCrearReversion()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var original = CrearOriginal(EstadoMovimientoFinanciero.Pendiente);
        context.MovimientosFinancieros.Add(original);
        await context.SaveChangesAsync();

        await repo.AddAsync(CrearReversion(original.CompraId!.Value));
        await context.SaveChangesAsync();

        var movimientos = await context.MovimientosFinancieros.OrderBy(m => m.Id).ToListAsync();
        var unico = Assert.Single(movimientos);
        Assert.Equal(original.Id, unico.Id);
        Assert.Equal(EstadoMovimientoFinanciero.Anulado, unico.Estado);
        Assert.NotNull(unico.FechaAnulacion);
        Assert.Equal(9, unico.AnuladoPorUsuarioId);
    }

    [Fact]
    public async Task AddAsync_ReversionCompraPagada_PropagaRelacionSinEnumLegacy()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var original = CrearOriginal(EstadoMovimientoFinanciero.Pagado);
        context.MovimientosFinancieros.Add(original);
        await context.SaveChangesAsync();

        await repo.AddAsync(CrearReversion(original.CompraId!.Value));
        await context.SaveChangesAsync();

        var movimientos = await context.MovimientosFinancieros
            .Include(m => m.MetodoPagoCatalogo)
            .OrderBy(m => m.Id)
            .ToListAsync();
        Assert.Equal(2, movimientos.Count);
        Assert.Equal(EstadoMovimientoFinanciero.Pagado, movimientos[0].Estado);

        var reversion = movimientos[1];
        Assert.Equal(EstadoMovimientoFinanciero.Pendiente, reversion.Estado);
        Assert.Equal(original.Id, reversion.ReferenciaId);
        Assert.Equal(original.MetodoPagoId, reversion.MetodoPagoId);
        Assert.Equal("Transferencia bancaria", reversion.MetodoPagoCatalogo!.Nombre);
        Assert.Null(reversion.MetodoPago);
        Assert.Equal(CategoriaMovimientoFinanciero.Reversion, reversion.Categoria);
        Assert.Equal(TipoMovimientoFinanciero.Ingreso, reversion.Tipo);
    }

    [Fact]
    public async Task AddAsync_ReversionCompra_IgnoraSnapshotModuloOrigenDesactualizado()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var original = CrearOriginal(EstadoMovimientoFinanciero.Pendiente);
        original.ModuloOrigen = "SnapshotLegacyIncorrecto";
        context.MovimientosFinancieros.Add(original);
        await context.SaveChangesAsync();

        var reversion = CrearReversion(original.CompraId!.Value);
        reversion.ModuloOrigen = "OtroSnapshotLegacy";

        await repo.AddAsync(reversion);
        await context.SaveChangesAsync();

        var unico = Assert.Single(await context.MovimientosFinancieros.ToListAsync());
        Assert.Equal(original.Id, unico.Id);
        Assert.Equal(EstadoMovimientoFinanciero.Anulado, unico.Estado);
    }

    [Fact]
    public async Task AddAsync_MovimientoConEnumLegacy_SeNormalizaARelacionYElEnumNoSePersiste()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var catalogo = CrearCatalogo();
        context.Set<CatalogoMetodoPago>().Add(catalogo);
        await context.SaveChangesAsync();

        var movimiento = new MovimientoFinanciero
        {
            Tipo = TipoMovimientoFinanciero.Egreso,
            Categoria = CategoriaMovimientoFinanciero.GastoOperativo,
            Concepto = "Pago legado",
            Monto = 50m,
            Estado = EstadoMovimientoFinanciero.Pagado,
            MetodoPago = MetodoPago.Transferencia,
            EsAutomatico = false,
            ModuloOrigen = "Manual",
            CreadoPorUsuarioId = 9
        };

        await repo.AddAsync(movimiento);
        await context.SaveChangesAsync();

        var persistido = await context.MovimientosFinancieros
            .Include(m => m.MetodoPagoCatalogo)
            .SingleAsync();
        Assert.Equal(catalogo.Id, persistido.MetodoPagoId);
        Assert.Equal("Transferencia bancaria", persistido.MetodoPagoCatalogo!.Nombre);
        Assert.Null(persistido.MetodoPago);
    }

    [Fact]
    public async Task AddAsync_ReversionCompraConOriginalAnulado_EsRechazada()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var original = CrearOriginal(EstadoMovimientoFinanciero.Anulado);
        context.MovimientosFinancieros.Add(original);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            repo.AddAsync(CrearReversion(original.CompraId!.Value)));

        Assert.Single(await context.MovimientosFinancieros.ToListAsync());
    }

    [Fact]
    public async Task GetByCompraIdAsync_PrefiereMovimientoOriginalSobreReversionYCargaMetodoPago()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var original = CrearOriginal(EstadoMovimientoFinanciero.Pagado);
        context.MovimientosFinancieros.Add(original);
        await context.SaveChangesAsync();
        await repo.AddAsync(CrearReversion(original.CompraId!.Value));
        await context.SaveChangesAsync();

        var encontrado = await repo.GetByCompraIdAsync(original.CompraId.Value);

        Assert.NotNull(encontrado);
        Assert.Equal(original.Id, encontrado!.Id);
        Assert.Equal("Compra", encontrado.ModuloOrigen);
        Assert.Equal("Transferencia bancaria", encontrado.MetodoPagoCatalogo!.Nombre);
    }

    [Fact]
    public async Task GetByCompraIdAsync_UsaCompraIdAunqueSnapshotModuloOrigenNoCoincida()
    {
        await using var context = CrearContexto();
        var repo = CrearRepositorio(context);
        var original = CrearOriginal(EstadoMovimientoFinanciero.Pagado);
        original.ModuloOrigen = "LegacyDesalineado";
        context.MovimientosFinancieros.Add(original);
        await context.SaveChangesAsync();

        var encontrado = await repo.GetByCompraIdAsync(original.CompraId!.Value);

        Assert.NotNull(encontrado);
        Assert.Equal(original.Id, encontrado!.Id);
        Assert.Equal("LegacyDesalineado", encontrado.ModuloOrigen);
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"finanzas-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static MovimientoFinancieroRepository CrearRepositorio(AppDbContext context)
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(9, 1, "Admin", true));
        return new MovimientoFinancieroRepository(context, scope.Object);
    }

    private static MovimientoFinanciero CrearOriginal(EstadoMovimientoFinanciero estado)
    {
        var catalogo = CrearCatalogo();
        return new MovimientoFinanciero
        {
            Tipo = TipoMovimientoFinanciero.Egreso,
            Categoria = CategoriaMovimientoFinanciero.Compra,
            Concepto = "Compra original",
            Monto = 100m,
            Estado = estado,
            MetodoPagoId = catalogo.Id,
            MetodoPagoCatalogo = catalogo,
            MetodoPago = null,
            EsAutomatico = true,
            ModuloOrigen = "Compra",
            ReferenciaId = 77,
            CompraId = 77,
            CreadoPorUsuarioId = 9,
            CreadoPorNombreUsuario = "Admin"
        };
    }

    private static CatalogoMetodoPago CrearCatalogo() => new()
    {
        Id = 700,
        Codigo = "TRANSFERENCIA",
        Nombre = "Transferencia bancaria",
        Tipo = "Transferencia",
        Activo = true,
        Orden = 2
    };

    private static MovimientoFinanciero CrearReversion(int compraId) => new()
    {
        Tipo = TipoMovimientoFinanciero.Ingreso,
        Categoria = CategoriaMovimientoFinanciero.Reversion,
        Concepto = "Reversión de compra anulada",
        Descripcion = "Anulación de compra",
        Monto = 100m,
        Estado = EstadoMovimientoFinanciero.Pagado,
        EsAutomatico = true,
        ModuloOrigen = "Reversion",
        ReferenciaId = compraId,
        CompraId = compraId,
        CreadoPorUsuarioId = 9,
        CreadoPorNombreUsuario = "Admin"
    };
}
