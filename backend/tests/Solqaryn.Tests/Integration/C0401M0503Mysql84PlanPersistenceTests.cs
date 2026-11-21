using System.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// C04-01.M05.03: physical persistence, tenant isolation and transaction rollback.
/// Test-only integration, not a billing provider, HTTP endpoint or production grant.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0401M0503Mysql84PlanPersistenceTests
{
    [Fact]
    public async Task RealMysql84MembershipRollbackAndRevocationStayTenantScoped()
    {
        var raw = Environment.GetEnvironmentVariable("C0401_M0503_MYSQL_TEST")
            ?? throw new InvalidOperationException("C0401_M0503_EPHEMERAL_MYSQL84_REQUIRED");
        var server = new MySqlConnectionStringBuilder(raw);
        if (server.Server is not ("127.0.0.1" or "localhost") ||
            server.Port != 3306 || server.UserID != "root")
            throw new InvalidOperationException("C0401_M0503_LOOPBACK_ONLY");

        var isolated = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c0401_m0503_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated, m => m.MigrationsAssembly(
                "Solqaryn.Infrastructure.Migrations")).Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            await db.Database.OpenConnectionAsync();
            await using (var versionQuery = db.Database.GetDbConnection().CreateCommand())
            {
                versionQuery.CommandText = "SELECT VERSION()";
                var version = Convert.ToString(await versionQuery.ExecuteScalarAsync());
                Assert.True(version?.StartsWith("8.4.", StringComparison.Ordinal) == true,
                    "Test must execute against real MySQL 8.4");
            }
            await db.Database.CloseConnectionAsync();

            var tenantA = new Empresa("C0401 M0503 Tenant A");
            var tenantB = new Empresa("C0401 M0503 Tenant B");
            db.Set<Empresa>().AddRange(tenantA, tenantB);
            await db.SaveChangesAsync();
            var roleA = new Rol
            {
                Nombre = "C0401 M0503 A", NombreNormalizado = "C0401_M0503_A",
                Ambito = AmbitoAutorizacion.Empresa, EmpresaId = tenantA.Id, Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "C0401 M0503 B", NombreNormalizado = "C0401_M0503_B",
                Ambito = AmbitoAutorizacion.Empresa, EmpresaId = tenantB.Id, Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();
            var user = new Usuario
            {
                NombreUsuario = "c0401_m0503_" + Guid.NewGuid().ToString("N")[..10],
                NombreCompleto = "C0401 isolated persistence test",
                PasswordHash = "not-a-real-password",
                RolId = roleA.Id, Activo = true
            };
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.Add(new UsuarioEmpresa(user.Id, tenantA.Id, roleA.Id));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await using (var initial = new AppDbContext(options))
            {
                Assert.True(await initial.UsuarioEmpresas.AsNoTracking()
                    .AnyAsync(m => m.UsuarioId == user.Id &&
                                   m.EmpresaId == tenantA.Id && m.Activa));
                Assert.False(await initial.UsuarioEmpresas.AsNoTracking()
                    .AnyAsync(m => m.UsuarioId == user.Id &&
                                   m.EmpresaId == tenantB.Id && m.Activa));
            }

            // A real SQL rollback must undo tenant B, even after SaveChanges.
            await using (var tx = await db.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted))
            {
                db.UsuarioEmpresas.Add(new UsuarioEmpresa(user.Id, tenantB.Id, roleB.Id));
                await db.SaveChangesAsync();
                Assert.True(await db.UsuarioEmpresas.AsNoTracking()
                    .AnyAsync(m => m.UsuarioId == user.Id &&
                                   m.EmpresaId == tenantB.Id && m.Activa));
                await tx.RollbackAsync();
            }
            db.ChangeTracker.Clear();

            await using (var afterRollback = new AppDbContext(options))
            {
                Assert.False(await afterRollback.UsuarioEmpresas.AsNoTracking()
                    .AnyAsync(m => m.UsuarioId == user.Id &&
                                   m.EmpresaId == tenantB.Id && m.Activa));
                Assert.True(await afterRollback.UsuarioEmpresas.AsNoTracking()
                    .AnyAsync(m => m.UsuarioId == user.Id &&
                                   m.EmpresaId == tenantA.Id && m.Activa));
            }

            // A committed revocation must survive disposal and fresh DB reads.
            var activeA = await db.UsuarioEmpresas.SingleAsync(
                m => m.UsuarioId == user.Id && m.EmpresaId == tenantA.Id);
            activeA.Desactivar();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            await using (var revoked = new AppDbContext(options))
            {
                Assert.False(await revoked.UsuarioEmpresas.AsNoTracking()
                    .AnyAsync(m => m.UsuarioId == user.Id &&
                                   m.EmpresaId == tenantA.Id && m.Activa));
                Assert.False(await revoked.UsuarioEmpresas.AsNoTracking()
                    .AnyAsync(m => m.UsuarioId == user.Id &&
                                   m.EmpresaId == tenantB.Id && m.Activa));
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
