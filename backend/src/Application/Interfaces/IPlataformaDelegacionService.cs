using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

/// <summary>
/// Consulta de capacidades globales delegadas. Nunca concede autoridad por
/// claims JWT, membresias de empresa, el nombre del rol ni EsAdministrador solo.
/// La asignacion y revocacion de grants son procesos administrativos separados.
/// </summary>
public interface IPlataformaDelegacionService
{
    Task<bool> TienePermisoDelegadoAsync(
        ModuloSistema modulo,
        AccionPermiso accion,
        CancellationToken cancellationToken = default);
}
