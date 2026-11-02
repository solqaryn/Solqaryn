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

    public ModuloSistema Modulo => _modulo;
    public AccionPermiso Accion => _accion;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var empresaId = await TenantPermissionContext.RequireEmpresaIdAsync(
            http, http.RequestAborted);

        var support = Solqaryn.Application.Security.VerifiedPlatformSupportTenant
            .ResolveCandidate(http);
        if (support is not null)
        {
            if (!HttpMethods.IsGet(http.Request.Method) ||
                _accion != AccionPermiso.Ver ||
                _modulo != support.Modulo ||
                empresaId != support.EmpresaId)
            {
                throw new Solqaryn.Application.Exceptions.ForbiddenAccessException(
                    "La sesión temporal de soporte no autoriza esta ruta, empresa o acción.");
            }

            Solqaryn.Application.Security.VerifiedPlatformSupportTenant.MarkAuthorized(http, support);
            await next();
            return;
        }

        var permisoService = http.RequestServices.GetRequiredService<IPermisoService>();
        await permisoService.VerificarPermisoAsync(empresaId, _modulo, _accion);
        TenantPermissionContext.MarkAuthorizedEmpresaId(http, empresaId);
        await next();
    }
}
