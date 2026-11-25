using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Solqaryn.API.Controllers;

/// <summary>Registra stock físico inicial dentro de la transacción de creación del producto o variante.</summary>
internal static class StockInicialProducto
{
    internal static async Task ValidarAlmacenAsync(HttpContext context, int almacenId)
    {
        var empresaId = TenantPermissionContext.GetAuthorizedEmpresaId(context);
        var permisos = context.RequestServices.GetRequiredService<IPermisoService>();
        await permisos.VerificarPermisoAsync(empresaId, ModuloSistema.Inventario, AccionPermiso.Crear);
        await permisos.VerificarPermisoAsync(empresaId, ModuloSistema.Almacenes, AccionPermiso.Ver);

        if (almacenId <= 0)
            throw new BusinessRuleException("Selecciona un almacén válido para el stock inicial.");

        var almacen = await context.RequestServices.GetRequiredService<IAlmacenRepository>().GetByIdAsync(almacenId);
        if (almacen is null || almacen.Eliminado || !almacen.Activo
            || almacen.Sucursal is null || almacen.Sucursal.Eliminado || !almacen.Sucursal.Activa
            || almacen.Sucursal.EmpresaId != empresaId
            || (almacen.Tipo != TipoAlmacen.Tienda && almacen.Tipo != TipoAlmacen.Bodega))
            throw new BusinessRuleException("Selecciona una tienda o bodega activa de tu empresa para el stock inicial.");
    }

    internal static async Task RegistrarAsync(HttpContext context, int almacenId, ProductoVarianteDto variante)
    {
        if (variante.Cantidad <= 0) return;
        await context.RequestServices.GetRequiredService<IExistenciaVarianteService>().CreateAsync(new CreateExistenciaVarianteDto
        {
            ProductoVarianteId = variante.Id,
            AlmacenId = almacenId,
            StockFisico = variante.Cantidad,
            StockReservado = 0,
            StockTransito = 0,
            StockMinimo = variante.UmbralStockBajo
        });
    }
}
