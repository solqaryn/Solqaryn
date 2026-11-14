using Solqaryn.Domain.Entities;

namespace Solqaryn.Domain.ValueObjects;

public sealed record AsignacionReservaAutomatica(
    int ProductoVarianteId,
    int AlmacenId,
    int? UbicacionAlmacenId,
    int Cantidad)
{
    public static AsignacionReservaAutomatica Crear(
        int productoVarianteId,
        int almacenId,
        int? ubicacionAlmacenId,
        int cantidad)
    {
        if (productoVarianteId <= 0)
            throw new ArgumentOutOfRangeException(nameof(productoVarianteId), "La variante debe ser válida.");
        if (almacenId <= 0)
            throw new ArgumentOutOfRangeException(nameof(almacenId), "El almacén debe ser válido.");
        if (ubicacionAlmacenId is <= 0)
            throw new ArgumentOutOfRangeException(nameof(ubicacionAlmacenId), "La ubicación debe ser válida cuando se especifica.");
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad asignada debe ser mayor que cero.");

        return new(productoVarianteId, almacenId, ubicacionAlmacenId, cantidad);
    }
}

public sealed class ReservaAutomaticaPedido
{
    private readonly IReadOnlyList<AsignacionReservaAutomatica> _asignaciones;
    private readonly IReadOnlyDictionary<int, int> _requerimientosPorVariante;

    public int PedidoVentaId { get; }
    public IReadOnlyList<AsignacionReservaAutomatica> Asignaciones => _asignaciones;
    public IReadOnlyDictionary<int, int> RequerimientosPorVariante => _requerimientosPorVariante;

    private ReservaAutomaticaPedido(
        int pedidoVentaId,
        IReadOnlyList<AsignacionReservaAutomatica> asignaciones,
        IReadOnlyDictionary<int, int> requerimientosPorVariante)
    {
        PedidoVentaId = pedidoVentaId;
        _asignaciones = asignaciones;
        _requerimientosPorVariante = requerimientosPorVariante;
    }

    internal static ReservaAutomaticaPedido Crear(
        int pedidoVentaId,
        IEnumerable<PedidoVentaDetalle> detalles,
        IEnumerable<AsignacionReservaAutomatica> asignaciones)
    {
        if (pedidoVentaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(pedidoVentaId), "El pedido debe estar persistido.");

        ArgumentNullException.ThrowIfNull(detalles);
        ArgumentNullException.ThrowIfNull(asignaciones);

        var detallesMaterializados = detalles.ToList();
        if (detallesMaterializados.Count == 0)
            throw new InvalidOperationException("El pedido debe contener detalles para reservar inventario.");

        var requerimientos = new Dictionary<int, int>();
        foreach (var detalle in detallesMaterializados)
        {
            if (detalle.ProductoVarianteId is not int varianteId || varianteId <= 0)
                throw new InvalidOperationException("Todos los detalles del pedido deben tener una variante válida para reservar inventario.");

            var cantidad = ConvertirCantidadEntera(detalle.Cantidad);
            requerimientos[varianteId] = checked(requerimientos.GetValueOrDefault(varianteId) + cantidad);
        }

        var asignacionesMaterializadas = asignaciones.ToList();
        if (asignacionesMaterializadas.Count == 0)
            throw new InvalidOperationException("Se requieren asignaciones físicas explícitas para reservar inventario.");

        var clavesFisicas = new HashSet<(int VarianteId, int AlmacenId, int? UbicacionId)>();
        var asignadoPorVariante = new Dictionary<int, int>();

        foreach (var asignacion in asignacionesMaterializadas)
        {
            var validada = AsignacionReservaAutomatica.Crear(
                asignacion.ProductoVarianteId,
                asignacion.AlmacenId,
                asignacion.UbicacionAlmacenId,
                asignacion.Cantidad);

            var clave = (validada.ProductoVarianteId, validada.AlmacenId, validada.UbicacionAlmacenId);
            if (!clavesFisicas.Add(clave))
                throw new InvalidOperationException("No se puede repetir la misma clave física de reserva.");

            asignadoPorVariante[validada.ProductoVarianteId] =
                checked(asignadoPorVariante.GetValueOrDefault(validada.ProductoVarianteId) + validada.Cantidad);
        }

        foreach (var requerimiento in requerimientos)
        {
            if (!asignadoPorVariante.TryGetValue(requerimiento.Key, out var cantidadAsignada))
                throw new InvalidOperationException($"Falta asignación física para la variante {requerimiento.Key}.");
            if (cantidadAsignada != requerimiento.Value)
                throw new InvalidOperationException(
                    $"La cantidad asignada ({cantidadAsignada}) debe coincidir exactamente con la requerida ({requerimiento.Value}) para la variante {requerimiento.Key}.");
        }

        var variantesExtra = asignadoPorVariante.Keys.Except(requerimientos.Keys).ToArray();
        if (variantesExtra.Length > 0)
            throw new InvalidOperationException($"Existen asignaciones para variantes no requeridas: {string.Join(", ", variantesExtra)}.");

        return new ReservaAutomaticaPedido(
            pedidoVentaId,
            asignacionesMaterializadas.AsReadOnly(),
            new Dictionary<int, int>(requerimientos));
    }

    private static int ConvertirCantidadEntera(decimal cantidad)
    {
        if (cantidad <= 0 || cantidad != decimal.Truncate(cantidad) || cantidad > int.MaxValue)
            throw new InvalidOperationException("La cantidad del pedido debe ser un entero positivo representable para reservar inventario.");

        return decimal.ToInt32(cantidad);
    }
}
