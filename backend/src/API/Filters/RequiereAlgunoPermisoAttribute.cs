using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Solqaryn.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequiereAlgunoPermisoAttribute : Attribute, IAsyncActionFilter
{
    private readonly ModuloSistema _modulo;
    private readonly AccionPermiso[] _acciones;

    public RequiereAlgunoPermisoAttribute(
        ModuloSistema modulo,
        params AccionPermiso[] acciones)
    {
        if (acciones is null || acciones.Length == 0)
            throw new ArgumentException("Debe indicarse al menos una acción permitida.", nameof(acciones));

        _modulo = modulo;
        _acciones = acciones.Distinct().ToArray();
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var empresaId = await TenantPermissionContext.RequireEmpresaIdAsync(context.HttpContext, context.HttpContext.RequestAborted);
        var permisoService = context.HttpContext.RequestServices.GetRequiredService<IPermisoService>();
        foreach (var accion in _acciones)
        {
            if (await permisoService.TienePermisoAsync(empresaId, _modulo, accion))
            {
                await next();
                return;
            }
        }

        throw new ForbiddenAccessException(
            $"No tienes permisos para ejecutar esta operación en el módulo {_modulo} dentro de la empresa solicitada.");
    }
}
