using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Security;

/// <summary>
/// Vinculación server-side SOLO para rutas anónimas /tienda. La establece el
/// middleware de cuarentena al constatar una única Empresa; ni el host
/// arbitrario, ni EmpresaId de una cabecera/query/DTO son autoridad.
/// Una vez se habiliten 2+ empresas, debe sustituirse por dominio->tenant
/// verificado. Nunca autoriza endpoints administrativos ni RBAC.
/// </summary>
public static class VerifiedPublicStoreTenant
{
    public const string AuthorizedEmpresaIdItemKey = "SOLQARYN_VERIFIED_PUBLIC_STORE_EMPRESA_ID";
    // Solamente el middleware que comprobo el proxy firmado o el owner legacy
    // puede marcar esta respuesta como privada ANTES del filtro MVC de ETag.
    public const string PrivateNoStoreItemKey = "SOLQARYN_PUBLIC_STORE_PRIVATE_NOSTORE";

    public static void RequirePrivateNoStore(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Items[PrivateNoStoreItemKey] = true;
    }

    public static bool RequiresPrivateNoStore(HttpContext? context) =>
        context is not null &&
        context.Items.TryGetValue(PrivateNoStoreItemKey, out var required) &&
        required is true;

    public static int? Resolve(HttpContext? context)
    {
        if (context is null ||
            !context.Request.Path.StartsWithSegments("/tienda", StringComparison.OrdinalIgnoreCase))
            return null;

        return context.Items.TryGetValue(AuthorizedEmpresaIdItemKey, out var value)
            && value is int tenantId && tenantId > 0
                ? tenantId
                : null;
    }
}
