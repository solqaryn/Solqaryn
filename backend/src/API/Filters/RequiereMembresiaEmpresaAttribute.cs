using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Solqaryn.API.Filters;

/// <summary>
/// Para desplegables operativos que sólo requieren una membresía empresarial
/// activa y no un permiso de administración. La cabecera selecciona el tenant;
/// exclusivamente UsuarioEmpresa + Usuario/Empresa/Rol vigentes lo autorizan.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequiereMembresiaEmpresaAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (http.User.Identity?.IsAuthenticated != true)
            throw new ForbiddenAccessException("Se requiere una sesión autenticada.");

        var empresaId = await TenantPermissionContext.RequireEmpresaIdAsync(
            http, http.RequestAborted);
        var scope = http.RequestServices.GetRequiredService<IUsuarioScopeService>();
        var actual = await scope.ObtenerActualAsync(empresaId, http.RequestAborted);
        if (actual is null || actual.EmpresaId != empresaId)
            throw new ForbiddenAccessException(
                "No existe membresía empresarial activa para esta operación.");

        // La consulta EF posterior no debe operar con filtros comerciales
        // ausentes ni aceptar un EmpresaId del cuerpo/query como autoridad.
        TenantPermissionContext.MarkAuthorizedEmpresaId(http, empresaId);
        await next();
    }
}
