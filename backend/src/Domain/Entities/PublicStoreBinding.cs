namespace Solqaryn.Domain.Entities;

/// <summary>Publicación de tienda: el host y StoreKey identifican una sola empresa.
/// El registro sólo se modifica desde la autoridad global de plataforma.</summary>
public sealed class PublicStoreBinding
{
    public long Id { get; set; }
    public int EmpresaId { get; set; }
    public string Host { get; set; } = string.Empty;
    public string StoreKey { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacionUtc { get; set; } = DateTime.UtcNow;
}
