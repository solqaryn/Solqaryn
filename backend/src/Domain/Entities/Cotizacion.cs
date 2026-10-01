using Solqaryn.Domain.Common;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Domain.Entities;

public class Cotizacion : AuditableEntity
{
    public EstadoCotizacion Estado { get; private set; } = EstadoCotizacion.Borrador;

    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public string ClienteNombreSnapshot { get; set; } = string.Empty;
    public string? ClienteDocumentoSnapshot { get; set; }
    public string? Observaciones { get; set; }

    public DateTime? FechaEnvioUtc { get; private set; }
    public int? EnviadaPorUsuarioId { get; private set; }
    public DateTime? FechaAceptacionUtc { get; private set; }
    public int? AceptadaPorUsuarioId { get; private set; }
    public DateTime? FechaRechazoUtc { get; private set; }
    public int? RechazadaPorUsuarioId { get; private set; }
    public string? MotivoRechazo { get; private set; }
    public DateTime? FechaConversionUtc { get; private set; }
    public int? ConvertidaPorUsuarioId { get; private set; }

    public ICollection<CotizacionDetalle> Detalles { get; set; } = new List<CotizacionDetalle>();

    public bool EsEditable => Estado == EstadoCotizacion.Borrador;
    public decimal Total => Detalles.Sum(x => x.Total);

    public void AsegurarEditable()
    {
        if (!EsEditable)
            throw new InvalidOperationException("Solo una cotización en borrador puede modificarse.");
    }

    public void Enviar(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoCotizacion.Borrador)
            throw new InvalidOperationException("Solo una cotización en borrador puede enviarse.");

        ValidarUsuario(usuarioId);
        ValidarDocumento();
        var fechaValidada = AsegurarUtc(fechaUtc, nameof(fechaUtc));

        Estado = EstadoCotizacion.Enviada;
        FechaEnvioUtc = fechaValidada;
        EnviadaPorUsuarioId = usuarioId;
    }

    public void Aceptar(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoCotizacion.Enviada)
            throw new InvalidOperationException("Solo una cotización enviada puede aceptarse.");

        ValidarUsuario(usuarioId);
        var fechaValidada = AsegurarUtc(fechaUtc, nameof(fechaUtc));

        Estado = EstadoCotizacion.Aceptada;
        FechaAceptacionUtc = fechaValidada;
        AceptadaPorUsuarioId = usuarioId;
    }

    public void Rechazar(int usuarioId, string motivo, DateTime fechaUtc)
    {
        if (Estado != EstadoCotizacion.Enviada)
            throw new InvalidOperationException("Solo una cotización enviada puede rechazarse.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de rechazo es obligatorio.", nameof(motivo));

        ValidarUsuario(usuarioId);
        var fechaValidada = AsegurarUtc(fechaUtc, nameof(fechaUtc));
        var motivoValidado = motivo.Trim();

        Estado = EstadoCotizacion.Rechazada;
        FechaRechazoUtc = fechaValidada;
        RechazadaPorUsuarioId = usuarioId;
        MotivoRechazo = motivoValidado;
    }

    public void Convertir(int usuarioId, DateTime fechaUtc)
    {
        if (Estado != EstadoCotizacion.Aceptada)
            throw new InvalidOperationException("Solo una cotización aceptada puede convertirse.");

        ValidarUsuario(usuarioId);
        var fechaValidada = AsegurarUtc(fechaUtc, nameof(fechaUtc));

        Estado = EstadoCotizacion.Convertida;
        FechaConversionUtc = fechaValidada;
        ConvertidaPorUsuarioId = usuarioId;
    }

    public Cotizacion DuplicarComoBorrador()
    {
        var copia = new Cotizacion
        {
            ClienteId = ClienteId,
            ClienteNombreSnapshot = ClienteNombreSnapshot,
            ClienteDocumentoSnapshot = ClienteDocumentoSnapshot,
            Observaciones = Observaciones
        };

        foreach (var detalle in Detalles)
        {
            var detalleCopia = new CotizacionDetalle
            {
                ProductoId = detalle.ProductoId,
                ProductoVarianteId = detalle.ProductoVarianteId,
                ProductoSkuSnapshot = detalle.ProductoSkuSnapshot,
                ProductoNombreSnapshot = detalle.ProductoNombreSnapshot,
                ProductoMarcaSnapshot = detalle.ProductoMarcaSnapshot,
                ProductoModeloSnapshot = detalle.ProductoModeloSnapshot,
                ProductoColorSnapshot = detalle.ProductoColorSnapshot,
                ProductoTallaSnapshot = detalle.ProductoTallaSnapshot
            };
            detalleCopia.EstablecerValores(detalle.Cantidad, detalle.PrecioUnitario);
            copia.Detalles.Add(detalleCopia);
        }

        return copia;
    }

    public void ValidarDocumento()
    {
        if (ClienteId <= 0)
            throw new InvalidOperationException("El cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(ClienteNombreSnapshot))
            throw new InvalidOperationException("El snapshot del cliente es obligatorio.");
        if (Detalles.Count == 0)
            throw new InvalidOperationException("La cotización debe contener al menos un detalle.");

        foreach (var detalle in Detalles)
            detalle.Validar();
    }

    private static void ValidarUsuario(int usuarioId)
    {
        if (usuarioId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "El usuario debe ser válido.");
    }

    private static DateTime AsegurarUtc(DateTime fecha, string parametro)
    {
        if (fecha.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", parametro);
        return fecha;
    }
}
