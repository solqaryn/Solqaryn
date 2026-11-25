using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("categorias/{categoriaId:int}/imagenes")]
public sealed class CategoriaGaleriaController(ICategoriaGaleriaService galerias, IUsuarioScopeService usuarios) : ControllerBase
{
    [HttpGet]
    [RequierePermiso(ModuloSistema.Categorias, AccionPermiso.Ver)]
    public async Task<IActionResult> Obtener(int categoriaId, CancellationToken cancellationToken)
    {
        var tenant = await ResolverTenantAsync(cancellationToken);
        var galeria = await galerias.ObtenerAsync(tenant, categoriaId, cancellationToken);
        return galeria is null ? NotFound(ApiResponse<object>.Fail("Categoría no encontrada."))
            : Ok(ApiResponse<CategoriaGaleriaDto>.Ok(galeria));
    }

    [HttpPut]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(4_500_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 4_500_000)]
    [RequierePermiso(ModuloSistema.Categorias, AccionPermiso.Editar)]
    public async Task<IActionResult> Guardar(int categoriaId, [FromForm] ActualizarCategoriaGaleriaDto dto, CancellationToken cancellationToken)
    {
        var tenant = await ResolverTenantAsync(cancellationToken);
        var galeria = await galerias.GuardarAsync(tenant, categoriaId, dto, cancellationToken);
        return galeria is null ? NotFound(ApiResponse<object>.Fail("Categoría no encontrada."))
            : Ok(ApiResponse<CategoriaGaleriaDto>.Ok(galeria, "Imágenes de categoría guardadas."));
    }

    private async Task<StorageTenantContext> ResolverTenantAsync(CancellationToken cancellationToken)
    {
        var empresaId = TenantPermissionContext.GetAuthorizedEmpresaId(HttpContext);
        var scope = await usuarios.ObtenerActualAsync(empresaId, cancellationToken);
        if (scope is null || scope.EmpresaId != empresaId)
            throw new ForbiddenAccessException("La galería requiere una empresa autorizada.");
        return StorageTenantContext.Desde(scope);
    }
}
