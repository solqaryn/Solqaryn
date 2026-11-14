using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-016.E: prueba positiva dirigida de sesion global sin tenant.
/// Reutiliza el escenario afirmativo de P1-016.A; EF InMemory no
/// acredita HTTP/JWT o MySQL DEV remotos.
/// </summary>
public sealed class P1016EDirectedPositiveTests
{
    [Fact]
    public async Task P1016E_SuperadminSinMembresiaConservaSesionGlobalConTenantInactivo()
    {
        await new P1016APlatformWithoutTenantTests()
            .P1016A_SuperadminSinMembresiasEntraAunqueEmpresaPilotoEsteInactiva();
    }
}
