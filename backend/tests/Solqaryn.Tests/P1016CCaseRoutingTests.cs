using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-016.C: caso dirigido de denegación de sesión global desde una identidad
/// empresarial, aun al enviar X-Empresa-Id. Reutiliza la prueba P1-016.A real.
/// La fixture EF InMemory no acredita HTTP/JWT ni MySQL remotos.
/// </summary>
public sealed class P1016CCaseRoutingTests
{
    [Fact]
    public async Task P1016C_AdminEmpresarialNoAccedeSesionGlobalAunqueEnvieTenantHeader()
    {
        await new P1016APlatformWithoutTenantTests()
            .P1016A_AdminEmpresarialNoHeredaAccesoGlobalPorRolNiPorHeader();
    }
}
