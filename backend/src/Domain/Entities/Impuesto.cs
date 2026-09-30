using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// Módulo real y administrable de impuestos. La configuración se gestiona
/// desde la interfaz y el motor conserva un snapshot monetario de cada aplicación.
public class Impuesto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    public TipoImpuesto Tipo { get; set; }
    public decimal Tasa { get; set; }
    public decimal? MontoFijo { get; set; }

    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    public bool IncluidoEnPrecio { get; set; }
    public bool SeCalculaAntesDescuento { get; set; }
    public bool Acumulativo { get; set; } = true;
    public int Prioridad { get; set; } = 100;
    public bool RequiereRetencion { get; set; }

    public bool Activo { get; set; } = true;
    public bool Eliminado { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public int? CreadoPorUsuarioId { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public ICollection<ImpuestoProducto> Productos { get; set; } = new List<ImpuestoProducto>();
    public ICollection<ImpuestoCategoria> Categorias { get; set; } = new List<ImpuestoCategoria>();
    public ICollection<ImpuestoOperacion> Operaciones { get; set; } = new List<ImpuestoOperacion>();
    public ICollection<ImpuestoCliente> ClientesExentos { get; set; } = new List<ImpuestoCliente>();
    public ICollection<ImpuestoProveedor> ProveedoresExentos { get; set; } = new List<ImpuestoProveedor>();
    public ICollection<HistorialAplicacionImpuesto> Historial { get; set; } = new List<HistorialAplicacionImpuesto>();
}

public class ImpuestoProducto { public int Id { get; set; } public int ImpuestoId { get; set; } public int ProductoId { get; set; } }
public class ImpuestoCategoria { public int Id { get; set; } public int ImpuestoId { get; set; } public int CategoriaId { get; set; } }
public class ImpuestoOperacion { public int Id { get; set; } public int ImpuestoId { get; set; } public OperacionImpuesto Operacion { get; set; } }
public class ImpuestoCliente { public int Id { get; set; } public int ImpuestoId { get; set; } public int ClienteId { get; set; } }
public class ImpuestoProveedor { public int Id { get; set; } public int ImpuestoId { get; set; } public int ProveedorId { get; set; } }

public class HistorialAplicacionImpuesto
{
    public int Id { get; set; }
    public int ImpuestoId { get; set; }
    public Impuesto? Impuesto { get; set; }
    public string DocumentoTipo { get; set; } = string.Empty;
    public int DocumentoId { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal TasaAplicada { get; set; }
    public decimal MontoAplicado { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

public class VentaImpuesto
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int ImpuestoId { get; set; }
    public string ImpuestoNombreSnapshot { get; set; } = string.Empty;
    public string ImpuestoCodigoSnapshot { get; set; } = string.Empty;
    public decimal TasaSnapshot { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal MontoAplicado { get; set; }
    public bool IncluidoEnPrecioSnapshot { get; set; }
}

public class CompraImpuesto
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public int ImpuestoId { get; set; }
    public string ImpuestoNombreSnapshot { get; set; } = string.Empty;
    public string ImpuestoCodigoSnapshot { get; set; } = string.Empty;
    public decimal TasaSnapshot { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal MontoAplicado { get; set; }
    public bool IncluidoEnPrecioSnapshot { get; set; }
}
