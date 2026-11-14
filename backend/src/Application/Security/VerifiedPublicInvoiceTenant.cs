using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Security;

/// <summary>
/// La autoridad de lectura de un PDF público puede derivarse del hash de un
/// token impredecible ya localizado contra un enlace persistido con EmpresaId.
/// No se deriva de cabeceras, parámetros de empresa, cache ni roles externos.
/// Se restringe a la ruta de PDF público y a la solicitud HTTP corriente.
/// </summary>
public static class VerifiedPublicInvoiceTenant
{
    public const string AuthorizedEmpresaIdItemKey = "SOLQARYN_VERIFIED_PUBLIC_INVOICE_EMPRESA_ID";

    public static int? Resolve(HttpContext? context)
    {
        if (context is null ||
            !context.Request.Path.StartsWithSegments(
                "/facturas/publico", StringComparison.OrdinalIgnoreCase))
            return null;

        return context.Items.TryGetValue(AuthorizedEmpresaIdItemKey, out var value)
            && value is int tenantId && tenantId > 0
                ? tenantId
                : null;
    }
}
