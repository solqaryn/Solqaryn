using Solqaryn.Application.Governance;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Governance;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class GobiernoCambiosAuthorizationServiceTests
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
        Escenario(bool conSucursal = false)
    {
        var usuario = new Usuario { Id = 7, Activo = true, RolId = 900 };
        var empresa = new Empresa("Empresa A") { Id = 11 };
        var membresia = new UsuarioEmpresa(7, 11, 19);
        var rol = new Rol { Id = 19, EmpresaId = 11, Activo = true };
        var permiso = new Permiso
        {
            Id = 47,
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
        var sucursal = conSucursal ? new Sucursal { Id = 3, EmpresaId = 11 } : null;
        return (actor, new IdentidadUniversalSnapshot(
            usuario, empresa, membresia, rol, sucursal));
    }

    private static SolicitudPreparacionCambio Solicitud(
        TipoCambioGobierno tipo = TipoCambioGobierno.CodigoDev) =>
        new("C00-04.M03.03", "D01", "Consulta segura y reversible",
            "fbd8fbb9ffbe4c500ebc831d50421c887cd8fe63",
            "governance:auth:0001", 7, tipo, true);

    private static Task<ResultadoPreparacionCambio> Consultar(
        Fuente fuente, ContextoTenantActual actor,
        SolicitudPreparacionCambio? solicitud = null,
        int empresaId = 11, int? sucursalId = null)
    {
        var service = new GobiernoCambiosAuthorizationService(
            new IdentidadUniversalAuthorizationGuard(fuente),
            new GobiernoCambiosPreparacionService());
        return service.PrepararConsultaAutorizadaAsync(
            actor, empresaId, solicitud ?? Solicitud(), sucursalId);
    }

    [Fact]
    public async Task TenantA_ConPermisoActual_ObtieneSoloPreparacionPendiente()
    {
        var (actor, snapshot) = Escenario();
        var fuente = new Fuente { Actual = snapshot };
        var resultado = await Consultar(fuente, actor);
        Assert.Equal("PENDIENTE_AUTORIZACION", resultado.Estado);
        Assert.False(resultado.AutorizaEjecucion);
        Assert.False(resultado.AutorizaPersistencia);
        Assert.False(resultado.AutorizaConcesionPermisos);
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task SucursalA_Verificada_ConsultaPeroNoAprueba()
    {
        var (actor, snapshot) = Escenario(conSucursal: true);
        var resultado = await Consultar(
            new Fuente { Actual = snapshot }, actor, sucursalId: 3);
        Assert.False(resultado.AutorizaEjecucion);
        Assert.Equal(EvaluacionCambioGobierno.PendienteAutorizacion,
            resultado.Evaluacion);
    }

    [Fact]
    public async Task TenantA_NoConsultaTenantB()
    {
        var (actor, snapshot) = Escenario();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Consultar(fuente, actor, empresaId: 22));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public async Task MembresiaRevocada_FallaCerrado()
    {
        var (actor, snapshot) = Escenario();
        snapshot.Membresia.Desactivar();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Consultar(new Fuente { Actual = snapshot }, actor));
    }

    [Fact]
    public async Task RolCambiado_NoReutilizaSesionAntigua()
    {
        var (actor, snapshot) = Escenario();
        snapshot.Membresia.CambiarRol(20);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Consultar(new Fuente { Actual = snapshot }, actor));
    }

    [Fact]
    public async Task AdministradorLegacy_SinPermisoExplicito_FallaCerrado()
    {
        var (actor, snapshot) = Escenario();
        snapshot.Rol.EsAdministrador = true;
        snapshot.Rol.Permisos.Clear();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Consultar(new Fuente { Actual = snapshot }, actor));
    }

    [Fact]
    public async Task PermisoSoloVer_NoPermiteAdministrar()
    {
        var (actor, snapshot) = Escenario();
        Assert.Single(snapshot.Rol.Permisos).Permiso.Accion = AccionPermiso.Ver;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Consultar(new Fuente { Actual = snapshot }, actor));
    }

    [Fact]
    public async Task SucursalDeOtroTenant_NoSeAutoriza()
    {
        var (actor, snapshot) = Escenario(conSucursal: true);
        snapshot.Sucursal!.EmpresaId = 22;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Consultar(new Fuente { Actual = snapshot }, actor, sucursalId: 3));
    }

    [Theory]
    [InlineData(TipoCambioGobierno.SeguridadYPermisos)]
    [InlineData(TipoCambioGobierno.PersistenciaYMigracion)]
    [InlineData(TipoCambioGobierno.InfraestructuraYProduccion)]
    public async Task PropuestaSensibleSiempreNoGoSinEfectos(TipoCambioGobierno tipo)
    {
        var (actor, snapshot) = Escenario();
        var resultado = await Consultar(new Fuente { Actual = snapshot },
            actor, Solicitud(tipo));
        Assert.Equal("NO_GO", resultado.Estado);
        Assert.False(resultado.AutorizaEjecucion);
        Assert.False(resultado.AutorizaPersistencia);
        Assert.False(resultado.AutorizaConcesionPermisos);
    }

    [Fact]
    public async Task SolicitudManipulada_FallaAntesDeConsultarFuente()
    {
        var (actor, snapshot) = Escenario();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Consultar(fuente, actor, Solicitud() with
            {
                MicrotareaId = "C00-04.M03.03\n"
            }));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public void DependenciasNulas_FallanCerrado()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GobiernoCambiosAuthorizationService(
                null!, new GobiernoCambiosPreparacionService()));
        Assert.Throws<ArgumentNullException>(() =>
            new GobiernoCambiosAuthorizationService(
                new IdentidadUniversalAuthorizationGuard(new Fuente()), null!));
    }
}
