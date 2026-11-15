using Microsoft.Extensions.Logging;
using Solqaryn.Application.Governance;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Governance;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class GobiernoCambiosAuditoriaServiceTests
{
    private const string Sha = "fbd8fbb9ffbe4c500ebc831d50421c887cd8fe63";
    private static readonly Guid Correlacion =
        Guid.Parse("84b115a3-c7aa-4aa3-9cb6-580d04cd6550");

    private sealed class Fuente : IIdentidadUniversalSnapshotSource
    {
        public IdentidadUniversalSnapshot? Actual { get; init; }
        public int Consultas { get; private set; }

        public Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
            int usuarioId, int empresaId, int? sucursalId,
            CancellationToken cancellationToken)
        {
            Consultas++;
            return Task.FromResult(Actual);
        }
    }

    private sealed class LoggerCaptura : ILogger<GobiernoCambiosAuditoriaService>
    {
        public List<string> Mensajes { get; } = new();
        public List<LogLevel> Niveles { get; } = new();
        public List<EventId> Eventos { get; } = new();
        public bool Enabled { get; set; } = true;
        public bool ThrowOnWrite { get; set; }

        public bool IsEnabled(LogLevel level) => Enabled;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public void Log<TState>(LogLevel level, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (ThrowOnWrite)
                throw new InvalidOperationException("writer disconnected with internal secret");
            Niveles.Add(level);
            Eventos.Add(eventId);
            Mensajes.Add(formatter(state, exception));
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
        return (IdentidadUniversalReglas.CrearContextoVerificado(
            usuario, empresa, membresia, rol),
            new IdentidadUniversalSnapshot(usuario, empresa, membresia, rol, null));
    }

    private static SolicitudPreparacionCambio Solicitud(
        TipoCambioGobierno tipo = TipoCambioGobierno.CodigoDev) =>
        new("C00-04.M03.06", "D01", "JWT ultra-secret-value",
            Sha, "secret-idempotency-key", 7, tipo, true);

    private static GobiernoCambiosAuditoriaService Crear(
        Fuente fuente, LoggerCaptura logger) =>
        new(new GobiernoCambiosAuthorizationService(
            new IdentidadUniversalAuthorizationGuard(fuente),
            new GobiernoCambiosPreparacionService()), logger);

    [Fact]
    public async Task AutorizadoSoloPreparaYRegistraResultadoControlado()
    {
        var (actor, snapshot) = Escenario();
        var log = new LoggerCaptura();
        var fuente = new Fuente { Actual = snapshot };
        var result = await Crear(fuente, log).PrepararConsultaAuditadaAsync(
            actor, 11, Solicitud(), Correlacion);

        Assert.Equal("PENDIENTE_AUTORIZACION", result.Estado);
        Assert.False(result.AutorizaEjecucion);
        Assert.False(result.AutorizaPersistencia);
        Assert.False(result.AutorizaConcesionPermisos);
        Assert.Equal(1, fuente.Consultas);
        Assert.Equal(LogLevel.Information, Assert.Single(log.Niveles));
        Assert.Equal(4606, Assert.Single(log.Eventos).Id);
        Assert.Contains(Correlacion.ToString(), Assert.Single(log.Mensajes));
        Assert.DoesNotContain("ultra-secret", log.Mensajes[0]);
        Assert.DoesNotContain("secret-idempotency", log.Mensajes[0]);
        Assert.DoesNotContain("Empresa A", log.Mensajes[0]);
    }

    [Fact]
    public async Task TenantBEsDenegadoAntesDeConsultarFuenteSinFiltrarDatos()
    {
        var (actor, snapshot) = Escenario();
        var log = new LoggerCaptura();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Crear(fuente, log).PrepararConsultaAuditadaAsync(
                actor, 22, Solicitud(), Correlacion));
        Assert.Equal(0, fuente.Consultas);
        Assert.Equal(LogLevel.Warning, Assert.Single(log.Niveles));
        Assert.Equal(4607, Assert.Single(log.Eventos).Id);
        Assert.Contains("ACCESO_DENEGADO_O_AUDITORIA", log.Mensajes[0]);
        Assert.DoesNotContain("22", log.Mensajes[0]);
    }

    [Fact]
    public async Task MembresiaRevocadaSiempreDeniegaYSeAudita()
    {
        var (actor, snapshot) = Escenario();
        snapshot.Membresia.Desactivar();
        var log = new LoggerCaptura();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Crear(new Fuente { Actual = snapshot }, log)
                .PrepararConsultaAuditadaAsync(actor, 11, Solicitud(), Correlacion));
        Assert.Equal(LogLevel.Warning, Assert.Single(log.Niveles));
    }

    [Fact]
    public async Task CambioSensibleRegistraNoGoPeroNuncaAprueba()
    {
        var (actor, snapshot) = Escenario();
        var log = new LoggerCaptura();
        var result = await Crear(new Fuente { Actual = snapshot }, log)
            .PrepararConsultaAuditadaAsync(actor, 11,
                Solicitud(TipoCambioGobierno.SeguridadYPermisos), Correlacion);
        Assert.Equal("NO_GO", result.Estado);
        Assert.False(result.AutorizaEjecucion);
        Assert.Contains("NO_GO", Assert.Single(log.Mensajes));
    }

    [Fact]
    public async Task EntradaForjadaNuncaSeEmiteEnLog()
    {
        var (actor, snapshot) = Escenario();
        var log = new LoggerCaptura();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Crear(fuente, log).PrepararConsultaAuditadaAsync(
                actor, 11, Solicitud() with
                {
                    MicrotareaId = "C00-04.M03.06\nBearer unsafe"
                }, Correlacion));
        Assert.Equal(0, fuente.Consultas);
        Assert.Contains("ENTRADA_INVALIDA", Assert.Single(log.Mensajes));
        Assert.DoesNotContain("Bearer", log.Mensajes[0]);
    }

    [Fact]
    public async Task CanalDeshabilitadoNoPermiteNiConsulta()
    {
        var (actor, snapshot) = Escenario();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Crear(fuente, new LoggerCaptura { Enabled = false })
                .PrepararConsultaAuditadaAsync(actor, 11, Solicitud(), Correlacion));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public async Task ErrorDelProveedorNoSeConvierteEnPreparacionExitosa()
    {
        var (actor, snapshot) = Escenario();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Crear(new Fuente { Actual = snapshot },
                new LoggerCaptura { ThrowOnWrite = true })
                .PrepararConsultaAuditadaAsync(actor, 11, Solicitud(), Correlacion));
    }

    [Fact]
    public async Task CorrelacionVaciaSeRechazaAntesDeIO()
    {
        var (actor, snapshot) = Escenario();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Crear(fuente, new LoggerCaptura())
                .PrepararConsultaAuditadaAsync(actor, 11, Solicitud(), Guid.Empty));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public void DependenciasNulasNoSeAceptan()
    {
        var log = new LoggerCaptura();
        var fuente = new Fuente();
        var auth = new GobiernoCambiosAuthorizationService(
            new IdentidadUniversalAuthorizationGuard(fuente),
            new GobiernoCambiosPreparacionService());
        Assert.Throws<ArgumentNullException>(() =>
            new GobiernoCambiosAuditoriaService(null!, log));
        Assert.Throws<ArgumentNullException>(() =>
            new GobiernoCambiosAuditoriaService(auth, null!));
    }
}
