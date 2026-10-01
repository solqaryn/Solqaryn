using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N24FacturaProveedorConstraintModelTests
{
    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n24-factura-proveedor-constraints-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void Checks_persistentes_blindan_identidad_estado_moneda_y_fechas_de_cabecera()
    {
        using var context = CrearContexto();
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var entity = designTimeModel.FindEntityType(typeof(FacturaProveedor));

        Assert.NotNull(entity);
        var checks = entity!.GetCheckConstraints().Select(c => c.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("CK_FacturasProveedor_IdsValidos", checks);
        Assert.Contains("CK_FacturasProveedor_EstadoValido", checks);
        Assert.Contains("CK_FacturasProveedor_MonedaIso3", checks);
        Assert.Contains("CK_FacturasProveedor_FechasValidas", checks);
    }

    [Fact]
    public void Checks_persistentes_blindan_ids_importes_y_descuento_de_detalle()
    {
        using var context = CrearContexto();
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        var entity = designTimeModel.FindEntityType(typeof(FacturaProveedorDetalle));

        Assert.NotNull(entity);
        var checks = entity!.GetCheckConstraints().Select(c => c.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("CK_FacturaProveedorDetalles_IdsValidos", checks);
        Assert.Contains("CK_FacturaProveedorDetalles_ImportesValidos", checks);
        Assert.Contains("CK_FacturaProveedorDetalles_DescuentoValido", checks);
    }
}
