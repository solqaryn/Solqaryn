using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-014.D: sin privilegio global, la confirmación del wizard no crea empresa.
/// El servidor aplica RBAC aunque Angular haya avanzado al paso 3.
/// </summary>
public sealed class P1014DGuidedOnboardingUnauthorizedTests
{
    [Fact]
    public async Task P1014D_AltaGuiadaSinPrivilegioGlobalNoCreaEmpresa()
    {
        // Reutiliza el fixture negativo del controlador real: gates ON,
        // operador empresarial sin rol global, ForbidResult (403 lógico),
        // cero registros Empresa y UsuarioEmpresa. No HTTP remoto/MySQL.
        await new PlataformaEmpresasOnboardingSecurityTests()
            .AdministradorEmpresarialSinRolDePlataformaNoPuedeCrearEmpresa();
    }
}
