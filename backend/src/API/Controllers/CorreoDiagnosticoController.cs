using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

/// Diagnóstico seguro del transporte SMTP de dev. Nunca devuelve usuario,
/// contraseña, certificados ni mensajes técnicos completos del proveedor.
[ApiController]
[Authorize]
[Route("facturas/correo")]
public sealed class CorreoDiagnosticoController : ControllerBase
{
    private readonly IEmailService _emailService;

    public CorreoDiagnosticoController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost("probar")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Compartir)]
    public async Task<IActionResult> ProbarConexion(CancellationToken cancellationToken)
    {
        var resultado = await _emailService.ProbarConexionAsync(cancellationToken);
        return Ok(ApiResponse<ResultadoDiagnosticoSmtp>.Ok(resultado, resultado.Mensaje));
    }
}
