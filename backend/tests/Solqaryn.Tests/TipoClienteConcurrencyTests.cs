using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public class TipoClienteConcurrencyTests
{
    private static string GetConnectionString(string dbName) =>
        $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;";

    private DbContextOptions<AppDbContext> CreateOptions(string dbName)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(GetConnectionString(dbName), new MySqlServerVersion(new Version(8, 4, 3)))
            .Options;
    }

    [Fact]
    public async Task Concurrency_MarcarPredeterminado_ConDosContextosIndependientes_SoloUnGanador()
    {
        var dbName = $"test_concurrency_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);

        // 1. Setup inicial con Migraciones reales
        await using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync();

            var tipoA = new TipoCliente
            {
                Codigo = "TIPO_A",
                Nombre = "Tipo A",
                NombreNormalizado = "TIPO A",
                ColorHex = "#111111",
                Activo = true,
                EsPredeterminado = false,
                EsSistema = false
            };

            var tipoB = new TipoCliente
            {
                Codigo = "TIPO_B",
                Nombre = "Tipo B",
                NombreNormalizado = "TIPO B",
                ColorHex = "#222222",
                Activo = true,
                EsPredeterminado = false,
                EsSistema = false
            };

            setupContext.TipoClientes.AddRange(tipoA, tipoB);
            await setupContext.SaveChangesAsync();
        }

        int idA, idB;
        await using (var setupContext = new AppDbContext(options))
        {
            var tipos = await setupContext.TipoClientes.AsNoTracking().ToListAsync();
            idA = tipos.First(t => t.Codigo == "TIPO_A").Id;
            idB = tipos.First(t => t.Codigo == "TIPO_B").Id;
        }

        // 2. Crear dos pilares de infraestructura completamente independientes
        await using var contextA = new AppDbContext(options);
        await using var contextB = new AppDbContext(options);

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(c => c.UsuarioId).Returns(1);
        userMock.Setup(c => c.NombreUsuario).Returns("concurrency_user");

        var auditoriaMockA = new Mock<IAuditoriaService>();
        var auditoriaMockB = new Mock<IAuditoriaService>();

        var serviceA = new TipoClienteService(
            new TipoClienteRepository(contextA),
            userMock.Object,
            auditoriaMockA.Object,
            new UnitOfWork(contextA));

        var serviceB = new TipoClienteService(
            new TipoClienteRepository(contextB),
            userMock.Object,
            auditoriaMockB.Object,
            new UnitOfWork(contextB));

        var updateDtoA = new UpdateTipoClienteDto { Nombre = "Tipo A", ColorHex = "#111111", Activo = true, EsPredeterminado = true };
        var updateDtoB = new UpdateTipoClienteDto { Nombre = "Tipo B", ColorHex = "#222222", Activo = true, EsPredeterminado = true };

        // 3. Ejecución concurrente sincrónica usando una barrera de inicio
        var barrier = new TaskCompletionSource<bool>();

        var taskA = Task.Run(async () =>
        {
            await barrier.Task;
            return await serviceA.UpdateAsync(idA, updateDtoA);
        });

        var taskB = Task.Run(async () =>
        {
            await barrier.Task;
            return await serviceB.UpdateAsync(idB, updateDtoB);
        });

        // Disparar ambas tareas en paralelo exacto
        barrier.SetResult(true);

        Exception? exA = null;
        Exception? exB = null;

        try { await taskA; } catch (Exception ex) { exA = ex; }
        try { await taskB; } catch (Exception ex) { exB = ex; }

        // 4. Verificación con un 3er Contexto Independiente
        await using var verifyContext = new AppDbContext(options);
        try
        {
            var dbTipos = await verifyContext.TipoClientes.AsNoTracking().ToListAsync();
            var predeterminados = dbTipos.Where(t => t.EsPredeterminado).ToList();

            // Debe haber exactamente un solo predeterminado
            Assert.Single(predeterminados);

            // El scheduler puede serializar las dos operaciones: en ese caso ambas
            // pueden completarse, pero la restricción persistida debe dejar un solo ganador.
            Assert.True(exA is null || exB is null, "Al menos una operación debe completarse.");
            foreach (var error in new[] { exA, exB }.Where(error => error is not null))
            {
                Assert.IsType<BusinessRuleException>(error);
                Assert.Contains("Conflicto de concurrencia", error!.Message);
            }
            Assert.Contains(predeterminados[0].Id, new[] { idA, idB });
        }
        finally
        {
            // Destruir la base de datos temporal
            await verifyContext.Database.EnsureDeletedAsync();
        }
    }
}
