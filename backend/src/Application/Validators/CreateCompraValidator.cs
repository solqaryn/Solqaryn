using FluentValidation;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Validators;

public class CreateCompraValidator : AbstractValidator<CreateCompraDto>
{
    public CreateCompraValidator()
    {
        RuleFor(x => x.ProveedorNombre).NotEmpty().WithMessage("El nombre del proveedor es obligatorio.");
        RuleFor(x => x.Descuento).GreaterThanOrEqualTo(0).WithMessage("El descuento no puede ser negativo.");
        RuleFor(x => x.Impuesto).GreaterThanOrEqualTo(0).WithMessage("El impuesto no puede ser negativo.");
        RuleFor(x => x.Detalles).NotEmpty().WithMessage("Debe agregar al menos un producto a la compra.");

        RuleForEach(x => x.Detalles).ChildRules(detalle =>
        {
            detalle.RuleFor(d => d.ProductoId).GreaterThan(0).WithMessage("Producto inválido.");
            detalle.RuleFor(d => d.Cantidad).GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0.");
            detalle.RuleFor(d => d.CostoUnitario).GreaterThan(0).WithMessage("El costo unitario debe ser mayor a 0.");
        });
    }
}
