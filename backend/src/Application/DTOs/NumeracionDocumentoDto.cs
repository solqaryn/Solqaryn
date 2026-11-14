namespace Solqaryn.Application.DTOs;

/// <summary>
/// Contrato explícito del scope semántico de numeración documental.
/// La sucursal es opcional únicamente para tipos configurados a nivel empresa.
/// </summary>
public class ScopeNumeracionDocumentoDto
{
    public int EmpresaId { get; set; }
    public int? SucursalId { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
}

public class SecuenciaDocumentoDto : ScopeNumeracionDocumentoDto
{
    public int Id { get; set; }
    public long UltimoValor { get; set; }
    public string Prefijo { get; set; } = string.Empty;
    public int LongitudNumero { get; set; }
    public bool Activa { get; set; }
}

public class ConfigurarSecuenciaDocumentoDto : ScopeNumeracionDocumentoDto
{
    public string Prefijo { get; set; } = string.Empty;
    public int LongitudNumero { get; set; } = 6;
}
