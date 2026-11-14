using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class AsientoContableWriterTests
{
    [Fact]
    public async Task CreateAsync_NumeroExistente_RetornaReplaySinAuditoria()
    {
        await using var db = CreateDb();
        var existing = AsientoContableApplicationService.CrearAggregate(new CrearAsientoContableDto
        {
            Concepto = "Asiento existente",
            Numero = "ASI-001",
            Detalles =
            [
                new() { CuentaContableId = 1, Debe = 100m, Haber = 0m },
                new() { CuentaContableId = 2, Debe = 0m, Haber = 100m }
            ]
        });
        db.AsientosContables.Add(existing);
        await db.SaveChangesAsync();

        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var writer = new AsientoContableWriter(db, auditoria.Object);

        var result = await writer.CreateAsync(new CrearAsientoContableDto
        {
            Concepto = "Reintento",
            Numero = "ASI-001",
            Detalles =
            [
                new() { CuentaContableId = 1, Debe = 100m, Haber = 0m },
                new() { CuentaContableId = 2, Debe = 0m, Haber = 100m }
            ]
        });

        Assert.False(result.Created);
        Assert.Equal(existing.Id, result.Id);
        Assert.Equal("ASI-001", result.Asiento.Numero);
        Assert.Equal(1, await db.AsientosContables.CountAsync());
        auditoria.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_CuentaInexistente_FallaAntesDePersistir()
    {
        await using var db = CreateDb();
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var writer = new AsientoContableWriter(db, auditoria.Object);

        var dto = new CrearAsientoContableDto
        {
            Concepto = "Cuenta inexistente",
            Detalles =
            [
                new() { CuentaContableId = 901, Debe = 50m, Haber = 0m },
                new() { CuentaContableId = 902, Debe = 0m, Haber = 50m }
            ]
        };

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => writer.CreateAsync(dto));
        Assert.Equal(0, await db.AsientosContables.CountAsync());
        auditoria.VerifyNoOtherCalls();
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
