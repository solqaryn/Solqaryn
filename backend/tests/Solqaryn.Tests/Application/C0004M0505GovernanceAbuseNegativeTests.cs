using Solqaryn.Application.Governance;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Governance;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C00-04.M05.05: adversarial checks against the real Application authorization
/// guard. The snapshot is a test double, NOT an HTTP/MySQL/JWT integration.
/// </summary>
public sealed class C0004M0505GovernanceAbuseNegativeTests
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

    private static (ContextoTenantActual Actor, IdentidadUniversalSnapshot Snapshot, Fuente Fuente)
        Escenario()
    {
        var usuario = new Usuario { Id = 71, Activo = true, RolId = 999 };
        var empresa = new Empresa("Empresa A") { Id = 211 };
        var membresia = new UsuarioEmpresa(71, 211, 201);
        var rol = new Rol { Id = 201, EmpresaId = 211, Activo = true };
        var permiso = new Permiso
        {
            Id = 701,
            Modulo = ModuloSistema.Configuracion,
            Accion = AccionPermiso.Administrar,
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
        var snapshot = new IdentidadUniversalSnapshot(
            usuario, empresa, membresia, rol, null);
        return (actor, snapshot, new Fuente { Actual = snapshot });
    }

    private static SolicitudPreparacionCambio Solicitud(
        TipoCambioGobierno tipo = TipoCambioGobierno.CodigoDev) =>
        new("C00-04.M05.05", "D01", "Consulta de prueba sin ejecucion",
            "fbd8fbb9ffbe4c500ebc831d50421c887cd8fe63",
            "governance:abuse:005", 5, tipo, true);

    private static Task<ResultadoPreparacionCambio> Consultar(
        Fuente fuente, ContextoTenantActual actor,
        int empresa = 211, SolicitudPreparacionCambio? solicitud = null)
    {
        var servicio = new GobiernoCambiosAuthorizationService(
            new IdentidadUniversalAuthorizationGuard(fuente),
            new GobiernoCambiosPreparacionService());
        return servicio.PrepararConsultaAutorizadaAsync(
            actor, empresa, solicitud ?? Solicitud());
    }

    [Fact]
    public async Task ConsultaValidaEsInerte_ConPermisoEmpresarialExplicito()
    {
        var (actor, _, fuente) = Escenario();
        var respuesta = await Consultar(fuente, actor);
        Assert.Equal("PENDIENTE_AUTORIZACION", respuesta.Estado);
        Assert.False(respuesta.AutorizaEjecucion);
        Assert.False(respuesta.AutorizaPersistencia);
        Assert.False(respuesta.AutorizaConcesionPermisos);
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task TenantBNoPuedeSerConsultadoDesdeSesionTenantA_NiHaceIo()
    {
        var (actor, _, fuente) = Escenario();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Consultar(fuente, actor, empresa: 212));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public async Task AdministradorLegacySinAsignacionRelacional_NoPuedeEscalar()
    {
        var (actor, snapshot, fuente) = Escenario();
        snapshot.Rol.EsAdministrador = true;
        snapshot.Rol.Permisos.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Consultar(fuente, actor));
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task PermisoRevocadoEnSnapshotActual_NoPuedeSerReutilizado()
    {
        var (actor, snapshot, fuente) = Escenario();
        Assert.Single(snapshot.Rol.Permisos).Permiso.Activo = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Consultar(fuente, actor));
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task AmbitoDePermisoAlterado_NoConfiereAdministracionEmpresarial()
    {
        var (actor, snapshot, fuente) = Escenario();
        Assert.Single(snapshot.Rol.Permisos).Permiso.Ambito = (AmbitoAutorizacion)999;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Consultar(fuente, actor));
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task OperacionSensibleConRollback_NoGoInclusoConPermisoDeLectura()
    {
        var (actor, _, fuente) = Escenario();
        var respuesta = await Consultar(fuente, actor,
            solicitud: Solicitud(TipoCambioGobierno.SeguridadYPermisos));
        Assert.Equal("NO_GO", respuesta.Estado);
        Assert.False(respuesta.AutorizaEjecucion);
        Assert.False(respuesta.AutorizaPersistencia);
        Assert.False(respuesta.AutorizaConcesionPermisos);
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task IdAdulteradoSeRechazaAntesDeConsultarFuente()
    {
        var (actor, _, fuente) = Escenario();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Consultar(fuente, actor,
                solicitud: Solicitud() with { MicrotareaId = "C00-04.M05.05\n" }));
        Assert.Equal(0, fuente.Consultas);
    }
}
