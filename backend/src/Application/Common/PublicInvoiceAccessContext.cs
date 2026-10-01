namespace Solqaryn.Application.Common;

/// <summary>
/// Clave interna utilizada para transportar, dentro de una única solicitud HTTP,
/// el FacturaId autorizado por un token público cuyo hash ya fue localizado.
/// Nunca contiene el token real ni se persiste fuera de HttpContext.Items.
/// </summary>
public static class PublicInvoiceAccessContext
{
    public const string FacturaIdKey = "Solqaryn.PublicInvoiceAccess.FacturaId";
}
