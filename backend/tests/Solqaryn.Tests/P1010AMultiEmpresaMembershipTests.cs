using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-010.A: un usuario puede tener dos empresas, pero su rol y autoridad
/// se obtienen exclusivamente de la membresia vigente de cada tenant.
/// Pruebas de servicio real sobre EF InMemory; no certifican HTTP/MySQL live.
/// </summary>
public sealed class P1010AMultiEmpresaMembershipTests
{
    [Fact]
    public async Task DosEmpresasConRolesDistintosRequierenSeleccionExplicita()
    {
        var f = await CrearEscenarioAsync();
        await using var db = f.Db;

        var scopeA = await f.Scope.ObtenerActualAsync(f.EmpresaA.Id);
        var scopeB = await f.Scope.ObtenerActualAsync(f.EmpresaB.Id);

        Assert.NotNull(scopeA);
        Assert.NotNull(scopeB);
        Assert.Equal(f.Usuario.Id, scopeA.UsuarioId);
        Assert.Equal(f.Usuario.Id, scopeB.UsuarioId);
        Assert.Equal(f.EmpresaA.Id, scopeA.EmpresaId);
        Assert.Equal(f.EmpresaB.Id, scopeB.EmpresaId);
        Assert.Equal(f.RolA.Id, scopeA.RolId);
        Assert.Equal(f.RolB.Id, scopeB.RolId);
        Assert.True(scopeA.EsAdministrador);
        Assert.False(scopeB.EsAdministrador);

        // El selector sin EmpresaId no puede elegir arbitrariamente A o B.
        Assert.Null(await f.Scope.ObtenerUnicoActualAsync());
        Assert.Null(await f.Scope.ObtenerActualAsync(0));
    }

    [Fact]
    public async Task DesactivarYReactivarMembresiaBNoAlteraRolDeA()
    {
        var f = await CrearEscenarioAsync();
        await using var db = f.Db;

        f.MembresiaB.Desactivar();
        await db.SaveChangesAsync();

        Assert.Null(await f.Scope.ObtenerActualAsync(f.EmpresaB.Id));
        Assert.Equal(f.RolA.Id, (await f.Scope.ObtenerActualAsync(f.EmpresaA.Id))!.RolId);
        Assert.Equal(f.EmpresaA.Id, (await f.Scope.ObtenerUnicoActualAsync())!.EmpresaId);

        f.MembresiaB.Activar();
        await db.SaveChangesAsync();

        Assert.Equal(f.RolB.Id, (await f.Scope.ObtenerActualAsync(f.EmpresaB.Id))!.RolId);
        Assert.Null(await f.Scope.ObtenerUnicoActualAsync());
    }

    [Fact]
    public async Task AsignarRolPrivadoDeAEnBSeRechazaSinRevocarA()
    {
        var f = await CrearEscenarioAsync();
        await using var db = f.Db;

        // El identificador de rol existe, pero pertenece a otra empresa.
        // Ningun dato de B debe heredar la autoridad administrativa de A.
        f.MembresiaB.CambiarRol(f.RolA.Id);
        await db.SaveChangesAsync();

        Assert.Null(await f.Scope.ObtenerActualAsync(f.EmpresaB.Id));
        Assert.True((await f.Scope.ObtenerActualAsync(f.EmpresaA.Id))!.EsAdministrador);

        f.MembresiaB.CambiarRol(f.RolB.Id);
        await db.SaveChangesAsync();
        Assert.False((await f.Scope.ObtenerActualAsync(f.EmpresaB.Id))!.EsAdministrador);
    }

    [Fact]
    public async Task BloqueoDelUsuarioRevocaAmbasEmpresasAunqueJwtSigaAutenticado()
    {
        var f = await CrearEscenarioAsync();
        await using var db = f.Db;

        f.Usuario.Bloqueado = true;
        await db.SaveChangesAsync();

        Assert.Null(await f.Scope.ObtenerActualAsync(f.EmpresaA.Id));
        Assert.Null(await f.Scope.ObtenerActualAsync(f.EmpresaB.Id));
        Assert.Null(await f.Scope.ObtenerUnicoActualAsync());

        f.Usuario.Bloqueado = false;
        await db.SaveChangesAsync();
        Assert.NotNull(await f.Scope.ObtenerActualAsync(f.EmpresaA.Id));
        Assert.NotNull(await f.Scope.ObtenerActualAsync(f.EmpresaB.Id));
    }

    [Fact]
    public async Task UsuarioInactivoNoPuedeListarNiSeleccionarEmpresasHastaReactivarse()
    {
        var f = await CrearEscenarioAsync();
        await using var db = f.Db;

        // Con ambas membresias activas, usuario inactivo no puede enumerar tenants.
        f.Usuario.Activo = false;
        await db.SaveChangesAsync();

        Assert.Null(await f.Scope.ObtenerActualAsync(f.EmpresaA.Id));
        Assert.Null(await f.Scope.ObtenerActualAsync(f.EmpresaB.Id));
        Assert.Null(await f.Scope.ObtenerUnicoActualAsync());
        Assert.Empty(await f.Scope.ListarEmpresasSeleccionablesAsync());

        // La reactivacion solo restituye autoridad segun la membresia de cada tenant.
        f.Usuario.Activo = true;
        await db.SaveChangesAsync();

        Assert.Equal(f.RolA.Id, (await f.Scope.ObtenerActualAsync(f.EmpresaA.Id))!.RolId);
        Assert.Equal(f.RolB.Id, (await f.Scope.ObtenerActualAsync(f.EmpresaB.Id))!.RolId);
        Assert.Equal(2, (await f.Scope.ListarEmpresasSeleccionablesAsync()).Count);
        Assert.Null(await f.Scope.ObtenerUnicoActualAsync());
    }

    [Fact]
    public async Task EmpresaBInactivaNoRevocaLaMembresiaActivaDeA()
    {
        var f = await CrearEscenarioAsync();
        await using var db = f.Db;

        f.EmpresaB.Desactivar();
        await db.SaveChangesAsync();

        Assert.Null(await f.Scope.ObtenerActualAsync(f.EmpresaB.Id));
        Assert.Equal(f.EmpresaA.Id, (await f.Scope.ObtenerActualAsync(f.EmpresaA.Id))!.EmpresaId);
        Assert.Equal(f.EmpresaA.Id, (await f.Scope.ObtenerUnicoActualAsync())!.EmpresaId);

        f.EmpresaB.Activar();
        await db.SaveChangesAsync();
        Assert.NotNull(await f.Scope.ObtenerActualAsync(f.EmpresaB.Id));
    }

    [Fact]
    public async Task RolBInactivoNoPuedeAutorizarAunqueMembresiaEsteActiva()
    {
        var f = await CrearEscenarioAsync();
        await using var db = f.Db;

        f.RolB.Activo = false;
        await db.SaveChangesAsync();

        Assert.Null(await f.Scope.ObtenerActualAsync(f.EmpresaB.Id));
        Assert.True((await f.Scope.ObtenerActualAsync(f.EmpresaA.Id))!.EsAdministrador);

        f.RolB.Activo = true;
        await db.SaveChangesAsync();
        Assert.False((await f.Scope.ObtenerActualAsync(f.EmpresaB.Id))!.EsAdministrador);
    }

    [Fact]
    public async Task SinMembresiasActivasRolLegacyNoAutorizaNingunaEmpresa()
    {
        var f = await CrearEscenarioAsync();
        await using var db = f.Db;

        f.MembresiaA.Desactivar();
        f.MembresiaB.Desactivar();
        await db.SaveChangesAsync();

        // Usuario.RolId conserva una referencia legacy; no es concesion RBAC.
        Assert.Equal(f.RolA.Id, f.Usuario.RolId);
        Assert.Null(await f.Scope.ObtenerActualAsync(f.EmpresaA.Id));
        Assert.Null(await f.Scope.ObtenerActualAsync(f.EmpresaB.Id));
        Assert.Null(await f.Scope.ObtenerUnicoActualAsync());
    }

    private static async Task<(
        AppDbContext Db,
        UsuarioScopeService Scope,
        Usuario Usuario,
        Empresa EmpresaA,
        Empresa EmpresaB,
        Rol RolA,
        Rol RolB,
        UsuarioEmpresa MembresiaA,
        UsuarioEmpresa MembresiaB)> CrearEscenarioAsync()
    {
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1010a-" + Guid.NewGuid().ToString("N"))
                .Options);

        var empresaA = new Empresa("P1010A Empresa A");
        var empresaB = new Empresa("P1010A Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var rolA = new Rol
        {
            Nombre = "Administrador A",
            NombreNormalizado = "P1010A_ADMIN_A",
            EmpresaId = empresaA.Id,
            Ambito = AmbitoAutorizacion.Empresa,
            EsAdministrador = true,
            EsSistema = false,
            Activo = true
        };
        var rolB = new Rol
        {
            Nombre = "Consulta B",
            NombreNormalizado = "P1010A_CONSULTA_B",
            EmpresaId = empresaB.Id,
            Ambito = AmbitoAutorizacion.Empresa,
            EsAdministrador = false,
            EsSistema = false,
            Activo = true
        };
        db.Roles.AddRange(rolA, rolB);
        await db.SaveChangesAsync();

        var usuario = new Usuario
        {
            NombreUsuario = "p1010a-operador",
            NombreCompleto = "Operador multiempresa P1010A",
            PasswordHash = "fixture-sin-clave-real",
            RolId = rolA.Id,
            Activo = true
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var membresiaA = new UsuarioEmpresa(usuario.Id, empresaA.Id, rolA.Id);
        var membresiaB = new UsuarioEmpresa(usuario.Id, empresaB.Id, rolB.Id);
        db.Set<UsuarioEmpresa>().AddRange(membresiaA, membresiaB);
        await db.SaveChangesAsync();

        var identidad = new Mock<ICurrentUserService>(MockBehavior.Strict);
        identidad.SetupGet(x => x.EstaAutenticado).Returns(true);
        identidad.SetupGet(x => x.UsuarioId).Returns(usuario.Id);

        return (db, new UsuarioScopeService(db, identidad.Object),
            usuario, empresaA, empresaB, rolA, rolB, membresiaA, membresiaB);
    }
}
