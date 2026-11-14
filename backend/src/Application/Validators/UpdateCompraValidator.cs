using FluentValidation;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Validators;

public class UpdateCompraValidator : AbstractValidator<UpdateCompraDto>
{
    public UpdateCompraValidator()
    {
        RuleFor(x => x.ProveedorNombre).NotEmpty().WithMessage("El nombre del proveedor es obligatorio.");
        RuleFor(x => x.Descuento).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Impuesto).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Detalles).NotEmpty().WithMessage("Debe agregar al menos un producto a la compra.");

        RuleForEach(x => x.Detalles).ChildRules(detalle =>
        {
            detalle.RuleFor(d => d.ProductoId).GreaterThan(0);
            detalle.RuleFor(d => d.Cantidad).GreaterThan(0);
            detalle.RuleFor(d => d.CostoUnitario).GreaterThan(0);
        });
    }
}

