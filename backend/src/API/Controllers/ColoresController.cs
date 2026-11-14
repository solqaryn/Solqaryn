using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("colores")]
public class ColoresController : CatalogoProductoControllerBase
{
    protected override TipoCatalogoProducto Tipo => TipoCatalogoProducto.Color;
    public ColoresController(ICatalogoProductoService service) : base(service) { }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Colores, AccionPermiso.Ver)]
    public Task<IActionResult> GetAll([FromQuery] string? buscar = null) => Listar(buscar);

    [HttpGet("activos")]
    [RequierePermiso(ModuloSistema.Colores, AccionPermiso.Ver)]
    public Task<IActionResult> GetActivos() => ListarActivos();

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Colores, AccionPermiso.Ver)]
    public Task<IActionResult> GetById(int id) => Obtener(id);

    [HttpPost]
    [RequierePermiso(ModuloSistema.Colores, AccionPermiso.Crear)]
    public Task<IActionResult> Create([FromBody] CreateCatalogoProductoDto dto) => Crear(dto);

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Colores, AccionPermiso.Editar)]
    public Task<IActionResult> Update(int id, [FromBody] UpdateCatalogoProductoDto dto) => Actualizar(id, dto);

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Colores, AccionPermiso.Activar)]
    public Task<IActionResult> Activar(int id) => CambiarEstado(id, true);

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Colores, AccionPermiso.Desactivar)]
    public Task<IActionResult> Desactivar(int id) => CambiarEstado(id, false);

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Colores, AccionPermiso.EliminarLogico)]
    public Task<IActionResult> Delete(int id) => Eliminar(id);
}
