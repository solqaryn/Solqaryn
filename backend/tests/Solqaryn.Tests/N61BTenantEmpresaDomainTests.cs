using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class N61BTenantEmpresaDomainTests
{
    [Fact]
    public void Crear_normaliza_nombre_y_activa_la_raiz()
    {
        var empresa = new Empresa("  Solqaryn Centro  ");

        Assert.Equal("Solqaryn Centro", empresa.Nombre);
        Assert.True(empresa.Activa);
        Assert.NotEqual(typeof(EmpresaConfiguracion), empresa.GetType());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_rechaza_nombre_vacio(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Empresa(nombre));
    }

    [Fact]
    public void CambiarNombre_aplica_la_misma_invariante()
    {
        var empresa = new Empresa("Empresa A");

        empresa.CambiarNombre("  Empresa B  ");

        Assert.Equal("Empresa B", empresa.Nombre);
        Assert.Throws<ArgumentException>(() => empresa.CambiarNombre(" "));
    }

    [Fact]
    public void Estado_activo_se_administra_sin_inventar_aislamiento_tenant()
    {
        var empresa = new Empresa("Empresa A");

        empresa.Desactivar();
        Assert.False(empresa.Activa);

        empresa.Activar();
        Assert.True(empresa.Activa);
    }
}
