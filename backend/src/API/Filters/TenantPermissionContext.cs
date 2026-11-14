using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Solqaryn.API.Filters;

internal static class TenantPermissionContext
{
    internal const string EmpresaHeader = "X-Empresa-Id";
    internal const string AuthorizedEmpresaIdItemKey = Solqaryn.Application.Security.VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey;

    internal static async Task<int> RequireEmpresaIdAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        // La cabecera es solo un selector, nunca prueba de autoridad.
        // Si el cliente envía un selector explícito ambiguo o inválido,
        // no usar el fallback de membresía única para ocultar el error.
        if (httpContext.Request.Headers.TryGetValue(EmpresaHeader, out var values))
        {
            if (values.Count != 1 ||
                !int.TryParse(values[0], out var selectedEmpresaId) ||
                selectedEmpresaId <= 0)
            {
                throw new ForbiddenAccessException(
                    "Se requiere un selector de empresa válido y único.");
            }

            return selectedEmpresaId;
        }

        var scopeService = httpContext.RequestServices.GetRequiredService<IUsuarioScopeService>();
        var contextoUnico = await scopeService.ObtenerUnicoActualAsync(cancellationToken);
        if (contextoUnico is not null)
            return contextoUnico.EmpresaId;

        throw new ForbiddenAccessException(
            "Se requiere un contexto tenant válido para autorizar esta operación.");
    }

    internal static void MarkAuthorizedEmpresaId(HttpContext httpContext, int empresaId)
    {
        if (empresaId <= 0)
            throw new ForbiddenAccessException("No se puede marcar un tenant autorizado inválido.");

        httpContext.Items[AuthorizedEmpresaIdItemKey] = empresaId;
    }

    internal static int GetAuthorizedEmpresaId(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(AuthorizedEmpresaIdItemKey, out var value) &&
            value is int empresaId &&
            empresaId > 0)
        {
            return empresaId;
        }

        throw new ForbiddenAccessException(
            "La operación no tiene un tenant previamente autorizado por el gate de permisos.");
    }
}
