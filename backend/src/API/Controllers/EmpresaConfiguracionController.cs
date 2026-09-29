using InventoryApp.API.Filters;
using InventoryApp.Application.Common;
using InventoryApp.Application.DTOs;
using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApp.API.Controllers;

[ApiController]
[Route("empresa-configuracion")]
public class EmpresaConfiguracionController : ControllerBase
{
    private readonly IEmpresaConfiguracionService _service;

    public EmpresaConfiguracionController(IEmpresaConfiguracionService service)
    {
        _service = service;
    }

    [HttpGet("publica")]
    [AllowAnonymous]
    [PublicHttpCache(PublicHttpCacheProfile.Identity)]
    public async Task<IActionResult> GetPublica()
    {
        var config = await _service.GetActivaAsync();
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(config));
    }

    [HttpGet]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> Get()
    {
        var config = await _service.GetActivaAsync();
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(config));
    }

    [HttpPut]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> Update([FromBody] UpdateEmpresaConfiguracionDto dto)
    {
        var actualizada = await _service.UpdateAsync(dto);
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(actualizada, "Configuracion de empresa actualizada."));
    }

    [HttpPost("logo")]
    [Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> UpdateLogo(IFormFile logo)
    {
        var actualizada = await _service.UpdateLogoAsync(logo);
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(actualizada, "Logo actualizado correctamente."));
    }

    [HttpDelete("logo")]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> RestaurarLogo()
    {
        var actualizada = await _service.RestaurarLogoAsync();
        return Ok(ApiResponse<EmpresaConfiguracionDto>.Ok(actualizada, "Logo restaurado correctamente."));
    }

    [HttpGet("tenant/{empresaId:int}")]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetTenant(int empresaId, CancellationToken cancellationToken)
    {
        var config = await _service.GetTenantAsync(empresaId, cancellationToken);
        return Ok(ApiResponse<ConfigEmpresaTenantDto>.Ok(config));
    }

    [HttpPut("tenant/{empresaId:int}")]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> UpdateTenant(
        int empresaId,
        [FromBody] UpdateConfigEmpresaTenantDto dto,
        CancellationToken cancellationToken)
    {
        var actualizada = await _service.UpdateTenantAsync(empresaId, dto, cancellationToken);
        return Ok(ApiResponse<ConfigEmpresaTenantDto>.Ok(actualizada, "Configuración tenant actualizada."));
    }

    [HttpPost("tenant/{empresaId:int}/logo")]
    [Authorize]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> UpdateTenantLogo(
        int empresaId,
        IFormFile logo,
        CancellationToken cancellationToken)
    {
        var actualizada = await _service.UpdateTenantLogoAsync(empresaId, logo, cancellationToken);
        return Ok(ApiResponse<ConfigEmpresaTenantDto>.Ok(actualizada, "Logo tenant actualizado."));
    }

    [HttpDelete("tenant/{empresaId:int}/logo")]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> RestaurarTenantLogo(int empresaId, CancellationToken cancellationToken)
    {
        var actualizada = await _service.RestaurarTenantLogoAsync(empresaId, cancellationToken);
        return Ok(ApiResponse<ConfigEmpresaTenantDto>.Ok(actualizada, "Logo tenant restaurado."));
    }

    [HttpPut("tenant/{empresaId:int}/plantillas/{tipoPlantilla}")]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> UpsertPlantillaTenant(
        int empresaId,
        string tipoPlantilla,
        [FromBody] UpdatePlantillaCorreoEmpresaDto dto,
        CancellationToken cancellationToken)
    {
        var actualizada = await _service.UpsertPlantillaTenantAsync(
            empresaId,
            tipoPlantilla,
            dto,
            cancellationToken);
        return Ok(ApiResponse<ConfigEmpresaTenantDto>.Ok(actualizada, "Plantilla tenant actualizada."));
    }

    [HttpDelete("tenant/{empresaId:int}/plantillas/{tipoPlantilla}")]
    [Authorize]
    [RequierePermiso(ModuloSistema.Configuracion, AccionPermiso.Editar)]
    public async Task<IActionResult> DesactivarPlantillaTenant(
        int empresaId,
        string tipoPlantilla,
        [FromQuery] long version,
        CancellationToken cancellationToken)
    {
        var actualizada = await _service.DesactivarPlantillaTenantAsync(
            empresaId,
            tipoPlantilla,
            version,
            cancellationToken);
        return Ok(ApiResponse<ConfigEmpresaTenantDto>.Ok(actualizada, "Plantilla tenant desactivada."));
    }
}