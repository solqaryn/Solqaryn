using Solqaryn.Application.Bancos;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums.Bancos;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class CuentaBancariaRepositoryContractTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly CuentaBancariaRepository _repository;

    public CuentaBancariaRepositoryContractTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new CuentaBancariaRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private static void SetEstado(CuentaBancaria cuenta, EstadoCuentaBancaria estado)
    {
        typeof(CuentaBancaria).GetProperty("Estado", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(cuenta, estado);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Expected_Cuenta()
    {
        var cuenta = new CuentaBancaria(1, "Principal", "123456789", "HNL", 1000m);
        await _context.CuentasBancarias.AddAsync(cuenta);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(cuenta.Id);

        Assert.NotNull(result);
        Assert.Equal("Principal", result.Nombre);
        Assert.Equal("123456789", result.NumeroCuenta);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_Not_Found()
    {
        var result = await _repository.GetByIdAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActivasAsync_Returns_Only_Active_Cuentas()
    {
        var activa1 = new CuentaBancaria(1, "Activa 1", "111", "HNL");
        var activa2 = new CuentaBancaria(1, "Activa 2", "222", "USD");
        var inactiva = new CuentaBancaria(1, "Inactiva", "333", "HNL");

        await _context.CuentasBancarias.AddRangeAsync(activa1, activa2, inactiva);
        await _context.SaveChangesAsync();

        SetEstado(inactiva, EstadoCuentaBancaria.Inactiva);
        _context.CuentasBancarias.Update(inactiva);
        await _context.SaveChangesAsync();

        var result = await _repository.GetActivasAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.NumeroCuenta == "111");
        Assert.Contains(result, c => c.NumeroCuenta == "222");
    }

    [Fact]
    public async Task GetAllAsync_Applies_Pagination_And_Filters_Correctly()
    {
        var c1 = new CuentaBancaria(1, "A Cuenta", "111", "HNL");
        var c2 = new CuentaBancaria(1, "B Cuenta", "222", "USD");
        var c3 = new CuentaBancaria(2, "C Cuenta", "333", "HNL");
        var c4 = new CuentaBancaria(2, "D Cuenta", "444", "EUR");
        await _context.CuentasBancarias.AddRangeAsync(c1, c2, c3, c4);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync(new CuentaBancariaQueryFilter
        {
            BancoId = 1,
            Page = 1,
            PageSize = 1
        });

        Assert.Single(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal("A Cuenta", result.Items.First().Nombre);
    }

    [Fact]
    public async Task GetAllAsync_Returns_EmptyPage_When_Out_Of_Bounds()
    {
        var cuenta = new CuentaBancaria(1, "A Cuenta", "111", "HNL");
        await _context.CuentasBancarias.AddAsync(cuenta);
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync(new CuentaBancariaQueryFilter { Page = 2, PageSize = 10 });

        Assert.Empty(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task AddAsync_Update_SaveChangesAsync_Persists_Changes()
    {
        var cuenta = new CuentaBancaria(1, "Nueva", "987", "USD", 500m);
        await _repository.AddAsync(cuenta);
        await _repository.SaveChangesAsync();

        var saved = await _context.CuentasBancarias.FindAsync(cuenta.Id);
        Assert.NotNull(saved);

        saved!.UpdateNombre("Modificada");
        _repository.Update(saved);
        await _repository.SaveChangesAsync();

        var updated = await _context.CuentasBancarias.FindAsync(cuenta.Id);
        Assert.Equal("Modificada", updated!.Nombre);
    }
}
