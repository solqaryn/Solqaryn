using MySql.EntityFrameworkCore.Extensions;
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
            .UseMySQL(GetConnectionString(dbName), mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
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
            await Phase7MySqlTestDatabase.InitializeFreshAsync(setupContext);

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

            // Unicidad nueva: cada empresa tiene su propio tipo predeterminado.
            // Ambos participantes deben pertenecer a la misma empresa para que
            // el índice (EmpresaId, EsPredeterminadoUnico) controle la carrera.
            var empresaPrueba = new Empresa("Empresa concurrencia");
            setupContext.Set<Empresa>().Add(empresaPrueba);
            await setupContext.SaveChangesAsync();

            setupContext.TipoClientes.AddRange(tipoA, tipoB);
            setupContext.Entry(tipoA).Property<int?>("EmpresaId").CurrentValue = empresaPrueba.Id;
            setupContext.Entry(tipoB).Property<int?>("EmpresaId").CurrentValue = empresaPrueba.Id;
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
    [Fact]
    public async Task Predeterminado_puede_existir_en_dos_empresas_diferentes()
    {
        var dbName = $"test_company_defaults_{Guid.NewGuid():N}";
        await using var db = new AppDbContext(CreateOptions(dbName));
        try
        {
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var empresaA = new Empresa("Empresa A");
            var empresaB = new Empresa("Empresa B");
            db.Set<Empresa>().AddRange(empresaA, empresaB);
            await db.SaveChangesAsync();

            var tipoA = new TipoCliente
            {
                Codigo = "GENERAL", Nombre = "General", NombreNormalizado = "GENERAL",
                ColorHex = "#111111", Activo = true, EsPredeterminado = true
            };
            var tipoB = new TipoCliente
            {
                Codigo = "GENERAL", Nombre = "General", NombreNormalizado = "GENERAL",
                ColorHex = "#222222", Activo = true, EsPredeterminado = true
            };

            db.TipoClientes.AddRange(tipoA, tipoB);
            db.Entry(tipoA).Property<int?>("EmpresaId").CurrentValue = empresaA.Id;
            db.Entry(tipoB).Property<int?>("EmpresaId").CurrentValue = empresaB.Id;
            await db.SaveChangesAsync();

            Assert.Equal(2, await db.TipoClientes.AsNoTracking()
                .CountAsync(t => t.EsPredeterminado));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

}
