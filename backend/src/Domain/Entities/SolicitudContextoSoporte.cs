using System;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Intención de solicitar asistencia temporal sobre una empresa.
/// No concede autoridad, no cambia el tenant activo y NO sustituye
/// la validación de rol de plataforma, permisos, membresía y auditoría persistida.
/// El reloj proviene del servidor, nunca del cliente.
/// </summary>
public sealed class SolicitudContextoSoporte
{
    public const int DuracionMaximaMinutos = 15;
    public const int JustificacionMinimaCaracteres = 20;
    public const int JustificacionMaximaCaracteres = 500;

    /// <summary>
    /// El servidor calcula ambos instantes a partir de un reloj confiable.
    /// El solicitante no puede inyectar inicio ni expiración por separado.
    /// Esta fábrica NO autoriza una sesión de soporte.
    /// </summary>
    public static SolicitudContextoSoporte CrearDesdeRelojServidor(
        int usuarioPlataformaId,
        int empresaDestinoId,
        string justificacion,
        int duracionMinutos,
        TimeProvider relojServidor)
    {
        ArgumentNullException.ThrowIfNull(relojServidor);
        if (duracionMinutos <= 0 || duracionMinutos > DuracionMaximaMinutos)
            throw new ArgumentOutOfRangeException(nameof(duracionMinutos));

        var inicioUtc = relojServidor.GetUtcNow().ToUniversalTime();
        return new SolicitudContextoSoporte(
            usuarioPlataformaId,
            empresaDestinoId,
            justificacion,
            inicioUtc,
            inicioUtc.AddMinutes(duracionMinutos));
    }

    private SolicitudContextoSoporte(
        int usuarioPlataformaId,
        int empresaDestinoId,
        string justificacion,
        DateTimeOffset inicioUtc,
        DateTimeOffset expiracionUtc)
    {
        if (usuarioPlataformaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(usuarioPlataformaId));
        if (empresaDestinoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaDestinoId));

        if (inicioUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("La hora de inicio debe ser UTC.", nameof(inicioUtc));
        if (expiracionUtc.Offset != TimeSpan.Zero)
            throw new ArgumentException("La expiración debe ser UTC.", nameof(expiracionUtc));
        if (expiracionUtc <= inicioUtc ||
            expiracionUtc > inicioUtc.AddMinutes(DuracionMaximaMinutos))
            throw new ArgumentOutOfRangeException(nameof(expiracionUtc));

        var motivo = justificacion?.Trim();
        if (string.IsNullOrEmpty(motivo) ||
            motivo.Length < JustificacionMinimaCaracteres ||
            motivo.Length > JustificacionMaximaCaracteres ||
            motivo.Any(char.IsControl))
            throw new ArgumentException("Se requiere una justificación válida, acotada y sin caracteres de control.", nameof(justificacion));

        UsuarioPlataformaId = usuarioPlataformaId;
        EmpresaDestinoId = empresaDestinoId;
        Justificacion = motivo;
        InicioUtc = inicioUtc;
        ExpiracionUtc = expiracionUtc;
    }

    public int UsuarioPlataformaId { get; }
    public int EmpresaDestinoId { get; }
    public string Justificacion { get; }
    public DateTimeOffset InicioUtc { get; }
    public DateTimeOffset ExpiracionUtc { get; }
}
