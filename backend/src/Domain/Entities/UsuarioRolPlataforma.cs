using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Asignación de autoridad global de plataforma a un usuario.
/// No representa membresía en una Empresa y nunca debe sustituir UsuarioEmpresa.
/// </summary>
public sealed class UsuarioRolPlataforma : AuditableEntity
{
    private UsuarioRolPlataforma()
    {
    }

    public UsuarioRolPlataforma(int usuarioId, int rolId)
    {
        ValidarPositivo(usuarioId, nameof(usuarioId));
        ValidarPositivo(rolId, nameof(rolId));

        UsuarioId = usuarioId;
        RolId = rolId;
    }

    public int UsuarioId { get; private set; }
    public int RolId { get; private set; }
    public bool Activa { get; private set; } = true;

    public void CambiarRol(int rolId)
    {
        ValidarPositivo(rolId, nameof(rolId));
        RolId = rolId;
    }

    public void Activar() => Activa = true;
    public void Desactivar() => Activa = false;

    private static void ValidarPositivo(int valor, string parametro)
    {
        if (valor <= 0)
        {
            throw new ArgumentOutOfRangeException(parametro, "El identificador debe ser positivo.");
        }
    }
}
