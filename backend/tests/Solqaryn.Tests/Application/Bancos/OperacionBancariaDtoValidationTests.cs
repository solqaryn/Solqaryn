using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Validators.Bancos;
using Xunit;

namespace Solqaryn.Tests.Application.Bancos;

public class OperacionBancariaDtoValidationTests
{
    private readonly DepositoBancarioValidator _validator = new();
    private readonly TransferenciaBancariaValidator _transferenciaValidator = new();

    [Fact]
    public void Valida_IdempotencyKey_Valida_No_Genera_Errores()
    {
        var dto = new DepositoBancarioDto { CuentaId = 1, Monto = 100, IdempotencyKey = "valid-key-123" };
        var result = _validator.Validate(dto);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid@key")]
    [InlineData("invalid/key")]
    [InlineData(null)]
    public void Invalida_IdempotencyKey_Insegura_Genera_Errores(string? key)
    {
        var dto = new DepositoBancarioDto { CuentaId = 1, Monto = 100, IdempotencyKey = key! };
        var result = _validator.Validate(dto);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Transferencia_Misma_Cuenta_Genera_Errores()
    {
        var dto = new TransferenciaBancariaDto { CuentaId = 1, CuentaDestinoId = 1, Monto = 100, IdempotencyKey = "valid-key-123" };
        var result = _transferenciaValidator.Validate(dto);
        Assert.False(result.IsValid);
    }
}
