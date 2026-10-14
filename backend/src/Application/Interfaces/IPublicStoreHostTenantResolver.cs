using Microsoft.AspNetCore.Http;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Contrato de vinculación pública explícita, configurada exclusivamente
/// en el servidor. La cabecera Host localiza una entrada previamente aprobada;
/// X-Empresa-Id, query y cabeceras reenviadas no conceden autoridad.
/// </summary>
public interface IPublicStoreHostTenantResolver
{
    int? ResolveEmpresaId(HostString requestHost);
}
