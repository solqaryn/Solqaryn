using Solqaryn.Domain.Security;

namespace Solqaryn.Domain.Security;

/// <summary>
/// Reglas puras de dominio para C10-04: cache, trabajos y artefactos privados.
/// No autentican, no conceden permisos y no inician workers, backups o restauraciones.
/// La autoridad de identidad, membresia, rol, cuota y recurso debe revalidarse en
/// Application/API e Infrastructure antes de cualquier efecto.
/// </summary>
public static class C1004CacheJobsDomainRules
{
    private const int MaxIntentosPermitidos = 12;
    private static readonly TimeSpan MaxHorizonteTrabajo = TimeSpan.FromHours(24);

    public static void ExigirPropiedadTenant(ContextoTenantActual contexto, int actorId, int empresaPropietariaId)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        if (actorId <= 0 || actorId != contexto.UsuarioId)
            throw new InvalidOperationException("El actor no pertenece al contexto verificado.");

        // Un identificador del cliente nunca puede sustituir al owner persistido.
        contexto.ExigirEmpresa(empresaPropietariaId);
    }

    /// <summary>
    /// Comprueba forma, limites y pertenencia. No es autorizacion ni enqueue.
    /// maxIntentos y deadline deben venir de politica server-side, no del cliente.
    /// </summary>
    public static void ValidarSolicitudTrabajo(
        ContextoTenantActual contexto,
        int actorId,
        int empresaPropietariaId,
        string operacion,
        string claveIdempotencia,
        string payloadSha256,
        long versionEsperada,
        int intentoActual,
        int maxIntentos,
        DateTimeOffset deadlineUtc,
        DateTimeOffset ahoraUtc)
    {
        ExigirPropiedadTenant(contexto, actorId, empresaPropietariaId);
        ExigirOperacion(operacion);
        ExigirClaveIdempotencia(claveIdempotencia);
        ExigirSha256(payloadSha256);

        if (versionEsperada < 0 || maxIntentos is < 1 or > MaxIntentosPermitidos ||
            intentoActual < 0 || intentoActual >= maxIntentos)
            throw new InvalidOperationException("Version o presupuesto de intentos no valido.");

        if (deadlineUtc <= ahoraUtc || deadlineUtc - ahoraUtc > MaxHorizonteTrabajo)
            throw new InvalidOperationException("El plazo de trabajo excede la politica server-side.");
    }

    /// <summary>
    /// La misma clave solo puede repetirse para la misma operacion y el mismo payload.
    /// No guarda registros de idempotencia ni garantiza atomicidad de persistencia.
    /// </summary>
    public static bool EsReintentoIdempotente(
        string operacionRegistrada,
        string shaRegistrado,
        string operacionEntrante,
        string shaEntrante)
    {
        ExigirOperacion(operacionRegistrada);
        ExigirOperacion(operacionEntrante);
        ExigirSha256(shaRegistrado);
        ExigirSha256(shaEntrante);
        if (!string.Equals(operacionRegistrada, operacionEntrante, StringComparison.Ordinal) ||
            !string.Equals(shaRegistrado, shaEntrante, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Conflicto de contenido para una clave idempotente.");

        return true;
    }

    /// <summary>
    /// Precondiciones del lease; la operacion CAS atomica real corresponde al repositorio.
    /// </summary>
    public static void ExigirLeaseVigente(
        long versionEsperada,
        long versionPersistida,
        string tokenPresentado,
        string tokenPersistido,
        DateTimeOffset venceUtc,
        DateTimeOffset ahoraUtc)
    {
        if (versionEsperada <= 0 || versionPersistida != versionEsperada ||
            string.IsNullOrWhiteSpace(tokenPresentado) ||
            string.IsNullOrWhiteSpace(tokenPersistido) ||
            !string.Equals(tokenPresentado, tokenPersistido, StringComparison.Ordinal) ||
            venceUtc <= ahoraUtc)
            throw new InvalidOperationException("El lease no esta vigente o no coincide con el estado persistido.");
    }

    /// <summary>
    /// La validacion de integridad no concede acceso de descarga: se exige RBAC otra vez.
    /// </summary>
    public static void ExigirArtefactoIntegro(
        ContextoTenantActual contexto,
        int actorId,
        int empresaPropietariaId,
        bool confirmado,
        long bytesDeclarados,
        long bytesReales,
        string shaDeclarado,
        string shaReal,
        DateTimeOffset venceUtc,
        DateTimeOffset ahoraUtc)
    {
        ExigirPropiedadTenant(contexto, actorId, empresaPropietariaId);
        ExigirSha256(shaDeclarado);
        ExigirSha256(shaReal);
        if (!confirmado || bytesDeclarados <= 0 || bytesDeclarados != bytesReales ||
            !string.Equals(shaDeclarado, shaReal, StringComparison.OrdinalIgnoreCase) ||
            venceUtc <= ahoraUtc)
            throw new InvalidOperationException("El artefacto no esta confirmado o carece de integridad vigente.");
    }

    private static void ExigirOperacion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 3 or > 64 ||
            value.Any(c => !((c >= 'a' && c <= 'z') ||
                             (c >= '0' && c <= '9') || c is '.' or '-' or '_')))
            throw new ArgumentException("La clave de operacion tiene formato no valido.", nameof(value));
        // Formato seguro NO es un allowlist ni un permiso de ejecutar la operacion.
    }

    private static void ExigirClaveIdempotencia(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 8 or > 128 ||
            value.Any(c => !((c >= 'a' && c <= 'z') ||
                             (c >= 'A' && c <= 'Z') ||
                             (c >= '0' && c <= '9') || c is '-' or '_')))
            throw new ArgumentException("Clave de idempotencia no valida.", nameof(value));
    }

    private static void ExigirSha256(string? value)
    {
        if (value is null || value.Length != 64 || value.Any(c =>
                !((c >= '0' && c <= '9') ||
                  (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))))
            throw new ArgumentException("Huella SHA-256 no valida.", nameof(value));
    }
}
