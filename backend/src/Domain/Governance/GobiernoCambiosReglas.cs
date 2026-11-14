using System.Text.RegularExpressions;

namespace Solqaryn.Domain.Governance;

/// <summary>
/// Clasifica propuestas de gobierno sin otorgar autorización ni acceso.
/// </summary>
public enum TipoCambioGobierno
{
    Documental = 1,
    CodigoDev = 2,
    SeguridadYPermisos = 3,
    PersistenciaYMigracion = 4,
    InfraestructuraYProduccion = 5
}

public enum EvaluacionCambioGobierno
{
    PendienteAutorizacion = 0,
    NoGo = 1
}

/// <summary>
/// Propuesta inerte. La autorización depende de actor, rol, tenant,
/// DAG y fuentes institucionales verificados fuera de Domain.
/// </summary>
public sealed record PropuestaCambioGobernado(
    string MicrotareaId,
    string DecisionId,
    string Motivo,
    string EvidenciaSha,
    string IdempotencyKey,
    long RevisionEsperada,
    TipoCambioGobierno Tipo,
    EvaluacionCambioGobierno Evaluacion)
{
    public bool AutorizaEjecucion => false;
}

/// <summary>
/// C00-04.M03.01: invariantes puras sin efectos, bases de datos, HTTP,
/// workers ni ejecución de decisiones. El formato de un ID jamás
/// demuestra por sí mismo su inclusión en el catálogo oficial.
/// </summary>
public static class GobiernoCambiosReglas
{
    private static readonly Regex IdMicrotarea = new(
        @"^C[0-9]{2}-[0-9]{2}\.M[0-9]{2}\.[0-9]{2}\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex IdDecision = new(
        @"^D[0-9]{2}\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ShaGit = new(
        @"^[a-fA-F0-9]{40}\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ClaveIdempotencia = new(
        @"^[a-zA-Z0-9_.:-]{12,128}\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static PropuestaCambioGobernado CrearPropuesta(
        string? microtareaId,
        string? decisionId,
        string? motivo,
        string? evidenciaSha,
        string? idempotencyKey,
        long revisionEsperada,
        TipoCambioGobierno tipo,
        bool rollbackDocumentado)
    {
        if (microtareaId is null || !IdMicrotarea.IsMatch(microtareaId))
            throw new ArgumentException("ID de microtarea no válido.", nameof(microtareaId));

        if (decisionId is null || !IdDecision.IsMatch(decisionId))
            throw new ArgumentException("ID de decisión no válido.", nameof(decisionId));

        if (string.IsNullOrWhiteSpace(motivo) || motivo.Length > 600 ||
            motivo.Any(char.IsControl))
            throw new ArgumentException("Motivo vacío o no seguro.", nameof(motivo));

        if (evidenciaSha is null || !ShaGit.IsMatch(evidenciaSha))
            throw new ArgumentException("SHA de evidencia no válido.", nameof(evidenciaSha));

        if (idempotencyKey is null || !ClaveIdempotencia.IsMatch(idempotencyKey))
            throw new ArgumentException("Clave de idempotencia inválida.", nameof(idempotencyKey));

        if (revisionEsperada <= 0)
            throw new ArgumentOutOfRangeException(nameof(revisionEsperada));

        if (!Enum.IsDefined(tipo))
            throw new ArgumentOutOfRangeException(nameof(tipo));

        // Aun con rollback, ninguna propuesta ejecuta ni concede permisos.
        // Los cambios sensibles quedan explícitamente NO_GO.
        var evaluacion = rollbackDocumentado &&
                         (tipo is TipoCambioGobierno.Documental or TipoCambioGobierno.CodigoDev)
            ? EvaluacionCambioGobierno.PendienteAutorizacion
            : EvaluacionCambioGobierno.NoGo;

        return new PropuestaCambioGobernado(
            microtareaId, decisionId, motivo, evidenciaSha, idempotencyKey,
            revisionEsperada, tipo, evaluacion);
    }
}
