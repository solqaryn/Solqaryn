using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// Bitácora centralizada de acciones del sistema. Complementa (no reemplaza) los
/// campos de auditoría que ya vive en cada entidad (CreadoPor/ConfirmadoPor/etc.) —
/// esta tabla permite ver TODO en una sola vista cronológica, filtrable.
public class RegistroAuditoria
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public int? UsuarioId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;

    /// <summary>Empresa/tenant afectado; null para operaciones de plataforma o registros legacy.</summary>
    public int? EmpresaId { get; set; }

    /// <summary>Plano de autoridad usado por la operación.</summary>
    public AmbitoAutorizacion? AmbitoAutoridad { get; set; }

    /// <summary>Fuente verificable de la autorización.</summary>
    public OrigenAutorizacion? OrigenAutorizacion { get; set; }

    public ModuloSistema Modulo { get; set; }
    public AccionPermiso Accion { get; set; }

    /// Nombre de la entidad de dominio afectada (ej. "Rol", "Descuento", "Venta").
    public string? Entidad { get; set; }

    /// Id del registro afectado (ProductoId, CompraId, VentaId, etc.), si aplica.
    public int? ReferenciaId { get; set; }

    /// Descripción legible: "Anuló la compra COM-000004 (motivo: producto dañado)".
    public string Descripcion { get; set; } = string.Empty;

    /// Estado del registro antes del cambio, serializado como JSON. Null en
    /// creaciones (no había estado previo).
    public string? ValoresAnteriores { get; set; }

    /// Estado del registro después del cambio, serializado como JSON.
    public string? ValoresNuevos { get; set; }

    /// Motivo explícito cuando la acción lo requiere (anulaciones, rechazos).
    public string? Motivo { get; set; }

    public string? Ip { get; set; }
    public string? UserAgent { get; set; }

    /// Permite correlacionar todas las entradas de auditoría generadas dentro
    /// de una misma solicitud HTTP (útil para depurar una operación compuesta).
    public string? CorrelationId { get; set; }

    /// "Exito" | "Error". Las entradas de error se generan cuando una acción
    /// auditable fue intentada pero falló (ej. una regla de negocio la bloqueó).
    public string Resultado { get; set; } = "Exito";
    public string? Error { get; set; }
}
