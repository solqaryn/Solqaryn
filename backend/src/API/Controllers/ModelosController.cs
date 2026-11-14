using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("modelos")]
public class ModelosController : CatalogoProductoControllerBase
{
    protected override TipoCatalogoProducto Tipo => TipoCatalogoProducto.Modelo;
    public ModelosController(ICatalogoProductoService service) : base(service) { }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Modelos, AccionPermiso.Ver)]
    public Task<IActionResult> GetAll([FromQuery] string? buscar = null, [FromQuery] int? marcaId = null) => Listar(buscar, marcaId);

    [HttpGet("activos")]
    [RequierePermiso(ModuloSistema.Modelos, AccionPermiso.Ver)]
    public Task<IActionResult> GetActivos([FromQuery] int marcaId) => ListarActivos(marcaId);

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Modelos, AccionPermiso.Ver)]
    public Task<IActionResult> GetById(int id) => Obtener(id);

    [HttpPost]
    [RequierePermiso(ModuloSistema.Modelos, AccionPermiso.Crear)]
    public Task<IActionResult> Create([FromBody] CreateCatalogoProductoDto dto) => Crear(dto);

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Modelos, AccionPermiso.Editar)]
    public Task<IActionResult> Update(int id, [FromBody] UpdateCatalogoProductoDto dto) => Actualizar(id, dto);

    [HttpPatch("{id:int}/activar")]
    [RequierePermiso(ModuloSistema.Modelos, AccionPermiso.Activar)]
    public Task<IActionResult> Activar(int id) => CambiarEstado(id, true);

    [HttpPatch("{id:int}/desactivar")]
    [RequierePermiso(ModuloSistema.Modelos, AccionPermiso.Desactivar)]
    public Task<IActionResult> Desactivar(int id) => CambiarEstado(id, false);

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Modelos, AccionPermiso.EliminarLogico)]
    public Task<IActionResult> Delete(int id) => Eliminar(id);
}
