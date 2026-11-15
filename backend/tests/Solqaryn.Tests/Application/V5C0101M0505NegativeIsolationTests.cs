using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C01-01.M05.05: rechazo negativo y aislamiento del servicio REAL de
/// identidad universal. Fuente de snapshots en memoria para no tocar DB.
/// Sin simulación de un JWT/HTTP real ni privilegios de producción.
/// </summary>
public sealed class V5C0101M0505NegativeIsolationTests
{
    private sealed class Fuente : IIdentidadUniversalSnapshotSource
    {
        public IdentidadUniversalSnapshot? Actual { get; set; }
        public int Consultas { get; private set; }

        public Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
            int usuarioId, int empresaId, int? sucursalId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Consultas++;
            return Task.FromResult(Actual);
        }
    }

    private static IdentidadUniversalSnapshot Snapshot(
        int usuarioId = 7, int empresaId = 11, int rolId = 19, int? sucursalId = null)
    {
        var u = new Usuario { Id = usuarioId, Activo = true, RolId = 999 };
        var e = new Empresa($"Empresa {empresaId}") { Id = empresaId };
        var m = new UsuarioEmpresa(usuarioId, empresaId, rolId);
        var r = new Rol { Id = rolId, EmpresaId = empresaId, Activo = true };
        var s = sucursalId.HasValue
            ? new Sucursal { Id = sucursalId.Value, EmpresaId = empresaId }
            : null;
        return new IdentidadUniversalSnapshot(u, e, m, r, s);
    }

    private static ContextoTenantActual Contexto(IdentidadUniversalSnapshot s)
        => IdentidadUniversalReglas.CrearContextoVerificado(
            s.Usuario, s.Empresa, s.Membresia, s.Rol);

    [Fact]
    public async Task SolicitudEmpresaB_SeRechazaAntesDeConsultarFuenteDeA()
    {
        var a = Snapshot();
        var fuente = new Fuente { Actual = a };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(fuente)
                .ResolverActualAsync(Contexto(a), 22));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public async Task FuenteDevuelveEmpresaB_AunqueSeSolicitoA_NoConcedeContexto()
    {
        var a = Snapshot();
        var fuente = new Fuente { Actual = Snapshot(empresaId: 22, rolId: 29) };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(fuente)
                .ResolverActualAsync(Contexto(a), 11));
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task UsuarioA1_NoHeredaMembresiaDeA2EnMismaEmpresa()
    {
        var a1 = Snapshot();
        var fuente = new Fuente { Actual = Snapshot(usuarioId: 8) };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(fuente)
                .ResolverActualAsync(Contexto(a1), 11));
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task RevocacionDeMembresia_SeDeniegaEnSiguienteResolucion()
    {
        var a = Snapshot();
        var fuente = new Fuente { Actual = a };
        var servicio = new IdentidadUniversalApplicationService(fuente);
        var actor = Contexto(a);

        Assert.Equal(11, (await servicio.ResolverActualAsync(actor, 11)).EmpresaId);
        a.Membresia.Desactivar();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ResolverActualAsync(actor, 11));
        Assert.Equal(2, fuente.Consultas);
    }

    [Fact]
    public async Task CambioDeRol_ImpideReutilizarContextoAutenticadoAntiguo()
    {
        var a = Snapshot();
        var fuente = new Fuente { Actual = a };
        var servicio = new IdentidadUniversalApplicationService(fuente);
        var actor = Contexto(a);

        Assert.Equal(19, (await servicio.ResolverActualAsync(actor, 11)).RolId);
        a.Membresia.CambiarRol(20);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ResolverActualAsync(actor, 11));
        Assert.Equal(2, fuente.Consultas);
    }

    [Fact]
    public async Task SucursalDeEmpresaB_SeRechazaTrasCambioDePropietario()
    {
        var a = Snapshot(sucursalId: 3);
        var fuente = new Fuente { Actual = a };
        var servicio = new IdentidadUniversalApplicationService(fuente);
        var actor = Contexto(a);

        Assert.Equal(11, (await servicio.ResolverActualAsync(actor, 11, 3)).EmpresaId);
        a.Sucursal!.EmpresaId = 22;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            servicio.ResolverActualAsync(actor, 11, 3));
        Assert.Equal(2, fuente.Consultas);
    }
}
