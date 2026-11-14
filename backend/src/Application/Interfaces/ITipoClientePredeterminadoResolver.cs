using System.Threading.Tasks;

namespace Solqaryn.Application.Interfaces;

public interface ITipoClientePredeterminadoResolver
{
    /// <summary>
    /// Resuelve el ID del tipo de cliente predeterminado según las reglas de consistencia de Solqaryn:
    /// 1. Si hay exactamente un tipo de cliente marcado como predeterminado y activo, retorna su ID.
    /// 2. Si no hay ninguno, retorna el ID del tipo de cliente con código 'SIN_CLASIFICAR'.
    /// 3. Si hay más de uno marcado como predeterminado y activo, lanza una BusinessRuleException detallada.
    /// </summary>
    Task<int> ResolverIdPredeterminadoAsync();
}
