using FluentValidation;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Validators;

public sealed class CreateSucursalValidator : AbstractValidator<CreateSucursalDto>
{
    public CreateSucursalValidator()
    {
        AplicarReglas(this);
    }

    internal static void AplicarReglas<T>(AbstractValidator<T> validator) where T : CreateSucursalDto
    {
        validator.RuleFor(x => x.EmpresaId)
            .NotNull().WithMessage("EmpresaId es obligatorio para establecer el tenant propietario de la sucursal.")
            .GreaterThan(0).WithMessage("EmpresaId debe ser mayor que cero.");
        validator.RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código de la sucursal es obligatorio.")
            .MaximumLength(40);
        validator.RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la sucursal es obligatorio.")
            .MaximumLength(150);
        validator.RuleFor(x => x.Direccion).MaximumLength(500);
        validator.RuleFor(x => x.Telefono).MaximumLength(50);
        validator.RuleFor(x => x.Correo)
            .MaximumLength(254)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Correo))
            .WithMessage("El correo de la sucursal no tiene un formato válido.");
        validator.RuleFor(x => x.ZonaHoraria)
            .NotEmpty().WithMessage("La zona horaria es obligatoria.")
            .MaximumLength(100);
    }
}

public sealed class UpdateSucursalValidator : AbstractValidator<UpdateSucursalDto>
{
    public UpdateSucursalValidator()
    {
        RuleFor(x => x.EmpresaId)
            .NotNull().WithMessage("EmpresaId es obligatorio para conservar el tenant propietario de la sucursal.")
            .GreaterThan(0).WithMessage("EmpresaId debe ser mayor que cero.");
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código de la sucursal es obligatorio.")
            .MaximumLength(40);
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la sucursal es obligatorio.")
            .MaximumLength(150);
        RuleFor(x => x.Direccion).MaximumLength(500);
        RuleFor(x => x.Telefono).MaximumLength(50);
        RuleFor(x => x.Correo)
            .MaximumLength(254)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Correo))
            .WithMessage("El correo de la sucursal no tiene un formato válido.");
        RuleFor(x => x.ZonaHoraria)
            .NotEmpty().WithMessage("La zona horaria es obligatoria.")
            .MaximumLength(100);
    }
}

public sealed class SucursalFiltroValidator : AbstractValidator<SucursalFiltroDto>
{
    public SucursalFiltroValidator()
    {
        RuleFor(x => x.EmpresaId)
            .GreaterThan(0)
            .When(x => x.EmpresaId.HasValue)
            .WithMessage("EmpresaId debe ser mayor que cero.");
        RuleFor(x => x.Pagina).GreaterThan(0).WithMessage("Página debe ser mayor que cero.");
        RuleFor(x => x.TamanoPagina)
            .InclusiveBetween(1, 100)
            .WithMessage("Tamaño de página debe estar entre 1 y 100.");
        RuleFor(x => x.Buscar).MaximumLength(150);
    }
}
