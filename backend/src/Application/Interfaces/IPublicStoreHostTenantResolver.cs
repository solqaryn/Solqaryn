using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Contrato de vinculación pública explícita, configurada exclusivamente
/// en el servidor. La cabecera Host localiza una entrada previamente aprobada;
/// X-Empresa-Id, query y cabeceras reenviadas no conceden autoridad.
/// </summary>
public interface IPublicStoreHostTenantResolver
{
    bool IsEnabled { get; }
    int? ResolveEmpresaId(HostString requestHost);
    // En un origen compartido, la ruta pública /tiendas/{storeKey} identifica
    // una entrada aprobada adicional. Nunca aceptar storeKey desde una cabecera
    // sin comprobación criptográfica previa en el middleware.
    int? ResolveEmpresaId(HostString requestHost, string? storeKey);
}
