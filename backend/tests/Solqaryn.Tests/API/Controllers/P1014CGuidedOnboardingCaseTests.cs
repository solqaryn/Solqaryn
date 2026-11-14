using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-014.C: caso negativo acotado al contrato del asistente guiado.
/// El backend, no el cliente Angular, decide si un alta es admisible.
/// </summary>
public sealed class P1014CGuidedOnboardingCaseTests
{
    [Fact]
    public async Task P1014C_NoCrearEmpresaSinGateOnboarding()
    {
        // Fixture sintético: onboarding OFF, aislamiento ON y operador con
        // autoridad global. Reutiliza las aserciones del caso existente:
        // HTTP lógico 503 y cero Empresa/UsuarioEmpresa/ConfigEmpresa.
        await new PlataformaEmpresasOnboardingSecurityTests()
            .P1013A_AltaRechazadaPorCualquierGateApagado(
                onboarding: false,
                aislamiento: true);
    }
}
