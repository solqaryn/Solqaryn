using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class TipoCliente : AuditableEntity
{
    public string Codigo { get; set; } = string.Empty;
    public bool EsSistema { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string NombreNormalizado { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string ColorHex { get; set; } = "#FFFFFF";
    public bool Activo { get; set; } = true;
    public int Orden { get; set; }
    public bool EsPredeterminado { get; set; }
    public bool Eliminado { get; set; }
    public DateTime? FechaEliminacion { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }
    
    public string? EsPredeterminadoUnico { get; set; }

    public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
}
