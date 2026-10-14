using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Las tareas globales legacy de startup nunca deben reparar usuarios ni
/// crear impuestos sin tenant cuando hay dos empresas (incluso si B está inactiva).
/// MySQL efímero; ninguna instancia de DEV/QA/PROD se modifica.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StartupLegacyJobsMultiEmpresaIsolationIntegrationTests
{
    [Fact]
    public async Task ConDosEmpresas_StartupNoCambiaUsuariosHistoricosNiSiembraImpuestosGlobales()
    {
        var root = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL efímero de CI no configurado.");
        var dbName = "p4_jobs_ab_" + Guid.NewGuid().ToString("N");
        var connectionString = new MySqlConnectionStringBuilder(root)
        {
            Database = dbName
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connectionString,
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int usuarioId;
            await using (var db = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
                var empresaA = new Empresa("Job Empresa A");
                var empresaB = new Empresa("Job Empresa B");
                empresaB.Desactivar();
                db.Set<Empresa>().AddRange(empresaA, empresaB);

                var rol = new Rol
                {
                    Nombre = "Job Rol plantilla",
                    NombreNormalizado = "JOB_ROL_PLANTILLA",
                    EsSistema = true,
                    Activo = true
                };
                db.Roles.Add(rol);
                await db.SaveChangesAsync();

                var usuario = new Usuario
                {
                    NombreUsuario = "job-legacy-" + Guid.NewGuid().ToString("N"),
                    NombreCompleto = "Usuario histórico ficticio",
                    PasswordHash = "hash-inerte-de-prueba",
                    RolId = rol.Id,
                    Activo = true,
                    Eliminado = true
                };
                db.Usuarios.Add(usuario);
                await db.SaveChangesAsync();
                usuarioId = usuario.Id;

                Assert.Equal(2, await db.Set<Empresa>().CountAsync());
                Assert.True(await db.Usuarios.IgnoreQueryFilters()
                    .Where(u => u.Id == usuarioId).Select(u => u.Eliminado).SingleAsync());

                // Estos mismos seeds se ejecutan durante el arranque real.
                var result = await new ProductionDataRepairService(db).RepairAsync();
                await new SeedFiscalService(db).SeedDefaultsAsync();

                Assert.Equal(2, result.EmpresasTotales);
                Assert.Null(result.EmpresaMaterializadaId);
                Assert.Equal(0, result.MembresiasLegacyCreadas);
                Assert.Equal(0, await db.UsuarioEmpresas.IgnoreQueryFilters().CountAsync());
                Assert.Equal(0, await db.Impuestos.IgnoreQueryFilters().CountAsync());
            }

            // Abrir nuevo DbContext para verificar que no solo era un cambio
            // en el ChangeTracker: en MySQL el usuario sigue eliminado.
            await using var verifica = new AppDbContext(options);
            var eliminado = await verifica.Usuarios.IgnoreQueryFilters()
                .AsNoTracking()
                .Where(u => u.Id == usuarioId)
                .Select(u => u.Eliminado)
                .SingleAsync();
            Assert.True(eliminado);
            Assert.Equal(0, await verifica.Impuestos.IgnoreQueryFilters().CountAsync());
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
