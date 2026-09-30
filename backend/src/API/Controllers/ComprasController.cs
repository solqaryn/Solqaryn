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
[Route("compras")]
public class ComprasController : ControllerBase
{
    private readonly ICompraService _compraService;
    private readonly IProductoService _productoService;
    private readonly ICompraDocumentoService _documentoService;
    private readonly IProductoEscanerService _productoEscanerService;

    public ComprasController(
        ICompraService compraService,
        IProductoService productoService,
        ICompraDocumentoService documentoService,
        IProductoEscanerService productoEscanerService)
    {
        _compraService = compraService;
        _productoService = productoService;
        _documentoService = documentoService;
        _productoEscanerService = productoEscanerService;
    }

    [HttpGet("productos/por-codigo")]
    [RequiereAlgunoPermiso(ModuloSistema.Compras, AccionPermiso.Crear, AccionPermiso.Editar)]
    public async Task<IActionResult> BuscarProductoPorCodigo(
        [FromQuery] string codigo,
        CancellationToken cancellationToken)
    {
        var resultado = await _productoEscanerService.ResolverParaCompraAsync(
            codigo,
            cancellationToken);
        return CrearRespuestaEscaner(resultado);
    }

    [HttpGet("productos/buscar")]
    [RequiereAlgunoPermiso(ModuloSistema.Compras, AccionPermiso.Crear, AccionPermiso.Editar)]
    public async Task<IActionResult> BuscarProductos(
        [FromQuery] string termino,
        [FromQuery] int limite = 30,
        CancellationToken cancellationToken = default)
    {
        var resultados = await _productoEscanerService.BuscarParaCompraAsync(
            termino,
            limite,
            cancellationToken);
        return Ok(ApiResponse<List<ProductoEscaneadoCompraDto>>.Ok(resultados));
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request)
    {
        var resultado = await _compraService.GetPagedAsync(request);
        return Ok(ApiResponse<PagedResult<CompraDto>>.Ok(resultado));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var compra = await _compraService.GetByIdAsync(id);
        if (compra is null) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));
        return Ok(ApiResponse<CompraDto>.Ok(compra));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Crear)]
    public async Task<IActionResult> Create([FromBody] CreateCompraDto dto)
    {
        await ValidarProductosActivosAsync(dto.Detalles.Select(d => d.ProductoId));
        var creada = await _compraService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = creada.Id },
            ApiResponse<CompraDto>.Ok(creada, "Compra creada en estado Borrador."));
    }

    [HttpPost("calcular")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Crear)]
    public async Task<IActionResult> Calcular([FromBody] CalcularCompraRequest request)
    {
        await ValidarProductosActivosAsync(request.Detalles.Select(d => d.ProductoId));
        var resultado = await _compraService.CalcularVistaPreviaAsync(request);
        return Ok(ApiResponse<ResultadoCalculoDto>.Ok(resultado));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Editar)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCompraDto dto)
    {
        await ValidarProductosActivosAsync(dto.Detalles.Select(d => d.ProductoId));
        var actualizada = await _compraService.UpdateAsync(id, dto);
        if (actualizada is null) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));
        return Ok(ApiResponse<CompraDto>.Ok(actualizada, "Compra actualizada correctamente."));
    }

    [HttpPost("{id:int}/confirmar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Confirmar)]
    public async Task<IActionResult> Confirmar(int id)
    {
        var borrador = await _compraService.GetByIdAsync(id);
        if (borrador is null) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));

        await ValidarProductosActivosAsync(borrador.Detalles.Select(d => d.ProductoId));
        var confirmada = await _compraService.ConfirmarAsync(id);
        if (confirmada is null) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));
        return Ok(ApiResponse<CompraDto>.Ok(confirmada, "Compra confirmada: stock actualizado."));
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Anular)]
    public async Task<IActionResult> Anular(int id, [FromBody] AnularDocumentoDto dto)
    {
        var anulada = await _compraService.AnularAsync(id, dto.MotivoAnulacion);
        if (anulada is null) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));
        return Ok(ApiResponse<CompraDto>.Ok(anulada, "Compra anulada: stock revertido."));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.EliminarLogico)]
    public async Task<IActionResult> DeleteBorrador(int id)
    {
        var eliminada = await _compraService.DeleteBorradorAsync(id);
        if (!eliminada) return NotFound(ApiResponse<object>.Fail("Compra no encontrada."));
        return Ok(ApiResponse<object>.Ok(new { }, "Borrador de compra eliminado lógicamente."));
    }

    [HttpGet("{id:int}/documentos")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Ver)]
    public async Task<IActionResult> GetDocumentos(int id)
    {
        var documentos = await _documentoService.GetByCompraAsync(id);
        return Ok(ApiResponse<List<CompraDocumentoDto>>.Ok(documentos));
    }

    [HttpPost("{id:int}/documentos")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Editar)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocumento(int id, [FromForm] IFormFile archivo)
    {
        var documento = await _documentoService.UploadAsync(id, archivo);
        return Ok(ApiResponse<CompraDocumentoDto>.Ok(documento, "Comprobante adjuntado correctamente."));
    }

    [HttpGet("{id:int}/documentos/{documentoId:int}/descargar")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Exportar)]
    public async Task<IActionResult> DownloadDocumento(int id, int documentoId)
    {
        var descarga = await _documentoService.DownloadAsync(id, documentoId);
        if (descarga is null)
            return NotFound(ApiResponse<object>.Fail("El comprobante no existe o el archivo ya no está disponible."));

        return File(descarga.Value.Contenido, descarga.Value.ContentType, descarga.Value.NombreArchivo);
    }

    [HttpDelete("{id:int}/documentos/{documentoId:int}")]
    [RequierePermiso(ModuloSistema.Compras, AccionPermiso.Editar)]
    public async Task<IActionResult> DeleteDocumento(int id, int documentoId)
    {
        var eliminado = await _documentoService.DeleteAsync(id, documentoId);
        if (!eliminado)
            return NotFound(ApiResponse<object>.Fail("Comprobante no encontrado."));

        return Ok(ApiResponse<object>.Ok(new { }, "Comprobante retirado correctamente."));
    }

    private IActionResult CrearRespuestaEscaner(
        ResultadoResolucionProductoEscaner<ProductoEscaneadoCompraDto> resultado) =>
        resultado.Estado switch
        {
            EstadoResolucionProductoEscaner.Encontrado =>
                Ok(ApiResponse<ProductoEscaneadoCompraDto>.Ok(
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

    private async Task ValidarProductosActivosAsync(IEnumerable<int> productoIds)
    {
        foreach (var productoId in productoIds.Distinct())
        {
            var producto = await _productoService.GetByIdAsync(productoId)
                ?? throw new BusinessRuleException($"El producto con id {productoId} no existe.");

            if (!producto.Activo)
                throw new BusinessRuleException(
                    $"El producto '{producto.Nombre}' está inactivo. Actívalo antes de incluirlo en una compra.");
        }
    }
}
