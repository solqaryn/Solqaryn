using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Entities;

/// <summary>
/// Configuración de WhatsApp Business aislada por empresa.
/// Las credenciales se modelan únicamente como referencias opacas a un almacén seguro;
/// esta entidad nunca persiste el access token ni el webhook secret en claro.
/// </summary>
public sealed class ConfiguracionWhatsAppEmpresa : AuditableEntity
{
    private ConfiguracionWhatsAppEmpresa()
    {
    }

    public ConfiguracionWhatsAppEmpresa(
        int empresaId,
        string numeroTelefonoE164,
        string tokenSecretoReferencia,
        string webhookSecretoReferencia)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        EmpresaId = empresaId;
        NumeroTelefonoE164 = NormalizarTelefonoE164(numeroTelefonoE164);
        TokenSecretoReferencia = NormalizarReferenciaSecreta(tokenSecretoReferencia, nameof(tokenSecretoReferencia));
        WebhookSecretoReferencia = NormalizarReferenciaSecreta(webhookSecretoReferencia, nameof(webhookSecretoReferencia));
    }

    public int EmpresaId { get; private set; }
    public Empresa Empresa { get; private set; } = null!;

    /// <summary>
    /// Número remitente en formato E.164. Es configuración operativa no secreta.
    /// </summary>
    public string NumeroTelefonoE164 { get; private set; } = string.Empty;

    /// <summary>
    /// Referencia opaca al access token de Meta/WhatsApp en almacenamiento seguro.
    /// Nunca contiene el token en claro.
    /// </summary>
    public string TokenSecretoReferencia { get; private set; } = string.Empty;

    /// <summary>
    /// Referencia opaca al secreto empleado para validar callbacks/webhooks.
    /// Nunca contiene el secreto en claro.
    /// </summary>
    public string WebhookSecretoReferencia { get; private set; } = string.Empty;

    public bool Activa { get; private set; } = true;

    /// <summary>
    /// Token monotónico de concurrencia de aplicación para cambios de configuración.
    /// </summary>
    public long Version { get; private set; } = 1;

    public void Reconfigurar(
        string numeroTelefonoE164,
        string tokenSecretoReferencia,
        string webhookSecretoReferencia)
    {
        NumeroTelefonoE164 = NormalizarTelefonoE164(numeroTelefonoE164);
        TokenSecretoReferencia = NormalizarReferenciaSecreta(tokenSecretoReferencia, nameof(tokenSecretoReferencia));
        WebhookSecretoReferencia = NormalizarReferenciaSecreta(webhookSecretoReferencia, nameof(webhookSecretoReferencia));
        Version++;
    }

    public void Desactivar()
    {
        if (!Activa)
        {
            return;
        }

        Activa = false;
        Version++;
    }

    private static string NormalizarTelefonoE164(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El número de WhatsApp es obligatorio.", nameof(value));
        }

        var telefono = value.Trim();
        if (telefono.Length is < 9 or > 16 || telefono[0] != '+' || telefono.Skip(1).Any(c => !char.IsDigit(c)))
        {
            throw new ArgumentException("El número de WhatsApp debe usar formato E.164.", nameof(value));
        }

        return telefono;
    }

    private static string NormalizarReferenciaSecreta(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("La referencia al secreto es obligatoria.", parameterName);
        }

        var referencia = value.Trim();
        if (!referencia.Contains("://", StringComparison.Ordinal))
        {
            throw new ArgumentException("La credencial debe ser una referencia opaca a almacenamiento seguro.", parameterName);
        }

        return referencia;
    }
}
