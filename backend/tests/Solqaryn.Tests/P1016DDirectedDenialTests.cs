using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-016.D: rechazo dirigido de sesion global con rol empresarial y
/// X-Empresa-Id. Reutiliza la fixture negativa real de P1-016.A.
/// El alcance EF InMemory no demuestra HTTP/JWT ni MySQL remoto.
/// </summary>
public sealed class P1016DDirectedDenialTests
{
    [Fact]
    public async Task P1016D_AdministradorEmpresarialConTenantNoObtieneSesionGlobal()
    {
        await new P1016APlatformWithoutTenantTests()
            .P1016A_AdminEmpresarialNoHeredaAccesoGlobalPorRolNiPorHeader();
    }
}
