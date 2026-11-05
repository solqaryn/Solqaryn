using System.Reflection;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-011.D: el gate dirigido del PR carece de MySQL; valida SOLO que la
/// regresión de integración exista y esté registrada, no el rollback.
/// </summary>
public sealed class P1011DDirectedContractTests
{
    [Fact]
    public void P1011D_ExisteRegresionRealMySql_DeAltaAtomicaConAuditoriaFallida()
    {
        var integrationType = typeof(AltaAdminNuevoAuditoriaRollbackRealMySqlTests);
        Assert.Contains(integrationType.CustomAttributes, atributo =>
            atributo.AttributeType == typeof(TraitAttribute)
            && atributo.ConstructorArguments.Count == 2
            && Equals(atributo.ConstructorArguments[0].Value, "Category")
            && Equals(atributo.ConstructorArguments[1].Value, "Integration"));

        var method = integrationType.GetMethod(
            nameof(AltaAdminNuevoAuditoriaRollbackRealMySqlTests
                .AuditoriaFallida_CausaRollbackRealDeAltaAdministradorNuevo),
            BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<FactAttribute>());
        Assert.Equal(typeof(Task), method.ReturnType);

        // El test existente ejerce MySQL real y su propio fixture temporal.
        var original = typeof(PlataformaEmpresasMySqlTransactionIntegrationTests)
            .GetMethod(
                "AltaA_B_EsTransaccional_ConAuditoriaYRollbackEnMySqlReal",
                BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(original);
        Assert.NotNull(original!.GetCustomAttribute<FactAttribute>());
        Assert.Equal(typeof(Task), original.ReturnType);
    }
}

/// <summary>
/// Escojido por Category=Integration en CI MySQL 8.4 efímero, NO por el gate
/// dirigido sin base. Reutiliza la aserción causal original de rollback.
/// </summary>
[Trait("Category", "Integration")]
public sealed class AltaAdminNuevoAuditoriaRollbackRealMySqlTests
{
    [Fact]
    public async Task AuditoriaFallida_CausaRollbackRealDeAltaAdministradorNuevo()
    {
        // Si no existe ConnectionStrings__DefaultConnection el fixture original
        // falla cerrado. Nunca convertir la falta de MySQL en SKIP o PASS.
        await new PlataformaEmpresasMySqlTransactionIntegrationTests()
            .AltaA_B_EsTransaccional_ConAuditoriaYRollbackEnMySqlReal();
    }
}
