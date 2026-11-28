using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

/// <summary>Identidad fiscal y visual de la empresa autorizada, nunca la configuración global.</summary>
public interface IEmpresaDocumentoIdentity
{
    Task<EmpresaConfiguracion> ObtenerAsync(bool escritura, CancellationToken cancellationToken = default);
}
