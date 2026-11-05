using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// P1-011.D ejecuta el negativo real de rollback MySQL del alta atómica.
/// Reutiliza el fixture P1-011.A (BD efímera y fallo interceptado de auditoría)
/// sin crear datos remotos ni sustituirlo con EF InMemory o reflexión.
/// </summary>
public sealed class P1011DMySqlAuditRollbackTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task P1011D_AltaAdministradorNuevo_AuditoriaFallida_NoDejaPersistenciaParcial()
    {
        // El test reutilizado ejecuta el alta con administrador nuevo y la falla
        // de RegistroAuditoria; verifica después, desde otro DbContext, que no
        // existan usuario, empresa, configuración, membresía ni auditoría
        // residuales. Destruye en finally su base MySQL temporal.
        await new PlataformaEmpresasMySqlTransactionIntegrationTests()
            .AltaA_B_EsTransaccional_ConAuditoriaYRollbackEnMySqlReal();
    }
}
