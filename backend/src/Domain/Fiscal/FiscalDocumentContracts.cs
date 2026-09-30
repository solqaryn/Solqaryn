namespace Solqaryn.Domain.Fiscal;

/// <summary>
/// Identifica el perfil fiscal aplicable a un documento sin convertir ninguna legislación,
/// autoridad tributaria, proveedor ni formato nacional en una regla universal del dominio.
/// Las reglas específicas se resuelven detrás del proveedor/adaptador seleccionado.
/// </summary>
public sealed class PerfilFiscalDocumento
{
    private const int CodigoMaxLength = 80;

    public PerfilFiscalDocumento(
        int empresaId,
        int? sucursalId,
        string jurisdiccion,
        string proveedor,
        string tipoDocumento)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId), "La empresa es obligatoria.");
        if (sucursalId.HasValue && sucursalId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(sucursalId), "La sucursal debe ser positiva cuando se informa.");

        EmpresaId = empresaId;
        SucursalId = sucursalId;
        Jurisdiccion = NormalizarCodigo(jurisdiccion, nameof(jurisdiccion));
        Proveedor = NormalizarCodigo(proveedor, nameof(proveedor));
        TipoDocumento = NormalizarCodigo(tipoDocumento, nameof(tipoDocumento));
    }

    public int EmpresaId { get; }
    public int? SucursalId { get; }
    public string Jurisdiccion { get; }
    public string Proveedor { get; }
    public string TipoDocumento { get; }

    private static string NormalizarCodigo(string value, string parametro)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El código es obligatorio.", parametro);

        var normalizado = value.Trim().ToUpperInvariant();
        if (normalizado.Length > CodigoMaxLength)
            throw new ArgumentException($"El código no puede exceder {CodigoMaxLength} caracteres.", parametro);

        return normalizado;
    }
}

/// <summary>
/// Solicitud provider-neutral. La clave de idempotencia y el hash del snapshot permiten que
/// application/persistence impidan emisiones duplicadas o mutaciones silenciosas del documento.
/// El contenido/serialización legal concreta no pertenece a este contrato común.
/// </summary>
public sealed class SolicitudEmisionFiscal
{
    private const int ClaveIdempotenciaMaxLength = 128;
    private const int HashSnapshotMaxLength = 160;

    public SolicitudEmisionFiscal(
        PerfilFiscalDocumento perfil,
        int facturaId,
        string claveIdempotencia,
        string hashSnapshot)
    {
        Perfil = perfil ?? throw new ArgumentNullException(nameof(perfil));
        if (facturaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(facturaId), "La factura es obligatoria.");

        FacturaId = facturaId;
        ClaveIdempotencia = NormalizarRequerido(claveIdempotencia, ClaveIdempotenciaMaxLength, nameof(claveIdempotencia));
        HashSnapshot = NormalizarRequerido(hashSnapshot, HashSnapshotMaxLength, nameof(hashSnapshot));
    }

    public PerfilFiscalDocumento Perfil { get; }
    public int FacturaId { get; }
    public string ClaveIdempotencia { get; }
    public string HashSnapshot { get; }

    private static string NormalizarRequerido(string value, int maxLength, string parametro)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El valor es obligatorio.", parametro);

        var normalizado = value.Trim();
        if (normalizado.Length > maxLength)
            throw new ArgumentException($"El valor no puede exceder {maxLength} caracteres.", parametro);

        return normalizado;
    }
}

/// <summary>
/// Estado técnico provider-neutral. No equivale por sí mismo a una conclusión jurídica universal.
/// </summary>
public enum EstadoResultadoFiscal
{
    Pendiente = 1,
    Confirmado = 2,
    Rechazado = 3
}

/// <summary>
/// Resultado normalizado de un adaptador fiscal. Referencia/códigos son opacos para el core y
/// conservan su semántica específica dentro del proveedor seleccionado.
/// </summary>
public sealed class ResultadoEmisionFiscal
{
    private const int ReferenciaMaxLength = 200;
    private const int CodigoMaxLength = 120;
    private const int MensajeMaxLength = 1000;

    public ResultadoEmisionFiscal(
        EstadoResultadoFiscal estado,
        string? referenciaExterna = null,
        string? codigoProveedor = null,
        string? mensaje = null,
        bool esTransitorio = false)
    {
        if (!Enum.IsDefined(typeof(EstadoResultadoFiscal), estado))
            throw new ArgumentOutOfRangeException(nameof(estado));

        Estado = estado;
        ReferenciaExterna = NormalizarOpcional(referenciaExterna, ReferenciaMaxLength, nameof(referenciaExterna));
        CodigoProveedor = NormalizarOpcional(codigoProveedor, CodigoMaxLength, nameof(codigoProveedor));
        Mensaje = NormalizarOpcional(mensaje, MensajeMaxLength, nameof(mensaje));
        EsTransitorio = esTransitorio;
    }

    public EstadoResultadoFiscal Estado { get; }
    public string? ReferenciaExterna { get; }
    public string? CodigoProveedor { get; }
    public string? Mensaje { get; }
    public bool EsTransitorio { get; }

    private static string? NormalizarOpcional(string? value, int maxLength, string parametro)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalizado = value.Trim();
        if (normalizado.Length > maxLength)
            throw new ArgumentException($"El valor no puede exceder {maxLength} caracteres.", parametro);

        return normalizado;
    }
}
