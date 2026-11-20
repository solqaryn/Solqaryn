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
/// C02-01.M05.03: production UsuarioScopeService queries against disposable
/// Oracle EF / MySQL 8.4. No HTTP/JWT authority is certified by this fixture.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0201M0503MySqlTenantScopeIntegrationTests
{
    [Fact]
    public async Task RealMysqlQueriesRequireFreshTenantMembershipAndMatchingRole()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral MySQL CI connection is required");
        var source = new MySqlConnectionStringBuilder(raw);
        if (source.Server is not ("127.0.0.1" or "localhost"))
            throw new InvalidOperationException("Only loopback MySQL CI is permitted");
        var connection = new MySqlConnectionStringBuilder(raw)
        {
            Database = "v22_c0201_m0503_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, o => o.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);

            var a = new Empresa("V22 M0503 Empresa A");
            var b = new Empresa("V22 M0503 Empresa B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();

            var roleA = new Rol
            {
                Nombre = "V22 scope A",
                NombreNormalizado = "V22_M0503_A",
                EmpresaId = a.Id,
                Ambito = AmbitoAutorizacion.Empresa,
                Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "V22 scope B",
                NombreNormalizado = "V22_M0503_B",
                EmpresaId = b.Id,
                Ambito = AmbitoAutorizacion.Empresa,
                Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();

            var actor = new Usuario
            {
                NombreUsuario = "v22_m0503_" + Guid.NewGuid().ToString("N")[..12],
                NombreCompleto = "Scoped integration actor",
                PasswordHash = "unusable-test-only",
                RolId = roleA.Id,
                Activo = true
            };
            db.Usuarios.Add(actor);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.AddRange(
                new UsuarioEmpresa(actor.Id, a.Id, roleA.Id),
                new UsuarioEmpresa(actor.Id, b.Id, roleB.Id));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
            current.SetupGet(x => x.EstaAutenticado).Returns(true);
            current.SetupGet(x => x.UsuarioId).Returns(actor.Id);
            var scope = new UsuarioScopeService(db, current.Object);

            // Two current memberships must never auto-select the wrong company.
            Assert.Null(await scope.ObtenerUnicoActualAsync());
            var selected = await scope.ListarEmpresasSeleccionablesAsync();
            Assert.Equal(new[] { a.Id, b.Id }.OrderBy(x => x),
                selected.Select(x => x.EmpresaId).OrderBy(x => x));
            Assert.Equal(a.Id, (await scope.ObtenerActualAsync(a.Id))?.EmpresaId);
            Assert.Equal(b.Id, (await scope.ObtenerActualAsync(b.Id))?.EmpresaId);
            Assert.Null(await scope.ObtenerActualAsync(0));
            Assert.Null(await scope.ObtenerActualAsync(int.MaxValue));

            // DB-persisted B revocation is observed on subsequent SQL queries.
            var memberB = await db.UsuarioEmpresas.SingleAsync(x =>
                x.UsuarioId == actor.Id && x.EmpresaId == b.Id);
            memberB.Desactivar();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Null(await scope.ObtenerActualAsync(b.Id));
            Assert.Equal(a.Id, (await scope.ObtenerUnicoActualAsync())?.EmpresaId);
            Assert.Equal(a.Id, Assert.Single(await scope.ListarEmpresasSeleccionablesAsync()).EmpresaId);

            // Cross-tenant role B must not authorize membership A.
            var memberA = await db.UsuarioEmpresas.SingleAsync(x =>
                x.UsuarioId == actor.Id && x.EmpresaId == a.Id);
            memberA.CambiarRol(roleB.Id);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Null(await scope.ObtenerActualAsync(a.Id));
            Assert.Null(await scope.ObtenerUnicoActualAsync());
            Assert.Empty(await scope.ListarEmpresasSeleccionablesAsync());

            // Recover A's role, then block the user: no stale grant survives.
            memberA = await db.UsuarioEmpresas.SingleAsync(x =>
                x.UsuarioId == actor.Id && x.EmpresaId == a.Id);
            memberA.CambiarRol(roleA.Id);
            var user = await db.Usuarios.SingleAsync(x => x.Id == actor.Id);
            user.Bloqueado = true;
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Null(await scope.ObtenerActualAsync(a.Id));
            Assert.Empty(await scope.ListarEmpresasSeleccionablesAsync());

            var anonymous = new Mock<ICurrentUserService>(MockBehavior.Strict);
            anonymous.SetupGet(x => x.EstaAutenticado).Returns(false);
            var noJwt = new UsuarioScopeService(db, anonymous.Object);
            Assert.Null(await noJwt.ObtenerUnicoActualAsync());
            Assert.Empty(await noJwt.ListarEmpresasSeleccionablesAsync());
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
