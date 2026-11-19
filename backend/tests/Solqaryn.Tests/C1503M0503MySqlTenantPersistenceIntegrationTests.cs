using Microsoft.EntityFrameworkCore;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C15-03.M05.03: Oracle EF / MySQL 8.4 EFIMERO, persistencia real de
/// frontera tenant A/B, revocacion y rollback. No prueba API/JWT ni PROD.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1503M0503MySqlTenantPersistenceIntegrationTests
{
    private static MySqlConnectionStringBuilder LocalOnly(string raw)
    {
        var parsed = new MySqlConnectionStringBuilder(raw);
        if (parsed.Server is not ("127.0.0.1" or "localhost") || parsed.Port != 3306)
            throw new InvalidOperationException("No production, remote host or alternative port");
        return parsed;
    }

    [Theory]
    [InlineData("Server=mysql.production.invalid;Port=3306;User=root;Password=test;")]
    [InlineData("Server=127.0.0.1;Port=3307;User=root;Password=test;")]
    public void RechazaProveedorNoAislado(string connection) =>
        Assert.Throws<InvalidOperationException>(() => LocalOnly(connection));

    private static async Task<UsuarioPlataformaScopeActual?> GlobalAsync(
        AppDbContext db, int userId, string name)
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns((int?)userId);
        current.SetupGet(x => x.NombreUsuario).Returns(name);
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        return await new PlataformaScopeService(db, current.Object).ObtenerActualAsync();
    }

    [Fact]
    public async Task MySql84_TenantMembershipRevocationRollbackAndNoGlobalEscalation()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Local ephemeral MySQL mandatory");
        var connection = new MySqlConnectionStringBuilder(LocalOnly(raw).ConnectionString)
        {
            Database = "v20_c1503_m0503_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int companyAId, companyBId, platformId, tenantAId, tenantBId, roleAId, roleBId;
            await using (var db = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
                var a = new Empresa("C1503 isolated A");
                var b = new Empresa("C1503 isolated B");
                db.Set<Empresa>().AddRange(a, b);
                await db.SaveChangesAsync();
                tenantAId = a.Id;
                tenantBId = b.Id;
                Assert.NotEqual(tenantAId, tenantBId);

                var roleA = new Rol {
                    Nombre = "C1503 Empresa A", NombreNormalizado = "C1503_EMPRESA_A",
                    Ambito = AmbitoAutorizacion.Empresa, EmpresaId = tenantAId,
                    EsSistema = false, EsAdministrador = true, Activo = true
                };
                var roleB = new Rol {
                    Nombre = "C1503 Empresa B", NombreNormalizado = "C1503_EMPRESA_B",
                    Ambito = AmbitoAutorizacion.Empresa, EmpresaId = tenantBId,
                    EsSistema = false, EsAdministrador = true, Activo = true
                };
                var global = new Rol {
                    Nombre = "C1503 Global", NombreNormalizado = "C1503_GLOBAL",
                    Ambito = AmbitoAutorizacion.Plataforma, EmpresaId = null,
                    EsSistema = true, EsAdministrador = true, Activo = true
                };
                db.Roles.AddRange(roleA, roleB, global);
                await db.SaveChangesAsync();
                roleAId = roleA.Id;
                roleBId = roleB.Id;

                var companyA = new Usuario {
                    NombreUsuario = "c1503-company-a", NombreCompleto = "Company A",
                    PasswordHash = "not-authenticatable", RolId = roleAId, Activo = true
                };
                var companyB = new Usuario {
                    NombreUsuario = "c1503-company-b", NombreCompleto = "Company B",
                    PasswordHash = "not-authenticatable", RolId = roleBId, Activo = true
                };
                var platform = new Usuario {
                    NombreUsuario = "c1503-platform", NombreCompleto = "Platform",
                    PasswordHash = "not-authenticatable", RolId = roleAId, Activo = true
                };
                db.Usuarios.AddRange(companyA, companyB, platform);
                await db.SaveChangesAsync();
                companyAId = companyA.Id;
                companyBId = companyB.Id;
                platformId = platform.Id;
                db.Set<UsuarioEmpresa>().AddRange(
                    new UsuarioEmpresa(companyAId, tenantAId, roleAId),
                    new UsuarioEmpresa(companyBId, tenantBId, roleBId));
                db.Set<UsuarioRolPlataforma>().Add(new UsuarioRolPlataforma(platformId, global.Id));
                await db.SaveChangesAsync();
            }

            // A/B persisten en conexiones/contextos reales, sin grants por el rol legacy.
            await using (var db = new AppDbContext(options))
            {
                var a = await db.UsuarioEmpresas.IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(x => x.UsuarioId == companyAId && x.Activa);
                var b = await db.UsuarioEmpresas.IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(x => x.UsuarioId == companyBId && x.Activa);
                Assert.Equal(tenantAId, a.EmpresaId);
                Assert.Equal(tenantBId, b.EmpresaId);
                Assert.Equal(roleAId, a.ObtenerRolEfectivo(companyAId, tenantAId));
                Assert.Equal(roleBId, b.ObtenerRolEfectivo(companyBId, tenantBId));
                Assert.Throws<InvalidOperationException>(() => a.ObtenerRolEfectivo(companyAId, tenantBId));
                Assert.Throws<InvalidOperationException>(() => b.ObtenerRolEfectivo(companyBId, tenantAId));
                Assert.Null(await GlobalAsync(db, companyAId, "c1503-company-a"));
                Assert.Null(await GlobalAsync(db, companyBId, "c1503-company-b"));
                Assert.NotNull(await GlobalAsync(db, platformId, "c1503-platform"));
            }

            // Un grant cruzado dentro de una transaccion que falla no debe persistir.
            await using (var db = new AppDbContext(options))
            {
                await using var tx = await db.Database.BeginTransactionAsync();
                db.Set<UsuarioEmpresa>().Add(new UsuarioEmpresa(companyAId, tenantBId, roleBId));
                await db.SaveChangesAsync();
                Assert.Single(await db.UsuarioEmpresas.IgnoreQueryFilters()
                    .Where(x => x.UsuarioId == companyAId && x.EmpresaId == tenantBId).ToListAsync());
                await tx.RollbackAsync();
            }
            await using (var db = new AppDbContext(options))
            {
                Assert.Empty(await db.UsuarioEmpresas.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.UsuarioId == companyAId && x.EmpresaId == tenantBId).ToListAsync());
                var a = await db.UsuarioEmpresas.IgnoreQueryFilters()
                    .SingleAsync(x => x.UsuarioId == companyAId && x.EmpresaId == tenantAId);
                a.Desactivar();
                await db.SaveChangesAsync();
            }
            await using (var db = new AppDbContext(options))
            {
                var a = await db.UsuarioEmpresas.IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(x => x.UsuarioId == companyAId && x.EmpresaId == tenantAId);
                Assert.False(a.Activa);
                Assert.Throws<InvalidOperationException>(() => a.ObtenerRolEfectivo(companyAId, tenantAId));
                var b = await db.UsuarioEmpresas.IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(x => x.UsuarioId == companyBId && x.EmpresaId == tenantBId);
                Assert.True(b.Activa);
                Assert.Equal(roleBId, b.ObtenerRolEfectivo(companyBId, tenantBId));
                Assert.Null(await GlobalAsync(db, companyAId, "c1503-company-a"));
                Assert.NotNull(await GlobalAsync(db, platformId, "c1503-platform"));
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
