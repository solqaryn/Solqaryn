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
[Route("ventas")]
public class VentasController : ControllerBase
{
    private readonly IVentaService _ventaService;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoEscanerService _productoEscanerService;

    public VentasController(
        IVentaService ventaService,
        IProductoRepository productoRepository,
        IProductoEscanerService productoEscanerService)
    {
        _ventaService = ventaService;
        _productoRepository = productoRepository;
        _productoEscanerService = productoEscanerService;
    }

    [HttpGet("productos/por-codigo")]
    [RequiereAlgunoPermiso(ModuloSistema.Ventas, AccionPermiso.Crear, AccionPermiso.Editar)]
    public async Task<IActionResult> BuscarProductoPorCodigo(
        [FromQuery] string codigo,
        CancellationToken cancellationToken)
    {
        var resultado = await _productoEscanerService.ResolverParaVentaAsync(
            codigo,
            cancellationToken);
        return CrearRespuestaEscaner(resultado);
    }

    [HttpGet("productos/buscar")]
    [RequiereAlgunoPermiso(ModuloSistema.Ventas, AccionPermiso.Crear, AccionPermiso.Editar)]
    public async Task<IActionResult> BuscarProductos(
        [FromQuery] string termino,
        [FromQuery] int limite = 30,
        CancellationToken cancellationToken = default)
    {
        var resultados = await _productoEscanerService.BuscarParaVentaAsync(
            termino,
            limite,
            cancellationToken);
        return Ok(ApiResponse<List<ProductoEscaneadoVentaDto>>.Ok(resultados));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request)
    {
        var resultado = await _ventaService.GetPagedAsync(request);
        return Ok(ApiResponse<PagedResult<VentaDto>>.Ok(resultado));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var venta = await _ventaService.GetByIdAsync(id);
        if (venta is null) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));
        return Ok(ApiResponse<VentaDto>.Ok(venta));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateVentaDto dto)
    {
        await ValidarProductosVendiblesAsync(dto.Detalles.Select(d => d.ProductoId));
        var creada = await _ventaService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<VentaDto>.Ok(creada, "Venta creada en estado Borrador."));
    }

    [HttpPost("calcular")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Crear)]
    public async Task<IActionResult> Calcular([FromBody] CalcularVentaRequest request)
    {
        await ValidarProductosVendiblesAsync(request.Detalles.Select(d => d.ProductoId));
        var resultado = await _ventaService.CalcularVistaPreviaAsync(request);
        return Ok(ApiResponse<ResultadoCalculoDto>.Ok(resultado));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVentaDto dto)
    {
        await ValidarProductosVendiblesAsync(dto.Detalles.Select(d => d.ProductoId));
        var actualizada = await _ventaService.UpdateAsync(id, dto);
        if (actualizada is null) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));
        return Ok(ApiResponse<VentaDto>.Ok(actualizada, "Venta actualizada correctamente."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Confirmar(int id)
    {
        var borrador = await _ventaService.GetByIdAsync(id);
        if (borrador is null) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));

        await ValidarProductosVendiblesAsync(borrador.Detalles.Select(d => d.ProductoId));
        var confirmada = await _ventaService.ConfirmarAsync(id);
        if (confirmada is null) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));
        return Ok(ApiResponse<VentaDto>.Ok(confirmada, "Venta confirmada: stock actualizado y factura generada."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularDocumentoDto dto)
    {
        var anulada = await _ventaService.AnularAsync(id, dto.MotivoAnulacion);
        if (anulada is null) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));
        return Ok(ApiResponse<VentaDto>.Ok(anulada, "Venta anulada: stock revertido y factura anulada."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Ventas, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> DeleteBorrador(int id)
    {
        var eliminada = await _ventaService.DeleteBorradorAsync(id);
        if (!eliminada) return NotFound(ApiResponse<object>.Fail("Venta no encontrada."));
        return Ok(ApiResponse<object>.Ok(new { }, "Borrador de venta eliminado lógicamente."));
    }

    private IActionResult CrearRespuestaEscaner(
        ResultadoResolucionProductoEscaner<ProductoEscaneadoVentaDto> resultado) =>
        resultado.Estado switch
        {
            EstadoResolucionProductoEscaner.Encontrado =>
                Ok(ApiResponse<ProductoEscaneadoVentaDto>.Ok(
                    resultado.Dato!,
                    "Producto localizado correctamente.")),
            EstadoResolucionProductoEscaner.EntradaInvalida =>
                BadRequest(ApiResponse<object>.Fail(resultado.Mensaje)),
            EstadoResolucionProductoEscaner.NoEncontrado =>
                NotFound(ApiResponse<object>.Fail(resultado.Mensaje)),
            EstadoResolucionProductoEscaner.Conflicto =>
                Conflict(ApiResponse<object>.Fail(resultado.Mensaje)),
            EstadoResolucionProductoEscaner.NoOperativo =>
                UnprocessableEntity(ApiResponse<object>.Fail(resultado.Mensaje)),
            _ => StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("No fue posible resolver el producto escaneado."))
        };

    private async Task ValidarProductosVendiblesAsync(IEnumerable<int> productoIds)
    {
        foreach (var productoId in productoIds.Distinct())
        {
            var producto = await _productoRepository.GetByIdAsync(productoId)
                ?? throw new BusinessRuleException($"El producto con id {productoId} no existe.");

            if (!producto.Activo)
                throw new BusinessRuleException(
                    $"El producto '{producto.Nombre}' está inactivo. Actívalo antes de incluirlo en una venta.");
            if (producto.TipoInventario != TipoInventario.MercaderiaVenta)
                throw new BusinessRuleException(
                    $"El producto '{producto.Nombre}' es un insumo administrativo y no puede venderse ni facturarse.");
        }
    }
}
