using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class LoteInventario : AuditableEntity
{
    public int ProductoVarianteId { get; set; }
    public ProductoVariante ProductoVariante { get; set; } = null!;

    public string Codigo { get; private set; } = string.Empty;
    public DateTime? FechaFabricacion { get; private set; }
    public DateTime? FechaVencimiento { get; private set; }
    public bool Activo { get; private set; } = true;

    public void ConfigurarIdentidad(
        string codigo,
        DateTime? fechaFabricacion,
        DateTime? fechaVencimiento,
        bool requiereVencimiento)
    {
        if (ProductoVarianteId <= 0)
            throw new InvalidOperationException("La variante es obligatoria para identificar un lote.");
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código de lote es obligatorio.", nameof(codigo));
        if (requiereVencimiento && !fechaVencimiento.HasValue)
            throw new InvalidOperationException("La fecha de vencimiento es obligatoria para una variante que controla vencimiento.");
        if (fechaFabricacion.HasValue && fechaVencimiento.HasValue && fechaVencimiento.Value.Date < fechaFabricacion.Value.Date)
            throw new InvalidOperationException("La fecha de vencimiento no puede ser anterior a la fecha de fabricación.");

        Codigo = codigo.Trim().ToUpperInvariant();
        FechaFabricacion = fechaFabricacion?.Date;
        FechaVencimiento = fechaVencimiento?.Date;
    }

    public void Desactivar() => Activo = false;

    public bool EstaVencido(DateTime fechaUtc) =>
        FechaVencimiento.HasValue && FechaVencimiento.Value.Date < fechaUtc.Date;

    public bool VenceDentroDe(DateTime fechaUtc, int dias)
    {
        if (dias < 0)
            throw new ArgumentOutOfRangeException(nameof(dias));

        return FechaVencimiento.HasValue &&
               !EstaVencido(fechaUtc) &&
               FechaVencimiento.Value.Date <= fechaUtc.Date.AddDays(dias);
    }
}
