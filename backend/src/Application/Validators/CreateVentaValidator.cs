using FluentValidation;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Validators;

public class CreateVentaValidator : AbstractValidator<CreateVentaDto>
{
    public CreateVentaValidator()
    {
        RuleFor(x => x.Descuento).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Impuesto).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Detalles).NotEmpty().WithMessage("Debe agregar al menos un producto a la venta.");

        RuleForEach(x => x.Detalles).ChildRules(detalle =>
        {
            detalle.RuleFor(d => d.ProductoId).GreaterThan(0).WithMessage("Producto inválido.");
            detalle.RuleFor(d => d.Cantidad).GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
            detalle.RuleFor(d => d.PrecioUnitario)
                .GreaterThan(0).WithMessage("El precio unitario debe ser mayor a 0.")
                .Must(TieneMaximoDosDecimales)
                .WithMessage("El precio unitario admite como máximo 2 decimales.");
        });
    }

    private static bool TieneMaximoDosDecimales(decimal valor) =>
        Math.Round(valor, 2, MidpointRounding.AwayFromZero) == valor;
}
