using InventoryApp.Application.DTOs;

namespace InventoryApp.Application.Interfaces;

public interface IWhatsAppPublicoService
{
    Task<WhatsAppPublicoDto> ObtenerAsync(CancellationToken cancellationToken = default);
}
