using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-011.E: prueba positiva dirigida de alta empresarial con administrador nuevo.
/// Reutiliza aserciones causales de P1-011.A sobre rol empresarial, BCrypt,
/// membresia unica, configuracion y auditoria sin exponer credenciales.
/// </summary>
public sealed class P1011EPositiveAdminOnboardingRegressionTests
{
    [Fact]
    public async Task P1011E_NuevoAdministradorSeCreaConRolEmpresarialYAuditoria()
    {
        await new PlataformaEmpresasOnboardingSecurityTests()
            .P1011A_NuevoAdministradorSeCreaConBCryptRolBaseYUnaMembresiaDeEmpresa();
    }
}
