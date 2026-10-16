using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solqaryn.Application.Common;
using Solqaryn.Application.Security;

namespace Solqaryn.API.Controllers;

/// <summary>
/// Prueba pública de vinculación resuelta en servidor, sin datos comerciales.
/// El middleware valida la firma del proxy, la allowlist y empresa activa.
/// La pantalla multitienda no abre el catálogo hasta la siguiente microfase.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("tienda/binding")]
public sealed class TiendaBindingController : ControllerBase
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Consultar()
    {
        Response.Headers.CacheControl = "private, no-store, max-age=0";
        var empresa = VerifiedPublicStoreTenant.Resolve(HttpContext);
        if (empresa is not > 0)
            return StatusCode(503, ApiResponse<object>.Fail("Tienda no autorizada."));
        return Ok(ApiResponse<object>.Ok(new { empresaId = empresa.Value, verificada = true }));
    }
}
