using FluentValidation;
using Solqaryn.Application.Bancos;
using Solqaryn.Application.DTOs.Bancos;

namespace Solqaryn.Application.Validators.Bancos;

public class OperacionBancariaBaseValidator<T> : AbstractValidator<T> where T : OperacionBancariaBaseDto
{
    public OperacionBancariaBaseValidator()
    {
        RuleFor(x => x.CuentaId).GreaterThan(0).WithMessage("La cuenta es requerida.");
        RuleFor(x => x.Monto).GreaterThan(0).WithMessage("El monto debe ser mayor que cero.");
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("La clave de idempotencia es requerida.")
            .Must(key =>
            {
                try
                {
                    BancosIdempotencyKey.Create(key);
                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }).WithMessage("La clave de idempotencia es inválida o contiene caracteres no seguros.");
    }
}

public sealed class DepositoBancarioValidator : OperacionBancariaBaseValidator<DepositoBancarioDto> { }
public sealed class RetiroBancarioValidator : OperacionBancariaBaseValidator<RetiroBancarioDto> { }
public sealed class ComisionBancariaValidator : OperacionBancariaBaseValidator<ComisionBancariaDto> { }
public sealed class InteresBancarioValidator : OperacionBancariaBaseValidator<InteresBancarioDto> { }
public sealed class ConciliacionBancariaValidator : OperacionBancariaBaseValidator<ConciliacionBancariaDto> { }

public sealed class TransferenciaBancariaValidator : OperacionBancariaBaseValidator<TransferenciaBancariaDto>
{
    public TransferenciaBancariaValidator()
    {
        RuleFor(x => x.CuentaDestinoId).GreaterThan(0).WithMessage("La cuenta destino es requerida.");
        RuleFor(x => x).Must(x => x.CuentaId != x.CuentaDestinoId)
            .WithMessage("La cuenta destino debe ser diferente a la cuenta de origen.");
    }
}
