using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-014.B: reutiliza el contrato del backend sobre respuestas Created y
/// rechazo sin filas. EF InMemory no demuestra COMMIT MySQL real.
/// La negativa de respuesta HTTP tardía está en la prueba Angular P1014B.
/// </summary>
public sealed class P1014BGuidedOnboardingCommitAcknowledgmentTests
{
    [Fact]
    public async Task P1014B_RespuestaCreatedSoloTrasAltaAutorizada_SinFilasEnRechazo()
    {
        await new PlataformaEmpresasOnboardingSecurityTests()
            .P1014A_ConfirmacionGuiadaNoEludeGatesNiCreaDatosSinAutorizacion();
    }
}
