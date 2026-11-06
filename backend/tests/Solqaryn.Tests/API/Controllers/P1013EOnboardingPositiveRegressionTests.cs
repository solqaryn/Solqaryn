using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-013.E: regresion positiva sintética: ambos gates ON permiten el alta
/// exclusivamente bajo autoridad de plataforma sin escalada global.
/// </summary>
public sealed class P1013EOnboardingPositiveRegressionTests
{
    [Fact]
    public async Task P1013E_DobleGateOnPermiteAltaControladaConAuditoria()
    {
        var regression = new PlataformaEmpresasOnboardingSecurityTests();
        await regression.P1013A_DirectorioNoAnunciaAltaSinDobleGate(
            onboarding: true, aislamiento: true, puedeCrear: true);
        await regression.P1012A_PlataformaCreaEmpresaConAdminRegistradoSinEscaladaGlobal();
    }
}
