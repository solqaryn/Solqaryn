using FluentValidation;
using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Validators;

public sealed class CreateAlmacenValidator : AbstractValidator<CreateAlmacenDto>
{
    public CreateAlmacenValidator()
    {
        AplicarReglas(this);
    }

    internal static void AplicarReglas<T>(AbstractValidator<T> validator) where T : CreateAlmacenDto
    {
        validator.RuleFor(x => x.SucursalId)
            .GreaterThan(0).WithMessage("SucursalId debe ser mayor que cero.");
        validator.RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código del almacén es obligatorio.")
            .MaximumLength(40);
        validator.RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del almacén es obligatorio.")
            .MaximumLength(150);
        validator.RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("El tipo de almacén es obligatorio.")
            .MaximumLength(30)
            .Must(EsTipoValido).WithMessage("El tipo de almacén no es válido.");
    }

    internal static bool EsTipoValido(string? valor) =>
        !string.IsNullOrWhiteSpace(valor) &&
        Enum.TryParse<TipoAlmacen>(valor.Trim(), true, out var tipo) &&
        Enum.IsDefined(tipo);
}

public sealed class UpdateAlmacenValidator : AbstractValidator<UpdateAlmacenDto>
{
    public UpdateAlmacenValidator()
    {
        RuleFor(x => x.SucursalId)
            .GreaterThan(0).WithMessage("SucursalId debe ser mayor que cero.");
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código del almacén es obligatorio.")
            .MaximumLength(40);
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del almacén es obligatorio.")
            .MaximumLength(150);
        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("El tipo de almacén es obligatorio.")
            .MaximumLength(30)
            .Must(CreateAlmacenValidator.EsTipoValido).WithMessage("El tipo de almacén no es válido.");
    }
}

public sealed class AlmacenFiltroValidator : AbstractValidator<AlmacenFiltroDto>
{
    public AlmacenFiltroValidator()
    {
        RuleFor(x => x.SucursalId)
            .GreaterThan(0)
            .When(x => x.SucursalId.HasValue)
            .WithMessage("SucursalId debe ser mayor que cero.");
        RuleFor(x => x.Tipo)
            .Must(CreateAlmacenValidator.EsTipoValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Tipo))
            .WithMessage("El tipo de almacén no es válido.");
        RuleFor(x => x.Pagina).GreaterThan(0).WithMessage("Página debe ser mayor que cero.");
        RuleFor(x => x.TamanoPagina)
            .InclusiveBetween(1, 100)
            .WithMessage("Tamaño de página debe estar entre 1 y 100.");
        RuleFor(x => x.Buscar).MaximumLength(150);
    }
}
