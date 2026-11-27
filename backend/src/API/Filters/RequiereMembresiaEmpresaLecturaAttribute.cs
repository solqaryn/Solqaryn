using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Solqaryn.API.Filters;

/// <summary>
/// Shared read-only catalogues are consumed by operational users without
/// requiring catalog administration grants. Authenticate a current company
/// membership before resolving the EF read scope; never grant write authority.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequiereMembresiaEmpresaLecturaAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        if (!HttpMethods.IsGet(http.Request.Method) || http.User.Identity?.IsAuthenticated != true)
            throw new ForbiddenAccessException("Se requiere una consulta autenticada.");

        var scope = http.RequestServices.GetRequiredService<IUsuarioScopeService>();
        UsuarioTenantScopeActual? membership;
        if (http.Request.Headers.TryGetValue(TenantPermissionContext.EmpresaHeader, out var raw))
        {
            if (raw.Count != 1 || !int.TryParse(raw[0], out var requested) || requested <= 0)
                throw new ForbiddenAccessException("El contexto empresarial solicitado es inválido.");
            membership = await scope.ObtenerActualAsync(requested, http.RequestAborted);
            if (membership?.EmpresaId != requested)
                throw new ForbiddenAccessException("No existe una membresía empresarial vigente autorizada.");
        }
        else
        {
            membership = await scope.ObtenerUnicoActualAsync(http.RequestAborted);
        }

        if (membership is null || membership.EmpresaId <= 0)
            throw new ForbiddenAccessException("Se requiere una membresía empresarial vigente y un contexto inequívoco.");

        // A different authorized context must never be silently overridden.
        if (http.Items.TryGetValue(TenantPermissionContext.AuthorizedEmpresaIdItemKey, out var authorized) &&
            !Equals(authorized, membership.EmpresaId))
            throw new ForbiddenAccessException("La empresa no coincide con el contexto autorizado.");

        var key = AppDbContext.AuthorizedCommercialReadEmpresaItemKey;
        if (http.Items.ContainsKey(key))
            throw new ForbiddenAccessException("El contexto de lectura ya fue establecido.");

        http.Items[key] = membership.EmpresaId;
        try
        {
            await next();
        }
        finally
        {
            http.Items.Remove(key);
        }
    }
}
