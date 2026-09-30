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

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var empresaId = await TenantPermissionContext.RequireEmpresaIdAsync(context.HttpContext, context.HttpContext.RequestAborted);
        var permisoService = context.HttpContext.RequestServices.GetRequiredService<IPermisoService>();
        await permisoService.VerificarPermisoAsync(empresaId, _modulo, _accion);
        await next();
    }
}
