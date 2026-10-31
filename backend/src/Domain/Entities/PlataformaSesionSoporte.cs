using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Concesión temporal, explícita y de solo lectura a un tenant para soporte.
/// El secreto de acceso nunca se persiste; solo se conserva su digest SHA-256.
/// </summary>
public sealed class PlataformaSesionSoporte
{
    public int Id { get; set; }
    public int OperadorUsuarioId { get; set; }
    public int EmpresaId { get; set; }
    public ModuloSistema Modulo { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreadaUtc { get; set; }
    public DateTime ExpiraUtc { get; set; }
    public DateTime? RevocadaUtc { get; set; }
    public int? RevocadaPorUsuarioId { get; set; }
    public int Version { get; set; } = 1;
}