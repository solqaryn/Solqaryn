using Solqaryn.Application.Governance;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Governance;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.Governance;
using Xunit;

namespace Solqaryn.Tests;

public sealed class GobiernoCambiosIntegracionLocalTests
{
    private const string Sha = "fbd8fbb9ffbe4c500ebc831d50421c887cd8fe63";

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

    private static (ContextoTenantActual Actor, IdentidadUniversalSnapshot Snapshot) Escenario()
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
            RolId = 19, RolEntidad = rol, PermisoId = 47, Permiso = permiso
        });
        var actor = IdentidadUniversalReglas.CrearContextoVerificado(
            usuario, empresa, membresia, rol);
        return (actor, new IdentidadUniversalSnapshot(
            usuario, empresa, membresia, rol, null));
    }

    private static SolicitudPreparacionCambio Solicitud(
        TipoCambioGobierno tipo = TipoCambioGobierno.CodigoDev) =>
        new("C00-04.M03.05", "D01", "Diagnostico solo lectura y reversible",
            Sha, "governance:integracion:0001", 7, tipo, true);

    private static GobiernoCambiosIntegracionLocal Crear(
        Fuente fuente, GobiernoCambiosProyeccionAislada proyeccion) =>
        new(new GobiernoCambiosAuthorizationService(
            new IdentidadUniversalAuthorizationGuard(fuente),
            new GobiernoCambiosPreparacionService()), proyeccion);

    private static LecturaGobiernoAislada Lectura(string sha = Sha) =>
        new("C00-04.M03.05", 7, sha, "VALIDANDO",
            new DateTimeOffset(2026, 11, 14, 20, 30, 0, TimeSpan.Zero));

    [Fact]
    public async Task AutorizacionActualYPersistenciaInerte_DevuelveCoincidenciaNoAutoritativa()
    {
        var (actor, snapshot) = Escenario();
        var fuente = new Fuente { Actual = snapshot };
        var local = new GobiernoCambiosProyeccionAislada();
        Assert.True(local.IntentarRegistrar(Lectura()));
        var r = await Crear(fuente, local).ConsultarAsync(actor, 11, Solicitud());
        Assert.True(r.CopiaLocalCoincideConSolicitud);
        Assert.Equal("PENDIENTE_AUTORIZACION", r.Preparacion.Estado);
        Assert.False(r.AutorizaEjecucion);
        Assert.False(r.AutorizaAprobacion);
        Assert.False(r.AutorizaPersistenciaOficial);
        Assert.False(r.AutorizaConcesionPermisos);
        Assert.Equal(1, fuente.Consultas);
        Assert.Equal(1, local.Cantidad);
    }

    [Fact]
    public async Task SinCopiaOConShaDistinto_JamasConcedeAutoridad()
    {
        var (actor, snapshot) = Escenario();
        var fuente = new Fuente { Actual = snapshot };
        var local = new GobiernoCambiosProyeccionAislada();
        var sut = Crear(fuente, local);
        var sinCopia = await sut.ConsultarAsync(actor, 11, Solicitud());
        Assert.False(sinCopia.CopiaLocalCoincideConSolicitud);
        Assert.True(local.IntentarRegistrar(Lectura(new string('a', 40))));
        var distinto = await sut.ConsultarAsync(actor, 11, Solicitud());
        Assert.False(distinto.CopiaLocalCoincideConSolicitud);
        Assert.False(distinto.AutorizaEjecucion);
        Assert.Equal("PENDIENTE_AUTORIZACION", distinto.Preparacion.Estado);
    }

    [Fact]
    public async Task TenantA_NoConsultaTenantB_NiLeeFuente()
    {
        var (actor, snapshot) = Escenario();
        var fuente = new Fuente { Actual = snapshot };
        var local = new GobiernoCambiosProyeccionAislada();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Crear(fuente, local).ConsultarAsync(actor, 22, Solicitud()));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public async Task MembresiaRevocada_DenegacionAnteriorACopiaLocal()
    {
        var (actor, snapshot) = Escenario();
        snapshot.Membresia.Desactivar();
        var fuente = new Fuente { Actual = snapshot };
        var local = new GobiernoCambiosProyeccionAislada();
        Assert.True(local.IntentarRegistrar(Lectura()));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Crear(fuente, local).ConsultarAsync(actor, 11, Solicitud()));
    }

    [Fact]
    public async Task IdentificadorManipulado_FallaAntesDeConsultarFuente()
    {
        var (actor, snapshot) = Escenario();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Crear(fuente, new GobiernoCambiosProyeccionAislada()).ConsultarAsync(
                actor, 11, Solicitud() with { MicrotareaId = "C00-04.M03.05\n" }));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public async Task PropuestaSensible_ConCopiaLocal_SigueNoGo()
    {
        var (actor, snapshot) = Escenario();
        var local = new GobiernoCambiosProyeccionAislada();
        Assert.True(local.IntentarRegistrar(Lectura()));
        var resultado = await Crear(new Fuente { Actual = snapshot }, local)
            .ConsultarAsync(actor, 11, Solicitud(TipoCambioGobierno.SeguridadYPermisos));
        Assert.Equal("NO_GO", resultado.Preparacion.Estado);
        Assert.False(resultado.AutorizaEjecucion);
        Assert.False(resultado.AutorizaPersistenciaOficial);
    }

    [Fact]
    public void DependenciasNulas_FallanCerrado()
    {
        var f = new Fuente();
        var a = new GobiernoCambiosAuthorizationService(
            new IdentidadUniversalAuthorizationGuard(f),
            new GobiernoCambiosPreparacionService());
        Assert.Throws<ArgumentNullException>(() =>
            new GobiernoCambiosIntegracionLocal(null!, new GobiernoCambiosProyeccionAislada()));
        Assert.Throws<ArgumentNullException>(() =>
            new GobiernoCambiosIntegracionLocal(a, null!));
    }
}
