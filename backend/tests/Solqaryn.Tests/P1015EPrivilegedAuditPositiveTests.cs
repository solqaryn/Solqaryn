using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-015.E: caso positivo dirigido de concesion de autoridad global.
/// Reutiliza la prueba P1-015.A para comprobar CreatedResult, actor,
/// motivo normalizado, correlacion, EmpresaId null, ambito plataforma
/// y auditoria de exito. Solo EF InMemory; no HTTP/JWT/MySQL DEV remoto.
/// </summary>
public sealed class P1015EPrivilegedAuditPositiveTests
{
    [Fact]
    public async Task P1015E_ConcesionAutorizadaAuditaActorMotivoCorrelacionYAmbitoGlobal()
    {
        await new P1015APrivilegedAccessAuditTests()
            .PrivilegedWrite_RecordsActorReasonCorrelationAndPlatformScope();
    }
}
