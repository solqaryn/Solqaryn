using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// N3.6.B — contrato puro de dominio para devoluciones de cliente.
/// No materializa stock, Kardex, caja, crédito, nota de crédito ni mutaciones sobre Venta/Factura.
/// Esos efectos pertenecen a las capas posteriores y deben ejecutarse transaccionalmente.
/// </summary>
public class DevolucionCliente : ConfirmableEntity
{
    private readonly List<DevolucionClienteDetalle> _detalles = new();

    private DevolucionCliente()
    {
    }

    public int VentaId { get; private set; }
    public Venta Venta { get; private set; } = null!;

    public int? FacturaId { get; private set; }
    public Factura? Factura { get; private set; }

    public EstadoDevolucionCliente Estado { get; private set; } = EstadoDevolucionCliente.Borrador;
    public string? Observaciones { get; private set; }

    // N3.6.B define el contrato durable; unicidad física y resolución de replay/conflicto pertenecen a C/D.
    public string? IdempotencyKey { get; private set; }
    public string? IdempotencyFingerprint { get; private set; }

    public IReadOnlyCollection<DevolucionClienteDetalle> Detalles => _detalles.AsReadOnly();
    public bool EsEditable => Estado == EstadoDevolucionCliente.Borrador;
    public bool EstaConfirmada => Estado == EstadoDevolucionCliente.Confirmada;
    public bool EstaAnulada => Estado == EstadoDevolucionCliente.Anulada;
    public decimal MontoReferencia => _detalles.Sum(x => x.MontoReferencia);

    public static DevolucionCliente CrearDesdeVenta(Venta venta, Factura? factura = null)
    {
        ArgumentNullException.ThrowIfNull(venta);

        if (venta.Id <= 0)
            throw new InvalidOperationException("La venta de origen debe estar persistida.");
        if (venta.Eliminado)
            throw new InvalidOperationException("Una venta eliminada no puede originar una devolución.");
        if (venta.Estado != EstadoDocumento.Confirmada)
            throw new InvalidOperationException("Solo una venta confirmada puede originar una devolución.");

        if (factura is not null)
        {
            if (factura.Id <= 0)
                throw new InvalidOperationException("La factura de referencia debe estar persistida.");
            if (factura.VentaId != venta.Id)
                throw new InvalidOperationException("La factura de referencia debe pertenecer a la venta de origen.");
            if (factura.Estado is EstadoFactura.Borrador or EstadoFactura.Anulada or EstadoFactura.Cancelada)
                throw new InvalidOperationException("La factura de referencia no está en un estado elegible para devolución.");
        }

        return new DevolucionCliente
        {
            VentaId = venta.Id,
            Venta = venta,
            FacturaId = factura?.Id,
            Factura = factura
        };
    }

    public void AgregarDetalle(
        VentaDetalle detalleVenta,
        int cantidad,
        int cantidadYaDevuelta,
        TipoResolucionDevolucionCliente resolucion)
    {
        AsegurarEditable();
        ArgumentNullException.ThrowIfNull(detalleVenta);

        if (detalleVenta.VentaId != VentaId)
            throw new InvalidOperationException("El detalle debe pertenecer a la venta de origen.");
        if (_detalles.Any(x => x.VentaDetalleId == detalleVenta.Id))
            throw new InvalidOperationException("Una línea de venta solo puede aparecer una vez dentro de la misma devolución.");

        _detalles.Add(DevolucionClienteDetalle.CrearDesdeVentaDetalle(
            detalleVenta,
            cantidad,
            cantidadYaDevuelta,
            resolucion));
    }

    public void ActualizarObservaciones(string? observaciones)
    {
        AsegurarEditable();
        Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
    }

    public void EstablecerIdempotencia(string key, string fingerprint)
    {
        AsegurarEditable();

        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length > 128)
            throw new ArgumentException("La clave de idempotencia es obligatoria y no puede superar 128 caracteres.", nameof(key));
        if (string.IsNullOrWhiteSpace(fingerprint))
            throw new ArgumentException("El fingerprint de idempotencia debe ser SHA-256 hexadecimal.", nameof(fingerprint));

        var keyNormalizada = key.Trim();
        var fingerprintNormalizado = fingerprint.Trim().ToLowerInvariant();
        if (fingerprintNormalizado.Length != 64 || !EsHexadecimal(fingerprintNormalizado))
            throw new ArgumentException("El fingerprint de idempotencia debe ser SHA-256 hexadecimal.", nameof(fingerprint));

        if (IdempotencyKey is not null && !string.Equals(IdempotencyKey, keyNormalizada, StringComparison.Ordinal))
            throw new InvalidOperationException("La clave de idempotencia de una devolución no puede sustituirse.");
        if (IdempotencyFingerprint is not null && !string.Equals(IdempotencyFingerprint, fingerprintNormalizado, StringComparison.Ordinal))
            throw new InvalidOperationException("El fingerprint de idempotencia de una devolución no puede sustituirse.");

        IdempotencyKey = keyNormalizada;
        IdempotencyFingerprint = fingerprintNormalizado;
    }

    public void Confirmar(int usuarioId, string nombreUsuario, DateTime fechaUtc)
    {
        if (Estado != EstadoDevolucionCliente.Borrador)
            throw new InvalidOperationException("Solo una devolución en borrador puede confirmarse.");

        ValidarUsuario(usuarioId);
        var nombreValidado = ValidarNombreUsuario(nombreUsuario, nameof(nombreUsuario));
        var fechaValidada = AsegurarUtc(fechaUtc, nameof(fechaUtc));
        ValidarDocumento(requerirIdempotencia: true);

        Estado = EstadoDevolucionCliente.Confirmada;
        FechaConfirmacion = fechaValidada;
        ConfirmadoPorUsuarioId = usuarioId;
        ConfirmadoPorNombreUsuario = nombreValidado;
    }

    public void Anular(int usuarioId, string nombreUsuario, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoDevolucionCliente.Confirmada)
            throw new InvalidOperationException("Solo una devolución confirmada puede anularse.");

        ValidarUsuario(usuarioId);
        var nombreValidado = ValidarNombreUsuario(nombreUsuario, nameof(nombreUsuario));
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));
        var fechaValidada = AsegurarUtc(fechaUtc, nameof(fechaUtc));

        Estado = EstadoDevolucionCliente.Anulada;
        FechaAnulacion = fechaValidada;
        AnuladoPorUsuarioId = usuarioId;
        AnuladoPorNombreUsuario = nombreValidado;
        MotivoAnulacion = motivo.Trim();
    }

    public void ValidarDocumento(bool requerirIdempotencia = false)
    {
        if (VentaId <= 0)
            throw new InvalidOperationException("La venta de origen es obligatoria.");
        if (FacturaId is <= 0)
            throw new InvalidOperationException("La factura debe ser válida cuando se especifica.");
        if (_detalles.Count == 0)
            throw new InvalidOperationException("La devolución debe contener al menos un detalle.");

        var tieneKey = !string.IsNullOrWhiteSpace(IdempotencyKey);
        var tieneFingerprint = !string.IsNullOrWhiteSpace(IdempotencyFingerprint);
        if (tieneKey != tieneFingerprint)
            throw new InvalidOperationException("La idempotencia debe persistir clave y fingerprint de forma atómica.");
        if (requerirIdempotencia && !tieneKey)
            throw new InvalidOperationException("La devolución debe definir idempotencia antes de confirmarse.");
        if (tieneKey && (IdempotencyKey!.Length > 128 || IdempotencyFingerprint!.Length != 64 || !EsHexadecimal(IdempotencyFingerprint)))
            throw new InvalidOperationException("La idempotencia persistida no cumple el contrato durable.");

        foreach (var detalle in _detalles)
            detalle.Validar();
    }

    private void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo una devolución en borrador puede modificarse.");
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }

    private static string ValidarNombreUsuario(string nombreUsuario, string parametro)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new ArgumentException("El nombre del usuario es obligatorio.", parametro);
        return nombreUsuario.Trim();
    }

    private static DateTime AsegurarUtc(DateTime fecha, string parametro)
    {
        if (fecha.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", parametro);
        return fecha;
    }

    private static bool EsHexadecimal(string valor) => valor.All(c =>
        (c >= '0' && c <= '9') ||
        (c >= 'a' && c <= 'f') ||
        (c >= 'A' && c <= 'F'));
}

public class DevolucionClienteDetalle : AuditableEntity
{
    public int DevolucionClienteId { get; private set; }
    public DevolucionCliente DevolucionCliente { get; private set; } = null!;

    public int VentaDetalleId { get; private set; }
    public int ProductoId { get; private set; }
    public int? ProductoVarianteId { get; private set; }

    public string? ProductoSkuSnapshot { get; private set; }
    public string ProductoNombreSnapshot { get; private set; } = string.Empty;
    public string ProductoMarcaSnapshot { get; private set; } = string.Empty;
    public string ProductoModeloSnapshot { get; private set; } = string.Empty;
    public string? ProductoColorSnapshot { get; private set; }
    public string? ProductoTallaSnapshot { get; private set; }

    public int Cantidad { get; private set; }
    public int CantidadVendidaSnapshot { get; private set; }
    public decimal PrecioUnitarioSnapshot { get; private set; }
    public TipoResolucionDevolucionCliente Resolucion { get; private set; }
    public decimal MontoReferencia => Cantidad * PrecioUnitarioSnapshot;

    internal static DevolucionClienteDetalle CrearDesdeVentaDetalle(
        VentaDetalle detalleVenta,
        int cantidad,
        int cantidadYaDevuelta,
        TipoResolucionDevolucionCliente resolucion)
    {
        ArgumentNullException.ThrowIfNull(detalleVenta);

        if (detalleVenta.Id <= 0)
            throw new InvalidOperationException("El detalle de venta de origen debe estar persistido.");
        if (detalleVenta.ProductoId <= 0)
            throw new InvalidOperationException("El producto de origen debe ser válido.");
        if (detalleVenta.ProductoVarianteId is <= 0)
            throw new InvalidOperationException("La variante debe ser válida cuando se especifica.");
        if (detalleVenta.Cantidad <= 0)
            throw new InvalidOperationException("La cantidad vendida debe ser mayor que cero.");
        if (cantidad <= 0)
            throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad devuelta debe ser mayor que cero.");
        if (cantidadYaDevuelta < 0)
            throw new ArgumentOutOfRangeException(nameof(cantidadYaDevuelta), "La cantidad previamente devuelta no puede ser negativa.");
        if (cantidadYaDevuelta + cantidad > detalleVenta.Cantidad)
            throw new InvalidOperationException("La devolución acumulada no puede superar la cantidad originalmente vendida.");
        if (!Enum.IsDefined(typeof(TipoResolucionDevolucionCliente), resolucion))
            throw new ArgumentOutOfRangeException(nameof(resolucion), "La resolución de la devolución no es válida.");
        if (detalleVenta.PrecioUnitario < 0)
            throw new InvalidOperationException("El precio unitario de origen no puede ser negativo.");

        return new DevolucionClienteDetalle
        {
            VentaDetalleId = detalleVenta.Id,
            ProductoId = detalleVenta.ProductoId,
            ProductoVarianteId = detalleVenta.ProductoVarianteId,
            ProductoSkuSnapshot = detalleVenta.ProductoSkuSnapshot,
            ProductoNombreSnapshot = detalleVenta.ProductoNombreSnapshot,
            ProductoMarcaSnapshot = detalleVenta.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = detalleVenta.ProductoModeloSnapshot,
            ProductoColorSnapshot = detalleVenta.ProductoColorSnapshot,
            ProductoTallaSnapshot = detalleVenta.ProductoTallaSnapshot,
            Cantidad = cantidad,
            CantidadVendidaSnapshot = detalleVenta.Cantidad,
            PrecioUnitarioSnapshot = detalleVenta.PrecioUnitario,
            Resolucion = resolucion
        };
    }

    public void Validar()
    {
        if (VentaDetalleId <= 0)
            throw new InvalidOperationException("El detalle de venta de origen es obligatorio.");
        if (ProductoId <= 0)
            throw new InvalidOperationException("El producto es obligatorio.");
        if (ProductoVarianteId is <= 0)
            throw new InvalidOperationException("La variante debe ser válida cuando se especifica.");
        if (Cantidad <= 0 || CantidadVendidaSnapshot <= 0 || Cantidad > CantidadVendidaSnapshot)
            throw new InvalidOperationException("La cantidad devuelta debe mantenerse dentro de la cantidad vendida.");
        if (PrecioUnitarioSnapshot < 0)
            throw new InvalidOperationException("El precio unitario snapshot no puede ser negativo.");
        if (!Enum.IsDefined(typeof(TipoResolucionDevolucionCliente), Resolucion))
            throw new InvalidOperationException("La resolución de la devolución no es válida.");
    }
}
