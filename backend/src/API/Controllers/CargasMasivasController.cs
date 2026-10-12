using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("cargas-masivas")]
public class CargasMasivasController : ControllerBase
{
    private const string VersionPlantillaActual = "M9.1";
    private readonly ICargaMasivaService _service;
    private readonly AppDbContext _db;

    public CargasMasivasController(ICargaMasivaService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    [HttpGet("configuracion")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Ver)]
    public IActionResult Configuracion()
    {
        var configuracion = _service.ObtenerConfiguracion();
        configuracion.VersionPlantillaActual = VersionPlantillaActual;
        foreach (var tipo in configuracion.Tipos)
            tipo.VersionPlantilla = VersionPlantillaActual;
        return Ok(ApiResponse<CargaMasivaConfiguracionDto>.Ok(configuracion));
    }

    [HttpGet("plantillas/{tipo}")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarPlantilla(
        TipoCargaMasiva tipo,
        [FromQuery] string formato = "xlsx",
        [FromQuery] string? version = null)
    {
        if (!string.IsNullOrWhiteSpace(version) &&
            !string.Equals(version.Trim(), VersionPlantillaActual, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException($"La versión de plantilla '{version}' no está vigente. Descarga la versión {VersionPlantillaActual}.");

        var archivo = await _service.DescargarPlantillaAsync(tipo, formato);
        var extension = Path.GetExtension(archivo.NombreArchivo);
        var baseNombre = Path.GetFileNameWithoutExtension(archivo.NombreArchivo);
        var nombreVersionado = $"{baseNombre}-v{VersionPlantillaActual.Replace('.', '-')}{extension}";
        return File(archivo.Contenido, archivo.ContentType, nombreVersionado);
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.ConsultarHistorial)]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request)
    {
        var resultado = await _service.GetPagedAsync(request);
        return Ok(ApiResponse<PagedResult<CargaMasivaDto>>.Ok(resultado));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var carga = await _service.GetByIdAsync(id);
        if (carga is null) return NotFound(ApiResponse<object>.Fail("Carga masiva no encontrada."));
        return Ok(ApiResponse<CargaMasivaDetalleDto>.Ok(carga));
    }

    [HttpGet("{id:int}/progreso")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Ver)]
    public async Task<IActionResult> Progreso(int id)
    {
        var carga = await _service.GetByIdAsync(id);
        if (carga is null) return NotFound(ApiResponse<object>.Fail("Carga masiva no encontrada."));

        var progreso = ConstruirProgreso(carga);
        return Ok(ApiResponse<CargaMasivaProgresoDto>.Ok(progreso));
    }

    [HttpPost("validar")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Importar)]
    [RequestSizeLimit(CargaMasivaArchivoLimites.MaximoRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = CargaMasivaArchivoLimites.MaximoRequestBytes)]
    public async Task<IActionResult> Validar(
        [FromForm] TipoCargaMasiva tipo,
        IFormFile archivo,
        CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("Debes seleccionar un archivo CSV o XLSX."));

        await using var origen = archivo.OpenReadStream();
        await using var stream = new MemoryStream(capacity: checked((int)Math.Min(archivo.Length, CargaMasivaArchivoLimites.MaximoRequestBytes)));
        await origen.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        if (Path.GetExtension(archivo.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            CargaMasivaArchivoSecurity.ValidarXlsx(stream);
        stream.Position = 0;

        var resultado = await _service.ValidarAsync(
            tipo,
            archivo.FileName,
            archivo.ContentType,
            stream.Length,
            stream,
            cancellationToken);

        return Ok(ApiResponse<CargaMasivaDetalleDto>.Ok(
            resultado,
            resultado.ArchivoReutilizado
                ? "Se recuperó la validación existente de este archivo."
                : "Archivo validado. Revisa la vista previa antes de confirmar."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Confirmar(int id, CancellationToken cancellationToken)
    {
        await using var lease = await CargaMasivaConfirmationLock.TryAcquireAsync(_db, id, cancellationToken)
            ?? throw new BusinessRuleException("Esta carga ya está siendo confirmada por otra solicitud. Espera a que finalice.");

        var resultado = await _service.ConfirmarAsync(id, cancellationToken);
        return Ok(ApiResponse<CargaMasivaDetalleDto>.Ok(resultado, "Carga confirmada mediante una transacción completa."));
    }

    [HttpGet("{id:int}/errores")]
    [RequierePermiso(ModuloSistema.CargasMasivas, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarErrores(int id, [FromQuery] string formato = "xlsx")
    {
        var archivo = await _service.DescargarErroresAsync(id, formato);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }

    private static CargaMasivaProgresoDto ConstruirProgreso(CargaMasivaDetalleDto carga)
    {
        var confirmada = string.Equals(carga.Estado, EstadoCargaMasiva.Confirmada.ToString(), StringComparison.OrdinalIgnoreCase);
        var fallida = string.Equals(carga.Estado, EstadoCargaMasiva.Fallida.ToString(), StringComparison.OrdinalIgnoreCase);
        var conErrores = string.Equals(carga.Estado, EstadoCargaMasiva.ConErrores.ToString(), StringComparison.OrdinalIgnoreCase);
        var validada = string.Equals(carga.Estado, EstadoCargaMasiva.Validada.ToString(), StringComparison.OrdinalIgnoreCase);

        var filasOmitidas = carga.Errores
            .Where(x => x.EsAdvertencia && x.Codigo.StartsWith("OMIT", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.NumeroFila)
            .Distinct()
            .Count();

        var etapaActual = confirmada ? "Completada"
            : fallida ? "Fallida"
            : conErrores ? "Correccion"
            : validada ? "VistaPrevia"
            : "Validacion";
        var porcentaje = confirmada ? 100 : fallida ? 100 : conErrores ? 60 : validada ? 75 : 35;

        return new CargaMasivaProgresoDto
        {
            Id = carga.Id,
            Estado = carga.Estado,
            EtapaActual = etapaActual,
            Porcentaje = porcentaje,
            TotalFilas = carga.TotalFilas,
            FilasCorrectas = carga.FilasValidas,
            FilasConError = carga.FilasConError,
            FilasOmitidas = filasOmitidas,
            FilasProcesadas = carga.FilasProcesadas,
            RegistrosCreados = carga.RegistrosCreados,
            RegistrosActualizados = carga.RegistrosActualizados,
            VersionPlantilla = VersionPlantillaActual,
            Etapas =
            [
                Etapa("Carga", "Archivo recibido", true, false),
                Etapa("Lectura", "Lectura segura", carga.FechaValidacion.HasValue, false),
                Etapa("Validacion", "Validación de estructura y negocio", carga.FechaValidacion.HasValue, conErrores),
                Etapa("VistaPrevia", "Vista previa y decisión", validada || confirmada, conErrores),
                Etapa("Confirmacion", "Confirmación transaccional", confirmada, fallida)
            ]
        };
    }

    private static CargaMasivaEtapaDto Etapa(string codigo, string nombre, bool completada, bool error) => new()
    {
        Codigo = codigo,
        Nombre = nombre,
        Estado = error ? "Error" : completada ? "Completada" : "Pendiente",
        Porcentaje = error || completada ? 100 : 0
    };
}

internal static class CargaMasivaArchivoLimites
{
    public const long MaximoRequestBytes = 6L * 1024L * 1024L;
}
