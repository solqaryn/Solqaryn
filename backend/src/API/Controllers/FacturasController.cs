using System.Security.Claims;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

/// La factura se genera y anula automáticamente junto con su venta origen.
/// Correo, WhatsApp y enlaces públicos conservan A4 como PDF oficial; descarga
/// e impresión pueden solicitar un perfil de papel explícito.
[ApiController]
[Authorize]
[Route("facturas")]
public class FacturasController : ControllerBase
{
    private readonly IFacturaService _facturaService;
    private readonly IFacturaPdfService _facturaPdfService;
    private readonly IFacturaCompartirService _facturaCompartirService;
    private readonly IEmailService _emailService;
    private readonly IAuditoriaService _auditoria;

    public FacturasController(
        IFacturaService facturaService,
        IFacturaPdfService facturaPdfService,
        IFacturaCompartirService facturaCompartirService,
        IEmailService emailService,
        IAuditoriaService auditoria)
    {
        _facturaService = facturaService;
        _facturaPdfService = facturaPdfService;
        _facturaCompartirService = facturaCompartirService;
        _emailService = emailService;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var facturas = await _facturaService.GetAllAsync();
        return Ok(ApiResponse<List<FacturaDto>>.Ok(facturas));
    }

    [HttpGet("formatos-pdf")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public IActionResult GetFormatosPdf() =>
        Ok(ApiResponse<IReadOnlyList<FacturaFormatoPdfDto>>.Ok(FacturaFormatoPdfCatalogo.ObtenerTodos()));

    [HttpGet("correo/estado")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public IActionResult GetEstadoCorreo()
    {
        var estado = _emailService.ObtenerEstadoConfiguracion();
        return Ok(ApiResponse<EstadoConfiguracionSmtp>.Ok(
            estado,
            estado.Configurado ? "SMTP disponible." : "SMTP requiere configuración en dev."));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var factura = await _facturaService.GetByIdAsync(id);
        if (factura is null) return NotFound(ApiResponse<object>.Fail("Factura no encontrada."));
        return Ok(ApiResponse<FacturaDto>.Ok(factura));
    }

    [HttpGet("venta/{ventaId:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetByVenta(int ventaId)
    {
        var factura = await _facturaService.GetByVentaIdAsync(ventaId);
        if (factura is null) return NotFound(ApiResponse<object>.Fail("Esta venta no tiene factura generada."));
        return Ok(ApiResponse<FacturaDto>.Ok(factura));
    }

    [HttpPost("{id:int}/pagos")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Aplicar)]
    public async Task<IActionResult> RegistrarPago(int id, [FromBody] RegistrarFacturaPagoDto dto)
    {
        var anterior = await _facturaService.GetByIdAsync(id);
        var factura = await _facturaService.RegistrarPagoAsync(id, dto, ObtenerUsuarioId(), ObtenerNombreUsuario());
        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Aplicar,
            $"Pago registrado en factura {factura.NumeroFactura}.",
            id,
            entidad: "FacturaPago",
            valoresAnteriores: anterior is null ? null : new
            {
                anterior.Estado,
                anterior.TotalPagado,
                anterior.SaldoPendiente
            },
            valoresNuevos: new
            {
                dto.Monto,
                dto.MetodoPago,
                dto.Referencia,
                factura.Estado,
                factura.TotalPagado,
                factura.SaldoPendiente
            });
        return Ok(ApiResponse<FacturaDto>.Ok(factura, "Pago registrado correctamente."));
    }

    [HttpPost("{id:int}/pagos/{pagoId:int}/anular")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Anular)]
    public async Task<IActionResult> AnularPago(int id, int pagoId, [FromBody] AnularFacturaPagoDto dto)
    {
        var anterior = await _facturaService.GetByIdAsync(id);
        var factura = await _facturaService.AnularPagoAsync(id, pagoId, dto, ObtenerUsuarioId(), ObtenerNombreUsuario());
        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Anular,
            $"Pago {pagoId} anulado en factura {factura.NumeroFactura}.",
            pagoId,
            entidad: "FacturaPago",
            valoresAnteriores: anterior is null ? null : new
            {
                anterior.Estado,
                anterior.TotalPagado,
                anterior.SaldoPendiente
            },
            valoresNuevos: new
            {
                dto.Motivo,
                factura.Estado,
                factura.TotalPagado,
                factura.SaldoPendiente
            });
        return Ok(ApiResponse<FacturaDto>.Ok(factura, "Pago anulado correctamente."));
    }

    [HttpPost("{id:int}/estado")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.CambiarEstado)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoFacturaDto dto)
    {
        var anterior = await _facturaService.GetByIdAsync(id);
        var factura = await _facturaService.CambiarEstadoAsync(id, dto, ObtenerUsuarioId(), ObtenerNombreUsuario());
        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.CambiarEstado,
            $"Estado de factura {factura.NumeroFactura} actualizado de {anterior?.Estado ?? "N/D"} a {factura.Estado}.",
            id,
            entidad: "Factura",
            valoresAnteriores: anterior is null ? null : new { anterior.Estado },
            valoresNuevos: new { factura.Estado, dto.Motivo });
        return Ok(ApiResponse<FacturaDto>.Ok(factura, "Estado actualizado correctamente."));
    }

    [HttpGet("{id:int}/pdf")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Exportar)]
    public async Task<IActionResult> DescargarPdf(int id, [FromQuery] string? formato = null)
    {
        if (!FacturaFormatoPdfCatalogo.TryParse(formato, out var perfil))
        {
            var permitidos = string.Join(", ", FacturaFormatoPdfCatalogo.ObtenerTodos().Select(x => x.Codigo));
            return BadRequest(ApiResponse<object>.Fail($"Formato de PDF no válido. Valores permitidos: {permitidos}."));
        }

        var factura = await _facturaService.GetByIdAsync(id);
        if (factura is null) return NotFound(ApiResponse<object>.Fail("Factura no encontrada."));

        AplicarEncabezadosDocumentoPrivado();
        var codigo = FacturaFormatoPdfCatalogo.ObtenerCodigo(perfil);
        Response.Headers["X-Factura-Formato"] = codigo;

        var pdfBytes = await _facturaPdfService.GenerarPdfAsync(factura, perfil);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Exportar,
            $"PDF {codigo} descargado de factura: {factura.NumeroFactura}.",
            id,
            entidad: "Factura",
            valoresNuevos: new { factura.NumeroFactura, factura.Total, FormatoPdf = codigo });

        var nombreArchivo = string.IsNullOrWhiteSpace(formato)
            ? $"{factura.NumeroFactura}.pdf"
            : $"{factura.NumeroFactura}-{codigo}.pdf";
        return File(pdfBytes, "application/pdf", nombreArchivo);
    }

    [HttpPost("{id:int}/compartir/whatsapp")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> PrepararWhatsApp(int id)
    {
        var enlace = await _facturaCompartirService.PrepararCompartirAsync(id);
        return Ok(ApiResponse<EnlaceCompartirDto>.Ok(
            enlace,
            "Enlace temporal creado. Cualquier enlace anterior de esta factura fue revocado."));
    }

    [HttpPost("{id:int}/compartir/revocar")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> RevocarEnlaces(int id)
    {
        var revocados = await _facturaCompartirService.RevocarEnlacesAsync(id);
        return Ok(ApiResponse<object>.Ok(
            new { enlacesRevocados = revocados },
            revocados > 0
                ? "Los enlaces públicos vigentes fueron revocados."
                : "La factura no tenía enlaces públicos vigentes."));
    }

    [HttpPost("{id:int}/compartir/registrar")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> RegistrarIntento(int id, [FromBody] RegistrarEnvioDto dto)
    {
        await _facturaCompartirService.RegistrarIntentoAsync(id, dto);
        return Ok(ApiResponse<object>.Ok(new { }, "Intento registrado."));
    }

    [HttpGet("{id:int}/historial-envios")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetHistorialEnvios(int id)
    {
        var historial = await _facturaCompartirService.GetHistorialAsync(id);
        return Ok(ApiResponse<List<HistorialEnvioDto>>.Ok(historial));
    }

    [HttpPost("{id:int}/compartir/correo")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> EnviarPorCorreo(int id, [FromBody] EnviarCorreoFacturaDto dto)
    {
        var claveIdempotencia = Request.Headers["Idempotency-Key"].FirstOrDefault()?.Trim();
        if (!string.IsNullOrEmpty(claveIdempotencia) && claveIdempotencia.Length > 128)
            return BadRequest(ApiResponse<object>.Fail("La clave de idempotencia no puede superar 128 caracteres."));

        var resultado = await _facturaCompartirService.EnviarPorCorreoAsync(
            id,
            dto.Destinatario,
            claveIdempotencia,
            HttpContext.RequestAborted);

        if (resultado.Exito)
            return Ok(ApiResponse<ResultadoEnvioCorreoDto>.Ok(resultado, resultado.Mensaje));
        if (resultado.Codigo is "DESTINATARIO_INVALIDO" or "PDF_ERROR")
            return BadRequest(ApiResponse<object>.Fail(resultado.Mensaje));
        if (resultado.EsTransitorio)
        {
            Response.Headers.RetryAfter = "30";
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse<object>.Fail(resultado.Mensaje));
        }

        var status = resultado.Codigo == "SMTP_NO_CONFIGURADO"
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status502BadGateway;
        return StatusCode(status, ApiResponse<object>.Fail(resultado.Mensaje));
    }

    [HttpGet("publico/{token}/pdf")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> DescargarPdfPublico(string token)
    {
        AplicarEncabezadosDocumentoPublico();
        var resultado = await _facturaCompartirService.ObtenerPdfPorTokenAsync(token);
        if (resultado is null)
            return NotFound(ApiResponse<object>.Fail("Este enlace no es válido, fue revocado, alcanzó su límite o ya expiró."));
        return File(resultado.Value.Pdf, "application/pdf", resultado.Value.NombreArchivo);
    }

    private int? ObtenerUsuarioId()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.TryParse(valor, out var id) ? id : null;
    }

    private string? ObtenerNombreUsuario() =>
        User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;

    private void AplicarEncabezadosDocumentoPrivado()
    {
        Response.Headers["Cache-Control"] = "private, no-store, no-cache, max-age=0";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
    }

    private void AplicarEncabezadosDocumentoPublico()
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["X-Frame-Options"] = "DENY";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; sandbox";
    }
}