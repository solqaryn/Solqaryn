using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class ConteoInventario : AuditableEntity
{
    public string Numero { get; set; } = string.Empty;
    public TipoConteoInventario Tipo { get; set; } = TipoConteoInventario.General;

    public int AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    public int? UbicacionAlmacenId { get; set; }
    public UbicacionAlmacen? UbicacionAlmacen { get; set; }
    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public bool EsCiego { get; set; }
    public EstadoConteoInventario Estado { get; private set; } = EstadoConteoInventario.Borrador;
    public string? Observaciones { get; set; }

    public DateTime? FechaInicio { get; private set; }
    public int? IniciadoPorUsuarioId { get; private set; }
    public DateTime? FechaCierre { get; private set; }
    public int? CerradoPorUsuarioId { get; private set; }
    public DateTime? FechaAprobacion { get; private set; }
    public int? AprobadoPorUsuarioId { get; private set; }
    public DateTime? FechaCancelacion { get; private set; }
    public int? CanceladoPorUsuarioId { get; private set; }
    public string? MotivoCancelacion { get; private set; }

    public ICollection<ConteoInventarioDetalle> Detalles { get; set; } = new List<ConteoInventarioDetalle>();

    public void Iniciar(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoConteoInventario.Borrador)
            throw new InvalidOperationException("Solo un conteo en borrador puede iniciarse.");
        ValidarUsuario(usuarioId);
        ValidarDocumento();
        ValidarScope();
        ValidarClavesFisicasUnicas();

        Estado = EstadoConteoInventario.EnProceso;
        IniciadoPorUsuarioId = usuarioId;
        FechaInicio = fechaUtc;
    }

    public void Cerrar(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoConteoInventario.EnProceso)
            throw new InvalidOperationException("Solo un conteo en proceso puede cerrarse.");
        ValidarUsuario(usuarioId);
        if (Detalles.Count == 0)
            throw new InvalidOperationException("El conteo no contiene líneas materializadas.");
        if (Detalles.Any(x => !x.Capturada))
            throw new InvalidOperationException("Todas las líneas deben estar capturadas antes de cerrar el conteo.");

        foreach (var detalle in Detalles)
            detalle.CerrarDiferencia();

        Estado = EstadoConteoInventario.Cerrado;
        CerradoPorUsuarioId = usuarioId;
        FechaCierre = fechaUtc;
    }

    public void Aprobar(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoConteoInventario.Cerrado)
            throw new InvalidOperationException("Solo un conteo cerrado puede aprobarse.");
        ValidarUsuario(usuarioId);
        if (Detalles.Any(x => !x.Diferencia.HasValue))
            throw new InvalidOperationException("Todas las diferencias deben estar cerradas antes de aprobar el conteo.");

        Estado = EstadoConteoInventario.Aprobado;
        AprobadoPorUsuarioId = usuarioId;
        FechaAprobacion = fechaUtc;
    }

    public void Cancelar(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado is EstadoConteoInventario.Aprobado or EstadoConteoInventario.Cancelado)
            throw new InvalidOperationException("Un conteo aprobado o cancelado no puede cancelarse.");
        ValidarUsuario(usuarioId);
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de cancelación es obligatorio.", nameof(motivo));

        Estado = EstadoConteoInventario.Cancelado;
        CanceladoPorUsuarioId = usuarioId;
        FechaCancelacion = fechaUtc;
        MotivoCancelacion = motivo.Trim();
    }

    public int CantidadLineas => Detalles.Count;
    public int CantidadCapturadas => Detalles.Count(x => x.Capturada);
    public int CantidadConDiferencia => Detalles.Count(x => x.Diferencia.HasValue && x.Diferencia.Value != 0);
    public int DiferenciaNeta => Detalles.Where(x => x.Diferencia.HasValue).Sum(x => x.Diferencia!.Value);

    private void ValidarDocumento()
    {
        if (string.IsNullOrWhiteSpace(Numero))
            throw new InvalidOperationException("El número de conteo es obligatorio.");
        if (AlmacenId <= 0)
            throw new InvalidOperationException("El almacén es obligatorio.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("El conteo debe materializar al menos una línea antes de iniciar.");
        if (Detalles.Any(x => x.AlmacenId != AlmacenId))
            throw new InvalidOperationException("Todas las líneas deben pertenecer al almacén del conteo.");
        if (Detalles.Any(x => !x.SnapshotMaterializado))
            throw new InvalidOperationException("Todas las líneas deben materializar el snapshot de stock físico antes de iniciar.");
    }

    private void ValidarScope()
    {
        if (Tipo == TipoConteoInventario.PorUbicacion && !UbicacionAlmacenId.HasValue)
            throw new InvalidOperationException("El conteo por ubicación requiere una ubicación.");
        if (Tipo == TipoConteoInventario.PorCategoria && !CategoriaId.HasValue)
            throw new InvalidOperationException("El conteo por categoría requiere una categoría.");
        if (Tipo == TipoConteoInventario.Ciego && !EsCiego)
            throw new InvalidOperationException("El tipo de conteo ciego debe marcarse como ciego.");
        if (UbicacionAlmacenId.HasValue && Detalles.Any(x => x.UbicacionAlmacenId != UbicacionAlmacenId))
            throw new InvalidOperationException("Las líneas no respetan la ubicación definida en el scope del conteo.");
    }

    private void ValidarClavesFisicasUnicas()
    {
        foreach (var detalle in Detalles)
            detalle.ValidarClaveFisica();

        var duplicada = Detalles
            .GroupBy(x => x.ClaveFisicaNormalizada, StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicada is not null)
            throw new InvalidOperationException($"La clave física {duplicada.Key} está duplicada dentro del conteo.");
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }
}
