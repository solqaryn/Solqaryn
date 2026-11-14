using System.Linq;
using System.Threading.Tasks;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Application.Services;

public class TipoClientePredeterminadoResolver : ITipoClientePredeterminadoResolver
{
    private readonly ITipoClienteRepository _tipoClienteRepository;
    private readonly ILogger<TipoClientePredeterminadoResolver> _logger;

    public TipoClientePredeterminadoResolver(
        ITipoClienteRepository tipoClienteRepository,
        ILogger<TipoClientePredeterminadoResolver> logger)
    {
        _tipoClienteRepository = tipoClienteRepository;
        _logger = logger;
    }

    public async Task<int> ResolverIdPredeterminadoAsync()
    {
        var tiposActivos = await _tipoClienteRepository.GetActivosAsync();
        var predeterminados = tiposActivos.Where(t => t.EsPredeterminado && !t.Eliminado).ToList();

        if (predeterminados.Count == 1)
        {
            return predeterminados[0].Id;
        }

        if (predeterminados.Count > 1)
        {
            _logger.LogError("Inconsistencia crítica de base de datos: existen múltiples tipos de clientes activos marcados como predeterminados (IDs: {Ids}).", 
                string.Join(", ", predeterminados.Select(p => p.Id)));
            throw new BusinessRuleException("Inconsistencia en el sistema: existen múltiples tipos de clientes marcados como predeterminados.");
        }

        // Caso predeterminados.Count == 0 (Ninguno)
        var fallback = await _tipoClienteRepository.GetByCodigoAsync("SIN_CLASIFICAR");
        if (fallback is null || !fallback.Activo || fallback.Eliminado)
        {
            _logger.LogError("Inconsistencia crítica del sistema: no se encontró ningún tipo de cliente predeterminado activo ni el tipo de respaldo activo y no eliminado 'SIN_CLASIFICAR'.");
            throw new BusinessRuleException("Inconsistencia en el sistema: no se encontró el tipo de cliente predeterminado ni el de respaldo 'SIN_CLASIFICAR' activo.");
        }

        return fallback.Id;
    }
}
