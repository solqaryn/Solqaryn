using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C01-01.M05.04: aceptación positiva REAL del contrato Application/Domain
/// de identidad y pertenencia empresarial. Ejecuta código productivo,
/// con fuente de snapshots aislada en memoria. No certifica HTTP/JWT, EF,
/// MySQL, datos reales ni sesión de navegador.
/// </summary>
public sealed class V5C0101M0504PositiveAcceptanceTests
{
    private sealed class FuenteVerificable : IIdentidadUniversalSnapshotSource
    {
        private readonly Dictionary<int, IdentidadUniversalSnapshot> _porEmpresa;
        public List<(int UsuarioId, int EmpresaId, int? SucursalId)> Consultas { get; } = new();

        public FuenteVerificable(params IdentidadUniversalSnapshot[] snapshots)
        {
            _porEmpresa = snapshots.ToDictionary(x => x.Empresa.Id);
        }

        public Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
            int usuarioId, int empresaId, int? sucursalId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Consultas.Add((usuarioId, empresaId, sucursalId));
            _porEmpresa.TryGetValue(empresaId, out var actual);
            return Task.FromResult(actual);
        }
    }

    private static IdentidadUniversalSnapshot Snapshot(int empresaId, int rolId, int? sucursalId = null)
    {
        var empresa = new Empresa($"Empresa {empresaId}") { Id = empresaId };
        var usuario = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var membresia = new UsuarioEmpresa(usuario.Id, empresaId, rolId);
        var rol = new Rol { Id = rolId, EmpresaId = empresaId, Activo = true };
        var sucursal = sucursalId.HasValue
            ? new Sucursal { Id = sucursalId.Value, EmpresaId = empresaId }
            : null;
        return new IdentidadUniversalSnapshot(usuario, empresa, membresia, rol, sucursal);
    }

    private static ContextoTenantActual Actor(IdentidadUniversalSnapshot snapshot)
        => IdentidadUniversalReglas.CrearContextoVerificado(
            snapshot.Usuario, snapshot.Empresa, snapshot.Membresia, snapshot.Rol);

    [Fact]
    public async Task DosEmpresasConMismoUsuario_ConservanRolYContextoPropios()
    {
        var a = Snapshot(11, 19);
        var b = Snapshot(22, 29);
        var fuente = new FuenteVerificable(a, b);
        var servicio = new IdentidadUniversalApplicationService(fuente);

        var contextoA = await servicio.ResolverActualAsync(Actor(a), 11);
        var contextoB = await servicio.ResolverActualAsync(Actor(b), 22);

        Assert.Equal(7, contextoA.UsuarioId);
        Assert.Equal(7, contextoB.UsuarioId);
        Assert.Equal(11, contextoA.EmpresaId);
        Assert.Equal(19, contextoA.RolId);
        Assert.Equal(22, contextoB.EmpresaId);
        Assert.Equal(29, contextoB.RolId);
        Assert.Equal(new[] { 11, 22 }, fuente.Consultas.Select(x => x.EmpresaId));
    }

    [Fact]
    public async Task SucursalPropia_SeResuelveEnServidorSinAdquirirOtraImplicitamente()
    {
        var a = Snapshot(11, 19, sucursalId: 3);
        var fuente = new FuenteVerificable(a);
        var resuelto = await new IdentidadUniversalApplicationService(fuente)
            .ResolverActualAsync(Actor(a), 11, sucursalId: 3);

        Assert.Equal(11, resuelto.EmpresaId);
        Assert.Equal(19, resuelto.RolId);
        Assert.Single(fuente.Consultas);
        Assert.Equal(3, fuente.Consultas[0].SucursalId);
    }

    [Fact]
    public async Task ContextoVigente_RevalidadoEnCadaPeticionPreservaEmpresaYRol()
    {
        var a = Snapshot(11, 19);
        var fuente = new FuenteVerificable(a);
        var servicio = new IdentidadUniversalApplicationService(fuente);
        var actor = Actor(a);

        for (var n = 0; n < 3; n++)
        {
            var actual = await servicio.ResolverActualAsync(actor, 11);
            Assert.Equal(11, actual.EmpresaId);
            Assert.Equal(19, actual.RolId);
        }

        Assert.Equal(3, fuente.Consultas.Count);
        Assert.All(fuente.Consultas, x =>
        {
            Assert.Equal(7, x.UsuarioId);
            Assert.Equal(11, x.EmpresaId);
            Assert.Null(x.SucursalId);
        });
    }
}
