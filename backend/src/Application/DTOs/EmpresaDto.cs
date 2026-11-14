namespace Solqaryn.Application.DTOs;

public sealed class EmpresaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }
}

public sealed class CreateEmpresaDto
{
    public string Nombre { get; set; } = string.Empty;
}

public sealed class UpdateEmpresaDto
{
    public string Nombre { get; set; } = string.Empty;
}
