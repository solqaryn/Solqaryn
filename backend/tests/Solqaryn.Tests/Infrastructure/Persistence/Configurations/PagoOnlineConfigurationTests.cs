using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests.Infrastructure.Persistence.Configurations;

public sealed class PagoOnlineConfigurationTests
{
    [Fact]
    public void Modelo_Mapea_PagoOnline_Con_Fks_Restrictivas_Y_Precision_Monetaria()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(PagoOnline));

        Assert.NotNull(entity);
        Assert.Equal("PagosOnline", entity!.GetTableName());

        var monto = entity.FindProperty(nameof(PagoOnline.Monto));
        Assert.NotNull(monto);
        Assert.Equal(18, monto!.GetPrecision());
        Assert.Equal(2, monto.GetScale());

        var facturaFk = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Factura));
        var empresaFk = entity.GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(Empresa));
        Assert.Equal(DeleteBehavior.Restrict, facturaFk.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, empresaFk.DeleteBehavior);
    }

    [Fact]
    public void Modelo_Exige_Idempotencia_TenantBound_Para_Referencia_Y_Evento_Proveedor()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(PagoOnline));
        Assert.NotNull(entity);

        var indexes = entity!.GetIndexes().ToList();

        var referencia = indexes.Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(PagoOnline.EmpresaId),
                nameof(PagoOnline.Proveedor),
                nameof(PagoOnline.ReferenciaProveedor)
            }));
        Assert.True(referencia.IsUnique);

        var evento = indexes.Single(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(PagoOnline.EmpresaId),
                nameof(PagoOnline.Proveedor),
                nameof(PagoOnline.ProviderEventId)
            }));
        Assert.True(evento.IsUnique);
    }

    [Fact]
    public void Modelo_No_Contiene_Datos_De_Tarjeta_Sensibles()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(PagoOnline));
        Assert.NotNull(entity);

        var propertyNames = entity!.GetProperties()
            .Select(p => p.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, n => n.Contains("Pan", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, n => n.Contains("Cvv", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, n => n.Contains("CardNumber", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, n => n.Contains("NumeroTarjeta", StringComparison.OrdinalIgnoreCase));
    }
}
