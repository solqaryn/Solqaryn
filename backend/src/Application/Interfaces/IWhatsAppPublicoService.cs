using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IWhatsAppPublicoService
{
    Task<WhatsAppPublicoDto> ObtenerAsync(CancellationToken cancellationToken = default);
}
