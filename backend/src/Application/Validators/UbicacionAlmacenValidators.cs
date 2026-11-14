using FluentValidation;
using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Validators;

public sealed class CreateUbicacionAlmacenValidator : AbstractValidator<CreateUbicacionAlmacenDto>
{
    public CreateUbicacionAlmacenValidator()
    {
        RuleFor(x => x.AlmacenId)
            .GreaterThan(0).WithMessage("AlmacenId debe ser mayor que cero.");
        RuleFor(x => x.UbicacionPadreId)
            .GreaterThan(0)
            .When(x => x.UbicacionPadreId.HasValue)
            .WithMessage("UbicacionPadreId debe ser mayor que cero.");
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código de la ubicación es obligatorio.")
            .MaximumLength(60);
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la ubicación es obligatorio.")
            .MaximumLength(150);
        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("El tipo de ubicación es obligatorio.")
            .MaximumLength(30)
            .Must(EsTipoValido).WithMessage("El tipo de ubicación no es válido.");
    }

    internal static bool EsTipoValido(string? valor) =>
        !string.IsNullOrWhiteSpace(valor) &&
        Enum.TryParse<TipoUbicacionAlmacen>(valor.Trim(), true, out var tipo) &&
        Enum.IsDefined(tipo);
}

public sealed class UpdateUbicacionAlmacenValidator : AbstractValidator<UpdateUbicacionAlmacenDto>
{
    public UpdateUbicacionAlmacenValidator()
    {
        RuleFor(x => x.AlmacenId)
            .GreaterThan(0).WithMessage("AlmacenId debe ser mayor que cero.");
        RuleFor(x => x.UbicacionPadreId)
            .GreaterThan(0)
            .When(x => x.UbicacionPadreId.HasValue)
            .WithMessage("UbicacionPadreId debe ser mayor que cero.");
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código de la ubicación es obligatorio.")
            .MaximumLength(60);
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la ubicación es obligatorio.")
            .MaximumLength(150);
        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("El tipo de ubicación es obligatorio.")
            .MaximumLength(30)
            .Must(CreateUbicacionAlmacenValidator.EsTipoValido)
            .WithMessage("El tipo de ubicación no es válido.");
    }
}

public sealed class UbicacionAlmacenFiltroValidator : AbstractValidator<UbicacionAlmacenFiltroDto>
{
    public UbicacionAlmacenFiltroValidator()
    {
        RuleFor(x => x.AlmacenId)
            .GreaterThan(0)
            .When(x => x.AlmacenId.HasValue)
            .WithMessage("AlmacenId debe ser mayor que cero.");
        RuleFor(x => x.UbicacionPadreId)
            .GreaterThan(0)
            .When(x => x.UbicacionPadreId.HasValue)
            .WithMessage("UbicacionPadreId debe ser mayor que cero.");
        RuleFor(x => x.Tipo)
            .Must(CreateUbicacionAlmacenValidator.EsTipoValido)
            .When(x => !string.IsNullOrWhiteSpace(x.Tipo))
            .WithMessage("El tipo de ubicación no es válido.");
        RuleFor(x => x.Pagina)
            .GreaterThan(0).WithMessage("Página debe ser mayor que cero.");
        RuleFor(x => x.TamanoPagina)
            .InclusiveBetween(1, 100)
            .WithMessage("Tamaño de página debe estar entre 1 y 100.");
        RuleFor(x => x.Buscar).MaximumLength(150);
        RuleFor(x => x)
            .Must(x => !(x.SoloRaiz && x.UbicacionPadreId.HasValue))
            .WithMessage("SoloRaiz y UbicacionPadreId no pueden combinarse.");
    }
}
