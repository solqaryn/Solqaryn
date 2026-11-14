namespace Solqaryn.Application.DTOs;

public sealed class MetodoPagoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public bool RequiereReferencia { get; set; }
    public bool RequiereBanco { get; set; }
    public bool PermiteCambio { get; set; }
    public int Orden { get; set; }
    public string? Metadata { get; set; }
}

public sealed class CreateMetodoPagoDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public bool RequiereReferencia { get; set; }
    public bool RequiereBanco { get; set; }
    public bool PermiteCambio { get; set; }
    public int Orden { get; set; }
    public string? Metadata { get; set; }
}

public sealed class UpdateMetodoPagoDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public bool RequiereReferencia { get; set; }
    public bool RequiereBanco { get; set; }
    public bool PermiteCambio { get; set; }
    public int Orden { get; set; }
    public string? Metadata { get; set; }
}

public sealed class ReordenarMetodoPagoDto
{
    public int Id { get; set; }
    public int Orden { get; set; }
}
