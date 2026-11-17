using Solqaryn.Application.Governance;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Governance;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.Governance;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C00-04.M05.03: integración REAL entre Application y proyección
/// Infrastructure efímera. Nunca representa MySQL, Sheets o un proveedor remoto.
/// </summary>
public sealed class GobiernoCambiosM0503IntegracionCausalTests
{
    private const string Sha = "fbd8fbb9ffbe4c500ebc831d50421c887cd8fe63";

    private sealed class Fuente : IIdentidadUniversalSnapshotSource
    {
        public IdentidadUniversalSnapshot? Actual { get; set; }
        public bool Fallar { get; set; }
        public int Consultas { get; private set; }

        public Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
            int usuarioId, int empresaId, int? sucursalId,
            CancellationToken cancellationToken)
        {
            Consultas++;
            if (Fallar)
                throw new InvalidOperationException("Proveedor de identidad no disponible.");
            return Task.FromResult(Actual);
        }
    }

    private static (ContextoTenantActual Actor, IdentidadUniversalSnapshot Snapshot)
        Autorizado()
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
        return (
            IdentidadUniversalReglas.CrearContextoVerificado(
                usuario, empresa, membresia, rol),
            new IdentidadUniversalSnapshot(usuario, empresa, membresia, rol, null));
    }

    private static SolicitudPreparacionCambio Solicitud(
        TipoCambioGobierno tipo = TipoCambioGobierno.CodigoDev) =>
        new("C00-04.M05.03", "D01", "Diagnóstico local sin escritura",
            Sha, "governance:integration:0503", 7, tipo, true);

    private static LecturaGobiernoAislada Lectura(
        string sha = Sha, long revision = 7) =>
        new("C00-04.M05.03", revision, sha, "VALIDANDO",
            new DateTimeOffset(2026, 11, 17, 0, 0, 0, TimeSpan.Zero));

    private static GobiernoCambiosIntegracionLocal Integracion(
        Fuente fuente, GobiernoCambiosProyeccionAislada local) =>
        new(new GobiernoCambiosAuthorizationService(
            new IdentidadUniversalAuthorizationGuard(fuente),
            new GobiernoCambiosPreparacionService()), local);

    [Fact]
    public async Task LecturaCoincidente_AutorizadaEsSoloDiagnosticoInerte()
    {
        var (actor, snapshot) = Autorizado();
        var fuente = new Fuente { Actual = snapshot };
        var local = new GobiernoCambiosProyeccionAislada();
        Assert.True(local.IntentarRegistrar(Lectura()));

        var resultado = await Integracion(fuente, local)
            .ConsultarAsync(actor, 11, Solicitud());

        Assert.True(resultado.CopiaLocalCoincideConSolicitud);
        Assert.Equal("PENDIENTE_AUTORIZACION", resultado.Preparacion.Estado);
        Assert.False(resultado.AutorizaEjecucion);
        Assert.False(resultado.AutorizaAprobacion);
        Assert.False(resultado.AutorizaPersistenciaOficial);
        Assert.False(resultado.AutorizaConcesionPermisos);
        Assert.Equal(1, fuente.Consultas);
        Assert.Equal(1, local.Cantidad);
    }

    [Fact]
    public async Task ShaORevisionDeOtraCopia_NoSeConviertenEnCoincidencia()
    {
        var (actor, snapshot) = Autorizado();
        var local = new GobiernoCambiosProyeccionAislada();
        Assert.True(local.IntentarRegistrar(Lectura(new string('a', 40))));
        Assert.True(local.IntentarRegistrar(Lectura(revision: 8)));
        var resultado = await Integracion(new Fuente { Actual = snapshot }, local)
            .ConsultarAsync(actor, 11, Solicitud());

        Assert.False(resultado.CopiaLocalCoincideConSolicitud);
        Assert.False(resultado.AutorizaEjecucion);
        Assert.False(resultado.AutorizaPersistenciaOficial);
    }

    [Fact]
    public async Task TenantB_NoInvocaProveedorNiLeeCopiaLocal()
    {
        var (actor, snapshot) = Autorizado();
        var fuente = new Fuente { Actual = snapshot };
        var local = new GobiernoCambiosProyeccionAislada();
        Assert.True(local.IntentarRegistrar(Lectura()));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Integracion(fuente, local).ConsultarAsync(actor, 22, Solicitud()));
        Assert.Equal(0, fuente.Consultas);
        Assert.Equal(1, local.Cantidad);
    }

    [Fact]
    public async Task MembresiaRevocada_NoPermiteResultadoAunqueCopiaCoincida()
    {
        var (actor, snapshot) = Autorizado();
        snapshot.Membresia.Desactivar();
        var local = new GobiernoCambiosProyeccionAislada();
        Assert.True(local.IntentarRegistrar(Lectura()));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Integracion(new Fuente { Actual = snapshot }, local)
                .ConsultarAsync(actor, 11, Solicitud()));
        Assert.Equal(1, local.Cantidad);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProveedorSinSnapshotOFallido_NoEntregaExito(bool falla)
    {
        var (actor, _) = Autorizado();
        var fuente = new Fuente { Actual = null, Fallar = falla };
        var local = new GobiernoCambiosProyeccionAislada();
        Assert.True(local.IntentarRegistrar(Lectura()));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Integracion(fuente, local).ConsultarAsync(actor, 11, Solicitud()));
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task CopiaCoincidente_CambioSensibleSiempreDevuelveNoGo()
    {
        var (actor, snapshot) = Autorizado();
        var local = new GobiernoCambiosProyeccionAislada();
        Assert.True(local.IntentarRegistrar(Lectura()));
        var resultado = await Integracion(new Fuente { Actual = snapshot }, local)
            .ConsultarAsync(actor, 11, Solicitud(TipoCambioGobierno.SeguridadYPermisos));
        Assert.True(resultado.CopiaLocalCoincideConSolicitud);
        Assert.Equal("NO_GO", resultado.Preparacion.Estado);
        Assert.False(resultado.AutorizaEjecucion);
    }

    [Fact]
    public async Task CopiaConcurrente_SoloUnaRegistroPeroNoConcedePermisos()
    {
        var local = new GobiernoCambiosProyeccionAislada();
        var intentos = Enumerable.Range(0, 24)
            .Select(_ => Task.Run(() => local.IntentarRegistrar(Lectura())))
            .ToArray();
        var resultados = await Task.WhenAll(intentos);
        Assert.Equal(1, resultados.Count(r => r));
        Assert.Equal(1, local.Cantidad);
        Assert.False(local.Obtener("C00-04.M05.03", 7)!.AutorizaEjecucion);
        Assert.False(local.Obtener("C00-04.M05.03", 7)!.AutorizaPersistenciaOficial);
    }
}
