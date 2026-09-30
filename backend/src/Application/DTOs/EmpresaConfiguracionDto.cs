namespace Solqaryn.Application.DTOs;

public class EmpresaConfiguracionDto
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string Eslogan { get; set; } = string.Empty;
    public string? RTN { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? SitioWeb { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? WhatsApp { get; set; }
    public string? LogoUrl { get; set; }
    public string NombreVisibleSistema { get; set; } = string.Empty;
    public string DescripcionSistema { get; set; } = string.Empty;
    public string MensajeLogin { get; set; } = string.Empty;
    public string Copyright { get; set; } = string.Empty;
    public bool MostrarCopyright { get; set; }
    public bool UsarAnioAutomaticoCopyright { get; set; }
    public bool EncabezadoActivo { get; set; }
    public string? EncabezadoTexto { get; set; }
    public bool PiePaginaActivo { get; set; }
    public string? PiePaginaTexto { get; set; }
    public string Moneda { get; set; } = "HNL";
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
    public string FormatoFecha { get; set; } = "dd/MM/yyyy";
    public string? InformacionFiscal { get; set; }
    public string? TextoLegal { get; set; }
    public string? TextoFactura { get; set; }
    public string? TextoReportes { get; set; }
}

public class UpdateEmpresaConfiguracionDto
{
    public string NombreComercial { get; set; } = string.Empty;
    public string? RazonSocial { get; set; }
    public string Eslogan { get; set; } = string.Empty;
    public string? RTN { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public string? SitioWeb { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? WhatsApp { get; set; }
    public string NombreVisibleSistema { get; set; } = string.Empty;
    public string DescripcionSistema { get; set; } = string.Empty;
    public string MensajeLogin { get; set; } = string.Empty;
    public string Copyright { get; set; } = string.Empty;
    public bool MostrarCopyright { get; set; }
    public bool UsarAnioAutomaticoCopyright { get; set; }
    public bool EncabezadoActivo { get; set; }
    public string? EncabezadoTexto { get; set; }
    public bool PiePaginaActivo { get; set; }
    public string? PiePaginaTexto { get; set; }
    public string Moneda { get; set; } = "HNL";
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
    public string FormatoFecha { get; set; } = "dd/MM/yyyy";
    public string? InformacionFiscal { get; set; }
    public string? TextoLegal { get; set; }
    public string? TextoFactura { get; set; }
    public string? TextoReportes { get; set; }
}

/// <summary>
/// Contrato tenant-first para la configuración independiente de una empresa.
/// Nunca expone CorreoSecretoReferencia ni ninguna credencial en claro.
/// </summary>
public sealed class ConfigEmpresaTenantDto
{
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Rtn { get; set; }
    public string? Direccion { get; set; }
    public string? LogoUrl { get; set; }
    public string Moneda { get; set; } = "HNL";
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
    public string ImpuestosJson { get; set; } = "{}";
    public string EmisionJson { get; set; } = "{}";
    public string? CorreoRemitente { get; set; }
    public string? CorreoNombreRemitente { get; set; }
    public bool CorreoConfigurado { get; set; }
    public long Version { get; set; }
    public IReadOnlyList<PlantillaCorreoEmpresaDto> PlantillasCorreo { get; set; } = Array.Empty<PlantillaCorreoEmpresaDto>();
}

public sealed class UpdateConfigEmpresaTenantDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? Rtn { get; set; }
    public string? Direccion { get; set; }
    public string Moneda { get; set; } = "HNL";
    public string ZonaHoraria { get; set; } = "America/Tegucigalpa";
    public string ImpuestosJson { get; set; } = "{}";
    public string EmisionJson { get; set; } = "{}";
    public string? CorreoRemitente { get; set; }
    public string? CorreoNombreRemitente { get; set; }
    public long Version { get; set; }
}

public sealed class PlantillaCorreoEmpresaDto
{
    public string TipoPlantilla { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public bool Activa { get; set; }
}

public sealed class UpdatePlantillaCorreoEmpresaDto
{
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public bool Activa { get; set; } = true;
    public long Version { get; set; }
}