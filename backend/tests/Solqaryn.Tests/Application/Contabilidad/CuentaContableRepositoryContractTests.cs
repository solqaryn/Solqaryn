using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests.Application.Contabilidad;

public class CuentaContableRepositoryContractTests
{
    private readonly AppDbContext _context;
    private readonly ICuentaContableRepository _repository;

    public CuentaContableRepositoryContractTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new CuentaContableRepository(_context);
    }

    [Fact]
    public async Task AddAsync_AddsCuentaContable_Successfully()
    {
        var cuenta = new CuentaContable
        {
            Codigo = "1000",
            Nombre = "Activos",
            Tipo = TipoCuentaContable.Activo
        };

        await _repository.AddAsync(cuenta);
        await _repository.SaveChangesAsync();

        var savedCuenta = await _repository.GetByIdAsync(cuenta.Id);
        Assert.NotNull(savedCuenta);
        Assert.Equal("1000", savedCuenta.Codigo);
    }

    [Fact]
    public async Task GetByCodigoAsync_ReturnsCuenta_WhenExists()
    {
        var cuenta = new CuentaContable
        {
            Codigo = "2000",
            Nombre = "Pasivos",
            Tipo = TipoCuentaContable.Pasivo
        };
        await _context.Set<CuentaContable>().AddAsync(cuenta);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByCodigoAsync("2000");

        Assert.NotNull(result);
        Assert.Equal("Pasivos", result.Nombre);
    }

    [Fact]
    public async Task GetRaicesAsync_ReturnsOnlyRootAccounts()
    {
        var raiz = new CuentaContable
        {
            Codigo = "3000",
            Nombre = "Patrimonio",
            Tipo = TipoCuentaContable.Patrimonio
        };
        await _context.Set<CuentaContable>().AddAsync(raiz);
        await _context.SaveChangesAsync();

        var subcuenta = new CuentaContable
        {
            Codigo = "3100",
            Nombre = "Capital Social",
            Tipo = TipoCuentaContable.Patrimonio,
            CuentaPadreId = raiz.Id
        };
        await _context.Set<CuentaContable>().AddAsync(subcuenta);
        await _context.SaveChangesAsync();

        var raices = await _repository.GetRaicesAsync();

        Assert.Single(raices);
        Assert.Equal("3000", raices.First().Codigo);
        Assert.Single(raices.First().Subcuentas);
    }
}
