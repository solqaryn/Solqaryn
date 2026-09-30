using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class CargaMasivaError : BaseEntity
{
    public int CargaMasivaId { get; set; }
    public CargaMasiva CargaMasiva { get; set; } = null!;

    public int NumeroFila { get; set; }
    public string? Campo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? ValorOriginal { get; set; }
    public bool EsAdvertencia { get; set; }
}
