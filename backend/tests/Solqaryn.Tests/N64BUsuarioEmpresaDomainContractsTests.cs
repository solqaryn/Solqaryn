using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class N64BUsuarioEmpresaDomainContractsTests
{
    [Fact]
    public void Crear_establece_membresia_activa_con_rol_tenant_aware()
    {
        var membresia = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);

        Assert.Equal(7, membresia.UsuarioId);
        Assert.Equal(11, membresia.EmpresaId);
        Assert.Equal(3, membresia.RolId);
        Assert.True(membresia.Activa);
        Assert.Equal("7:11", membresia.ObtenerClaveLogica());
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(-1, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, -1, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, -1)]
    public void Crear_rechaza_identificadores_no_positivos(int usuarioId, int empresaId, int rolId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new UsuarioEmpresa(usuarioId, empresaId, rolId));
    }

    [Fact]
    public void Mismo_usuario_puede_tener_roles_distintos_en_empresas_distintas()
    {
        var empresaA = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);
        var empresaB = new UsuarioEmpresa(usuarioId: 7, empresaId: 12, rolId: 5);

        Assert.Equal(3, empresaA.ObtenerRolEfectivo(usuarioId: 7, empresaId: 11));
        Assert.Equal(5, empresaB.ObtenerRolEfectivo(usuarioId: 7, empresaId: 12));
        Assert.NotEqual(empresaA.ObtenerClaveLogica(), empresaB.ObtenerClaveLogica());
    }

    [Fact]
    public void ObtenerRolEfectivo_falla_cerrado_ante_contexto_ajeno_o_membresia_inactiva()
    {
        var membresia = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);

        Assert.Throws<InvalidOperationException>(
            () => membresia.ObtenerRolEfectivo(usuarioId: 8, empresaId: 11));
        Assert.Throws<InvalidOperationException>(
            () => membresia.ObtenerRolEfectivo(usuarioId: 7, empresaId: 12));

        membresia.Desactivar();

        Assert.Throws<InvalidOperationException>(
            () => membresia.ObtenerRolEfectivo(usuarioId: 7, empresaId: 11));
    }

    [Fact]
    public void CambiarRol_preserva_la_identidad_logica_y_valida_el_nuevo_rol()
    {
        var membresia = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);
        var clave = membresia.ObtenerClaveLogica();

        membresia.CambiarRol(5);

        Assert.Equal(clave, membresia.ObtenerClaveLogica());
        Assert.Equal(5, membresia.ObtenerRolEfectivo(usuarioId: 7, empresaId: 11));
        Assert.Throws<ArgumentOutOfRangeException>(() => membresia.CambiarRol(0));
    }

    [Fact]
    public void Activar_y_desactivar_controlan_el_uso_de_la_membresia()
    {
        var membresia = new UsuarioEmpresa(usuarioId: 7, empresaId: 11, rolId: 3);

        membresia.Desactivar();
        Assert.False(membresia.Activa);

        membresia.Activar();
        Assert.True(membresia.Activa);
        Assert.Equal(3, membresia.ObtenerRolEfectivo(usuarioId: 7, empresaId: 11));
    }
}
