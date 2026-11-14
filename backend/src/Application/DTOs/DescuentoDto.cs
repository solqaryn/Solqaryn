namespace Solqaryn.Application.DTOs;

public class DescuentoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CodigoPromocional { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public decimal? MontoMinimo { get; set; }
    public decimal? MontoMaximoDescuento { get; set; }
    public int? CantidadMinima { get; set; }
    public bool RequiereAprobacion { get; set; }
    public bool Acumulable { get; set; }
    public int Prioridad { get; set; }
    public int? LimiteTotalUsos { get; set; }
    public int? LimiteUsosPorCliente { get; set; }
    public int UsosRealizados { get; set; }
    public bool Activo { get; set; }
    public List<int> ProductoIds { get; set; } = new();
    public List<int> CategoriaIds { get; set; } = new();
    public List<int> ClienteIds { get; set; } = new();
    public List<int> RolIds { get; set; } = new();
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}

public class GuardarDescuentoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? CodigoPromocional { get; set; }
    public string Tipo { get; set; } = "Porcentaje";
    public decimal Valor { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public decimal? MontoMinimo { get; set; }
    public decimal? MontoMaximoDescuento { get; set; }
    public int? CantidadMinima { get; set; }
    public bool RequiereAprobacion { get; set; }
    public bool Acumulable { get; set; }
    public int Prioridad { get; set; } = 100;
    public int? LimiteTotalUsos { get; set; }
    public int? LimiteUsosPorCliente { get; set; }
    public List<int> ProductoIds { get; set; } = new();
    public List<int> CategoriaIds { get; set; } = new();
    public List<int> ClienteIds { get; set; } = new();
    public List<int> RolIds { get; set; } = new();
}
