using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-013.B: cobertura negativa heredada para cierre de gates de onboarding.
/// No activa flags, no despliega servicios y no sustituye la evidencia C/D/E.
/// </summary>
public sealed class P1013BOnboardingGateClosureRegressionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task P1013B_NoSeAutorizaAltaConUnGateDeshabilitado(
        bool onboarding, bool aislamiento)
    {
        await new PlataformaEmpresasOnboardingSecurityTests()
            .P1013A_AltaRechazadaPorCualquierGateApagado(onboarding, aislamiento);
    }
}
