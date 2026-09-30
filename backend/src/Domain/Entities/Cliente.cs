using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class Cliente : AuditableEntity
{
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? IdentidadORTN { get; set; }
    public string? IdentidadORTNNormalizada { get; private set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public int TipoClienteId { get; set; }
    public TipoCliente TipoCliente { get; set; } = null!;
    public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
}
