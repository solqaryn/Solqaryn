using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C01-01.M05.06: regresión real de la revalidación tenant y costo acotado
/// de lecturas Application. No afirma latencia p95, HTTP/JWT, DB ni producción.
/// </summary>
public sealed class V5C0101M0506RegresionTests
{
    private sealed class FuenteContada : IIdentidadUniversalSnapshotSource
    {
        private readonly Dictionary<int, IdentidadUniversalSnapshot> _datos;
        public int Lecturas { get; private set; }
        public FuenteContada(params IdentidadUniversalSnapshot[] datos)
            => _datos = datos.ToDictionary(x => x.Empresa.Id);

        public Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
            int usuarioId, int empresaId, int? sucursalId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Lecturas++;
            _datos.TryGetValue(empresaId, out var valor);
            return Task.FromResult(valor);
        }
    }

    private static IdentidadUniversalSnapshot Crear(int empresaId, int rolId, int? sucursalId = null)
    {
        var usuario = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var empresa = new Empresa($"Empresa {empresaId}") { Id = empresaId };
        var membresia = new UsuarioEmpresa(usuario.Id, empresaId, rolId);
        var rol = new Rol { Id = rolId, EmpresaId = empresaId, Activo = true };
        var sucursal = sucursalId.HasValue
            ? new Sucursal { Id = sucursalId.Value, EmpresaId = empresaId }
            : null;
        return new IdentidadUniversalSnapshot(usuario, empresa, membresia, rol, sucursal);
    }

    private static ContextoTenantActual Actor(IdentidadUniversalSnapshot x)
        => IdentidadUniversalReglas.CrearContextoVerificado(
            x.Usuario, x.Empresa, x.Membresia, x.Rol);

    [Fact]
    public async Task Regresion_128ResolucionesAlternadas_NoReusanContextoDeOtroTenant()
    {
        var a = Crear(11, 19);
        var b = Crear(22, 29);
        var fuente = new FuenteContada(a, b);
        var servicio = new IdentidadUniversalApplicationService(fuente);
        var actorA = Actor(a);
        var actorB = Actor(b);

        for (var i = 0; i < 64; i++)
        {
            var contextoA = await servicio.ResolverActualAsync(actorA, 11);
            var contextoB = await servicio.ResolverActualAsync(actorB, 22);
            Assert.Equal((11, 19), (contextoA.EmpresaId, contextoA.RolId));
            Assert.Equal((22, 29), (contextoB.EmpresaId, contextoB.RolId));
            Assert.Throws<InvalidOperationException>(() => contextoA.ExigirEmpresa(22));
            Assert.Throws<InvalidOperationException>(() => contextoB.ExigirEmpresa(11));
        }

        // Presupuesto determinista de I/O: una sola lectura confiable por resolución.
        // No usar cronómetro frágil ni convertir este conteo en métrica de producción.
        Assert.Equal(128, fuente.Lecturas);
    }

    [Fact]
    public async Task Regresion_64SolicitudesCrossTenant_SeRechazanSinAccesoAFuente()
    {
        var a = Crear(11, 19);
        var fuente = new FuenteContada(a);
        var servicio = new IdentidadUniversalApplicationService(fuente);
        var actor = Actor(a);

        for (var i = 0; i < 64; i++)
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => servicio.ResolverActualAsync(actor, 22));

        Assert.Equal(0, fuente.Lecturas);
    }

    [Fact]
    public async Task Regresion_CancelacionAntesDeLectura_NoEjecutaIOLegado()
    {
        var a = Crear(11, 19);
        var fuente = new FuenteContada(a);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var servicio = new IdentidadUniversalApplicationService(fuente);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => servicio.ResolverActualAsync(Actor(a), 11, cancellationToken: cts.Token));
        Assert.Equal(0, fuente.Lecturas);
    }

    [Fact]
    public async Task Regresion_RevocacionTrasExito_DeniegaSiguienteResolucion()
    {
        var a = Crear(11, 19);
        var fuente = new FuenteContada(a);
        var servicio = new IdentidadUniversalApplicationService(fuente);
        var actor = Actor(a);

        Assert.Equal(11, (await servicio.ResolverActualAsync(actor, 11)).EmpresaId);
        a.Membresia.Desactivar();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.ResolverActualAsync(actor, 11));
        Assert.Equal(2, fuente.Lecturas);
    }

    [Fact]
    public async Task Regresion_SucursalNoSolicitada_NoSeAdquiereImplicitamente()
    {
        var a = Crear(11, 19, 3);
        var fuente = new FuenteContada(a);
        var servicio = new IdentidadUniversalApplicationService(fuente);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.ResolverActualAsync(Actor(a), 11));
        Assert.Equal(1, fuente.Lecturas);
    }
}
