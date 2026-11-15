using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C01-01.M05.03: integración de identidad/rol/membresía persistidos en MySQL 8.4 efímero.
/// El test no conecta a Aiven ni a bases remotas y elimina solo su base aleatoria de CI.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0101M0503MySqlTenantContextIntegrationTests
{
    [Fact]
    public async Task IdentidadPersistida_RevalidaDosEmpresasRolMembresiaYRevocacion()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Se requiere MySQL efimero de CI.");
        var origen = new MySqlConnectionStringBuilder(raw);
        if (!string.Equals(origen.Server, "127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(origen.Server, "localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Pruebas C01-01.M05.03 solo admiten MySQL local efimero.");

        var connection = new MySqlConnectionStringBuilder(raw)
        {
            Database = "v5_identity_ab_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);

            var empresaA = new Empresa("C0101 M0503 A");
            var empresaB = new Empresa("C0101 M0503 B");
            db.Set<Empresa>().AddRange(empresaA, empresaB);
            await db.SaveChangesAsync();

            var rolA = new Rol
            {
                Nombre = "C0101 Rol A", NombreNormalizado = "C0101_ROL_A",
                EmpresaId = empresaA.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            var rolB = new Rol
            {
                Nombre = "C0101 Rol B", NombreNormalizado = "C0101_ROL_B",
                EmpresaId = empresaB.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            db.Roles.AddRange(rolA, rolB);
            await db.SaveChangesAsync();

            var usuario = new Usuario
            {
                NombreUsuario = "v5-identidad-ab", NombreCompleto = "Identidad V5 A B",
                PasswordHash = "hash-sintetico-no-autenticable",
                RolId = rolA.Id, Activo = true
            };
            db.Usuarios.Add(usuario);
            await db.SaveChangesAsync();

            db.UsuarioEmpresas.AddRange(
                new UsuarioEmpresa(usuario.Id, empresaA.Id, rolA.Id),
                new UsuarioEmpresa(usuario.Id, empresaB.Id, rolB.Id));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Releer identidad, ownership y memberships desde MySQL, sin usar instancias
            // rastreadas por EF ni un EmpresaId suministrado por el cliente.
            var persistido = await db.Usuarios.SingleAsync(x => x.Id == usuario.Id);
            var a = await db.Set<Empresa>().SingleAsync(x => x.Id == empresaA.Id);
            var b = await db.Set<Empresa>().SingleAsync(x => x.Id == empresaB.Id);
            var membresiaA = await db.UsuarioEmpresas.SingleAsync(x =>
                x.UsuarioId == usuario.Id && x.EmpresaId == a.Id);
            var membresiaB = await db.UsuarioEmpresas.SingleAsync(x =>
                x.UsuarioId == usuario.Id && x.EmpresaId == b.Id);
            var roleA = await db.Roles.SingleAsync(x => x.Id == rolA.Id);
            var roleB = await db.Roles.SingleAsync(x => x.Id == rolB.Id);

            var contextoA = IdentidadUniversalReglas.CrearContextoVerificado(
                persistido, a, membresiaA, roleA);
            var contextoB = IdentidadUniversalReglas.CrearContextoVerificado(
                persistido, b, membresiaB, roleB);
            Assert.Equal(a.Id, contextoA.EmpresaId);
            Assert.Equal(b.Id, contextoB.EmpresaId);
            Assert.Equal(roleA.Id, contextoA.RolId);
            Assert.Equal(roleB.Id, contextoB.RolId);
            Assert.Throws<InvalidOperationException>(() => contextoA.ExigirEmpresa(b.Id));
            Assert.Throws<InvalidOperationException>(() => contextoB.ExigirEmpresa(a.Id));
            Assert.Throws<InvalidOperationException>(() =>
                IdentidadUniversalReglas.CrearContextoVerificado(persistido, a, membresiaB, roleB));

            // Una sustitución de rol A por un rol tenant B invalida el contexto A.
            membresiaA.CambiarRol(roleB.Id);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            membresiaA = await db.UsuarioEmpresas.SingleAsync(x =>
                x.UsuarioId == usuario.Id && x.EmpresaId == a.Id);
            Assert.Throws<InvalidOperationException>(() =>
                IdentidadUniversalReglas.CrearContextoVerificado(persistido, a, membresiaA, roleB));

            // Revocar la membresía de B persiste y deniega la sesión tras nueva lectura.
            membresiaB = await db.UsuarioEmpresas.SingleAsync(x =>
                x.UsuarioId == usuario.Id && x.EmpresaId == b.Id);
            membresiaB.Desactivar();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            membresiaB = await db.UsuarioEmpresas.SingleAsync(x =>
                x.UsuarioId == usuario.Id && x.EmpresaId == b.Id);
            Assert.False(membresiaB.Activa);
            Assert.Throws<InvalidOperationException>(() =>
                IdentidadUniversalReglas.CrearContextoVerificado(persistido, b, membresiaB, roleB));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
