using FluentValidation;
using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Validators;

public class CreateUsuarioValidator : AbstractValidator<CreateUsuarioDto>
{
    public CreateUsuarioValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().WithMessage("El nombre de usuario es obligatorio.").MaximumLength(100);
        RuleFor(x => x.NombreCompleto).NotEmpty().WithMessage("El nombre completo es obligatorio.").MaximumLength(150);
        RuleFor(x => x.Password).MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");
        RuleFor(x => x.Rol).Must(r => r is "Administrador" or "Vendedor")
            .WithMessage("El rol debe ser 'Administrador' o 'Vendedor'.");
    }
}
