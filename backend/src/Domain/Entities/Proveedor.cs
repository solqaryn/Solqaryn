using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class Proveedor : AuditableEntity
{
    // Expand: propiedad nullable para registros históricos aún no atribuidos.
    // No usar como autoridad de acceso hasta certificar ownership completo.
    public int? EmpresaId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Documento { get; set; } // RTN u otro identificador fiscal
    public string? DocumentoNormalizado { get; private set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
}
