using Microsoft.Extensions.Logging;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application.Security;

public sealed class IdentidadUniversalAuditGuardTests
{
    private sealed class Fuente : IIdentidadUniversalSnapshotSource
    {
        public IdentidadUniversalSnapshot? Actual { get; set; }
        public Exception? Fallo { get; set; }
        public int Consultas { get; private set; }
        public Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
            int usuarioId, int empresaId, int? sucursalId, CancellationToken cancellationToken)
        {
            Consultas++;
            if (Fallo is not null) throw Fallo;
            return Task.FromResult(Actual);
        }
    }

    private sealed class CapturaLogger : ILogger<IdentidadUniversalAuditGuard>
    {
        public List<(LogLevel Nivel, int Evento, string Texto, Exception? Error)> Eventos { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            Eventos.Add((level, eventId.Id, formatter(state, exception), exception));
    }

    private static (ContextoTenantActual Actor, IdentidadUniversalSnapshot Actual) Preparar()
    {
        var usuario = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var empresa = new Empresa("Empresa Privada A") { Id = 11 };
        var membresia = new UsuarioEmpresa(7, 11, 19);
        var rol = new Rol { Id = 19, EmpresaId = 11, Activo = true };
        var permiso = new Permiso { Id = 47, Modulo = ModuloSistema.Usuarios,
            Accion = AccionPermiso.Ver, Ambito = AmbitoAutorizacion.Empresa, Activo = true };
        rol.Permisos.Add(new RolPermiso {
            RolId = 19, RolEntidad = rol, PermisoId = 47, Permiso = permiso
        });
        return (IdentidadUniversalReglas.CrearContextoVerificado(usuario, empresa, membresia, rol),
            new IdentidadUniversalSnapshot(usuario, empresa, membresia, rol, null));
    }

    private static IdentidadUniversalAuditGuard Servicio(Fuente fuente, CapturaLogger log) =>
        new(new IdentidadUniversalAuthorizationGuard(fuente), log);

    [Fact]
    public async Task TenantA_PermisoVigente_LogAllowSinPII()
    {
        var (actor, snapshot) = Preparar();
        var fuente = new Fuente { Actual = snapshot };
        var log = new CapturaLogger();
        var corr = Guid.NewGuid();
        var resultado = await Servicio(fuente, log).ExigirYAuditarAsync(
            actor, 11, ModuloSistema.Usuarios, AccionPermiso.Ver, corr);
        Assert.Equal(11, resultado.EmpresaId);
        Assert.Equal(1, fuente.Consultas);
        var evento = Assert.Single(log.Eventos);
        Assert.Equal(10306, evento.Evento);
        Assert.Equal(LogLevel.Information, evento.Nivel);
        Assert.Contains("ALLOW", evento.Texto);
        Assert.Contains(corr.ToString(), evento.Texto);
        Assert.DoesNotContain("Empresa Privada A", evento.Texto);
        Assert.Null(evento.Error);
    }

    [Fact]
    public async Task TenantB_DenegadoAntesDeConsultarFuente()
    {
        var (actor, snapshot) = Preparar();
        var fuente = new Fuente { Actual = snapshot };
        var log = new CapturaLogger();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Servicio(fuente, log).ExigirYAuditarAsync(
                actor, 22, ModuloSistema.Usuarios, AccionPermiso.Ver, Guid.NewGuid()));
        Assert.Equal(0, fuente.Consultas);
        Assert.Contains("ACCESS_DENIED", Assert.Single(log.Eventos).Texto);
    }

    [Fact]
    public async Task MembresiaRevocada_DenegadaYAditada()
    {
        var (actor, snapshot) = Preparar();
        snapshot.Membresia.Desactivar();
        var log = new CapturaLogger();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Servicio(new Fuente { Actual = snapshot }, log).ExigirYAuditarAsync(
                actor, 11, ModuloSistema.Usuarios, AccionPermiso.Ver, Guid.NewGuid()));
        Assert.Equal(LogLevel.Warning, Assert.Single(log.Eventos).Nivel);
    }

    [Fact]
    public async Task RolAdministradorSinPermisoRelacional_SigueDenegado()
    {
        var (actor, snapshot) = Preparar();
        snapshot.Rol.EsAdministrador = true;
        snapshot.Rol.Permisos.Clear();
        var log = new CapturaLogger();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Servicio(new Fuente { Actual = snapshot }, log).ExigirYAuditarAsync(
                actor, 11, ModuloSistema.Usuarios, AccionPermiso.Ver, Guid.NewGuid()));
        Assert.DoesNotContain(log.Eventos, e => e.Texto.Contains("ALLOW"));
    }

    [Fact]
    public async Task SucursalInvalida_NoHaceIOYRegistraCodigoSeguro()
    {
        var (actor, snapshot) = Preparar();
        var fuente = new Fuente { Actual = snapshot };
        var log = new CapturaLogger();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Servicio(fuente, log).ExigirYAuditarAsync(
                actor, 11, ModuloSistema.Usuarios, AccionPermiso.Ver, Guid.NewGuid(), 0));
        Assert.Equal(0, fuente.Consultas);
        Assert.Contains("INVALID_SCOPE", Assert.Single(log.Eventos).Texto);
    }

    [Fact]
    public async Task ExcepcionInesperada_NoFiltraMensajeNiObjetoALog()
    {
        var (actor, _) = Preparar();
        var log = new CapturaLogger();
        var fuente = new Fuente { Fallo = new Exception("secreto privado") };
        await Assert.ThrowsAsync<Exception>(() =>
            Servicio(fuente, log).ExigirYAuditarAsync(
                actor, 11, ModuloSistema.Usuarios, AccionPermiso.Ver, Guid.NewGuid()));
        var evento = Assert.Single(log.Eventos);
        Assert.Equal(LogLevel.Error, evento.Nivel);
        Assert.Contains("INTERNAL_ERROR", evento.Texto);
        Assert.DoesNotContain("secreto privado", evento.Texto);
        Assert.Null(evento.Error);
    }

    [Fact]
    public async Task Cancelacion_NoEquivaleAErrorTecnico()
    {
        var (actor, _) = Preparar();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var log = new CapturaLogger();
        var fuente = new Fuente { Fallo = new OperationCanceledException(cts.Token) };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Servicio(fuente, log).ExigirYAuditarAsync(
                actor, 11, ModuloSistema.Usuarios, AccionPermiso.Ver,
                Guid.NewGuid(), cancellationToken: cts.Token));
        var evento = Assert.Single(log.Eventos);
        Assert.Equal(LogLevel.Debug, evento.Nivel);
        Assert.Contains("REQUEST_CANCELLED", evento.Texto);
    }

    [Fact]
    public async Task CorrelacionVacia_RechazadaAntesDeIO()
    {
        var (actor, snapshot) = Preparar();
        var fuente = new Fuente { Actual = snapshot };
        var log = new CapturaLogger();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Servicio(fuente, log).ExigirYAuditarAsync(
                actor, 11, ModuloSistema.Usuarios, AccionPermiso.Ver, Guid.Empty));
        Assert.Equal(0, fuente.Consultas);
        Assert.Empty(log.Eventos);
    }
}
