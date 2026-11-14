using System.Collections.Concurrent;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Solqaryn.Application.Services;

public class FacturaCompartirService : IFacturaCompartirService
{
    private const int HorasValidezPredeterminadas = 72;
    private const int MaximoAccesosPredeterminado = 25;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> CorreoLocks = new();
    private static readonly ConcurrentDictionary<string, CacheCorreo> CorreosProcesados = new();

    private readonly IFacturaCompartirRepository _repository;
    private readonly IFacturaService _facturaService;
    private readonly IFacturaPdfService _facturaPdfService;
    private readonly IEmailService _emailService;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FacturaCompartirService(
        IFacturaCompartirRepository repository,
        IFacturaService facturaService,
        IFacturaPdfService facturaPdfService,
        IEmailService emailService,
        IAuditoriaService auditoria,
        ICurrentUserService currentUser,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _facturaService = facturaService;
        _facturaPdfService = facturaPdfService;
        _emailService = emailService;
        _auditoria = auditoria;
        _currentUser = currentUser;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<EnlaceCompartirDto> PrepararCompartirAsync(int facturaId)
    {
        var factura = await ObtenerFacturaCompartibleAsync(facturaId);
        var backendUrl = ResolverBackendPublicUrl();
        var ahora = DateTime.UtcNow;
        var horasValidez = Math.Clamp(
            _configuration.GetValue<int?>("AppSettings:EnlacePublicoFacturaHorasValidez")
                ?? ((_configuration.GetValue<int?>("AppSettings:EnlacePublicoFacturaDiasValidez") ?? 3) * 24),
            1,
            168);

        var enlacesRevocados = await _repository.ExpirarVigentesAsync(facturaId, ahora);
        var tokenPublico = GenerarTokenSeguro();
        var enlace = new EnlacePublicoFactura
        {
            Token = CalcularHashToken(tokenPublico),
            FacturaId = facturaId,
            FechaCreacion = ahora,
            FechaExpiracion = ahora.AddHours(horasValidez),
            CreadoPorUsuarioId = _currentUser.UsuarioId
        };

        await _repository.AddEnlaceAsync(enlace);
        await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Compartir,
            $"Enlace temporal generado para la factura {factura.NumeroFactura}.",
            facturaId,
            entidad: "Factura",
            valoresNuevos: new
            {
                EnlaceId = enlace.Id,
                enlace.FechaExpiracion,
                EnlacesAnterioresRevocados = enlacesRevocados
            });

        var urlPublica = $"{backendUrl}/facturas/publico/{tokenPublico}/pdf";
        var mensaje =
            $"Estimado/a {factura.ClienteNombre}, le compartimos la factura correspondiente a su compra " +
            $"realizada en {factura.EmpresaNombre}. Número de factura: {factura.NumeroFactura}. " +
            $"Total: L. {factura.Total:N2}. Puede descargarla aquí: {urlPublica} " +
            $"El enlace estará disponible hasta el {enlace.FechaExpiracion.ToLocalTime():dd/MM/yyyy HH:mm}. " +
            "Gracias por su preferencia.";

        return new EnlaceCompartirDto
        {
            UrlPdfPublica = urlPublica,
            FechaExpiracion = enlace.FechaExpiracion,
            MensajeWhatsApp = mensaje,
            TelefonoSugerido = WhatsAppSharePolicy.NormalizePhone(factura.ClienteTelefono)
        };
    }

    public async Task RegistrarIntentoAsync(int facturaId, RegistrarEnvioDto dto)
    {
        var factura = await _facturaService.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura no existe o no está disponible para el usuario actual.");

        var canal = dto.Canal?.Trim();
        if (!string.Equals(canal, "WhatsApp", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(canal, "Correo", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("El canal de envío debe ser WhatsApp o Correo.");
        }

        var destinatario = string.Equals(canal, "WhatsApp", StringComparison.OrdinalIgnoreCase)
            ? WhatsAppSharePolicy.MaskPhone(dto.Destinatario)
            : Truncar(dto.Destinatario?.Trim(), 200);
        if (string.IsNullOrWhiteSpace(destinatario))
            throw new BusinessRuleException(string.Equals(canal, "WhatsApp", StringComparison.OrdinalIgnoreCase)
                ? "El teléfono de WhatsApp no tiene un formato válido."
                : "El destinatario del envío es obligatorio.");

        var resultado = string.Equals(canal, "WhatsApp", StringComparison.OrdinalIgnoreCase)
            ? NormalizarResultadoWhatsApp(dto.Resultado)
            : (string.IsNullOrWhiteSpace(dto.Resultado) ? "Iniciado" : Truncar(dto.Resultado.Trim(), 20)!);
        var error = Truncar(dto.Error?.Trim(), 500);

        await _repository.AddHistorialAsync(new HistorialEnvioFactura
        {
            FacturaId = facturaId,
            Canal = string.Equals(canal, "Correo", StringComparison.OrdinalIgnoreCase) ? "Correo" : "WhatsApp",
            Destinatario = destinatario,
            Resultado = resultado,
            Error = error,
            UsuarioId = _currentUser.UsuarioId,
            UsuarioNombre = _currentUser.NombreUsuario
        });
        await _repository.SaveChangesAsync();

        var esError = string.Equals(resultado, "Error", StringComparison.OrdinalIgnoreCase) ||
                      resultado.StartsWith("Error ", StringComparison.OrdinalIgnoreCase);
        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Compartir,
            $"Intento de envío de la factura {factura.NumeroFactura} por {canal}: {resultado}.",
            facturaId,
            entidad: "Factura",
            resultado: esError ? "Error" : "Exito",
            error: error,
            valoresNuevos: new { Canal = canal, Destinatario = destinatario, Resultado = resultado });
    }

    public async Task<List<HistorialEnvioDto>> GetHistorialAsync(int facturaId)
    {
        _ = await _facturaService.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura no existe o no está disponible para el usuario actual.");

        var historial = await _repository.GetHistorialAsync(facturaId);
        return historial.Select(h => new HistorialEnvioDto
        {
            Id = h.Id,
            Canal = h.Canal,
            Destinatario = h.Destinatario,
            Resultado = h.Resultado,
            Error = h.Error,
            UsuarioNombre = h.UsuarioNombre,
            Fecha = h.Fecha
        }).ToList();
    }

    public async Task<int> RevocarEnlacesAsync(int facturaId)
    {
        var factura = await _facturaService.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura no existe o no está disponible para el usuario actual.");

        var ahora = DateTime.UtcNow;
        var revocados = await _repository.ExpirarVigentesAsync(facturaId, ahora);
        if (revocados > 0)
            await _repository.SaveChangesAsync();

        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Compartir,
            $"Se revocaron {revocados} enlace(s) público(s) de la factura {factura.NumeroFactura}.",
            facturaId,
            entidad: "Factura",
            valoresNuevos: new { EnlacesRevocados = revocados, FechaRevocacion = ahora });

        return revocados;
    }

    public async Task<(byte[] Pdf, string NombreArchivo)?> ObtenerPdfPorTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 32 || token.Length > 160)
            return null;

        var enlace = await _repository.GetPorTokenHashAsync(CalcularHashToken(token));
        if (enlace is null || enlace.FechaExpiracion <= DateTime.UtcNow)
            return null;

        var maximoAccesos = Math.Clamp(
            _configuration.GetValue<int?>("AppSettings:EnlacePublicoFacturaMaximoAccesos")
                ?? MaximoAccesosPredeterminado,
            1,
            500);

        if (enlace.VecesAccedido >= maximoAccesos)
            return null;

        var factura = await _facturaService.GetByIdAsync(enlace.FacturaId);
        if (factura is null || string.Equals(factura.Estado, "Anulada", StringComparison.OrdinalIgnoreCase))
            return null;

        enlace.VecesAccedido += 1;
        enlace.UltimoAcceso = DateTime.UtcNow;
        _repository.UpdateEnlace(enlace);
        await _repository.SaveChangesAsync();

        var pdf = await _facturaPdfService.GenerarPdfAsync(factura);

        await _auditoria.RegistrarAsync(
            ModuloSistema.Facturacion,
            AccionPermiso.Exportar,
            $"PDF público descargado mediante enlace temporal para la factura {factura.NumeroFactura}.",
            enlace.Id,
            entidad: "EnlacePublicoFactura",
            valoresNuevos: new
            {
                enlace.FacturaId,
                enlace.VecesAccedido,
                enlace.UltimoAcceso
            });

        return (pdf, $"{factura.NumeroFactura}.pdf");
    }

    public async Task<ResultadoEnvioCorreoDto> EnviarPorCorreoAsync(
        int facturaId,
        string destinatario,
        string? claveIdempotencia = null,
        CancellationToken cancellationToken = default)
    {
        destinatario = destinatario?.Trim() ?? string.Empty;
        if (!EsCorreoValido(destinatario))
        {
            return new ResultadoEnvioCorreoDto
            {
                Exito = false,
                Codigo = "DESTINATARIO_INVALIDO",
                Mensaje = "El correo indicado no tiene un formato válido."
            };
        }

        var claveCache = ConstruirClaveCorreo(facturaId, destinatario, claveIdempotencia);
        if (claveCache is null)
            return await EnviarCorreoCoreAsync(facturaId, destinatario, cancellationToken);

        if (TryObtenerCorreoProcesado(claveCache, out var procesado))
            return ClonarComoProcesado(procesado!);

        var bloqueo = CorreoLocks.GetOrAdd(claveCache, _ => new SemaphoreSlim(1, 1));
        await bloqueo.WaitAsync(cancellationToken);
        try
        {
            if (TryObtenerCorreoProcesado(claveCache, out procesado))
                return ClonarComoProcesado(procesado!);

            var resultado = await EnviarCorreoCoreAsync(facturaId, destinatario, cancellationToken);
            if (resultado.Exito)
            {
                var minutos = Math.Clamp(
                    _configuration.GetValue<int?>("AppSettings:CorreoFacturaIdempotenciaMinutos") ?? 15,
                    1,
                    60);
                CorreosProcesados[claveCache] = new CacheCorreo(
                    DateTime.UtcNow.AddMinutes(minutos),
                    resultado);
            }

            LimpiarCacheCorreoExpirado();
            return resultado;
        }
        finally
        {
            bloqueo.Release();
            if (bloqueo.CurrentCount == 1)
                CorreoLocks.TryRemove(claveCache, out _);
        }
    }

    private async Task<ResultadoEnvioCorreoDto> EnviarCorreoCoreAsync(
        int facturaId,
        string destinatario,
        CancellationToken cancellationToken)
    {
        var factura = await ObtenerFacturaCompartibleAsync(facturaId);

        byte[] pdf;
        try
        {
            pdf = await _facturaPdfService.GenerarPdfAsync(factura);
        }
        catch
        {
            await RegistrarIntentoAsync(facturaId, new RegistrarEnvioDto
            {
                Canal = "Correo",
                Destinatario = destinatario,
                Resultado = "Error PDF",
                Error = "No fue posible generar el PDF oficial A4 de la factura."
            });
            return new ResultadoEnvioCorreoDto
            {
                Exito = false,
                Codigo = "PDF_ERROR",
                Mensaje = "No se pudo generar el PDF de la factura para enviarlo por correo."
            };
        }

        var cliente = WebUtility.HtmlEncode(factura.ClienteNombre);
        var empresa = WebUtility.HtmlEncode(factura.EmpresaNombre);
        var numeroFactura = WebUtility.HtmlEncode(factura.NumeroFactura);
        var asunto = $"Factura {factura.NumeroFactura} - {factura.EmpresaNombre}";
        var cuerpo = $"""
            <!doctype html>
            <html lang="es">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;background:#f4f7fb;font-family:Arial,sans-serif;color:#263238;line-height:1.5">
              <div style="display:none;max-height:0;overflow:hidden">Factura {numeroFactura} de {empresa}.</div>
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f7fb;padding:24px 12px">
                <tr><td align="center">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:620px;background:#ffffff;border:1px solid #dbe3ec;border-radius:12px;overflow:hidden">
                    <tr><td style="background:#0b7db7;color:#ffffff;padding:20px 24px"><strong style="font-size:20px">{empresa}</strong></td></tr>
                    <tr><td style="padding:24px">
                      <p>Estimado/a {cliente},</p>
                      <p>Le compartimos la factura correspondiente a su compra.</p>
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="8" style="background:#f8fafc;border-radius:8px">
                        <tr><td><strong>Número de factura</strong></td><td align="right">{numeroFactura}</td></tr>
                        <tr><td><strong>Fecha</strong></td><td align="right">{factura.FechaEmision:dd/MM/yyyy}</td></tr>
                        <tr><td><strong>Total</strong></td><td align="right"><strong>L. {factura.Total:N2}</strong></td></tr>
                      </table>
                      <p>El detalle completo se encuentra en el PDF oficial A4 adjunto.</p>
                      <p style="margin-bottom:0">Gracias por su preferencia.</p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        var entrega = await _emailService.EnviarAsync(
            destinatario,
            asunto,
            cuerpo,
            new List<AdjuntoCorreo>
            {
                new()
                {
                    NombreArchivo = $"{factura.NumeroFactura}.pdf",
                    Contenido = pdf,
                    ContentType = "application/pdf"
                }
            },
            cancellationToken);

        var resultadoHistorial = entrega.Exito
            ? entrega.Intentos > 1 ? $"Enviado ({entrega.Intentos} intentos)" : "Enviado"
            : "Error";
        var errorHistorial = entrega.Exito
            ? null
            : $"{entrega.Codigo}: {entrega.Error ?? "No se pudo enviar el correo."}";

        await RegistrarIntentoAsync(facturaId, new RegistrarEnvioDto
        {
            Canal = "Correo",
            Destinatario = destinatario,
            Resultado = resultadoHistorial,
            Error = errorHistorial
        });

        return new ResultadoEnvioCorreoDto
        {
            Exito = entrega.Exito,
            Codigo = entrega.Codigo,
            Mensaje = entrega.Exito
                ? entrega.Intentos > 1
                    ? $"Correo enviado correctamente después de {entrega.Intentos} intentos."
                    : "Correo enviado correctamente."
                : entrega.Error ?? "No se pudo enviar el correo.",
            EsTransitorio = entrega.EsTransitorio,
            Intentos = entrega.Intentos,
            MessageId = entrega.MessageId
        };
    }

    private async Task<FacturaDto> ObtenerFacturaCompartibleAsync(int facturaId)
    {
        var factura = await _facturaService.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura no existe o no está disponible para el usuario actual.");

        if (string.Equals(factura.Estado, "Anulada", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("No se puede compartir una factura anulada.");

        return factura;
    }

    private string ResolverBackendPublicUrl()
    {
        var configurada = _configuration["AppSettings:BackendPublicUrl"]?.Trim().TrimEnd('/');
        if (EsUrlPublicaValida(configurada))
            return configurada!;

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null && request.Host.HasValue)
        {
            var inferida = $"{request.Scheme}://{request.Host}".TrimEnd('/');
            if (EsUrlPublicaValida(inferida))
                return inferida;
        }

        throw new BusinessRuleException(
            "No se ha configurado una URL pública válida para el backend. Revisa AppSettings:BackendPublicUrl.");
    }

    private static bool EsUrlPublicaValida(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || !Uri.TryCreate(valor, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return false;

        return !uri.IsLoopback &&
               !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
               !uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizarResultadoWhatsApp(string? resultado)
    {
        if (string.IsNullOrWhiteSpace(resultado) || string.Equals(resultado, "Iniciado", StringComparison.OrdinalIgnoreCase))
            return WhatsAppSharePolicy.ClientOpenRequested;
        var valor = Truncar(resultado.Trim(), 50)!;
        if (valor.Equals("WHATSAPP_SENT", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("DELIVERED", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("READ", StringComparison.OrdinalIgnoreCase) ||
            valor.Equals("SUCCESSFULLY_DELIVERED", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("WhatsApp manual no permite registrar entrega o lectura; solo estados de handoff.");
        if (!valor.Equals(WhatsAppSharePolicy.HandoffGenerated, StringComparison.OrdinalIgnoreCase) &&
            !valor.Equals(WhatsAppSharePolicy.ClientOpenRequested, StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("El resultado de WhatsApp debe describir la preparación o apertura del cliente.");
        return valor.ToUpperInvariant();
    }

    private static string GenerarTokenSeguro()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string CalcularHashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private string? ConstruirClaveCorreo(int facturaId, string destinatario, string? claveIdempotencia)
    {
        claveIdempotencia = claveIdempotencia?.Trim();
        if (string.IsNullOrWhiteSpace(claveIdempotencia)) return null;

        var usuario = _currentUser.UsuarioId?.ToString() ?? _currentUser.NombreUsuario ?? "anonimo";
        var valor = $"{facturaId}|{destinatario.ToLowerInvariant()}|{usuario}|{claveIdempotencia}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));
    }

    private static bool TryObtenerCorreoProcesado(string clave, out ResultadoEnvioCorreoDto? resultado)
    {
        resultado = null;
        if (!CorreosProcesados.TryGetValue(clave, out var cache)) return false;
        if (cache.ExpiraUtc <= DateTime.UtcNow)
        {
            CorreosProcesados.TryRemove(clave, out _);
            return false;
        }

        resultado = cache.Resultado;
        return true;
    }

    private static ResultadoEnvioCorreoDto ClonarComoProcesado(ResultadoEnvioCorreoDto resultado) => new()
    {
        Exito = resultado.Exito,
        Codigo = resultado.Codigo,
        Mensaje = "Esta solicitud ya había sido procesada; no se envió un correo duplicado.",
        EsTransitorio = resultado.EsTransitorio,
        YaProcesado = true,
        Intentos = resultado.Intentos,
        MessageId = resultado.MessageId
    };

    private static void LimpiarCacheCorreoExpirado()
    {
        if (CorreosProcesados.Count < 100) return;
        var ahora = DateTime.UtcNow;
        foreach (var item in CorreosProcesados.Where(x => x.Value.ExpiraUtc <= ahora).Take(100))
            CorreosProcesados.TryRemove(item.Key, out _);
    }

    private static bool EsCorreoValido(string correo) =>
        MailAddress.TryCreate(correo, out var direccion) &&
        string.Equals(direccion.Address, correo, StringComparison.OrdinalIgnoreCase);

    private static string? Truncar(string? valor, int longitudMaxima)
    {
        if (string.IsNullOrEmpty(valor)) return valor;
        return valor.Length <= longitudMaxima ? valor : valor[..longitudMaxima];
    }

    private sealed record CacheCorreo(DateTime ExpiraUtc, ResultadoEnvioCorreoDto Resultado);
}
