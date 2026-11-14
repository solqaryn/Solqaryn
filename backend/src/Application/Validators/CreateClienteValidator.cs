using FluentValidation;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Validators;

public class CreateClienteValidator : AbstractValidator<CreateClienteDto>
{
    public CreateClienteValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre del cliente es obligatorio.").MaximumLength(200);
        RuleFor(x => x.Correo).EmailAddress().When(x => !string.IsNullOrEmpty(x.Correo)).WithMessage("El correo no es válido.");
    }
}
