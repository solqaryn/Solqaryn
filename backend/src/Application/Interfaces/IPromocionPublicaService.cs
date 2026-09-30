using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface IPromocionPublicaService
{
    Task<OfertaPublicaDto?> ResolverAsync(
        int productoId,
        int? categoriaId,
        decimal precioNormal,
        DateTime fechaUtc);
}
