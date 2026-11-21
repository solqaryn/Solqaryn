using System.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>C04-01.M06.04: physical isolated MySQL 8.4 rollback and safe disable.</summary>
[Trait("Category", "Integration")]
public sealed class C0401M0604RollbackPreservationMySql84Tests
{
    private static MySqlConnectionStringBuilder LocalOnly(string raw)
    {
        var parsed = new MySqlConnectionStringBuilder(raw);
        if (parsed.Server is not ("127.0.0.1" or "localhost") ||
            parsed.Port != 3306 || parsed.UserID != "root")
            throw new InvalidOperationException("C0401_M0604_EPHEMERAL_LOOPBACK_ROOT_ONLY");
        return parsed;
    }

    [Theory]
    [InlineData("Server=remote.invalid;Port=3306;Database=customer;User=root;Password=synthetic;")]
    [InlineData("Server=localhost;Port=3307;Database=customer;User=root;Password=synthetic;")]
    [InlineData("Server=127.0.0.1;Port=3306;Database=customer;User=application;Password=synthetic;")]
    public void UnsafeDatabaseTargetIsRejectedBeforeConnection(string raw)
    {
        Assert.Throws<InvalidOperationException>(() => LocalOnly(raw));
    }

    [Fact]
    public async Task PhysicalRollbackAndDisablePreserveBothLegitimateTenants()
    {
        var raw = Environment.GetEnvironmentVariable("C0401_M0604_MYSQL")
            ?? throw new InvalidOperationException("C0401_M0604_EPHEMERAL_MYSQL84_REQUIRED");
        var isolated = LocalOnly(raw);
        isolated.Database = "c0401_m0604_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated.ConnectionString,
                x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;

        try
        {
            int aId, bId, userId, provisionalId;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                await setup.Database.OpenConnectionAsync();
                await using (var cmd = setup.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = "SELECT VERSION()";
                    var version = Convert.ToString(await cmd.ExecuteScalarAsync());
                    Assert.True(version?.StartsWith("8.4.", StringComparison.Ordinal) == true,
                        "Real MySQL 8.4 required");
                }
                await setup.Database.CloseConnectionAsync();

                var tenantA = new Empresa("C0401 M0604 legitimate A");
                var tenantB = new Empresa("C0401 M0604 legitimate B");
                setup.Set<Empresa>().AddRange(tenantA, tenantB);
                await setup.SaveChangesAsync();
                aId = tenantA.Id;
                bId = tenantB.Id;
                Assert.NotEqual(aId, bId);
                var roleA = new Rol
                {
                    Nombre = "C0401 M0604 A", NombreNormalizado = "C0401_M0604_A",
                    Ambito = AmbitoAutorizacion.Empresa, EmpresaId = aId, Activo = true
                };
                var roleB = new Rol
                {
                    Nombre = "C0401 M0604 B", NombreNormalizado = "C0401_M0604_B",
                    Ambito = AmbitoAutorizacion.Empresa, EmpresaId = bId, Activo = true
                };
                setup.Roles.AddRange(roleA, roleB);
                await setup.SaveChangesAsync();
                var user = new Usuario
                {
                    NombreUsuario = "c0401_m0604_" + Guid.NewGuid().ToString("N")[..10],
                    NombreCompleto = "C0401 legitimate preservation proof",
                    PasswordHash = "not-a-live-secret", RolId = roleA.Id, Activo = true
                };
                setup.Usuarios.Add(user);
                await setup.SaveChangesAsync();
                userId = user.Id;
                setup.UsuarioEmpresas.AddRange(
                    new UsuarioEmpresa(userId, aId, roleA.Id),
                    new UsuarioEmpresa(userId, bId, roleB.Id));
                await setup.SaveChangesAsync();
            }

            // Roll back an attempted disable of B; verify actual SQL from a new context.
            await using (var rollback = new AppDbContext(options))
            {
                await using var tx = await rollback.Database.BeginTransactionAsync(
                    IsolationLevel.ReadCommitted);
                var memberB = await rollback.UsuarioEmpresas.SingleAsync(
                    m => m.UsuarioId == userId && m.EmpresaId == bId);
                memberB.Desactivar();
                await rollback.SaveChangesAsync();
                Assert.False(await rollback.UsuarioEmpresas.AsNoTracking().AnyAsync(
                    m => m.UsuarioId == userId && m.EmpresaId == bId && m.Activa));
                await tx.RollbackAsync();
            }
            await using (var verified = new AppDbContext(options))
            {
                Assert.True(await verified.UsuarioEmpresas.AsNoTracking().AnyAsync(
                    m => m.UsuarioId == userId && m.EmpresaId == aId && m.Activa));
                Assert.True(await verified.UsuarioEmpresas.AsNoTracking().AnyAsync(
                    m => m.UsuarioId == userId && m.EmpresaId == bId && m.Activa));
            }

            // Uncommitted tenant C must disappear on transaction disposal.
            await using (var aborted = new AppDbContext(options))
            {
                await using (var tx = await aborted.Database.BeginTransactionAsync())
                {
                    var provisional = new Empresa("C0401 M0604 transient tenant");
                    aborted.Set<Empresa>().Add(provisional);
                    await aborted.SaveChangesAsync();
                    provisionalId = provisional.Id;
                    Assert.True(provisionalId > 0);
                }
            }
            await using (var verified = new AppDbContext(options))
            {
                Assert.False(await verified.Set<Empresa>().AsNoTracking()
                    .AnyAsync(x => x.Id == provisionalId));
            }

            // Committed disable B must not delete B company, account, A, or B row.
            await using (var disable = new AppDbContext(options))
            {
                var b = await disable.UsuarioEmpresas.SingleAsync(
                    m => m.UsuarioId == userId && m.EmpresaId == bId);
                b.Desactivar();
                await disable.SaveChangesAsync();
            }
            await using (var verified = new AppDbContext(options))
            {
                Assert.True(await verified.Set<Empresa>().AsNoTracking()
                    .AnyAsync(x => x.Id == aId));
                Assert.True(await verified.Set<Empresa>().AsNoTracking()
                    .AnyAsync(x => x.Id == bId));
                Assert.True(await verified.Usuarios.AsNoTracking()
                    .AnyAsync(x => x.Id == userId && x.Activo));
                Assert.True(await verified.UsuarioEmpresas.AsNoTracking()
                    .AnyAsync(m => m.UsuarioId == userId && m.EmpresaId == aId && m.Activa));
                Assert.True(await verified.UsuarioEmpresas.AsNoTracking()
                    .AnyAsync(m => m.UsuarioId == userId && m.EmpresaId == bId && !m.Activa));
                Assert.Equal(2, await verified.UsuarioEmpresas.AsNoTracking()
                    .CountAsync(m => m.UsuarioId == userId &&
                        (m.EmpresaId == aId || m.EmpresaId == bId)));
            }
        }
        finally
        {
            // Only the newly generated temporary database may be removed.
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}