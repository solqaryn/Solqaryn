using Solqaryn.Application.DTOs.Bancos;
using Xunit;

namespace Solqaryn.Tests;

public class ConciliacionBancariaContractsTests
{
    [Fact]
    public void Contracts_AssignCoreProperties()
    {
        var import = new ImportarEstadoCuentaRequestDto { CuentaBancariaId = 1, IdempotencyKey = "key1", Movimientos = new[] { new MovimientoEstadoCuentaDto { FechaOperacion = new DateTime(2026, 9, 2), Monto = 100m, ReferenciaExterna = "ref1", Descripcion = "desc1", IdentificadorExternoTransaccion = "ext1" } } };
        Assert.Equal(1, import.CuentaBancariaId);
        Assert.Single(import.Movimientos);
        var match = new ConciliarMovimientosRequestDto { CuentaBancariaId = 1, IdempotencyKey = "key2", Matches = new[] { new MatchConciliacionDto { MovimientoInternoId = 10, IdentificadorExternoTransaccion = "ext1" } } };
        Assert.Single(match.Matches);
        var close = new CerrarPeriodoConciliacionRequestDto { CuentaBancariaId = 1, Mes = 9, Anio = 2026, SaldoFinalEstadoCuenta = 1000m, IdempotencyKey = "key3" };
        Assert.Equal(1000m, close.SaldoFinalEstadoCuenta);
    }
}
