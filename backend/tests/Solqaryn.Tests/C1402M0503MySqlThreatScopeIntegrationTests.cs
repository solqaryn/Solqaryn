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

/// <summary>C14-02.M05.03: real, isolated Oracle EF/MySQL 8.4 authority contract.
/// Does not authenticate signed JWT, HTTP requests or production tenants.</summary>
[Trait("Category", "Integration")]
public sealed class C1402M0503MySqlThreatScopeIntegrationTests
{
    private static MySqlConnectionStringBuilder LocalOnly(string connection)
    {
        var parsed = new MySqlConnectionStringBuilder(connection);
        if (parsed.Server is not ("127.0.0.1" or "localhost") || parsed.Port != 3306)
            throw new InvalidOperationException("Only ephemeral localhost:3306 MySQL is permitted.");
        return parsed;
    }

    [Theory]
    [InlineData("Server=mysql.prod.example;Port=3306;User=root;Password=fixture;")]
    [InlineData("Server=127.0.0.1;Port=3307;User=root;Password=fixture;")]
    public void RefusesRemoteOrUnexpectedDatabasePort(string connection) =>
        Assert.Throws<InvalidOperationException>(() => LocalOnly(connection));

    private static async Task<UsuarioPlataformaScopeActual?> ScopeAsync(
        AppDbContext db, int id, string name, bool authenticated = true)
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns((int?)id);
        current.SetupGet(x => x.NombreUsuario).Returns(name);
        current.SetupGet(x => x.EstaAutenticado).Returns(authenticated);
        return await new PlataformaScopeService(db, current.Object).ObtenerActualAsync();
    }

    [Fact]
    public async Task MySql84_PersistedRoleTenantIdentityDuplicateRollbackAndRevocation()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral MySQL service is mandatory.");
        var isolated = new MySqlConnectionStringBuilder(LocalOnly(raw).ConnectionString)
        {
            Database = "v20_c1402_m0503_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        const string platformName = "v20-c1402-global";
        const string companyName = "v20-c1402-company";
        try
        {
            int platformId, companyId, secondRoleId;
            await using (var db = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
                var tenantA = new Empresa("C1402 integration A");
                var tenantB = new Empresa("C1402 integration B");
                db.Set<Empresa>().AddRange(tenantA, tenantB);
                await db.SaveChangesAsync();
                Assert.NotEqual(tenantA.Id, tenantB.Id);
                var platformRole = new Rol {
                    Nombre = "C1402 Platform 1", NombreNormalizado = "C1402_PLATFORM_1",
                    Ambito = AmbitoAutorizacion.Plataforma, EmpresaId = null,
                    EsSistema = true, EsAdministrador = true, Activo = true
                };
                var secondRole = new Rol {
                    Nombre = "C1402 Platform 2", NombreNormalizado = "C1402_PLATFORM_2",
                    Ambito = AmbitoAutorizacion.Plataforma, EmpresaId = null,
                    EsSistema = true, EsAdministrador = true, Activo = true
                };
                var companyRole = new Rol {
                    Nombre = "C1402 Company", NombreNormalizado = "C1402_COMPANY",
                    Ambito = AmbitoAutorizacion.Empresa, EmpresaId = tenantA.Id,
                    EsSistema = false, EsAdministrador = true, Activo = true
                };
                db.Roles.AddRange(platformRole, secondRole, companyRole);
                await db.SaveChangesAsync();
                secondRoleId = secondRole.Id;
                var platform = new Usuario {
                    NombreUsuario = platformName, NombreCompleto = "Platform fixture",
                    PasswordHash = "non-authenticatable-fixture", RolId = companyRole.Id, Activo = true
                };
                var company = new Usuario {
                    NombreUsuario = companyName, NombreCompleto = "Company fixture",
                    PasswordHash = "non-authenticatable-fixture", RolId = companyRole.Id, Activo = true
                };
                db.Usuarios.AddRange(platform, company);
                await db.SaveChangesAsync();
                platformId = platform.Id;
                companyId = company.Id;
                db.Set<UsuarioRolPlataforma>().Add(new UsuarioRolPlataforma(platformId, platformRole.Id));
                db.Set<UsuarioEmpresa>().Add(new UsuarioEmpresa(companyId, tenantA.Id, companyRole.Id));
                await db.SaveChangesAsync();
                Assert.Empty(await db.UsuarioEmpresas.IgnoreQueryFilters()
                    .Where(x => x.UsuarioId == platformId).ToListAsync());
            }

            await using (var db = new AppDbContext(options))
            {
                var allowed = await ScopeAsync(db, platformId, platformName);
                Assert.NotNull(allowed);
                Assert.Equal(platformId, allowed!.UsuarioId);
                Assert.Null(await ScopeAsync(db, companyId, companyName));
                Assert.Null(await ScopeAsync(db, platformId, companyName));
                Assert.Null(await ScopeAsync(db, platformId, platformName, false));
            }
            // Test the duplicate active assignment in the very same transaction.
            // Rollback must restore a single trusted role on a fresh connection.
            await using (var db = new AppDbContext(options))
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                db.Set<UsuarioRolPlataforma>().Add(new UsuarioRolPlataforma(platformId, secondRoleId));
                await db.SaveChangesAsync();
                Assert.Equal(2, await db.Set<UsuarioRolPlataforma>().IgnoreQueryFilters()
                    .CountAsync(x => x.UsuarioId == platformId && x.Activa));
                Assert.Null(await ScopeAsync(db, platformId, platformName));
                await transaction.RollbackAsync();
            }
            await using (var db = new AppDbContext(options))
            {
                Assert.NotNull(await ScopeAsync(db, platformId, platformName));
                Assert.Equal(1, await db.Set<UsuarioRolPlataforma>().IgnoreQueryFilters()
                    .CountAsync(x => x.UsuarioId == platformId && x.Activa));
            }
            await using (var db = new AppDbContext(options))
            {
                var assignment = await db.Set<UsuarioRolPlataforma>().IgnoreQueryFilters()
                    .SingleAsync(x => x.UsuarioId == platformId && x.Activa);
                assignment.Desactivar();
                await db.SaveChangesAsync();
            }
            await using (var db = new AppDbContext(options))
            {
                Assert.Null(await ScopeAsync(db, platformId, platformName));
                Assert.Equal(0, await db.Set<UsuarioRolPlataforma>().IgnoreQueryFilters()
                    .CountAsync(x => x.UsuarioId == platformId && x.Activa));
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
