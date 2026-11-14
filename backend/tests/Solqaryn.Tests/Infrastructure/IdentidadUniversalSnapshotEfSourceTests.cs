using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

/// <summary>
/// Pruebas reales del adaptador read-only. Las pruebas de MySQL requieren el
/// servicio local efimero de CI; nunca usan Aiven ni datos productivos.
/// </summary>
public sealed class IdentidadUniversalSnapshotEfSourceTests
{
    private static AppDbContext CrearEnMemoria() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("identidad-source-" + Guid.NewGuid().ToString("N"))
            .Options);

    private static async Task<(Usuario Usuario, Empresa EmpresaA, Empresa EmpresaB,
        UsuarioEmpresa Membresia, Rol Rol)> SembrarAsync(AppDbContext db)
    {
        var rol = new Rol
        {
            Nombre = "Operador empresa", NombreNormalizado = "OPERADOR_EMPRESA",
            Ambito = AmbitoAutorizacion.Empresa, EsSistema = true,
            Activo = true
        };
        var a = new Empresa("A");
        var b = new Empresa("B");
        db.Set<Rol>().Add(rol);
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "usuario-a", NombreCompleto = "Usuario A",
            PasswordHash = "fixture-no-real", RolId = rol.Id, Activo = true
        };
        db.Set<Usuario>().Add(usuario);
        await db.SaveChangesAsync();

        var membresia = new UsuarioEmpresa(usuario.Id, a.Id, rol.Id);
        db.Set<UsuarioEmpresa>().Add(membresia);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return (usuario, a, b, membresia, rol);
    }

    [Fact]
    public async Task Devuelve_instantanea_activa_del_tenant_A_sin_tracking_ni_lectura_de_B()
    {
        await using var db = CrearEnMemoria();
        var data = await SembrarAsync(db);
        var source = new IdentidadUniversalSnapshotEfSource(db);

        var actual = await source.ObtenerActualAsync(
            data.Usuario.Id, data.EmpresaA.Id, null, default);
        Assert.NotNull(actual);
        Assert.Equal(data.Usuario.Id, actual.Usuario.Id);
        Assert.Equal(data.EmpresaA.Id, actual.Empresa.Id);
        Assert.Equal(data.Rol.Id, actual.Membresia.RolId);
        Assert.Empty(db.ChangeTracker.Entries());

        Assert.Null(await source.ObtenerActualAsync(
            data.Usuario.Id, data.EmpresaB.Id, null, default));
        Assert.Null(await source.ObtenerActualAsync(
            data.Usuario.Id, 0, null, default));
        Assert.Null(await source.ObtenerActualAsync(
            data.Usuario.Id, data.EmpresaA.Id, -1, default));
    }

    [Fact]
    public async Task Niega_membresia_revocada_y_rol_de_empresa_ajena()
    {
        await using var db = CrearEnMemoria();
        var data = await SembrarAsync(db);
        var source = new IdentidadUniversalSnapshotEfSource(db);
        var membresia = await db.Set<UsuarioEmpresa>()
            .SingleAsync(x => x.Id == data.Membresia.Id);
        membresia.Desactivar();
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Null(await source.ObtenerActualAsync(
            data.Usuario.Id, data.EmpresaA.Id, null, default));

        membresia = await db.Set<UsuarioEmpresa>().SingleAsync(
            x => x.Id == data.Membresia.Id);
        membresia.Activar();
        var rol = await db.Set<Rol>().SingleAsync(x => x.Id == data.Rol.Id);
        rol.EsSistema = false;
        rol.EmpresaId = data.EmpresaB.Id;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Null(await source.ObtenerActualAsync(
            data.Usuario.Id, data.EmpresaA.Id, null, default));
    }

    [Fact]
    public async Task Niega_usuario_bloqueado_y_sucursal_ajena()
    {
        await using var db = CrearEnMemoria();
        var data = await SembrarAsync(db);
        var source = new IdentidadUniversalSnapshotEfSource(db);
        Assert.Null(await source.ObtenerActualAsync(
            data.Usuario.Id, data.EmpresaA.Id, 99999, default));

        var usuario = await db.Set<Usuario>().SingleAsync(
            x => x.Id == data.Usuario.Id);
        usuario.Bloqueado = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        Assert.Null(await source.ObtenerActualAsync(
            data.Usuario.Id, data.EmpresaA.Id, null, default));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task MySql_efimero_lee_membresia_A_y_niega_B_y_revocacion()
    {
        var root = Environment.GetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException(
                "Esta prueba requiere MySQL local efimero en CI.");
        var parsed = new MySqlConnectionStringBuilder(root);
        if (parsed.Server is not ("localhost" or "127.0.0.1"))
            throw new InvalidOperationException(
                "MySQL Integration solo acepta host loopback efimero.");

        var isolated = new MySqlConnectionStringBuilder(root)
        {
            Database = "v5_identity_snapshot_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated, mysql =>
                mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        await using var db = new AppDbContext(options);
        try
        {
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var data = await SembrarAsync(db);
            var source = new IdentidadUniversalSnapshotEfSource(db);
            var a = await source.ObtenerActualAsync(
                data.Usuario.Id, data.EmpresaA.Id, null, default);
            Assert.NotNull(a);
            Assert.Equal(data.EmpresaA.Id, a.Empresa.Id);
            Assert.Null(await source.ObtenerActualAsync(
                data.Usuario.Id, data.EmpresaB.Id, null, default));

            var m = await db.Set<UsuarioEmpresa>()
                .SingleAsync(x => x.Id == data.Membresia.Id);
            m.Desactivar();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Null(await source.ObtenerActualAsync(
                data.Usuario.Id, data.EmpresaA.Id, null, default));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }
}
