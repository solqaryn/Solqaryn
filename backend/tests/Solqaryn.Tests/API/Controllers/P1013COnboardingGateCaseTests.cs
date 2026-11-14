using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-013.C: fixture sintetico de un solo gate. Prepara un caso negativo
/// sin habilitar flags DEV ni modificar el backend productivo.
/// </summary>
public sealed class P1013COnboardingGateCaseTests
{
    [Fact]
    public async Task P1013C_AislamientoOffImpideAltaAunqueOnboardingEsteOn()
    {
        // Reutiliza una regresion ya existente con autoridad global autorizada:
        // onboarding ON, aislamiento OFF => 503 y cero efectos persistidos.
        await new PlataformaEmpresasOnboardingSecurityTests()
            .P1013A_AltaRechazadaPorCualquierGateApagado(
                onboarding: true,
                aislamiento: false);
    }
}
