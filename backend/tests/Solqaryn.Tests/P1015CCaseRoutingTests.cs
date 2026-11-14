using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-015.C: ejecución dirigida de un único negativo ya cubierto por P1-015.A.
/// Reutiliza el fixture y las aserciones existentes de autodelegación denegada.
/// EF InMemory NO certifica MySQL ni HTTP/JWT externo.
/// </summary>
public sealed class P1015CCaseRoutingTests
{
    [Fact]
    public async Task P1015C_AutodelegacionPrivilegiada_DebeNegarseYAuditarse()
    {
        await new P1015APrivilegedAccessAuditTests()
            .SelfGrant_IsDeniedAndAuditedWithoutNewPrivilege();
    }
}
