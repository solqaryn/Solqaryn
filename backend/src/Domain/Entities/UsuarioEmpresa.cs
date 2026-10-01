using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Membresía tenant-aware entre un Usuario y una Empresa.
///
/// N6.4.B define únicamente el contrato de dominio. La configuración EF,
/// constraints físicas, migración, backfill y consumidores de autenticación
/// pertenecen a las unidades posteriores de N6.4.
/// </summary>
public sealed class UsuarioEmpresa : AuditableEntity
{
    private UsuarioEmpresa()
    {
    }

    public UsuarioEmpresa(int usuarioId, int empresaId, int rolId)
    {
        ValidarIdentificadorPositivo(usuarioId, nameof(usuarioId));
        ValidarIdentificadorPositivo(empresaId, nameof(empresaId));
        ValidarIdentificadorPositivo(rolId, nameof(rolId));

        UsuarioId = usuarioId;
        EmpresaId = empresaId;
        RolId = rolId;
    }

    public int UsuarioId { get; private set; }

    public int EmpresaId { get; private set; }

    /// <summary>
    /// Rol efectivo exclusivamente dentro de <see cref="EmpresaId"/>.
    /// Usuario.RolId continúa como compatibilidad legacy hasta migrar consumidores,
    /// pero no debe sustituir este contrato en contextos tenant-aware.
    /// </summary>
    public int RolId { get; private set; }

    public bool Activa { get; private set; } = true;

    /// <summary>
    /// UsuarioEmpresa usa estado activa/inactiva y no soft-delete persistido.
    /// Esta proyección explícita mantiene compatibles consultas legacy sin crear
    /// una columna adicional ni alterar la migración física certificada en N6.4.C.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool Eliminado => false;

    /// <summary>
    /// Clave lógica de unicidad de la membresía. N6.4.C materializa la constraint
    /// física única (UsuarioId, EmpresaId).
    /// </summary>
    public string ObtenerClaveLogica() => $"{UsuarioId}:{EmpresaId}";

    /// <summary>
    /// Resuelve el rol solamente cuando usuario, empresa y estado coinciden.
    /// La existencia/actividad del Usuario, Empresa y Rol se valida en aplicación.
    /// </summary>
    public int ObtenerRolEfectivo(int usuarioId, int empresaId)
    {
        ValidarIdentificadorPositivo(usuarioId, nameof(usuarioId));
        ValidarIdentificadorPositivo(empresaId, nameof(empresaId));

        if (!Activa || UsuarioId != usuarioId || EmpresaId != empresaId)
        {
            throw new InvalidOperationException(
                "No existe una membresía activa e inequívoca para el usuario y la Empresa solicitados.");
        }

        return RolId;
    }

    public void CambiarRol(int rolId)
    {
        ValidarIdentificadorPositivo(rolId, nameof(rolId));
        RolId = rolId;
    }

    public void Activar() => Activa = true;

    public void Desactivar() => Activa = false;

    private static void ValidarIdentificadorPositivo(int valor, string parametro)
    {
        if (valor <= 0)
        {
            throw new ArgumentOutOfRangeException(parametro, "El identificador debe ser positivo.");
        }
    }
}
