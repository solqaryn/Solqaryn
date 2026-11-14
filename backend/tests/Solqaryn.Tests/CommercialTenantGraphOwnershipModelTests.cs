using Microsoft.EntityFrameworkCore;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CommercialTenantGraphOwnershipModelTests
{
    private static AppDbContext Db() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("tenant-entire-graph-" + Guid.NewGuid().ToString("N"))
            .Options);

    public static IEnumerable<object[]> NuevasEntidades() => new string[]
    {
        "Solqaryn.Domain.Entities.AjusteInventarioDetalle",
        "Solqaryn.Domain.Entities.AplicacionCuentaPorPagar",
        "Solqaryn.Domain.Entities.AsignacionCostoMovimientoInventario",
        "Solqaryn.Domain.Entities.CapaCostoInventario",
        "Solqaryn.Domain.Entities.ConsumoInsumoDetalle",
        "Solqaryn.Domain.Entities.ConteoInventarioDetalle",
        "Solqaryn.Domain.Entities.CostoEstandarInventario",
        "Solqaryn.Domain.Entities.CreditoCliente",
        "Solqaryn.Domain.Entities.CuentaPorPagar",
        "Solqaryn.Domain.Entities.DescuentoCliente",
        "Solqaryn.Domain.Entities.DescuentoProducto",
        "Solqaryn.Domain.Entities.DevolucionClienteDetalle",
        "Solqaryn.Domain.Entities.DevolucionCliente",
        "Solqaryn.Domain.Entities.DevolucionProveedor",
        "Solqaryn.Domain.Entities.DevolucionProveedorDetalle",
        "Solqaryn.Domain.Entities.EvaluacionProveedor",
        "Solqaryn.Domain.Entities.ExistenciaVariante",
        "Solqaryn.Domain.Entities.FacturaProveedorDetalle",
        "Solqaryn.Domain.Entities.FacturaProveedor",
        "Solqaryn.Domain.Entities.ImpuestoCliente",
        "Solqaryn.Domain.Entities.ImpuestoProducto",
        "Solqaryn.Domain.Entities.ImpuestoProveedor",
        "Solqaryn.Domain.Entities.LoteInventario",
        "Solqaryn.Domain.Entities.Bancos.MatchConciliacion",
        "Solqaryn.Domain.Entities.MovimientoFinanciero",
        "Solqaryn.Domain.Entities.MovimientoInventario",
        "Solqaryn.Domain.Entities.NotaCreditoCliente",
        "Solqaryn.Domain.Entities.NotaCreditoProveedor",
        "Solqaryn.Domain.Entities.OrdenCompraDetalle",
        "Solqaryn.Domain.Entities.OrdenCompra",
        "Solqaryn.Domain.Entities.PreparacionPedidoVenta",
        "Solqaryn.Domain.Entities.PreparacionPedidoVentaDetalle",
        "Solqaryn.Domain.Entities.RecepcionCompraDetalle",
        "Solqaryn.Domain.Entities.RecepcionCompra",
        "Solqaryn.Domain.Entities.ReservaInventarioDetalle",
        "Solqaryn.Domain.Entities.ReservaInventario",
        "Solqaryn.Domain.Entities.SerieInventario",
        "Solqaryn.Domain.Entities.SolicitudCompraDetalle",
        "Solqaryn.Domain.Entities.SolicitudCompra",
        "Solqaryn.Domain.Entities.ThreeWayMatchResult",
        "Solqaryn.Domain.Entities.TiendaCuentaCliente",
        "Solqaryn.Domain.Entities.TiendaDireccionCliente",
        "Solqaryn.Domain.Entities.TiendaFavoritoCliente",
        "Solqaryn.Domain.Entities.TiendaSesionCliente",
        "Solqaryn.Domain.Entities.TransferenciaInventarioDetalle",
        "Solqaryn.Domain.Entities.VariacionCostoEstandarInventario",
    }.Select(x => new object[] { x });

    public static IEnumerable<object[]> PadresAdicionales() => new string[]
    {
        "Solqaryn.Domain.Entities.CompraDetalle",
        "Solqaryn.Domain.Entities.CompraDocumento",
        "Solqaryn.Domain.Entities.CompraImpuesto",
        "Solqaryn.Domain.Entities.DocumentoFiscalEmision",
        "Solqaryn.Domain.Entities.FacturaDetalle",
        "Solqaryn.Domain.Entities.FacturaPago",
        "Solqaryn.Domain.Entities.HistorialUsoDescuento",
        "Solqaryn.Domain.Entities.PagoOnline",
        "Solqaryn.Domain.Entities.ProductoImagen",
        "Solqaryn.Domain.Entities.VentaDescuento",
        "Solqaryn.Domain.Entities.VentaDetalle",
        "Solqaryn.Domain.Entities.VentaImpuesto",
    }.Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(NuevasEntidades))]
    public void Entidad_comercial_tiene_owner_indice_identidad_y_fk_empresa(string entityName)
    {
        using var db = Db();
        var e = db.Model.FindEntityType(entityName);
        Assert.NotNull(e);
        var owner=e!.FindProperty("EmpresaId");
        Assert.NotNull(owner);
        Assert.True(owner!.IsNullable);
        Assert.Contains(e.GetIndexes(), idx =>
            idx.IsUnique && idx.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "EmpresaId", "Id" }));
        Assert.Contains(e.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Empresa) &&
            fk.Properties.Count == 1 && fk.Properties[0].Name == "EmpresaId" &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Theory]
    [MemberData(nameof(NuevasEntidades))]
    public void Relaciones_comerciales_a_otros_owners_tienen_indice_compuesto(string entityName)
    {
        using var db = Db();
        var entity = db.Model.FindEntityType(entityName)!;
        foreach (var fk in entity.GetForeignKeys().Where(x =>
                 x.PrincipalEntityType.ClrType != typeof(Empresa) &&
                 x.PrincipalEntityType.FindProperty("EmpresaId") != null))
        {
            var expected=new[] { "EmpresaId" }.Concat(fk.Properties.Select(p => p.Name));
            Assert.Contains(entity.GetIndexes(), idx =>
                idx.Properties.Select(p => p.Name).SequenceEqual(expected));
        }
    }

    [Theory]
    [MemberData(nameof(PadresAdicionales))]
    public void Owner_existente_tiene_unicidad_para_fk_compuesta(string entityName)
    {
        using var db=Db();
        var e=db.Model.FindEntityType(entityName)!;
        Assert.Contains(e.GetIndexes(), idx =>
            idx.IsUnique && idx.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "EmpresaId", "Id" }));
    }
}
