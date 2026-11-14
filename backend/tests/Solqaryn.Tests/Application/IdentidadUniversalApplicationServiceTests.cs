using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class IdentidadUniversalApplicationServiceTests
{
    private static (ContextoTenantActual Contexto, IdentidadUniversalSnapshot Snapshot) ValidarBase(
        bool conSucursal = false)
    {
        var usuario = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var empresa = new Empresa("Empresa A") { Id = 11 };
        var membresia = new UsuarioEmpresa(7, 11, 19);
        var rol = new Rol { Id = 19, EmpresaId = 11, Activo = true };
        var contexto = IdentidadUniversalReglas.CrearContextoVerificado(
            usuario, empresa, membresia, rol);
        var sucursal = conSucursal ? new Sucursal { Id = 3, EmpresaId = 11 } : null;
        return (contexto,
            new IdentidadUniversalSnapshot(usuario, empresa, membresia, rol, sucursal));
    }

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

    [Fact]
    public async Task ActorA_MembresiaActualA_ObtieneContextoSinRolLegacy()
    {
        var (actor, snapshot) = ValidarBase();
        var fuente = new Fuente { Actual = snapshot };
        var verificado = await new IdentidadUniversalApplicationService(fuente)
            .ResolverActualAsync(actor, 11);
        Assert.Equal(7, verificado.UsuarioId);
        Assert.Equal(11, verificado.EmpresaId);
        Assert.Equal(19, verificado.RolId);
        Assert.Equal(1, fuente.Consultas);
    }

    [Fact]
    public async Task ActorA_NuncaConsultaEmpresaB()
    {
        var (actor, snapshot) = ValidarBase();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(fuente)
                .ResolverActualAsync(actor, 22));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public async Task MembresiaRevocada_SeDeniegaInclusoConContextoAntiguo()
    {
        var (actor, snapshot) = ValidarBase();
        snapshot.Membresia.Desactivar();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(new Fuente { Actual = snapshot })
                .ResolverActualAsync(actor, 11));
    }

    [Fact]
    public async Task CambioDeRol_NoConservaPermisosDelContextoViejo()
    {
        var (actor, snapshot) = ValidarBase();
        snapshot.Membresia.CambiarRol(20);
        var nuevoRol = new Rol { Id = 20, EmpresaId = 11, Activo = true };
        var actualizado = snapshot with { Rol = nuevoRol };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(new Fuente { Actual = actualizado })
                .ResolverActualAsync(actor, 11));
    }

    [Fact]
    public async Task SinSnapshotActual_FallaCerrado()
    {
        var (actor, _) = ValidarBase();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(new Fuente())
                .ResolverActualAsync(actor, 11));
    }

    [Fact]
    public async Task SucursalA_Verificada_SeAcepta()
    {
        var (actor, snapshot) = ValidarBase(conSucursal: true);
        var actual = await new IdentidadUniversalApplicationService(
                new Fuente { Actual = snapshot })
            .ResolverActualAsync(actor, 11, 3);
        Assert.Equal(11, actual.EmpresaId);
    }

    [Fact]
    public async Task SucursalB_AunqueCoincidaElId_Denegada()
    {
        var (actor, snapshot) = ValidarBase(conSucursal: true);
        snapshot.Sucursal!.EmpresaId = 22;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(new Fuente { Actual = snapshot })
                .ResolverActualAsync(actor, 11, 3));
    }

    [Fact]
    public async Task SucursalNoSolicitada_NoSeAdquiereImplicitamente()
    {
        var (actor, snapshot) = ValidarBase(conSucursal: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(new Fuente { Actual = snapshot })
                .ResolverActualAsync(actor, 11));
    }

    [Fact]
    public async Task IdDeSucursalDistinto_SeDeniega()
    {
        var (actor, snapshot) = ValidarBase(conSucursal: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(new Fuente { Actual = snapshot })
                .ResolverActualAsync(actor, 11, 4));
    }

    [Fact]
    public async Task SucursalInvalida_NoIniciaConsulta()
    {
        var (actor, snapshot) = ValidarBase();
        var fuente = new Fuente { Actual = snapshot };
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            new IdentidadUniversalApplicationService(fuente)
                .ResolverActualAsync(actor, 11, 0));
        Assert.Equal(0, fuente.Consultas);
    }

    [Fact]
    public async Task IdentidadDeOtroUsuario_NoSeAceptaDesdeFuente()
    {
        var (actor, snapshot) = ValidarBase();
        snapshot.Usuario.Id = 8;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new IdentidadUniversalApplicationService(new Fuente { Actual = snapshot })
                .ResolverActualAsync(actor, 11));
    }
}
