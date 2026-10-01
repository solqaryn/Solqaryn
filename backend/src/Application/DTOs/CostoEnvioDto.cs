namespace Solqaryn.Application.DTOs;

public class CostoEnvioDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Departamento { get; set; }
    public string? Ciudad { get; set; }
    public string? Zona { get; set; }
    public string? Modalidad { get; set; }
    public decimal Monto { get; set; }
    public DateTime? VigenteDesde { get; set; }
    public DateTime? VigenteHasta { get; set; }
    public int Prioridad { get; set; }
    public bool EsPredeterminado { get; set; }
    public bool Activo { get; set; }
    public bool EstaVigente { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public class GuardarCostoEnvioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Departamento { get; set; }
    public string? Ciudad { get; set; }
    public string? Zona { get; set; }
    public string? Modalidad { get; set; }
    public decimal Monto { get; set; }
    public DateTime? VigenteDesde { get; set; }
    public DateTime? VigenteHasta { get; set; }
    public int Prioridad { get; set; }
    public bool EsPredeterminado { get; set; }
    public bool Activo { get; set; } = true;
}

public class ResolverCostoEnvioDto
{
    public string? Departamento { get; set; }
    public string? Ciudad { get; set; }
    public string? Zona { get; set; }
    public string? Modalidad { get; set; }
    public DateTime? FechaUtc { get; set; }
}
