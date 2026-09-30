using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Sucursal operativa de una Empresa. N6.2 establece a Sucursal como la raíz
/// de ownership tenant-aware para la jerarquía Sucursal -> Almacen -> Ubicacion.
/// N6.3.B hace explícito el contrato Empresa -> múltiples Sucursales sin adelantar
/// persistencia, autorización por empresa ni aislamiento global.
/// </summary>
public class Sucursal : AuditableEntity
{
    /// <summary>
    /// Identificador de la Empresa propietaria. Permanece nullable temporalmente
    /// por compatibilidad con filas legacy durante el rollout. Los writes nuevos
    /// y cualquier resolución de ownership deben utilizar un identificador positivo.
    /// </summary>
    public int? EmpresaId { get; set; }

    /// <summary>
    /// Resuelve de forma fail-closed la Empresa propietaria de la Sucursal.
    /// </summary>
    public int ObtenerEmpresaIdTenant()
    {
        if (!EmpresaId.HasValue || EmpresaId.Value <= 0)
        {
            throw new InvalidOperationException("La sucursal no tiene una Empresa tenant válida asignada.");
        }

        return EmpresaId.Value;
    }

    /// <summary>
    /// Asigna el owner inicial de la Sucursal. Una Sucursal ya perteneciente a otra
    /// Empresa no puede cambiar de owner accidentalmente mediante esta operación;
    /// la reasignación debe ser explícita para que la capa de aplicación pueda
    /// validar Empresa destino, autorización y auditoría en N6.3.D.
    /// </summary>
    public void AsignarEmpresa(int empresaId)
    {
        ValidarEmpresaId(empresaId);

        if (EmpresaId.HasValue && EmpresaId.Value > 0 && EmpresaId.Value != empresaId)
        {
            throw new InvalidOperationException(
                "La sucursal ya pertenece a otra Empresa. Utilice una reasignación explícita.");
        }

        EmpresaId = empresaId;
    }

    /// <summary>
    /// Cambia explícitamente el owner de una Sucursal ya asignada. Este contrato
    /// solo expresa la transición de dominio; validar existencia/estado de la Empresa,
    /// permisos y auditoría corresponde a N6.3.D y posteriores.
    /// </summary>
    public void ReasignarEmpresa(int empresaId)
    {
        ValidarEmpresaId(empresaId);
        _ = ObtenerEmpresaIdTenant();
        EmpresaId = empresaId;
    }

    /// <summary>
    /// Devuelve la clave lógica de unicidad del código de sucursal dentro de su Empresa.
    /// N6.3.B define la semántica como (EmpresaId, Codigo normalizado), permitiendo el
    /// mismo código en Empresas distintas. La constraint/index física se implementa
    /// exclusivamente en N6.3.C.
    /// </summary>
    public string ObtenerClaveCodigoEmpresa()
    {
        var empresaId = ObtenerEmpresaIdTenant();

        if (string.IsNullOrWhiteSpace(Codigo))
        {
            throw new InvalidOperationException("La sucursal no tiene un código válido.");
        }

        return $"{empresaId}:{Codigo.Trim().ToUpperInvariant()}";
    }

    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
    public bool Activa { get; set; } = true;

    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    private static void ValidarEmpresaId(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId), "La Empresa propietaria debe ser positiva.");
        }
    }
}