using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-016.B: cierre dirigido de sesion de plataforma sin dependencia de empresa activa.
/// Reutiliza aserciones del caso base P1-016.A: este wrapper no sustituye JWT/HTTP/MySQL remoto.
/// </summary>
public sealed class P1016BPlatformWithoutTenantClosureTests
{
    [Fact]
    public async Task P1016B_SuperadminGlobalSinTenantEntraSinMembresia()
    {
        await new P1016APlatformWithoutTenantTests()
            .P1016A_SuperadminSinMembresiasEntraAunqueEmpresaPilotoEsteInactiva();
    }

    [Fact]
    public async Task P1016B_EmpresarialNoObtieneSesionGlobalPorHeader()
    {
        await new P1016APlatformWithoutTenantTests()
            .P1016A_AdminEmpresarialNoHeredaAccesoGlobalPorRolNiPorHeader();
    }

    [Fact]
    public async Task P1016B_IdentidadSinRolGlobalEsRechazada()
    {
        await new P1016APlatformWithoutTenantTests()
            .P1016A_UsuarioSinMembresiaNiAsignacionGlobalRecibeForbid();
    }

    [Fact]
    public async Task P1016B_UsuarioGlobalBloqueadoNoConservaAccesoPorJWT()
    {
        await new P1016APlatformWithoutTenantTests()
            .P1016A_UsuarioGlobalBloqueadoNoConservaSesionPorJwtAnterior();
    }

    [Fact]
    public async Task P1016B_SinAutenticacionHTTPSeRechazaAntesDeAuditoria()
    {
        await new P1016APlatformWithoutTenantTests()
            .P1016A_IdentidadHttpNoAutenticadaSeRechazaAntesDeAuditar();
    }
}
