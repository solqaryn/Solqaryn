using FluentValidation;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Domain.Entities.Contabilidad;

namespace Solqaryn.Application.Validators;

public sealed class CreateCentroCostoValidator : AbstractValidator<CreateCentroCostoDto>
{
    public CreateCentroCostoValidator()
    {
        AplicarReglas(this);
    }

    internal static void AplicarReglas<T>(AbstractValidator<T> validator) where T : CreateCentroCostoDto
    {
        validator.RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código del centro de costo es obligatorio.")
            .MaximumLength(40);

        validator.RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del centro de costo es obligatorio.")
            .MaximumLength(150);

        validator.RuleFor(x => x.Descripcion)
            .MaximumLength(500);

        validator.RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de centro de costo no es válido.");

        validator.RuleFor(x => x.SucursalId)
            .NotNull().WithMessage("SucursalId es obligatorio cuando el tipo es Sucursal.")
            .GreaterThan(0).WithMessage("SucursalId debe ser mayor que cero.")
            .When(x => x.Tipo == TipoCentroCosto.Sucursal);

        validator.RuleFor(x => x.SucursalId)
            .Null().WithMessage("SucursalId debe permanecer nulo para centros de costo que no son Sucursal.")
            .When(x => x.Tipo != TipoCentroCosto.Sucursal);
    }
}

public sealed class UpdateCentroCostoValidator : AbstractValidator<UpdateCentroCostoDto>
{
    public UpdateCentroCostoValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código del centro de costo es obligatorio.")
            .MaximumLength(40);

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del centro de costo es obligatorio.")
            .MaximumLength(150);

        RuleFor(x => x.Descripcion)
            .MaximumLength(500);

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de centro de costo no es válido.");

        RuleFor(x => x.SucursalId)
            .NotNull().WithMessage("SucursalId es obligatorio cuando el tipo es Sucursal.")
            .GreaterThan(0).WithMessage("SucursalId debe ser mayor que cero.")
            .When(x => x.Tipo == TipoCentroCosto.Sucursal);

        RuleFor(x => x.SucursalId)
            .Null().WithMessage("SucursalId debe permanecer nulo para centros de costo que no son Sucursal.")
            .When(x => x.Tipo != TipoCentroCosto.Sucursal);
    }
}
