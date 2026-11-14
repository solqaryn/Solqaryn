using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Security;

/// <summary>
/// El propietario de altas comerciales procede exclusivamente de una marca
/// server-side establecida tras autorizar RBAC. Nunca del header, query o DTO.
/// Esta etapa no habilita el acceso multiempresa ni atribuye datos históricos.
/// </summary>
public static class VerifiedCommercialTenant
{
    public const string AuthorizedEmpresaIdItemKey = "SOLQARYN_AUTHORIZED_EMPRESA_ID";

    public static int? Resolve(HttpContext? httpContext)
    {
        if (httpContext is not null &&
            httpContext.Items.TryGetValue(AuthorizedEmpresaIdItemKey, out var value) &&
            value is int empresaId &&
            empresaId > 0)
        {
            return empresaId;
        }
        return null;
    }
}
