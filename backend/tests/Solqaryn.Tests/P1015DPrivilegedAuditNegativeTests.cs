using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-015.D: rechazo dirigido de autodelegacion privilegiada.
/// Reutiliza las aserciones reales de P1-015.A sobre ForbidResult, auditoria
/// Denegado, correlacion, EmpresaId nulo y ausencia de roles nuevos.
/// EF InMemory no acredita persistencia MySQL ni autenticacion JWT remota.
/// </summary>
public sealed class P1015DPrivilegedAuditNegativeTests
{
    [Fact]
    public async Task P1015D_AutodelegacionNoConcedeAccesoYRegistraDenegacion()
    {
        await new P1015APrivilegedAccessAuditTests()
            .SelfGrant_IsDeniedAndAuditedWithoutNewPrivilege();
    }
}
