using System;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Intención de solicitar asistencia temporal sobre una empresa.
/// No concede autoridad, no cambia el tenant activo y NO sustituye
/// la validación de rol de plataforma, permisos, membresía y auditoría persistida.
/// El caso de uso debe emplear un reloj UTC del servidor, nunca uno del cliente.
/// </summary>
public sealed class SolicitudContextoSoporte
{
    public const int DuracionMaximaMinutos = 15;
    public const int JustificacionMinimaCaracteres = 20;
    public const int JustificacionMaximaCaracteres = 500;

    public SolicitudContextoSoporte(
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
