namespace Solqaryn.Application.DTOs;

public class ImpuestoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Tasa { get; set; }
    public decimal? MontoFijo { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool IncluidoEnPrecio { get; set; }
    public bool SeCalculaAntesDescuento { get; set; }
    public bool Acumulativo { get; set; }
    public int Prioridad { get; set; }
    public bool RequiereRetencion { get; set; }
    public bool Activo { get; set; }
    public List<int> ProductoIds { get; set; } = new();
    public List<int> CategoriaIds { get; set; } = new();
    public List<string> Operaciones { get; set; } = new();
    public List<int> ClienteExentoIds { get; set; } = new();
    public List<int> ProveedorExentoIds { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

public class GuardarImpuestoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = "Porcentaje";
    public decimal Tasa { get; set; }
    public decimal? MontoFijo { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public bool IncluidoEnPrecio { get; set; }
    public bool SeCalculaAntesDescuento { get; set; }
    public bool Acumulativo { get; set; } = true;
    public int Prioridad { get; set; } = 100;
    public bool RequiereRetencion { get; set; }
    public List<int> ProductoIds { get; set; } = new();
    public List<int> CategoriaIds { get; set; } = new();
    public List<string> Operaciones { get; set; } = new(); // "Venta" | "Compra"
    public List<int> ClienteExentoIds { get; set; } = new();
    public List<int> ProveedorExentoIds { get; set; } = new();
}
