using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

/// Genera el PDF oficial de la factura. A4 se conserva como formato
/// predeterminado para correo, WhatsApp y enlaces públicos; descarga e impresión
/// pueden solicitar un perfil explícito sin alterar el snapshot fiscal.
public interface IFacturaPdfService
{
    Task<byte[]> GenerarPdfAsync(FacturaDto factura);

    Task<byte[]> GenerarPdfAsync(FacturaDto factura, FacturaFormatoPdf formato) =>
        GenerarPdfAsync(factura);

    /// <summary>
    /// Contrato tenant-aware para generación autenticada. El contexto debe
    /// provenir de UsuarioEmpresa validado server-side; EmpresaId del cliente no
    /// es autoridad por sí mismo. El PDF sigue siendo un deliverable efímero y no
    /// se persiste desde este contrato.
    /// </summary>
    Task<byte[]> GenerarPdfAsync(
        StorageTenantContext tenant,
        FacturaDto factura,
        FacturaFormatoPdf formato = FacturaFormatoPdf.A4)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(factura);
        return GenerarPdfAsync(factura, formato);
    }
}
