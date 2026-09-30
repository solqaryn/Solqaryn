using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public class N18ReservaInventarioPersistenceModelTests
{
    [Fact]
    public void Modelo_EF_Debe_Mapear_Reserva_Y_Detalle_Con_Integridad_Fisica()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n18-reservas-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);

        var reserva = db.Model.FindEntityType(typeof(ReservaInventario));
        var detalle = db.Model.FindEntityType(typeof(ReservaInventarioDetalle));

        Assert.NotNull(reserva);
        Assert.NotNull(detalle);
        Assert.Equal("ReservasInventario", reserva!.GetTableName());
        Assert.Equal("ReservaInventarioDetalles", detalle!.GetTableName());

        var numero = reserva.FindProperty(nameof(ReservaInventario.Numero));
        Assert.NotNull(numero);
        Assert.Equal(40, numero!.GetMaxLength());
        Assert.False(numero.IsNullable);

        var numeroUnico = reserva.GetIndexes().Single(i =>
            i.GetDatabaseName() == "UX_ReservasInventario_Numero");
        Assert.True(numeroUnico.IsUnique);

        var claveFisica = detalle.GetIndexes().Single(i =>
            i.GetDatabaseName() == "UX_ReservaDetalles_ClaveFisica");
        Assert.True(claveFisica.IsUnique);
        Assert.Equal(
            new[] { "ReservaInventarioId", "ProductoVarianteId", "AlmacenId", "UbicacionNormalizada" },
            claveFisica.Properties.Select(p => p.Name));

        var ubicacionNormalizada = detalle.FindProperty("UbicacionNormalizada");
        Assert.NotNull(ubicacionNormalizada);
        Assert.Contains("COALESCE", ubicacionNormalizada!.GetComputedColumnSql(), StringComparison.OrdinalIgnoreCase);

        var fkUbicacion = detalle.GetForeignKeys().Single(fk =>
            fk.GetConstraintName() == "FK_ReservaDetalles_Ubicacion_MismoAlmacen");
        Assert.Equal(new[] { "AlmacenId", "UbicacionAlmacenId" }, fkUbicacion.Properties.Select(p => p.Name));
        Assert.Equal(DeleteBehavior.Restrict, fkUbicacion.DeleteBehavior);

        var fkReserva = detalle.GetForeignKeys().Single(fk =>
            fk.GetConstraintName() == "FK_ReservaInventarioDetalles_ReservasInventario_ReservaInventarioId");
        Assert.Equal(DeleteBehavior.Cascade, fkReserva.DeleteBehavior);
    }

    [Fact]
    public void DbContext_Debe_Exponer_Reservas_Y_Detalles()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n18-dbsets-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);

        Assert.NotNull(db.ReservasInventario);
        Assert.NotNull(db.ReservaInventarioDetalles);
    }
}
