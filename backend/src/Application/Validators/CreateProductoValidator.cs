using FluentValidation;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Validators;

public class CreateProductoValidator : AbstractValidator<CreateProductoDto>
{
    public CreateProductoValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es obligatorio.").MaximumLength(150);
        RuleFor(x => x.Marca).MaximumLength(100);
        RuleFor(x => x.Modelo).MaximumLength(100);

        When(x => x.Variantes.Count == 0, () =>
        {
            // Compatibilidad temporal para clientes anteriores a ERP-N0.1.
            RuleFor(x => x.Cantidad).GreaterThanOrEqualTo(0).WithMessage("La cantidad no puede ser negativa.");
            RuleFor(x => x.Costo).GreaterThan(0).WithMessage("El costo debe ser mayor a 0.");
            RuleFor(x => x.Precio).GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");
            RuleFor(x => x.UmbralStockBajo).GreaterThanOrEqualTo(0).WithMessage("El umbral de stock bajo no puede ser negativo.");
        });

        RuleForEach(x => x.Variantes).ChildRules(variante =>
        {
            variante.RuleFor(x => x.Cantidad).GreaterThanOrEqualTo(0).WithMessage("La cantidad de la variante no puede ser negativa.");
            variante.RuleFor(x => x.Costo).GreaterThanOrEqualTo(0).WithMessage("El costo de la variante no puede ser negativo.");
            variante.RuleFor(x => x.Precio).GreaterThan(0).WithMessage("El precio de la variante debe ser mayor a 0.");
            variante.RuleFor(x => x.UmbralStockBajo).GreaterThanOrEqualTo(0).WithMessage("El umbral de la variante no puede ser negativo.");
            variante.RuleFor(x => x.Sku).MaximumLength(80);
            variante.RuleFor(x => x.CodigoBarras).MaximumLength(120);
        });

        RuleFor(x => x.Imagenes)
            .Must(imgs => imgs == null || imgs.Count <= ImagenValidationHelper.MaxImagenes)
            .WithMessage($"Un producto puede tener máximo {ImagenValidationHelper.MaxImagenes} fotos.");

        RuleForEach(x => x.Imagenes)
            .Must(ImagenValidationHelper.EsImagenValida)
            .WithMessage("Cada imagen debe ser JPG, JPEG, PNG o WEBP y pesar máximo 10 MB.");
    }
}
