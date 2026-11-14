using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Versión temporal de la política de costeo para el ámbito empresarial activo.
/// En N1.10 se vincula a EmpresaConfiguracion; ERP-N6 podrá tenantizar el ámbito
/// sin reinterpretar versiones históricas.
/// </summary>
public sealed class PoliticaCosteoInventario : AuditableEntity
{
    public int EmpresaConfiguracionId { get; private set; }
    public MetodoCosteoInventario Metodo { get; private set; } = MetodoCosteoInventario.PromedioPonderado;
    public DateTime VigenteDesdeUtc { get; private set; }
    public DateTime? VigenteHastaUtc { get; private set; }
    public string Motivo { get; private set; } = string.Empty;

    public bool EstaVigente => !VigenteHastaUtc.HasValue;

    private PoliticaCosteoInventario()
    {
    }

    public static PoliticaCosteoInventario Crear(
        int empresaConfiguracionId,
        MetodoCosteoInventario metodo,
        DateTime vigenteDesdeUtc,
        string motivo)
    {
        if (empresaConfiguracionId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaConfiguracionId), "La configuración empresarial debe ser válida.");
        if (!Enum.IsDefined(typeof(MetodoCosteoInventario), metodo))
            throw new ArgumentOutOfRangeException(nameof(metodo), "El método de costeo no es válido.");
        if (vigenteDesdeUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La vigencia debe expresarse en UTC.", nameof(vigenteDesdeUtc));
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de la política es obligatorio.", nameof(motivo));

        return new PoliticaCosteoInventario
        {
            EmpresaConfiguracionId = empresaConfiguracionId,
            Metodo = metodo,
            VigenteDesdeUtc = vigenteDesdeUtc,
            Motivo = motivo.Trim()
        };
    }

    public void Cerrar(DateTime vigenteHastaUtc)
    {
        if (!EstaVigente)
            throw new InvalidOperationException("La política ya fue cerrada.");
        if (vigenteHastaUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha de cierre debe expresarse en UTC.", nameof(vigenteHastaUtc));
        if (vigenteHastaUtc <= VigenteDesdeUtc)
            throw new ArgumentOutOfRangeException(nameof(vigenteHastaUtc), "El cierre debe ser posterior al inicio de vigencia.");

        VigenteHastaUtc = vigenteHastaUtc;
        FechaActualizacion = DateTime.UtcNow;
    }
}
