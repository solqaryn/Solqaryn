using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests;

public class N65BTenantIsolationDomainContractsTests
{
    [Fact]
    public void Contexto_solo_se_materializa_desde_membresia_activa_coincidente()
    {
        var membresia = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);

        var contexto = ContextoTenantActual.DesdeMembresia(
            membresia,
            usuarioId: 7,
            empresaId: 11);

        Assert.Equal(7, contexto.UsuarioId);
        Assert.Equal(11, contexto.EmpresaId);
        Assert.Equal(3, contexto.RolId);
        Assert.Empty(typeof(ContextoTenantActual).GetConstructors());
    }

    [Fact]
    public void Rol_efectivo_permanece_aislado_por_empresa()
    {
        var empresaA = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);
        var empresaB = new UsuarioEmpresa(usuarioId: 7, empresaId: 12, rolId: 5);

        var contextoA = ContextoTenantActual.DesdeMembresia(empresaA, 7, 11);
        var contextoB = ContextoTenantActual.DesdeMembresia(empresaB, 7, 12);

        Assert.Equal(3, contextoA.RolId);
        Assert.Equal(5, contextoB.RolId);
        Assert.NotEqual(contextoA.EmpresaId, contextoB.EmpresaId);
    }

    [Fact]
    public void Fabrica_falla_cerrado_para_usuario_empresa_o_estado_no_coincidente()
    {
        var membresia = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);

        Assert.Throws<InvalidOperationException>(
            () => ContextoTenantActual.DesdeMembresia(membresia, 8, 11));
        Assert.Throws<InvalidOperationException>(
            () => ContextoTenantActual.DesdeMembresia(membresia, 7, 12));

        membresia.Desactivar();

        Assert.Throws<InvalidOperationException>(
            () => ContextoTenantActual.DesdeMembresia(membresia, 7, 11));
    }

    [Fact]
    public void Fabrica_rechaza_membresia_ausente()
    {
        Assert.Throws<ArgumentNullException>(
            () => ContextoTenantActual.DesdeMembresia(null!, 7, 11));
    }

    [Fact]
    public void ExigirEmpresa_rechaza_otro_tenant()
    {
        var membresia = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);
        var contexto = ContextoTenantActual.DesdeMembresia(membresia, 7, 11);

        Assert.Throws<InvalidOperationException>(() => contexto.ExigirEmpresa(12));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ExigirEmpresa_rechaza_identificador_invalido(int empresaId)
    {
        var membresia = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);
        var contexto = ContextoTenantActual.DesdeMembresia(membresia, 7, 11);

        Assert.Throws<ArgumentOutOfRangeException>(() => contexto.ExigirEmpresa(empresaId));
    }

    [Fact]
    public void ExigirEmpresa_acepta_unicamente_el_tenant_verificado()
    {
        var membresia = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);
        var contexto = ContextoTenantActual.DesdeMembresia(membresia, 7, 11);

        contexto.ExigirEmpresa(11);
    }
}
