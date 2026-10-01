using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class SerieInventario : AuditableEntity
{
    private const int NumeroSerieMaxLength = 120;

    public int ProductoVarianteId { get; set; }
    public ProductoVariante ProductoVariante { get; set; } = null!;
    public int? LoteInventarioId { get; set; }
    public LoteInventario? LoteInventario { get; set; }
    public string NumeroSerie { get; private set; } = string.Empty;
    public EstadoSerieInventario Estado { get; private set; } = EstadoSerieInventario.Disponible;

    public void ConfigurarIdentidad(string numeroSerie)
    {
        if (ProductoVarianteId <= 0) throw new InvalidOperationException("La variante es obligatoria para identificar una serie.");
        if (string.IsNullOrWhiteSpace(numeroSerie)) throw new ArgumentException("El número de serie es obligatorio.", nameof(numeroSerie));

        var numeroNormalizado = numeroSerie.Trim().ToUpperInvariant();
        if (numeroNormalizado.Length > NumeroSerieMaxLength)
            throw new InvalidOperationException($"El número de serie no puede superar {NumeroSerieMaxLength} caracteres.");

        NumeroSerie = numeroNormalizado;
    }

    public void VincularLote(LoteInventario lote)
    {
        ArgumentNullException.ThrowIfNull(lote);
        if (ProductoVarianteId <= 0) throw new InvalidOperationException("La variante de la serie debe estar definida antes de vincular un lote.");
        if (lote.Id <= 0) throw new InvalidOperationException("El lote debe estar persistido antes de vincularse a una serie.");
        if (lote.ProductoVarianteId != ProductoVarianteId)
            throw new InvalidOperationException("La serie y el lote deben pertenecer a la misma variante.");

        LoteInventarioId = lote.Id;
        LoteInventario = lote;
    }

    public void Reservar()
    {
        ExigirEstado(EstadoSerieInventario.Disponible, "Sólo una serie disponible puede reservarse.");
        Estado = EstadoSerieInventario.Reservada;
    }

    public void LiberarReserva()
    {
        ExigirEstado(EstadoSerieInventario.Reservada, "Sólo una serie reservada puede liberarse.");
        Estado = EstadoSerieInventario.Disponible;
    }

    public void MarcarEnTransito()
    {
        if (Estado is not (EstadoSerieInventario.Disponible or EstadoSerieInventario.Reservada))
            throw new InvalidOperationException("Sólo una serie disponible o reservada puede entrar en tránsito.");
        Estado = EstadoSerieInventario.EnTransito;
    }

    public void RecibirDeTransito()
    {
        ExigirEstado(EstadoSerieInventario.EnTransito, "Sólo una serie en tránsito puede recibirse.");
        Estado = EstadoSerieInventario.Disponible;
    }

    public void Vender()
    {
        if (Estado is not (EstadoSerieInventario.Disponible or EstadoSerieInventario.Reservada))
            throw new InvalidOperationException("Sólo una serie disponible o reservada puede venderse.");
        Estado = EstadoSerieInventario.Vendida;
    }

    public void DarDeBaja()
    {
        if (Estado == EstadoSerieInventario.Vendida) throw new InvalidOperationException("Una serie vendida no puede darse de baja como inventario disponible.");
        if (Estado == EstadoSerieInventario.Baja) throw new InvalidOperationException("La serie ya está dada de baja.");
        Estado = EstadoSerieInventario.Baja;
    }

    private void ExigirEstado(EstadoSerieInventario esperado, string mensaje)
    {
        if (Estado != esperado) throw new InvalidOperationException(mensaje);
    }
}
