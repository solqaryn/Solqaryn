using System.Reflection;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-011.B: cierre de la cadena C/D/E sin duplicar transacciones.
/// La regresion positiva se ejecuta en el gate dirigido. El negativo
/// transaccional se ejecuta SOLO en el job Integration con MySQL 8.4.
/// </summary>
public sealed class P1011BAtomicAdminClosureTests
{
    [Fact]
    public async Task P1011B_AltaExitosaConservaBCryptRolMembresiaYAuditoria()
    {
        await new P1011EPositiveAdminOnboardingRegressionTests()
            .P1011E_NuevoAdministradorSeCreaConRolEmpresarialYAuditoria();
    }

    [Fact]
    public void P1011B_RollbackMySqlPorAuditoriaPermaneceComoRegresionDeIntegracion()
    {
        var integration = typeof(AltaAdminNuevoAuditoriaRollbackRealMySqlTests);
        Assert.Contains(integration.CustomAttributes, attribute =>
            attribute.AttributeType == typeof(TraitAttribute)
            && attribute.ConstructorArguments.Count == 2
            && Equals(attribute.ConstructorArguments[0].Value, "Category")
            && Equals(attribute.ConstructorArguments[1].Value, "Integration"));

        var method = integration.GetMethod(
            nameof(AltaAdminNuevoAuditoriaRollbackRealMySqlTests
                .AuditoriaFallida_CausaRollbackRealDeAltaAdministradorNuevo),
            BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<FactAttribute>());

        // Fixture real: tambien inyecta fallos de ConfigEmpresa y membresia,
        // ademas de RegistroAuditoria, y consulta ausencia de usuarios huerfanos.
        var original = typeof(PlataformaEmpresasMySqlTransactionIntegrationTests)
            .GetMethod("AltaA_B_EsTransaccional_ConAuditoriaYRollbackEnMySqlReal",
                BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(original);
        Assert.NotNull(original!.GetCustomAttribute<FactAttribute>());
    }
}
