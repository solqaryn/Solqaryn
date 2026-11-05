using System.Reflection;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-011.C prepara un negativo sin duplicar ni ejecutar MySQL en la CI dirigida.
/// Esta prueba solamente protege la ruta hacia el fixture de integración real.
/// No prueba el rollback; eso pertenece al job MySQL Category=Integration.
/// </summary>
public sealed class P1011CCaseRoutingTests
{
    [Fact]
    public void P1011C_AuditoriaFallida_ConservaRutaDeIntegracionMySql()
    {
        var tipo = typeof(PlataformaEmpresasMySqlTransactionIntegrationTests);
        Assert.Contains(tipo.GetCustomAttributes<TraitAttribute>(),
            atributo => atributo.Name == "Category" && atributo.Value == "Integration");

        var metodo = tipo.GetMethod(
            "AltaA_B_EsTransaccional_ConAuditoriaYRollbackEnMySqlReal",
            BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(metodo);
        Assert.NotNull(metodo!.GetCustomAttribute<FactAttribute>());
        Assert.Equal(typeof(Task), metodo.ReturnType);
    }
}
