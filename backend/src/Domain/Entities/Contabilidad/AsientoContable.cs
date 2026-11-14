using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities.Contabilidad;

public class AsientoContable : AuditableEntity
{
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Concepto { get; set; } = string.Empty;
    public string? Numero { get; set; }

    public int? DocumentoOrigenId { get; set; }
    public string? TipoDocumentoOrigen { get; set; }

    private readonly List<AsientoDetalle> _detalles = new();
    public IReadOnlyCollection<AsientoDetalle> Detalles => _detalles.AsReadOnly();

    public void AgregarDetalle(AsientoDetalle detalle)
    {
        ArgumentNullException.ThrowIfNull(detalle);
        _detalles.Add(detalle);
    }

    public bool EstaCuadrado()
    {
        if (_detalles.Count == 0) return false;
        return _detalles.Sum(d => d.Debe) == _detalles.Sum(d => d.Haber);
    }

    public void ValidarCuadre()
    {
        if (!EstaCuadrado())
        {
            throw new InvalidOperationException("El asiento contable no está cuadrado. El total del Debe debe ser igual al total del Haber.");
        }
    }
}
