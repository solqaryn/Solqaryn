using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class CargaMasiva : AuditableEntity
{
    public TipoCargaMasiva Tipo { get; set; }
    public EstadoCargaMasiva Estado { get; set; } = EstadoCargaMasiva.PendienteValidacion;

    public string NombreArchivo { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string HashArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Filas normalizadas y acciones previstas. No contiene el archivo original.
    /// Se conserva para confirmar exactamente la vista previa validada.
    /// </summary>
    public string DatosNormalizadosJson { get; set; } = "[]";

    public int TotalFilas { get; set; }
    public int FilasValidas { get; set; }
    public int FilasConError { get; set; }
    public int FilasConAdvertencia { get; set; }
    public int FilasProcesadas { get; set; }
    public int RegistrosCreados { get; set; }
    public int RegistrosActualizados { get; set; }

    public DateTime? FechaValidacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public int? ConfirmadoPorUsuarioId { get; set; }
    public string? ConfirmadoPorNombreUsuario { get; set; }
    public string? ErrorGeneral { get; set; }

    public ICollection<CargaMasivaError> Errores { get; set; } = new List<CargaMasivaError>();
}
