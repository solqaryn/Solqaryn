using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.DTOs;

public class CargaMasivaDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public int TotalFilas { get; set; }
    public int FilasValidas { get; set; }
    public int FilasConError { get; set; }
    public int FilasConAdvertencia { get; set; }
    public int FilasProcesadas { get; set; }
    public int RegistrosCreados { get; set; }
    public int RegistrosActualizados { get; set; }
    public DateTime? FechaValidacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public string? CreadoPorNombreUsuario { get; set; }
    public string? ConfirmadoPorNombreUsuario { get; set; }
    public string? ErrorGeneral { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public class CargaMasivaDetalleDto : CargaMasivaDto
{
    public bool PuedeConfirmarse { get; set; }
    public bool ArchivoReutilizado { get; set; }
    public List<CargaMasivaFilaDto> Filas { get; set; } = new();
    public List<CargaMasivaErrorDto> Errores { get; set; } = new();
}

public class CargaMasivaFilaDto
{
    public int NumeroFila { get; set; }
    public string Accion { get; set; } = "Crear";
    public bool EsValida { get; set; }
    public Dictionary<string, string?> Datos { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Mensajes { get; set; } = new();

    // Snapshot interno utilizado para impedir que una confirmación sobrescriba
    // cambios de inventario realizados después de validar el archivo.
    public int? ProductoIdSnapshot { get; set; }
    public int? ProductoVarianteIdSnapshot { get; set; }
    public int? CantidadActualSnapshot { get; set; }
    public DateTime? FechaValidacionSnapshot { get; set; }
}

public class CargaMasivaErrorDto
{
    public int NumeroFila { get; set; }
    public string? Campo { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string? ValorOriginal { get; set; }
    public bool EsAdvertencia { get; set; }
}

public sealed record ArchivoDescargableDto(byte[] Contenido, string ContentType, string NombreArchivo);

public class CargaMasivaConfiguracionDto
{
    public int MaximoBytes { get; set; }
    public int MaximoFilas { get; set; }
    public string[] ExtensionesPermitidas { get; set; } = Array.Empty<string>();
    public List<CargaMasivaTipoDto> Tipos { get; set; } = new();

    // M9: contrato explícito y versionado. El tamaño de lote describe la unidad
    // operativa recomendada para UI/telemetría sin debilitar la atomicidad de la
    // confirmación transaccional existente.
    public string VersionPlantillaActual { get; set; } = "M9.1";
    public int TamanoLoteProcesamiento { get; set; } = 250;
    public int MaximoFilasVistaPrevia { get; set; } = 200;
    public string[] EtapasProceso { get; set; } = ["Carga", "Lectura", "Validacion", "VistaPrevia", "Confirmacion"];
}

public class CargaMasivaTipoDto
{
    public string Tipo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string[] Columnas { get; set; } = Array.Empty<string>();
    public string VersionPlantilla { get; set; } = "M9.1";
}

public class CargaMasivaProgresoDto
{
    public int Id { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string EtapaActual { get; set; } = string.Empty;
    public int Porcentaje { get; set; }
    public int TotalFilas { get; set; }
    public int FilasCorrectas { get; set; }
    public int FilasConError { get; set; }
    public int FilasOmitidas { get; set; }
    public int FilasProcesadas { get; set; }
    public int RegistrosCreados { get; set; }
    public int RegistrosActualizados { get; set; }
    public string VersionPlantilla { get; set; } = "M9.1";
    public List<CargaMasivaEtapaDto> Etapas { get; set; } = new();
}

public class CargaMasivaEtapaDto
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = "Pendiente";
    public int Porcentaje { get; set; }
}

public class ValidarCargaMasivaRequest
{
    public TipoCargaMasiva Tipo { get; set; }
}
