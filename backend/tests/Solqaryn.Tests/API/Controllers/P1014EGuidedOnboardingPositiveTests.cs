using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-014.E: regresión positiva dirigida de la confirmación del asistente.
/// Reutiliza el caso integrado que comprueba CreatedResult, Empresa,
/// ConfigEmpresa y UsuarioEmpresa con autoridad de plataforma y flags activos.
/// También ejecuta su negativa, sin HTTP remoto ni MySQL DEV.
/// </summary>
public sealed class P1014EGuidedOnboardingPositiveTests
{
    [Fact]
    public async Task P1014E_ConfirmacionGuiadaCreaEmpresaConfiguracionYMembresiaConAutorizacion()
    {
        await new PlataformaEmpresasOnboardingSecurityTests()
            .P1014A_ConfirmacionGuiadaNoEludeGatesNiCreaDatosSinAutorizacion();
    }
}
