using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N310CreditoClientePersistenceTests
{
    [Fact]
    public void CreditoCliente_MapeaConfiguracionYRestriccionesFailClosed()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(CreditoCliente));

        Assert.NotNull(entity);
        Assert.Contains(entity!.GetIndexes(), x => !x.IsUnique && x.GetDatabaseName() == "IX_CreditosCliente_ClienteId");
        Assert.Contains(entity.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(Cliente) && x.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Equal("decimal(18,4)", entity.FindProperty(nameof(CreditoCliente.LimiteCredito))!.GetColumnType());
        Assert.Equal("decimal(18,4)", entity.FindProperty(nameof(CreditoCliente.UmbralAlertaPorcentaje))!.GetColumnType());
        Assert.Equal("decimal(18,4)", entity.FindProperty(nameof(CreditoCliente.MontoExcepcion))!.GetColumnType());
        Assert.Equal(3, entity.FindProperty(nameof(CreditoCliente.Moneda))!.GetMaxLength());
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=solqaryn_test;User=root;Password=test;", ServerVersion.Parse("8.0.36-mysql"))
            .Options;
        return new AppDbContext(options);
    }
}
