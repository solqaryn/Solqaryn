using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Intención durable de entregar un correo empresarial para un tenant.
/// La persistencia, el claim atómico y el worker pertenecen a las fases
/// posteriores; este agregado fija idempotencia, estados y tracking sin
/// almacenar secretos de SMTP/proveedor.
/// </summary>
public sealed class EmailEmpresarial : BaseEntity
{
    public const int LongitudMaximaDestinatario = 320;
    public const int LongitudMaximaAsunto = 998;
    public const int LongitudMaximaCuerpo = 65535;
    public const int LongitudMaximaCodigoPlantilla = 120;
    public const int LongitudMaximaVariables = 65535;
    public const int LongitudMaximaClaveIdempotencia = 200;
    public const int LongitudMaximaCorrelationId = 160;
    public const int LongitudMaximaProviderMessageId = 320;
    public const int LongitudMaximaError = 2000;
    public const int LongitudMaximaCodigoRebote = 160;

    private EmailEmpresarial()
    {
    }

    public Guid MensajeId { get; private set; }
    public int EmpresaId { get; private set; }
    public string Destinatario { get; private set; } = string.Empty;
    public string? Asunto { get; private set; }
    public string? CuerpoHtml { get; private set; }
    public string? CuerpoTexto { get; private set; }
    public string? PlantillaCodigo { get; private set; }
    public int? PlantillaVersion { get; private set; }
    public string? VariablesJson { get; private set; }
    public string ClaveIdempotencia { get; private set; } = string.Empty;
    public string? CorrelationId { get; private set; }

    public EstadoEntregaEmail Estado { get; private set; } = EstadoEntregaEmail.Pendiente;
    public int Intentos { get; private set; }
    public DateTime CreadoEnUtc { get; private set; }
    public DateTime DisponibleDesdeUtc { get; private set; }
    public DateTime? ProcesandoDesdeUtc { get; private set; }
    public DateTime? AceptadoProveedorEnUtc { get; private set; }
    public DateTime? EntregadoEnUtc { get; private set; }
    public DateTime? RebotadoEnUtc { get; private set; }
    public DateTime? UltimoIntentoEnUtc { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public TipoReboteEmail? TipoRebote { get; private set; }
    public string? CodigoRebote { get; private set; }
    public string? UltimoError { get; private set; }

    public bool EsDespachoTerminal => Estado is EstadoEntregaEmail.AceptadoProveedor
        or EstadoEntregaEmail.Entregado
        or EstadoEntregaEmail.Rebotado
        or EstadoEntregaEmail.FallidoFinal;

    public bool EsEntregaTerminal => Estado is EstadoEntregaEmail.Entregado
        or EstadoEntregaEmail.Rebotado
        or EstadoEntregaEmail.FallidoFinal;

    public static EmailEmpresarial CrearDirecto(
        int empresaId,
        string destinatario,
        string asunto,
        string cuerpoHtml,
        string claveIdempotencia,
        string? cuerpoTexto = null,
        string? correlationId = null,
        DateTime? creadoEnUtc = null)
    {
        var mensaje = CrearBase(
            empresaId,
            destinatario,
            claveIdempotencia,
            correlationId,
            creadoEnUtc ?? DateTime.UtcNow);

        mensaje.Asunto = NormalizarRequerido(asunto, LongitudMaximaAsunto, nameof(asunto));
        mensaje.CuerpoHtml = NormalizarRequerido(cuerpoHtml, LongitudMaximaCuerpo, nameof(cuerpoHtml));
        mensaje.CuerpoTexto = NormalizarOpcional(cuerpoTexto, LongitudMaximaCuerpo, nameof(cuerpoTexto));
        return mensaje;
    }

    public static EmailEmpresarial CrearDesdePlantilla(
        int empresaId,
        string destinatario,
        string plantillaCodigo,
        int plantillaVersion,
        string variablesJson,
        string claveIdempotencia,
        string? correlationId = null,
        DateTime? creadoEnUtc = null)
    {
        if (plantillaVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(plantillaVersion), "La versión de plantilla debe ser mayor que cero.");

        var mensaje = CrearBase(
            empresaId,
            destinatario,
            claveIdempotencia,
            correlationId,
            creadoEnUtc ?? DateTime.UtcNow);

        mensaje.PlantillaCodigo = NormalizarRequerido(
            plantillaCodigo,
            LongitudMaximaCodigoPlantilla,
            nameof(plantillaCodigo));
        mensaje.PlantillaVersion = plantillaVersion;
        mensaje.VariablesJson = NormalizarRequerido(
            variablesJson,
            LongitudMaximaVariables,
            nameof(variablesJson));
        return mensaje;
    }

    public void MarcarProcesando(DateTime ahoraUtc)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));

        if (Estado is not (EstadoEntregaEmail.Pendiente or EstadoEntregaEmail.ReintentoPendiente))
            throw new InvalidOperationException($"El correo en estado {Estado} no puede pasar a procesamiento.");

        if (ahoraUtc < DisponibleDesdeUtc)
            throw new InvalidOperationException("El correo todavía no está disponible para reintento.");

        Estado = EstadoEntregaEmail.Procesando;
        Intentos++;
        ProcesandoDesdeUtc = ahoraUtc;
        UltimoIntentoEnUtc = ahoraUtc;
        UltimoError = null;
        FechaActualizacion = ahoraUtc;
    }

    public void MarcarAceptadoProveedor(string providerMessageId, DateTime ahoraUtc)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));

        if (Estado != EstadoEntregaEmail.Procesando)
            throw new InvalidOperationException("Solo un correo en procesamiento puede marcarse como aceptado por el proveedor.");

        ProviderMessageId = NormalizarRequerido(
            providerMessageId,
            LongitudMaximaProviderMessageId,
            nameof(providerMessageId));
        Estado = EstadoEntregaEmail.AceptadoProveedor;
        AceptadoProveedorEnUtc = ahoraUtc;
        ProcesandoDesdeUtc = null;
        UltimoError = null;
        FechaActualizacion = ahoraUtc;
    }

    public void MarcarEntregado(DateTime ahoraUtc)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));

        if (Estado != EstadoEntregaEmail.AceptadoProveedor)
            throw new InvalidOperationException("Solo un correo aceptado por el proveedor puede marcarse como entregado.");

        Estado = EstadoEntregaEmail.Entregado;
        EntregadoEnUtc = ahoraUtc;
        FechaActualizacion = ahoraUtc;
    }

    public void RegistrarRebote(
        TipoReboteEmail tipo,
        string? codigo,
        string motivo,
        DateTime ahoraUtc)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));

        if (Estado != EstadoEntregaEmail.AceptadoProveedor)
            throw new InvalidOperationException("Solo un correo aceptado por el proveedor puede registrar un rebote.");

        if (!Enum.IsDefined(tipo))
            throw new ArgumentOutOfRangeException(nameof(tipo));

        TipoRebote = tipo;
        CodigoRebote = NormalizarOpcional(codigo, LongitudMaximaCodigoRebote, nameof(codigo));
        UltimoError = NormalizarRequerido(motivo, LongitudMaximaError, nameof(motivo));
        RebotadoEnUtc = ahoraUtc;
        Estado = EstadoEntregaEmail.Rebotado;
        FechaActualizacion = ahoraUtc;
    }

    public void RegistrarFallo(
        string error,
        DateTime ahoraUtc,
        DateTime disponibleDesdeUtc,
        int maximoIntentos)
    {
        ahoraUtc = ExigirUtc(ahoraUtc, nameof(ahoraUtc));
        disponibleDesdeUtc = ExigirUtc(disponibleDesdeUtc, nameof(disponibleDesdeUtc));

        if (Estado != EstadoEntregaEmail.Procesando)
            throw new InvalidOperationException("Solo un correo en procesamiento puede registrar un fallo.");

        if (maximoIntentos <= 0)
            throw new ArgumentOutOfRangeException(nameof(maximoIntentos), "El máximo de intentos debe ser mayor que cero.");

        var agotado = Intentos >= maximoIntentos;
        if (!agotado && disponibleDesdeUtc < ahoraUtc)
            throw new ArgumentOutOfRangeException(
                nameof(disponibleDesdeUtc),
                "La próxima disponibilidad no puede quedar en el pasado.");

        UltimoError = NormalizarRequerido(error, LongitudMaximaError, nameof(error));
        ProcesandoDesdeUtc = null;
        FechaActualizacion = ahoraUtc;

        if (agotado)
        {
            Estado = EstadoEntregaEmail.FallidoFinal;
            DisponibleDesdeUtc = ahoraUtc;
            return;
        }

        Estado = EstadoEntregaEmail.ReintentoPendiente;
        DisponibleDesdeUtc = disponibleDesdeUtc;
    }

    private static EmailEmpresarial CrearBase(
        int empresaId,
        string destinatario,
        string claveIdempotencia,
        string? correlationId,
        DateTime creadoEnUtc)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId), "La empresa/tenant del correo es obligatoria.");

        creadoEnUtc = ExigirUtc(creadoEnUtc, nameof(creadoEnUtc));
        return new EmailEmpresarial
        {
            EmpresaId = empresaId,
            MensajeId = Guid.NewGuid(),
            Destinatario = NormalizarRequerido(destinatario, LongitudMaximaDestinatario, nameof(destinatario)),
            ClaveIdempotencia = NormalizarRequerido(
                claveIdempotencia,
                LongitudMaximaClaveIdempotencia,
                nameof(claveIdempotencia)),
            CorrelationId = NormalizarOpcional(correlationId, LongitudMaximaCorrelationId, nameof(correlationId)),
            CreadoEnUtc = creadoEnUtc,
            DisponibleDesdeUtc = creadoEnUtc,
            Estado = EstadoEntregaEmail.Pendiente,
            FechaCreacion = creadoEnUtc,
            FechaActualizacion = creadoEnUtc
        };
    }

    private static string NormalizarRequerido(string? value, int longitudMaxima, string parametro)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El valor es obligatorio.", parametro);

        var normalizado = value.Trim();
        if (normalizado.Length > longitudMaxima)
            throw new ArgumentException($"El valor excede {longitudMaxima} caracteres.", parametro);

        return normalizado;
    }

    private static string? NormalizarOpcional(string? value, int longitudMaxima, string parametro)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalizado = value.Trim();
        if (normalizado.Length > longitudMaxima)
            throw new ArgumentException($"El valor excede {longitudMaxima} caracteres.", parametro);

        return normalizado;
    }

    private static DateTime ExigirUtc(DateTime value, string parametro)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", parametro);

        return value;
    }
}

public enum EstadoEntregaEmail
{
    Pendiente = 0,
    Procesando = 1,
    ReintentoPendiente = 2,
    AceptadoProveedor = 3,
    Entregado = 4,
    Rebotado = 5,
    FallidoFinal = 6
}

public enum TipoReboteEmail
{
    Permanente = 1,
    Temporal = 2,
    Desconocido = 3
}

/// <summary>
/// Versión inmutable de una plantilla de correo por tenant. Una nueva versión
/// se modela como una nueva instancia; desactivar una versión no altera su
/// contenido histórico.
/// </summary>
public sealed class PlantillaEmailEmpresarial : BaseEntity
{
    public const int LongitudMaximaCodigo = 120;
    public const int LongitudMaximaNombre = 200;
    public const int LongitudMaximaAsunto = 998;
    public const int LongitudMaximaCuerpo = 65535;

    private PlantillaEmailEmpresarial()
    {
    }

    public int EmpresaId { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string AsuntoPlantilla { get; private set; } = string.Empty;
    public string CuerpoHtmlPlantilla { get; private set; } = string.Empty;
    public string? CuerpoTextoPlantilla { get; private set; }
    public bool Activa { get; private set; } = true;

    public static PlantillaEmailEmpresarial Crear(
        int empresaId,
        string codigo,
        int version,
        string nombre,
        string asuntoPlantilla,
        string cuerpoHtmlPlantilla,
        string? cuerpoTextoPlantilla = null,
        DateTime? creadoEnUtc = null)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId), "La empresa/tenant de la plantilla es obligatoria.");
        if (version <= 0)
            throw new ArgumentOutOfRangeException(nameof(version), "La versión debe ser mayor que cero.");

        var ahoraUtc = creadoEnUtc ?? DateTime.UtcNow;
        if (ahoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", nameof(creadoEnUtc));

        return new PlantillaEmailEmpresarial
        {
            EmpresaId = empresaId,
            Codigo = Normalizar(codigo, LongitudMaximaCodigo, nameof(codigo)),
            Version = version,
            Nombre = Normalizar(nombre, LongitudMaximaNombre, nameof(nombre)),
            AsuntoPlantilla = Normalizar(asuntoPlantilla, LongitudMaximaAsunto, nameof(asuntoPlantilla)),
            CuerpoHtmlPlantilla = Normalizar(cuerpoHtmlPlantilla, LongitudMaximaCuerpo, nameof(cuerpoHtmlPlantilla)),
            CuerpoTextoPlantilla = NormalizarOpcional(cuerpoTextoPlantilla, LongitudMaximaCuerpo, nameof(cuerpoTextoPlantilla)),
            Activa = true,
            FechaCreacion = ahoraUtc,
            FechaActualizacion = ahoraUtc
        };
    }

    public void Desactivar(DateTime ahoraUtc)
    {
        if (ahoraUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", nameof(ahoraUtc));

        Activa = false;
        FechaActualizacion = ahoraUtc;
    }

    private static string Normalizar(string? value, int longitudMaxima, string parametro)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El valor es obligatorio.", parametro);

        var normalizado = value.Trim();
        if (normalizado.Length > longitudMaxima)
            throw new ArgumentException($"El valor excede {longitudMaxima} caracteres.", parametro);

        return normalizado;
    }

    private static string? NormalizarOpcional(string? value, int longitudMaxima, string parametro)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalizado = value.Trim();
        if (normalizado.Length > longitudMaxima)
            throw new ArgumentException($"El valor excede {longitudMaxima} caracteres.", parametro);

        return normalizado;
    }
}
