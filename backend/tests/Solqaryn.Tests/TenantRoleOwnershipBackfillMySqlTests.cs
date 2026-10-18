using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Verifica el backfill de ownership con MySQL efímero. Nunca apunta a Aiven.
/// </summary>
[Trait("Category", "Integration")]
public sealed class TenantRoleOwnershipBackfillMySqlTests
{
    [Fact]
    public async Task DosEmpresasNingunaMembresiaDemuestraOwner_AunqueSoloExistaUna()
    {
        var configured = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Se requiere una instancia MySQL efímera.");
        var parsed = new MySqlConnectionStringBuilder(configured);
        if (!string.Equals(parsed.Server, "localhost", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(parsed.Server, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Rechazado MySQL remoto para pruebas de backfill.");

        parsed.Database = "role_owner_backfill_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(parsed.ConnectionString, x =>
                x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(
                db, applyPendingOracleMigrations: false);

            var a = new Empresa("Role owner A");
            var b = new Empresa("Role owner B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();

            var unico = new Rol
            {
                Nombre = "Unico", NombreNormalizado = "ROL_UNICO",
                Ambito = AmbitoAutorizacion.Empresa, EsSistema = false
            };
            var ambiguo = new Rol
            {
                Nombre = "Ambiguo", NombreNormalizado = "ROL_AMBIGUO",
                Ambito = AmbitoAutorizacion.Empresa, EsSistema = false
            };
            var sinReferencia = new Rol
            {
                Nombre = "Sin referencia", NombreNormalizado = "SIN_REFERENCIA",
                Ambito = AmbitoAutorizacion.Empresa, EsSistema = false
            };
            var plantilla = new Rol
            {
                Nombre = "Plantilla", NombreNormalizado = "PLANTILLA",
                Ambito = AmbitoAutorizacion.Empresa, EsSistema = true
            };
            db.Roles.AddRange(unico, ambiguo, sinReferencia, plantilla);
            await db.SaveChangesAsync();

            var userA = new Usuario
            {
                NombreUsuario = "role-user-a", NombreCompleto = "Rol A",
                PasswordHash = "solo-test", RolId = unico.Id
            };
            var userB = new Usuario
            {
                NombreUsuario = "role-user-b", NombreCompleto = "Rol B",
                PasswordHash = "solo-test", RolId = ambiguo.Id
            };
            db.Usuarios.AddRange(userA, userB);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.AddRange(
                new UsuarioEmpresa(userA.Id, a.Id, unico.Id),
                new UsuarioEmpresa(userB.Id, a.Id, ambiguo.Id),
                new UsuarioEmpresa(userB.Id, b.Id, ambiguo.Id));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();

            var owned = await db.Roles.IgnoreQueryFilters()
                .AsNoTracking().SingleAsync(x => x.Id == unico.Id);
            var ambiguous = await db.Roles.IgnoreQueryFilters()
                .AsNoTracking().SingleAsync(x => x.Id == ambiguo.Id);
            var unknown = await db.Roles.IgnoreQueryFilters()
                .AsNoTracking().SingleAsync(x => x.Id == sinReferencia.Id);
            var shared = await db.Roles.IgnoreQueryFilters()
                .AsNoTracking().SingleAsync(x => x.Id == plantilla.Id);

            Assert.Null(owned.EmpresaId);
            Assert.False(owned.EsAsignableAEmpresa(a.Id));
            Assert.False(owned.EsAsignableAEmpresa(b.Id));
            Assert.Null(ambiguous.EmpresaId);
            Assert.False(ambiguous.EsAsignableAEmpresa(a.Id));
            Assert.False(ambiguous.EsAsignableAEmpresa(b.Id));
            Assert.Null(unknown.EmpresaId);
            Assert.False(unknown.EsAsignableAEmpresa(a.Id));
            Assert.Null(shared.EmpresaId);
            Assert.True(shared.EsAsignableAEmpresa(a.Id));
            Assert.True(shared.EsAsignableAEmpresa(b.Id));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task UnaSolaEmpresaPuedeAtribuirUnRolPersonalizadoSinDueno()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Se requiere una instancia MySQL efímera.");
        var connection = new MySqlConnectionStringBuilder(raw);
        if (!string.Equals(connection.Server, "localhost", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(connection.Server, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Rechazado MySQL remoto para backfill.");
        connection.Database = "role_owner_single_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection.ConnectionString, x =>
                x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(
                db, applyPendingOracleMigrations: false);
            var empresa = new Empresa("Empresa única");
            db.Set<Empresa>().Add(empresa);
            await db.SaveChangesAsync();

            var rol = new Rol
            {
                Nombre = "Rol anterior", NombreNormalizado = "ROL_ANTERIOR",
                EmpresaId = null, EsSistema = false, Ambito = AmbitoAutorizacion.Empresa
            };
            db.Roles.Add(rol);
            var permiso = new Permiso
            {
                Codigo = "ROL.OWNER.RECOVERY", Nombre = "Rol owner recovery",
                Modulo = ModuloSistema.Roles, Accion = AccionPermiso.Ver
            };
            db.Permisos.Add(permiso);
            await db.SaveChangesAsync();
            // Simula exclusivamente una fila legacy antes de los nuevos guards.
            // INSERT raw en DB efímera, nunca en DEV/Aiven.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"INSERT INTO RolPermisos (RolId, PermisoId, EmpresaId) VALUES ({rol.Id}, {permiso.Id}, NULL)");
            db.ChangeTracker.Clear();

            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();
            var grantOwner = await db.RolPermisos.IgnoreQueryFilters()
                .AsNoTracking().Where(x => x.RolId == rol.Id)
                .Select(x => EF.Property<int?>(x, "EmpresaId")).SingleAsync();
            var persistido = await db.Roles.IgnoreQueryFilters().AsNoTracking()
                .SingleAsync(x => x.Id == rol.Id);
            Assert.Equal(empresa.Id, persistido.EmpresaId);
            Assert.Equal(empresa.Id, grantOwner);
            Assert.True(persistido.EsAsignableAEmpresa(empresa.Id));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
