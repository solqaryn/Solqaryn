using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// N3.10.B — configuración y estado auditable del crédito comercial de un cliente.
/// No calcula consumo/disponible ni dispara bloqueos/alertas por una fórmula implícita:
/// esas políticas requieren autoridad explícita en capas posteriores.
/// </summary>
public class CreditoCliente : AuditableEntity
{
    private CreditoCliente() { }

    public int ClienteId { get; private set; }
    public Cliente Cliente { get; private set; } = null!;
    public string Moneda { get; private set; } = string.Empty;
    public decimal LimiteCredito { get; private set; }
    public int DiasCredito { get; private set; }
    public decimal? UmbralAlertaPorcentaje { get; private set; }
    public bool BloqueadoAutomaticamente { get; private set; }
    public string? MotivoBloqueo { get; private set; }
    public DateTime? BloqueadoUtc { get; private set; }
    public decimal? MontoExcepcion { get; private set; }
    public DateTime? ExcepcionVigenteHastaUtc { get; private set; }
    public string? ExcepcionAutorizadaPor { get; private set; }
    public DateTime? ExcepcionAutorizadaUtc { get; private set; }

    public static CreditoCliente Crear(
        Cliente cliente,
        string moneda,
        decimal limiteCredito,
        int diasCredito,
        decimal? umbralAlertaPorcentaje = null)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        if (cliente.Id <= 0)
            throw new InvalidOperationException("El cliente debe estar persistido para configurar crédito.");

        var credito = new CreditoCliente { ClienteId = cliente.Id, Cliente = cliente };
        credito.ActualizarPolitica(moneda, limiteCredito, diasCredito, umbralAlertaPorcentaje);
        return credito;
    }

    public void ActualizarPolitica(
        string moneda,
        decimal limiteCredito,
        int diasCredito,
        decimal? umbralAlertaPorcentaje)
    {
        Moneda = NormalizarMoneda(moneda);
        if (limiteCredito < 0m)
            throw new ArgumentOutOfRangeException(nameof(limiteCredito));
        if (diasCredito < 0)
            throw new ArgumentOutOfRangeException(nameof(diasCredito));
        if (umbralAlertaPorcentaje is <= 0m or > 100m)
            throw new ArgumentOutOfRangeException(nameof(umbralAlertaPorcentaje));

        LimiteCredito = decimal.Round(limiteCredito, 4, MidpointRounding.AwayFromZero);
        DiasCredito = diasCredito;
        UmbralAlertaPorcentaje = umbralAlertaPorcentaje is null
            ? null
            : decimal.Round(umbralAlertaPorcentaje.Value, 4, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Registra el resultado de una evaluación automática externa ya autorizada.
    /// No inventa aquí la fórmula que determina cuándo bloquear.
    /// </summary>
    public void AplicarBloqueoAutomatico(string motivo, DateTime ahoraUtc)
    {
        ValidarUtc(ahoraUtc, nameof(ahoraUtc));
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El bloqueo automático exige un motivo auditable.", nameof(motivo));

        BloqueadoAutomaticamente = true;
        MotivoBloqueo = motivo.Trim();
        BloqueadoUtc = ahoraUtc;
    }

    public void LiberarBloqueoAutomatico(DateTime ahoraUtc)
    {
        ValidarUtc(ahoraUtc, nameof(ahoraUtc));
        BloqueadoAutomaticamente = false;
        MotivoBloqueo = null;
        BloqueadoUtc = null;
    }

    public void AutorizarExcepcion(decimal monto, DateTime vigenteHastaUtc, string autorizadoPor, DateTime ahoraUtc)
    {
        if (monto <= 0m)
            throw new ArgumentOutOfRangeException(nameof(monto));
        ValidarUtc(ahoraUtc, nameof(ahoraUtc));
        ValidarUtc(vigenteHastaUtc, nameof(vigenteHastaUtc));
        if (vigenteHastaUtc <= ahoraUtc)
            throw new ArgumentOutOfRangeException(nameof(vigenteHastaUtc));
        if (string.IsNullOrWhiteSpace(autorizadoPor))
            throw new ArgumentException("La autorización excepcional exige actor responsable.", nameof(autorizadoPor));

        MontoExcepcion = decimal.Round(monto, 4, MidpointRounding.AwayFromZero);
        ExcepcionVigenteHastaUtc = vigenteHastaUtc;
        ExcepcionAutorizadaPor = autorizadoPor.Trim();
        ExcepcionAutorizadaUtc = ahoraUtc;
    }

    public bool TieneExcepcionVigente(DateTime ahoraUtc)
    {
        ValidarUtc(ahoraUtc, nameof(ahoraUtc));
        return MontoExcepcion is > 0m && ExcepcionVigenteHastaUtc is not null && ExcepcionVigenteHastaUtc > ahoraUtc;
    }

    public void RevocarExcepcion()
    {
        MontoExcepcion = null;
        ExcepcionVigenteHastaUtc = null;
        ExcepcionAutorizadaPor = null;
        ExcepcionAutorizadaUtc = null;
    }

    private static string NormalizarMoneda(string moneda)
    {
        if (string.IsNullOrWhiteSpace(moneda) || moneda.Trim().Length != 3)
            throw new ArgumentException("La moneda debe usar un código de tres caracteres.", nameof(moneda));
        return moneda.Trim().ToUpperInvariant();
    }

    private static void ValidarUtc(DateTime valor, string parametro)
    {
        if (valor.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", parametro);
    }
}
