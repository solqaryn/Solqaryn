using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Migrations;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;
using Xunit;

namespace Solqaryn.Tests;

public class N16TransferenciaInventarioPersistenceTests
{
    private const string TransferenciaFkCanonical = "FK_MovInv_TransferenciaInventarioId_N16";

    [Fact]
    public void ModeloEf_MapeaCabeceraDetalleConRestriccionesRelacionales()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n16-persistence-{Guid.NewGuid():N}")
            .Options;

        using var context = new AppDbContext(options);
        var model = context.GetService<IDesignTimeModel>().Model;
        var transferencia = model.FindEntityType(typeof(TransferenciaInventario));
        var detalle = model.FindEntityType(typeof(TransferenciaInventarioDetalle));
        var movimiento = model.FindEntityType(typeof(MovimientoInventario));

        Assert.NotNull(transferencia);
        Assert.NotNull(detalle);
        Assert.NotNull(movimiento);
        Assert.Equal("TransferenciasInventario", transferencia!.GetTableName());
        Assert.Equal("TransferenciaInventarioDetalles", detalle!.GetTableName());

        var numero = Assert.IsAssignableFrom<IReadOnlyProperty>(
            transferencia.FindProperty(nameof(TransferenciaInventario.Numero)));
        Assert.Equal(30, numero.GetMaxLength());

        var numeroIndex = Assert.Single(transferencia.GetIndexes()
            .Where(i => i.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(TransferenciaInventario.Numero) })));
        Assert.True(numeroIndex.IsUnique);
        Assert.Equal("UX_TransferenciasInventario_Numero", numeroIndex.GetDatabaseName());

        var checksCabecera = transferencia.GetCheckConstraints().Select(c => c.Name).ToHashSet();
        Assert.Contains("CK_TransferenciasInventario_AlmacenesDistintos", checksCabecera);
        Assert.Contains("CK_TransferenciasInventario_Estado_Valido", checksCabecera);

        var checksDetalle = detalle.GetCheckConstraints().Select(c => c.Name).ToHashSet();
        Assert.Contains("CK_TransferenciaInventarioDetalles_CantidadesNoNegativas", checksDetalle);
        Assert.Contains("CK_TransferenciaInventarioDetalles_Aprobada", checksDetalle);
        Assert.Contains("CK_TransferenciaInventarioDetalles_Despachada", checksDetalle);
        Assert.Contains("CK_TransferenciaInventarioDetalles_Recepcion", checksDetalle);
        Assert.Null(detalle.FindProperty(nameof(TransferenciaInventarioDetalle.RecepcionCerrada)));

        var cabeceraFk = Assert.Single(detalle.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(TransferenciaInventario)));
        Assert.Equal(DeleteBehavior.Cascade, cabeceraFk.DeleteBehavior);

        var almacenFks = transferencia.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(Almacen))
            .ToList();
        Assert.Equal(2, almacenFks.Count);
        Assert.All(almacenFks, fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));

        var ubicacionFks = detalle.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(UbicacionAlmacen))
            .ToList();
        Assert.Equal(2, ubicacionFks.Count);
        Assert.All(ubicacionFks, fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));

        Assert.NotNull(movimiento!.FindProperty(nameof(MovimientoInventario.TransferenciaInventarioId)));
        var movimientoTransferenciaFk = Assert.Single(movimiento.GetForeignKeys()
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(TransferenciaInventario)));
        Assert.Equal(DeleteBehavior.Restrict, movimientoTransferenciaFk.DeleteBehavior);
        var constraintName = movimientoTransferenciaFk.GetConstraintName();
        Assert.Equal(TransferenciaFkCanonical, constraintName);
        Assert.True(constraintName!.Length <= 64, "El identificador físico de la FK debe ser válido en MySQL.");
    }

    [Fact]
    public void MigracionOrigenTipado_UsaLaMismaFkCanonicaQueElModelo()
    {
        var campo = typeof(N1_6_TransferenciaOrigenMovimientoInventario)
            .GetField("TransferenciaFk", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(campo);
        Assert.True(campo!.IsLiteral, "TransferenciaFk debe permanecer como constante para Up/Down y postguard.");
        Assert.Equal(TransferenciaFkCanonical, campo.GetRawConstantValue());
        Assert.True(TransferenciaFkCanonical.Length <= 64);
    }
}
