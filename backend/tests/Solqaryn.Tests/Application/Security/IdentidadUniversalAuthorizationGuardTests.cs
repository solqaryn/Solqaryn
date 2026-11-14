using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application.Security;

public sealed class IdentidadUniversalAuthorizationGuardTests
{
    private sealed class Fuente : IIdentidadUniversalSnapshotSource
    {
        public IdentidadUniversalSnapshot? Actual { get; set; }
        public int Consultas { get; private set; }

        public Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
            int usuarioId, int empresaId, int? sucursalId,
            CancellationToken cancellationToken)
        {
            Consultas++;
            return Task.FromResult(Actual);
        }
    }

    private static (ContextoTenantActual Actor, IdentidadUniversalSnapshot Snapshot)
        Preparar(bool conSucursal = false)
    {
        // RolId global legacy deliberadamente NO coincide con la membresia.
        var usuario = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var empresa = new Empresa("Empresa A") { Id = 11 };
        var membresia = new UsuarioEmpresa(7, 11, 19);
        var rol = new Rol { Id = 19, EmpresaId = 11, Activo = true };
        var permiso = new Permiso
        {
            Id = 47,
            Modulo = ModuloSistema.Usuarios,
            Accion = AccionPermiso.Ver,
            Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };
        rol.Permisos.Add(new RolPermiso
        {
            RolId = rol.Id,
            RolEntidad = rol,
            PermisoId = permiso.Id,
            Permiso = permiso
        });

        var actor = IdentidadUniversalReglas.CrearContextoVerificado(
            usuario, empresa, membresia, rol);
        var sucursal = conSucursal ? new Sucursal { Id = 3, EmpresaId = 11 } : null;
        return (actor, new IdentidadUniversalSnapshot(
            usuario, empresa, membresia, rol, sucursal));
    }

    private static Task<ContextoTenantActual> Verificar(
        Fuente fuente, ContextoTenantActual actor, int empresaId = 11,
        int? sucursalId = null, ModuloSistema modulo = ModuloSistema.Usuarios,
        AccionPermiso accion = AccionPermiso.Ver) =>
        new IdentidadUniversalAuthorizationGuard(fuente)
            .ExigirAccesoActualAsync(actor, empresaId, modulo, accion, sucursalId);

    [Fact]
    public async Task TenantA_MembresiaYPermisoActual_AutorizadoSinRolLegacy()
    {
        var (actor, snapshot) = Preparar();
        var fuente = new Fuente { Actual = snapshot };
        var actual = await Verificar(fuente, actor);
        Assert.Equal(7, actual.UsuarioId);
        Assert.Equal(11, actual.EmpresaId);
        Assert.Equal(19, actual.RolId);
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task ActorA_NuncaConsultaTenantB_NiConRolGlobalLegacy()
    {
        var (actor, snapshot) = Preparar();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(fuente, actor, empresaId: 22));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public async Task MembresiaRevocada_NoAutorizadaConContextoAntiguo()
    {
        var (actor, snapshot) = Preparar();
        snapshot.Membresia.Desactivar();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = snapshot }, actor));
    }

    [Fact]
    public async Task CambioDeRol_RevocaContextoYNoEscalaConRolLegacy()
    {
        var (actor, snapshot) = Preparar();
        snapshot.Membresia.CambiarRol(20);
        var nuevo = snapshot with
        {
            Rol = new Rol { Id = 20, EmpresaId = 11, Activo = true }
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = nuevo }, actor));
    }

    [Fact]
    public async Task SinPermisoRelacional_NoSeConcedePorAdministrador()
    {
        var (actor, snapshot) = Preparar();
        snapshot.Rol.EsAdministrador = true;
        snapshot.Rol.Permisos.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = snapshot }, actor));
    }

    [Fact]
    public async Task PermisoInactivo_OEliminado_Denegado()
    {
        var (actor, snapshot) = Preparar();
        var permiso = Assert.Single(snapshot.Rol.Permisos).Permiso;
        permiso.Activo = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = snapshot }, actor));
        permiso.Activo = true;
        permiso.Eliminado = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = snapshot }, actor));
    }

    [Fact]
    public async Task PermisoPlataforma_NoSeHeredaEnTenantEmpresa()
    {
        var (actor, snapshot) = Preparar();
        Assert.Single(snapshot.Rol.Permisos).Permiso.Ambito =
            AmbitoAutorizacion.Plataforma;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = snapshot }, actor));
    }

    [Fact]
    public async Task PermisoDeOtroRol_OAccionDistinta_Denegado()
    {
        var (actor, snapshot) = Preparar();
        var relacion = Assert.Single(snapshot.Rol.Permisos);
        relacion.RolId = 20;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = snapshot }, actor));

        relacion.RolId = 19;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = snapshot }, actor,
                accion: AccionPermiso.Administrar));
    }

    [Fact]
    public async Task PermisoConReferenciaInconsistente_Denegado()
    {
        var (actor, snapshot) = Preparar();
        Assert.Single(snapshot.Rol.Permisos).PermisoId = 123;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = snapshot }, actor));
    }

    [Fact]
    public async Task SucursalA_Verificada_SeAcepta()
    {
        var (actor, snapshot) = Preparar(conSucursal: true);
        var actual = await Verificar(new Fuente { Actual = snapshot },
            actor, sucursalId: 3);
        Assert.Equal(11, actual.EmpresaId);
    }

    [Fact]
    public async Task SucursalB_IdCoincidente_Denegada()
    {
        var (actor, snapshot) = Preparar(conSucursal: true);
        snapshot.Sucursal!.EmpresaId = 22;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = snapshot }, actor, sucursalId: 3));
    }

    [Fact]
    public async Task SucursalNoPedida_NoSeAdquiereImplicitamente()
    {
        var (actor, snapshot) = Preparar(conSucursal: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente { Actual = snapshot }, actor));
    }

    [Fact]
    public async Task SnapshotNoExistente_Denegado()
    {
        var (actor, _) = Preparar();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(new Fuente(), actor));
    }

    [Fact]
    public async Task PermisoDesconocido_RechazadoAntesDeAccederFuente()
    {
        var (actor, snapshot) = Preparar();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Verificar(fuente, actor, modulo: (ModuloSistema)999));
        Assert.Equal(0, fuente.Consultas);
    }
}
