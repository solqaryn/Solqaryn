using FluentValidation;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Validators;

public class CreateCategoriaValidator : AbstractValidator<CreateCategoriaDto>
{
    public CreateCategoriaValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre de la categoría es obligatorio.").MaximumLength(100);
        RuleFor(x => x.Descripcion).MaximumLength(500);
    }
}

public class UpdateCategoriaValidator : AbstractValidator<UpdateCategoriaDto>
{
    public UpdateCategoriaValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre de la categoría es obligatorio.").MaximumLength(100);
        RuleFor(x => x.Descripcion).MaximumLength(500);
    }
}
