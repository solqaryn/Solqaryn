using Solqaryn.Domain.Fiscal;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Puerto provider-neutral para facturación fiscal/electrónica. Cada adaptador implementa las
/// reglas y protocolos de la jurisdicción/proveedor que declara soportar; el core no asume
/// legislación, autoridad, formato ni credencial universal.
/// </summary>
public interface IProveedorDocumentoFiscal
{
    string Codigo { get; }

    bool Soporta(PerfilFiscalDocumento perfil);

    Task<ResultadoEmisionFiscal> EmitirAsync(
        SolicitudEmisionFiscal solicitud,
        CancellationToken cancellationToken = default);

    Task<ResultadoEmisionFiscal> ConsultarAsync(
        PerfilFiscalDocumento perfil,
        string referenciaExterna,
        CancellationToken cancellationToken = default);
}
