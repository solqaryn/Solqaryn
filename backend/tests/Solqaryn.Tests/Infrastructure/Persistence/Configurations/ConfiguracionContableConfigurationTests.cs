using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests.Infrastructure.Persistence.Configurations;

public class ConfiguracionContableConfigurationTests
{
    [Fact]
    public async Task ConfiguracionContable_Persiste_Regla_Y_Relaciones()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        var debe = new CuentaContable { Codigo = "101", Nombre = "Caja", Tipo = TipoCuentaContable.Activo };
        var haber = new CuentaContable { Codigo = "401", Nombre = "Ventas", Tipo = TipoCuentaContable.Ingreso };
        context.Set<CuentaContable>().AddRange(debe, haber);
        await context.SaveChangesAsync();

        context.Set<ConfiguracionContable>().Add(new ConfiguracionContable
        {
            Evento = TipoEventoContable.Venta,
            CuentaDebeId = debe.Id,
            CuentaHaberId = haber.Id,
            Descripcion = "Regla venta"
        });
        await context.SaveChangesAsync();

        var persisted = await context.Set<ConfiguracionContable>()
            .Include(x => x.CuentaDebe)
            .Include(x => x.CuentaHaber)
            .SingleAsync(x => x.Evento == TipoEventoContable.Venta);

        Assert.True(persisted.Activo);
        Assert.Equal("101", persisted.CuentaDebe.Codigo);
        Assert.Equal("401", persisted.CuentaHaber.Codigo);
    }

    [Fact]
    public void Modelo_Exige_Evento_Unico_Y_DeleteRestrict()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(ConfiguracionContable));
        Assert.NotNull(entity);

        var eventoIndex = entity!.GetIndexes().Single(i => i.Properties.Single().Name == nameof(ConfiguracionContable.Evento));
        Assert.True(eventoIndex.IsUnique);

        Assert.All(entity.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }
}
