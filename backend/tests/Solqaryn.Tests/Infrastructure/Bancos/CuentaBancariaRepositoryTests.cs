using Solqaryn.Application.Bancos;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums.Bancos;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests.Infrastructure.Bancos;

public class CuentaBancariaRepositoryTests
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldAddCuentaBancaria()
    {
        using var context = CreateDbContext();
        var repository = new CuentaBancariaRepository(context);
        var cuenta = new CuentaBancaria(1, "Cuenta Principal", "1234567890", "HNL", 1000m);
        await repository.AddAsync(cuenta);
        await repository.SaveChangesAsync();
        var dbCuenta = await context.CuentasBancarias.FirstOrDefaultAsync();
        Assert.NotNull(dbCuenta);
        Assert.Equal("Cuenta Principal", dbCuenta!.Nombre);
        Assert.Equal("1234567890", dbCuenta.NumeroCuenta);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCuentaBancaria()
    {
        using var context = CreateDbContext();
        var repository = new CuentaBancariaRepository(context);
        var cuenta = new CuentaBancaria(1, "Cuenta A", "111", "USD", 500m);
        context.CuentasBancarias.Add(cuenta);
        await context.SaveChangesAsync();
        var result = await repository.GetByIdAsync(cuenta.Id);
        Assert.NotNull(result);
        Assert.Equal(cuenta.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        using var context = CreateDbContext();
        var repository = new CuentaBancariaRepository(context);
        var result = await repository.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedCuentas()
    {
        using var context = CreateDbContext();
        var repository = new CuentaBancariaRepository(context);
        context.CuentasBancarias.Add(new CuentaBancaria(1, "Cuenta 1", "1", "HNL"));
        context.CuentasBancarias.Add(new CuentaBancaria(1, "Cuenta 2", "2", "USD"));
        await context.SaveChangesAsync();
        var result = await repository.GetAllAsync(new CuentaBancariaQueryFilter());
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyPage_WhenNoCuentas()
    {
        using var context = CreateDbContext();
        var repository = new CuentaBancariaRepository(context);
        var result = await repository.GetAllAsync(new CuentaBancariaQueryFilter());
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetActivasAsync_ShouldReturnOnlyActivas()
    {
        using var context = CreateDbContext();
        var repository = new CuentaBancariaRepository(context);
        var cuenta1 = new CuentaBancaria(1, "Activa 1", "1", "HNL");
        var cuenta2 = new CuentaBancaria(1, "Inactiva 1", "2", "USD");
        cuenta2.Desactivar();
        var cuenta3 = new CuentaBancaria(1, "Activa 2", "3", "EUR");
        context.CuentasBancarias.AddRange(cuenta1, cuenta2, cuenta3);
        await context.SaveChangesAsync();
        var result = await repository.GetActivasAsync();
        Assert.Equal(2, result.Count);
        Assert.True(result.All(c => c.Estado == EstadoCuentaBancaria.Activa));
    }

    [Fact]
    public async Task GetActivasAsync_ShouldReturnEmptyList_WhenOnlyInactivas()
    {
        using var context = CreateDbContext();
        var repository = new CuentaBancariaRepository(context);
        var cuenta = new CuentaBancaria(1, "Inactiva 1", "1", "HNL");
        cuenta.Desactivar();
        context.CuentasBancarias.Add(cuenta);
        await context.SaveChangesAsync();
        var result = await repository.GetActivasAsync();
        Assert.Empty(result);
    }

    [Fact]
    public async Task Update_ShouldModifyCuentaBancaria()
    {
        using var context = CreateDbContext();
        var repository = new CuentaBancariaRepository(context);
        var cuenta = new CuentaBancaria(1, "Cuenta Original", "111", "HNL");
        context.CuentasBancarias.Add(cuenta);
        await context.SaveChangesAsync();
        cuenta.Desactivar();
        repository.Update(cuenta);
        await repository.SaveChangesAsync();
        var dbCuenta = await context.CuentasBancarias.FindAsync(cuenta.Id);
        Assert.Equal(EstadoCuentaBancaria.Inactiva, dbCuenta!.Estado);
    }
}
