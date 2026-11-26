using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioAdministracionEmpresaService _administracion;
    private readonly IPermisoService _permisoService;

    public UsuariosController(IUsuarioAdministracionEmpresaService administracion, IPermisoService permisoService)
    {
        _administracion = administracion;
        _permisoService = permisoService;
    }

    private int Empresa => TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext);
    private CancellationToken Ct => HttpContext.RequestAborted;

    [HttpGet]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll() =>
        Ok(ApiResponse<List<UsuarioDto>>.Ok(await _administracion.GetAllAsync(Empresa, Ct)));

    [HttpGet("paginado")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request) =>
        Ok(ApiResponse<PagedResult<UsuarioDto>>.Ok(await _administracion.GetPagedAsync(Empresa, request, Ct)));

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var usuario = await _administracion.GetByIdAsync(Empresa, id, Ct);
        if (usuario is null) return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));
        return Ok(ApiResponse<UsuarioDetalleDto>.Ok(usuario));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioDto dto)
    {
        var creado = await _administracion.CrearAsync(Empresa, dto, Ct);
        return Ok(ApiResponse<UsuarioDto>.Ok(creado, "Usuario creado correctamente."));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUsuarioDto dto)
    {
        var actualizado = await _administracion.ActualizarAsync(Empresa, id, dto, Ct);
        if (actualizado is null) return NotFound(ApiResponse<object>.Fail("Usuario no encontrado."));
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, "Usuario actualizado correctamente."));
    }

    [HttpPut("{id:int}/estado")]
    public async Task<IActionResult> UpdateEstado(int id, [FromBody] UpdateUsuarioEstadoDto dto)
    {
        var empresa = await AutorizarAsync(dto.Activo ? AccionPermiso.Activar : AccionPermiso.Desactivar);
        var actualizado = await _administracion.UpdateEstadoAsync(empresa, id, dto.Activo, Ct);
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, "Estado del usuario actualizado correctamente."));
    }

    [HttpPut("{id:int}/bloquear")]
    public async Task<IActionResult> Bloquear(int id, [FromBody] BloquearUsuarioDto dto)
    {
        var empresa = await AutorizarAsync(AccionPermiso.Desactivar);
        var actualizado = await _administracion.BloquearAsync(empresa, id, dto.Motivo, Ct);
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, "Usuario bloqueado correctamente."));
    }

    [HttpPut("{id:int}/desbloquear")]
    public async Task<IActionResult> Desbloquear(int id)
    {
        var empresa = await AutorizarAsync(AccionPermiso.Activar);
        var actualizado = await _administracion.DesbloquearAsync(empresa, id, Ct);
        return Ok(ApiResponse<UsuarioDto>.Ok(actualizado, "Usuario desbloqueado correctamente."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _administracion.EliminarAsync(Empresa, id, Ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Usuario eliminado correctamente."));
    }

    [HttpGet("{id:int}/empresas")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver)]
    public async Task<IActionResult> GetEmpresas(int id)
    {
        var empresa = Empresa;
        var membresias = await _administracion.GetEmpresasAsync(empresa, id, Ct);
        return Ok(ApiResponse<List<UsuarioEmpresaDto>>.Ok(membresias.Where(m => m.EmpresaId == empresa).ToList()));
    }

    [HttpPost("{id:int}/empresas")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.AsignarRol)]
    public async Task<IActionResult> AsignarEmpresa(int id, [FromBody] AsignarUsuarioEmpresaDto dto)
    {
        var empresa = TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext, dto.EmpresaId);
        var membresia = await _administracion.AsignarEmpresaAsync(empresa, id, dto.RolId, Ct);
        return Ok(ApiResponse<UsuarioEmpresaDto>.Ok(membresia, "Membresía empresarial asignada correctamente."));
    }

    [HttpPut("{id:int}/empresas/{empresaId:int}/rol")]
    [RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.AsignarRol, VincularEmpresaRuta = true)]
    public async Task<IActionResult> CambiarRolEmpresa(int id, int empresaId, [FromBody] CambiarRolUsuarioEmpresaDto dto)
    {
        var empresa = TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext, empresaId);
        var membresia = await _administracion.CambiarRolEmpresaAsync(empresa, id, dto.RolId, Ct);
        return Ok(ApiResponse<UsuarioEmpresaDto>.Ok(membresia, "Rol empresarial actualizado correctamente."));
    }

    [HttpPut("{id:int}/empresas/{empresaId:int}/estado")]
    public async Task<IActionResult> CambiarEstadoEmpresa(int id, int empresaId, [FromBody] UpdateUsuarioEmpresaEstadoDto dto)
    {
        var empresa = await AutorizarAsync(dto.Activa ? AccionPermiso.Activar : AccionPermiso.Desactivar, empresaId);
        var membresia = await _administracion.CambiarEstadoEmpresaAsync(empresa, id, dto.Activa, Ct);
        return Ok(ApiResponse<UsuarioEmpresaDto>.Ok(membresia,
            dto.Activa ? "Membresía empresarial activada correctamente." : "Membresía empresarial desactivada correctamente."));
    }

    private async Task<int> AutorizarAsync(AccionPermiso accion, int? empresaRuta = null)
    {
        var empresa = await TenantPermissionContext.RequireEmpresaIdAsync(HttpContext, Ct);
        if (empresaRuta.HasValue && empresaRuta.Value != empresa)
            throw new ForbiddenAccessException("La empresa no coincide con el contexto de autorización.");
        await _permisoService.VerificarPermisoAsync(empresa, ModuloSistema.Usuarios, accion);
        TenantPermissionContext.MarkAuthorizedEmpresaId(HttpContext, empresa);
        return empresa;
    }
}
