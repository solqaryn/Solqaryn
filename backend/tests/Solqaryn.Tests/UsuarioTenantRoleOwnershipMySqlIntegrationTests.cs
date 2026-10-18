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
/// MySQL físico: la FK de RolId no sustituye el control RBAC del ámbito
/// de plataforma ni de EmpresaId del rol al consultar una membresía.
/// Nunca utiliza ni borra una base de DEV/Aiven.
/// </summary>
[Trait("Category", "Integration")]
public sealed class UsuarioTenantRoleOwnershipMySqlIntegrationTests
{
    [Fact]
    public async Task MembresiaDeA_ConRolDeB_OPlataforma_NoObtienePermisos_Empresa()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Esta prueba exige MySQL efímero.");
        var origen = new MySqlConnectionStringBuilder(raw);
        if (!string.Equals(origen.Server, "127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(origen.Server, "localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Se rechaza probar aislamiento en MySQL remoto o Aiven.");

        var connection = new MySqlConnectionStringBuilder(raw)
        {
            Database = "p4_role_owner_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, x =>
                x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);

            var a = new Empresa("Roles A");
            var b = new Empresa("Roles B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();

            var roleB = new Rol
            {
                Nombre = "P4 ROL B",
                NombreNormalizado = "P4_ROL_B",
                Ambito = AmbitoAutorizacion.Empresa,
                EmpresaId = b.Id, Activo = true
            };
            var rolePlatform = new Rol
            {
                Nombre = "P4 PLATAFORMA",
                NombreNormalizado = "P4_PLATAFORMA",
                Ambito = AmbitoAutorizacion.Plataforma,
                EmpresaId = null, EsAdministrador = true, Activo = true
            };
            var template = new Rol
            {
                Nombre = "P4 PLANTILLA",
                NombreNormalizado = "P4_PLANTILLA",
                Ambito = AmbitoAutorizacion.Empresa,
                EmpresaId = null, EsSistema = true, Activo = true
            };
            db.Roles.AddRange(roleB, rolePlatform, template);
            await db.SaveChangesAsync();

            var user = new Usuario
            {
                NombreUsuario = "p4-role-owner",
                NombreCompleto = "Usuario de prueba P4",
                PasswordHash = "no-es-credencial-real",
                RolId = roleB.Id, Activo = true
            };
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();

            var badA = new UsuarioEmpresa(user.Id, a.Id, roleB.Id);
            var correctB = new UsuarioEmpresa(user.Id, b.Id, roleB.Id);
            db.UsuarioEmpresas.AddRange(badA, correctB);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var current = new Mock<ICurrentUserService>();
            current.SetupGet(x => x.EstaAutenticado).Returns(true);
            current.SetupGet(x => x.UsuarioId).Returns(user.Id);
            var scope = new UsuarioScopeService(db, current.Object);

            Assert.Null(await scope.ObtenerActualAsync(a.Id));
            Assert.Equal(roleB.Id, (await scope.ObtenerActualAsync(b.Id))?.RolId);

            var membershipA = await db.UsuarioEmpresas.SingleAsync(m =>
                m.UsuarioId == user.Id && m.EmpresaId == a.Id);
            membershipA.CambiarRol(rolePlatform.Id);
            await db.SaveChangesAsync();
            Assert.Null(await scope.ObtenerActualAsync(a.Id));

            var orphan = new Rol {
                Nombre = "P4 ROL LEGACY SIN OWNER",
                NombreNormalizado = "P4_ROL_LEGACY_SIN_OWNER",
                Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false, EmpresaId = null, Activo = true
            };
            db.Roles.Add(orphan);
            await db.SaveChangesAsync();
            membershipA.CambiarRol(orphan.Id);
            await db.SaveChangesAsync();
            Assert.Null(await scope.ObtenerActualAsync(a.Id));

            membershipA.CambiarRol(template.Id);
            await db.SaveChangesAsync();
            Assert.Equal(template.Id, (await scope.ObtenerActualAsync(a.Id))?.RolId);
            Assert.Equal(roleB.Id, (await scope.ObtenerActualAsync(b.Id))?.RolId);

            // El setup hizo ChangeTracker.Clear; volver a cargar el rol
            // para que la desactivación se persista y la aserción sea causal.
            var rolBGuardado = await db.Roles.SingleAsync(r => r.Id == roleB.Id);
            rolBGuardado.Activo = false;
            await db.SaveChangesAsync();
            Assert.Null(await scope.ObtenerActualAsync(b.Id));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
