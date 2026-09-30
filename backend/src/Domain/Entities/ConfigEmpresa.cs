using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Configuración operativa aislada por empresa. La identidad legal/comercial
/// permanece en Empresa; esta entidad contiene sólo valores operativos del tenant.
/// </summary>
public sealed class ConfigEmpresa : AuditableEntity
{
    private ConfigEmpresa()
    {
    }

    public ConfigEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        EmpresaId = empresaId;
    }

    public int EmpresaId { get; private set; }
    public Empresa Empresa { get; private set; } = null!;

    public string Moneda { get; set; } = "HNL";
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";

    /// <summary>
    /// Política tributaria operativa serializada. No sustituye los maestros de
    /// impuestos; permite persistir parámetros tenant-specific sin compartir estado.
    /// </summary>
    public string ImpuestosJson { get; set; } = "{}";

    /// <summary>
    /// Parámetros de emisión no secretos. Las secuencias/correlativos continúan en
    /// SecuenciaDocumento, cuya clave ya es tenant-first.
    /// </summary>
    public string EmisionJson { get; set; } = "{}";

    public string? CorreoRemitente { get; set; }
    public string? CorreoNombreRemitente { get; set; }
    public bool CorreoConfigurado { get; set; }

    /// <summary>
    /// Referencia opaca a almacenamiento seguro; nunca contiene la credencial SMTP
    /// en claro y no debe exponerse en DTOs de lectura.
    /// </summary>
    public string? CorreoSecretoReferencia { get; set; }

    /// <summary>
    /// Token monotónico de concurrencia de aplicación.
    /// </summary>
    public long Version { get; set; } = 1;

    public ICollection<PlantillaCorreoEmpresa> PlantillasCorreo { get; private set; } = new List<PlantillaCorreoEmpresa>();
}

/// <summary>
/// Plantilla de correo tenant-scoped. Cada tipo de plantilla es único por empresa.
/// </summary>
public sealed class PlantillaCorreoEmpresa : AuditableEntity
{
    private PlantillaCorreoEmpresa()
    {
    }

    public PlantillaCorreoEmpresa(int empresaId, string tipoPlantilla, string asunto, string cuerpo)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        EmpresaId = empresaId;
        TipoPlantilla = NormalizarRequerido(tipoPlantilla, nameof(tipoPlantilla));
        Asunto = NormalizarRequerido(asunto, nameof(asunto));
        Cuerpo = NormalizarRequerido(cuerpo, nameof(cuerpo));
    }

    public int EmpresaId { get; private set; }
    public Empresa Empresa { get; private set; } = null!;

    public string TipoPlantilla { get; private set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;

    private static string NormalizarRequerido(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El valor es obligatorio.", parameterName);
        }

        return value.Trim();
    }
}
