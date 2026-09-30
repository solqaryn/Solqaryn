using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.ValueObjects;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Documento comercial de pedido. N3.2 lo mantiene independiente de la Venta legacy
/// y sin efectos de inventario, Kardex, facturación o finanzas.
/// </summary>
public class PedidoVenta : ConfirmableEntity
{
    private readonly List<PedidoVentaDetalle> _detalles = new();

    public int? CotizacionId { get; private set; }
    public Cotizacion? Cotizacion { get; private set; }

    public int ClienteId { get; private set; }
    public Cliente Cliente { get; private set; } = null!;
    public string ClienteNombreSnapshot { get; private set; } = string.Empty;
    public string? ClienteDocumentoSnapshot { get; private set; }
    public string? Observaciones { get; private set; }

    public EstadoPedidoVenta Estado { get; private set; } = EstadoPedidoVenta.Borrador;

    // N3.2.B — coordenadas durables de idempotencia. La unicidad física y la
    // resolución transaccional de replay/conflicto pertenecen a N3.2.C/D.
    public string? IdempotencyKey { get; private set; }
    public string? IdempotencyFingerprint { get; private set; }

    public IReadOnlyCollection<PedidoVentaDetalle> Detalles => _detalles.AsReadOnly();

    public bool EsEditable => Estado == EstadoPedidoVenta.Borrador;
    public bool EstaConfirmado => Estado == EstadoPedidoVenta.Confirmado;
    public bool EstaAnulado => Estado == EstadoPedidoVenta.Anulado;
    public decimal Total => _detalles.Sum(x => x.Total);

    public static PedidoVenta CrearDesdeCotizacion(Cotizacion cotizacion)
    {
        ArgumentNullException.ThrowIfNull(cotizacion);

        if (cotizacion.Id <= 0)
            throw new InvalidOperationException("La cotización de origen debe estar persistida.");
        if (cotizacion.Estado != EstadoCotizacion.Aceptada)
            throw new InvalidOperationException("Solo una cotización aceptada puede originar un pedido.");

        cotizacion.ValidarDocumento();

        var pedido = new PedidoVenta
        {
            CotizacionId = cotizacion.Id,
            Cotizacion = cotizacion,
            ClienteId = cotizacion.ClienteId,
            Cliente = cotizacion.Cliente,
            ClienteNombreSnapshot = cotizacion.ClienteNombreSnapshot,
            ClienteDocumentoSnapshot = cotizacion.ClienteDocumentoSnapshot,
            Observaciones = cotizacion.Observaciones
        };

        foreach (var detalle in cotizacion.Detalles)
            pedido._detalles.Add(PedidoVentaDetalle.CrearDesdeCotizacion(detalle));

        pedido.ValidarDocumento();
        return pedido;
    }

    public void EstablecerIdempotencia(string key, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Trim().Length > 128)
            throw new ArgumentException("La clave de idempotencia es obligatoria y no puede superar 128 caracteres.", nameof(key));
        if (string.IsNullOrWhiteSpace(fingerprint))
            throw new ArgumentException("El fingerprint de idempotencia debe ser SHA-256 hexadecimal.", nameof(fingerprint));

        var keyNormalizada = key.Trim();
        var fingerprintNormalizado = fingerprint.Trim().ToLowerInvariant();
        if (fingerprintNormalizado.Length != 64 || !EsHexadecimal(fingerprintNormalizado))
            throw new ArgumentException("El fingerprint de idempotencia debe ser SHA-256 hexadecimal.", nameof(fingerprint));

        if (IdempotencyKey is not null && !string.Equals(IdempotencyKey, keyNormalizada, StringComparison.Ordinal))
            throw new InvalidOperationException("La clave de idempotencia de un pedido no puede sustituirse.");
        if (IdempotencyFingerprint is not null && !string.Equals(IdempotencyFingerprint, fingerprintNormalizado, StringComparison.Ordinal))
            throw new InvalidOperationException("El fingerprint de idempotencia de un pedido no puede sustituirse.");

        IdempotencyKey = keyNormalizada;
        IdempotencyFingerprint = fingerprintNormalizado;
    }

    public ReservaAutomaticaPedido PrepararReservaAutomatica(IEnumerable<AsignacionReservaAutomatica> asignaciones)
    {
        if (Id <= 0)
            throw new InvalidOperationException("El pedido debe estar persistido antes de preparar su reserva automática.");
        if (Estado != EstadoPedidoVenta.Borrador)
            throw new InvalidOperationException("Solo un pedido en borrador puede preparar una reserva automática.");

        ValidarDocumento();
        return ReservaAutomaticaPedido.Crear(Id, _detalles, asignaciones);
    }

    public void ActualizarObservaciones(string? observaciones)
    {
        AsegurarEditable();
        Observaciones = string.IsNullOrWhiteSpace(observaciones)
            ? null
            : observaciones.Trim();
    }

    public void Confirmar(int usuarioId, string nombreUsuario, DateTime fechaUtc)
    {
        if (Estado != EstadoPedidoVenta.Borrador)
            throw new InvalidOperationException("Solo un pedido en borrador puede confirmarse.");

        ValidarUsuario(usuarioId);
        var nombreValidado = ValidarNombreUsuario(nombreUsuario, nameof(nombreUsuario));
        ValidarDocumento();
        var fechaValidada = AsegurarUtc(fechaUtc, nameof(fechaUtc));

        Estado = EstadoPedidoVenta.Confirmado;
        FechaConfirmacion = fechaValidada;
        ConfirmadoPorUsuarioId = usuarioId;
        ConfirmadoPorNombreUsuario = nombreValidado;
    }

    public void Anular(int usuarioId, string nombreUsuario, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoPedidoVenta.Confirmado)
            throw new InvalidOperationException("Solo un pedido confirmado puede anularse.");

        ValidarUsuario(usuarioId);
        var nombreValidado = ValidarNombreUsuario(nombreUsuario, nameof(nombreUsuario));
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));
        var fechaValidada = AsegurarUtc(fechaUtc, nameof(fechaUtc));

        Estado = EstadoPedidoVenta.Anulado;
        FechaAnulacion = fechaValidada;
        AnuladoPorUsuarioId = usuarioId;
        AnuladoPorNombreUsuario = nombreValidado;
        MotivoAnulacion = motivo.Trim();
    }

    public void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo un pedido en borrador puede modificarse.");
    }

    public void ValidarDocumento()
    {
        if (ClienteId <= 0)
            throw new InvalidOperationException("El cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(ClienteNombreSnapshot))
            throw new InvalidOperationException("El snapshot del cliente es obligatorio.");
        if (CotizacionId is <= 0)
            throw new InvalidOperationException("La cotización de origen debe ser válida cuando se especifica.");
        if (_detalles.Count == 0)
            throw new InvalidOperationException("El pedido debe contener al menos un detalle.");

        var tieneKey = !string.IsNullOrWhiteSpace(IdempotencyKey);
        var tieneFingerprint = !string.IsNullOrWhiteSpace(IdempotencyFingerprint);
        if (tieneKey != tieneFingerprint)
            throw new InvalidOperationException("La idempotencia del pedido debe persistir clave y fingerprint de forma atómica.");
        if (tieneKey && (IdempotencyKey!.Length > 128 || IdempotencyFingerprint!.Length != 64 || !EsHexadecimal(IdempotencyFingerprint)))
            throw new InvalidOperationException("La idempotencia persistida del pedido no cumple el contrato durable.");

        foreach (var detalle in _detalles)
            detalle.Validar();
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

public class PedidoVentaDetalle : AuditableEntity
{
    public int PedidoVentaId { get; private set; }
    public PedidoVenta PedidoVenta { get; private set; } = null!;

    public int ProductoId { get; private set; }
    public Producto Producto { get; private set; } = null!;

    public int? ProductoVarianteId { get; private set; }
    public ProductoVariante? ProductoVariante { get; private set; }

    public string? ProductoSkuSnapshot { get; private set; }
    public string? ProductoNombreSnapshot { get; private set; }
    public string? ProductoMarcaSnapshot { get; private set; }
    public string? ProductoModeloSnapshot { get; private set; }
    public string? ProductoColorSnapshot { get; private set; }
    public string? ProductoTallaSnapshot { get; private set; }

    public decimal Cantidad { get; private set; }
    public decimal PrecioUnitario { get; private set; }
    public decimal Total => Cantidad * PrecioUnitario;

    internal static PedidoVentaDetalle CrearDesdeCotizacion(CotizacionDetalle detalle)
    {
        ArgumentNullException.ThrowIfNull(detalle);
        detalle.Validar();

        return new PedidoVentaDetalle
        {
            ProductoId = detalle.ProductoId,
            Producto = detalle.Producto,
            ProductoVarianteId = detalle.ProductoVarianteId,
            ProductoVariante = detalle.ProductoVariante,
            ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
            ProductoNombreSnapshot = detalle.ProductoNombreSnapshot,
            ProductoMarcaSnapshot = detalle.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = detalle.ProductoModeloSnapshot,
            ProductoColorSnapshot = detalle.ProductoColorSnapshot,
            ProductoTallaSnapshot = detalle.ProductoTallaSnapshot,
            Cantidad = detalle.Cantidad,
            PrecioUnitario = detalle.PrecioUnitario
        };
    }

    public void Validar()
    {
        if (ProductoId <= 0)
            throw new InvalidOperationException("El producto es obligatorio.");
        if (ProductoVarianteId is <= 0)
            throw new InvalidOperationException("La variante debe ser válida cuando se especifica.");
        if (Cantidad <= 0)
            throw new InvalidOperationException("La cantidad debe ser mayor que cero.");
        if (PrecioUnitario < 0)
            throw new InvalidOperationException("El precio unitario no puede ser negativo.");
    }
}
