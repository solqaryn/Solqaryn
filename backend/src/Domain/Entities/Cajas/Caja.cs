using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums.Cajas;

namespace Solqaryn.Domain.Entities.Cajas;

public class Caja : BaseEntity
{
    public string Nombre { get; private set; } = null!;
    public EstadoCaja Estado { get; private set; }
    public int? SesionActivaId { get; private set; }

    protected Caja() { }

    public Caja(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre de la caja es requerido.", nameof(nombre));

        Nombre = nombre.Trim();
        Estado = EstadoCaja.Inactiva;
    }

    public void Activar()
    {
        Estado = EstadoCaja.Activa;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Desactivar()
    {
        if (SesionActivaId.HasValue)
            throw new InvalidOperationException("No se puede desactivar una caja con una sesión activa.");

        Estado = EstadoCaja.Inactiva;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void RegistrarSesionActiva(int sesionId)
    {
        if (Estado != EstadoCaja.Activa)
            throw new InvalidOperationException("La caja debe estar activa para registrar una sesión.");
        if (sesionId <= 0)
            throw new ArgumentOutOfRangeException(nameof(sesionId), "La sesión debe estar persistida.");
        if (SesionActivaId.HasValue)
            throw new InvalidOperationException("La caja ya tiene una sesión activa.");

        SesionActivaId = sesionId;
        FechaActualizacion = DateTime.UtcNow;
    }

    public void LiberarSesionActiva(int sesionId)
    {
        if (!SesionActivaId.HasValue || SesionActivaId.Value != sesionId)
            throw new InvalidOperationException("La sesión indicada no coincide con la sesión activa de la caja.");

        SesionActivaId = null;
        FechaActualizacion = DateTime.UtcNow;
    }
}
