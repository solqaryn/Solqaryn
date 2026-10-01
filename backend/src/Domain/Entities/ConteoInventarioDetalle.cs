using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

public class ConteoInventarioDetalle : AuditableEntity
{
    public int ConteoInventarioId { get; set; }
    public ConteoInventario ConteoInventario { get; set; } = null!;

    public int ProductoVarianteId { get; set; }
    public ProductoVariante ProductoVariante { get; set; } = null!;

    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;

    public int? UbicacionAlmacenId { get; set; }
    public UbicacionAlmacen? UbicacionAlmacen { get; set; }

    public int StockEsperadoSnapshot { get; private set; }
    public bool SnapshotMaterializado { get; private set; }
    public int? CantidadContada { get; private set; }
    public int? Diferencia { get; private set; }
    public DateTime? FechaConteo { get; private set; }
    public int? ContadoPorUsuarioId { get; private set; }

    public int? AjusteInventarioId { get; private set; }
    public AjusteInventario? AjusteInventario { get; set; }

    public string? ProductoSkuSnapshot { get; set; }
    public string? ProductoMarcaSnapshot { get; set; }
    public string? ProductoModeloSnapshot { get; set; }
    public string? ProductoColorSnapshot { get; set; }
    public string? ProductoTallaSnapshot { get; set; }

    public bool Capturada => CantidadContada.HasValue;

    public void MaterializarSnapshot(int stockFisicoEsperado)
    {
        if (stockFisicoEsperado < 0)
            throw new ArgumentOutOfRangeException(nameof(stockFisicoEsperado), "El stock físico esperado no puede ser negativo.");
        if (Capturada)
            throw new InvalidOperationException("No puede reemplazarse el snapshot después de capturar la línea.");

        StockEsperadoSnapshot = stockFisicoEsperado;
        SnapshotMaterializado = true;
        Diferencia = null;
    }

    public void RegistrarConteo(int cantidadContada, int usuarioId, DateTime fechaUtc)
    {
        if (cantidadContada < 0)
            throw new ArgumentOutOfRangeException(nameof(cantidadContada), "La cantidad contada no puede ser negativa.");
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario de conteo debe ser válido.");
        ValidarClaveFisica();
        ValidarSnapshotMaterializado();

        CantidadContada = cantidadContada;
        ContadoPorUsuarioId = usuarioId;
        FechaConteo = fechaUtc;
        Diferencia = null;
    }

    public void CerrarDiferencia()
    {
        ValidarSnapshotMaterializado();
        if (!CantidadContada.HasValue)
            throw new InvalidOperationException("La línea debe tener una cantidad contada antes de cerrar su diferencia.");

        Diferencia = CantidadContada.Value - StockEsperadoSnapshot;
    }

    public void VincularAjuste(int ajusteInventarioId)
    {
        if (!Diferencia.HasValue)
            throw new InvalidOperationException("La diferencia debe estar cerrada antes de vincular un ajuste.");
        if (Diferencia.Value == 0)
            throw new InvalidOperationException("Una línea sin diferencia no requiere ajuste.");
        if (ajusteInventarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(ajusteInventarioId), "El ajuste debe ser válido.");
        if (AjusteInventarioId.HasValue && AjusteInventarioId.Value != ajusteInventarioId)
            throw new InvalidOperationException("La línea ya está vinculada a otro ajuste.");

        AjusteInventarioId = ajusteInventarioId;
    }

    public void ValidarClaveFisica()
    {
        if (ProductoVarianteId <= 0)
            throw new InvalidOperationException("La variante es obligatoria en una línea de conteo.");
        if (AlmacenId <= 0)
            throw new InvalidOperationException("El almacén es obligatorio en una línea de conteo.");
    }

    public void ValidarSnapshotMaterializado()
    {
        if (!SnapshotMaterializado)
            throw new InvalidOperationException("La línea debe materializar su snapshot de stock físico antes de operar.");
    }

    public string ClaveFisicaNormalizada => $"{ProductoVarianteId}:{AlmacenId}:{UbicacionAlmacenId?.ToString() ?? "ROOT"}";
}
