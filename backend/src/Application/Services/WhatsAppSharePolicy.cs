namespace Solqaryn.Application.Services;

/// Canonical policy for user initiated WhatsApp handoff (wa.me), without a provider API.
public static class WhatsAppSharePolicy
{
    public const string HandoffGenerated = "WHATSAPP_HANDOFF_GENERATED";
    public const string ClientOpenRequested = "WHATSAPP_CLIENT_OPEN_REQUESTED";

    public static string NormalizePhone(string? value, string defaultCountryPrefix = "504")
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var raw = value.Trim();
        if (raw.Any(c => !char.IsDigit(c) && c is not ('+' or ' ' or '.' or '(' or ')' or '-'))) return string.Empty;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("00", StringComparison.Ordinal)) digits = digits[2..];
        var prefix = new string(defaultCountryPrefix.Where(char.IsDigit).ToArray());
        if (digits.Length == 8 && prefix.Length > 0) digits = prefix + digits;
        if (prefix.Length > 0 && digits.StartsWith(prefix + prefix, StringComparison.Ordinal) && digits.Length == prefix.Length * 2 + 8)
            return string.Empty;
        return digits.Length is >= 10 and <= 15 && digits[0] != '0' && digits.All(char.IsDigit)
            ? digits
            : string.Empty;
    }

    public static string MaskPhone(string? value)
    {
        var normalized = NormalizePhone(value);
        return normalized.Length == 0 ? string.Empty : new string('*', Math.Max(0, normalized.Length - 4)) + normalized[^4..];
    }
}
