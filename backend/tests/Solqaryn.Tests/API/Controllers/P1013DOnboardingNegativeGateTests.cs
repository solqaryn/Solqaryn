using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-013.D: regression negativa dirigida. El aislamiento certificado no
/// permite crear empresas cuando el gate de onboarding permanece apagado.
/// Solo datos sinteticos EF InMemory, sin tocar configuracion DEV.
/// </summary>
public sealed class P1013DOnboardingNegativeGateTests
{
    [Fact]
    public async Task P1013D_OnboardingOffRechazaAltaAunqueAislamientoEsteCertificado()
    {
        await new PlataformaEmpresasOnboardingSecurityTests()
            .P1013A_AltaRechazadaPorCualquierGateApagado(
                onboarding: false,
                aislamiento: true);
    }
}
