using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Solqaryn.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequierePermisoAttribute : Attribute, IAsyncActionFilter
{
    private readonly ModuloSistema _modulo;
    private readonly AccionPermiso _accion;

    public RequierePermisoAttribute(ModuloSistema modulo, AccionPermiso accion)
    {
        _modulo = modulo;
        _accion = accion;
    }

    // Opt-in: una ruta tenant-owned no puede utilizar el permiso de otra
    // membresía del mismo usuario. No modifica consumidores legacy del filtro.
    public bool VincularEmpresaRuta { get; set; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var empresaId = await TenantPermissionContext.RequireEmpresaIdAsync(context.HttpContext, context.HttpContext.RequestAborted);
        if (VincularEmpresaRuta &&
            (!context.RouteData.Values.TryGetValue("empresaId", out var valorRuta) ||
             !int.TryParse(valorRuta?.ToString(), out var empresaRuta) ||
             empresaRuta != empresaId))
        {
            throw new ForbiddenAccessException(
                "La empresa de la ruta no coincide con el contexto de autorización.");
        }

        var permisoService = context.HttpContext.RequestServices.GetRequiredService<IPermisoService>();
        await permisoService.VerificarPermisoAsync(empresaId, _modulo, _accion);
        TenantPermissionContext.MarkAuthorizedEmpresaId(context.HttpContext, empresaId);
        await next();
    }
}
