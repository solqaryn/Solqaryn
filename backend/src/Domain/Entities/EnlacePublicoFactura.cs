namespace Solqaryn.Domain.Entities;

/// Enlace público y temporal para acceder al PDF de una factura sin JWT.
///
/// La propiedad Token conserva su nombre por compatibilidad con la tabla
/// existente, pero almacena exclusivamente el hash SHA-256 del token. El valor
/// secreto completo solo se entrega una vez al crear el enlace y nunca se
/// persiste, registra en auditoría ni devuelve en consultas posteriores.
///
/// La revocación se representa adelantando FechaExpiracion; de esta forma se
/// mantiene el historial sin agregar eliminaciones físicas ni romper registros
/// productivos existentes.
public class EnlacePublicoFactura
{
    public int Id { get; set; }

    /// Hash SHA-256 hexadecimal del token público. Nunca contiene el token real.
    public string Token { get; set; } = string.Empty;

    public int FacturaId { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }
    public int? CreadoPorUsuarioId { get; set; }

    public int VecesAccedido { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}