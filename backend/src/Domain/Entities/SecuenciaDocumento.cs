using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Autoridad de dominio para el contador de numeración documental dentro de un scope
/// Empresa + Sucursal opcional + TipoDocumento. La persistencia y la serialización
/// concurrente pertenecen a N6.6.C; la reserva/emisión transaccional pertenece a N6.6.D.
/// </summary>
public sealed class SecuenciaDocumento : AuditableEntity
{
    private const int TipoDocumentoMaxLength = 80;
    private const int PrefijoMaxLength = 40;

    private SecuenciaDocumento()
    {
    }

    public SecuenciaDocumento(
        int empresaId,
        int? sucursalId,
        string tipoDocumento,
        string? prefijo = null,
        int longitudNumero = 6,
        long valorInicial = 0)
    {
        ValidarScope(empresaId, sucursalId, tipoDocumento);

        if (valorInicial < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valorInicial), "El valor inicial no puede ser negativo.");
        }

        EmpresaId = empresaId;
        SucursalId = sucursalId;
        TipoDocumento = NormalizarTipoDocumento(tipoDocumento);
        UltimoValor = valorInicial;
        ActualizarFormato(prefijo, longitudNumero);
    }

    public int EmpresaId { get; private set; }

    /// <summary>
    /// Es nulo cuando el tipo documental se numera a nivel empresa.
    /// Cuando se informa, debe pertenecer al tenant validado por la capa de aplicación.
    /// </summary>
    public int? SucursalId { get; private set; }

    /// <summary>
    /// Identificador semántico del tipo documental. No representa formato fiscal ni rango SAR/CAI.
    /// </summary>
    public string TipoDocumento { get; private set; } = string.Empty;

    /// <summary>
    /// Último valor secuencial confirmado. El mecanismo concurrency-safe que lo incrementa
    /// se implementa en persistencia/aplicación, no mediante MAX()+1 ni memoria local.
    /// </summary>
    public long UltimoValor { get; private set; }

    /// <summary>
    /// Componente visible independiente del contador para evitar acoplar formato e identidad secuencial.
    /// </summary>
    public string Prefijo { get; private set; } = string.Empty;

    public int LongitudNumero { get; private set; } = 6;

    public bool Activa { get; private set; } = true;

    public void ActualizarFormato(string? prefijo, int longitudNumero)
    {
        if (longitudNumero is < 1 or > 18)
        {
            throw new ArgumentOutOfRangeException(nameof(longitudNumero), "La longitud numérica debe estar entre 1 y 18.");
        }

        var prefijoNormalizado = prefijo?.Trim() ?? string.Empty;
        if (prefijoNormalizado.Length > PrefijoMaxLength)
        {
            throw new ArgumentException($"El prefijo no puede exceder {PrefijoMaxLength} caracteres.", nameof(prefijo));
        }

        Prefijo = prefijoNormalizado;
        LongitudNumero = longitudNumero;
    }

    /// <summary>
    /// Registra en el agregado un valor ya reservado de forma durable por la capa responsable.
    /// No implementa locking ni persistencia; únicamente protege la invariante monotónica del dominio.
    /// </summary>
    public void RegistrarValorReservado(long valor)
    {
        if (!Activa)
        {
            throw new InvalidOperationException("No se puede reservar sobre una secuencia inactiva.");
        }

        if (UltimoValor == long.MaxValue)
        {
            throw new InvalidOperationException("La secuencia alcanzó el máximo valor soportado.");
        }

        var esperado = UltimoValor + 1;
        if (valor != esperado)
        {
            throw new InvalidOperationException($"El siguiente valor esperado es {esperado}.");
        }

        UltimoValor = valor;
    }

    public string Formatear(long valor)
    {
        if (valor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valor), "El valor a formatear debe ser positivo.");
        }

        return $"{Prefijo}{valor.ToString($"D{LongitudNumero}")}";
    }

    public void Activar() => Activa = true;

    public void Desactivar() => Activa = false;

    private static void ValidarScope(int empresaId, int? sucursalId, string tipoDocumento)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId), "La empresa es obligatoria.");
        }

        if (sucursalId.HasValue && sucursalId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sucursalId), "La sucursal debe ser positiva cuando se informa.");
        }

        if (string.IsNullOrWhiteSpace(tipoDocumento))
        {
            throw new ArgumentException("El tipo documental es obligatorio.", nameof(tipoDocumento));
        }
    }

    private static string NormalizarTipoDocumento(string tipoDocumento)
    {
        var normalizado = tipoDocumento.Trim().ToUpperInvariant();
        if (normalizado.Length > TipoDocumentoMaxLength)
        {
            throw new ArgumentException($"El tipo documental no puede exceder {TipoDocumentoMaxLength} caracteres.", nameof(tipoDocumento));
        }

        return normalizado;
    }
}
