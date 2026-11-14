using System.Globalization;
using System.Text;

namespace Solqaryn.Application.Common;

/// <summary>
/// Slug publico canonico con id estable al final. El texto mejora legibilidad,
/// mientras el id preserva resolucion aunque cambie el nombre comercial.
/// </summary>
public static class PublicSlug
{
    public static string Create(string? nombre, int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));

        var normalizado = (nombre ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalizado.Length);
        var separadorPendiente = false;

        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c))
            {
                if (separadorPendiente && builder.Length > 0) builder.Append('-');
                builder.Append(char.ToLowerInvariant(c));
                separadorPendiente = false;
            }
            else
            {
                separadorPendiente = builder.Length > 0;
            }
        }

        var baseSlug = builder.Length > 0 ? builder.ToString() : "item";
        return $"{baseSlug}-{id.ToString(CultureInfo.InvariantCulture)}";
    }

    public static bool TryGetId(string? slug, out int id)
    {
        id = 0;
        if (string.IsNullOrWhiteSpace(slug)) return false;

        var limpio = slug.Trim();
        var ultimoGuion = limpio.LastIndexOf('-');
        var candidato = ultimoGuion >= 0 ? limpio[(ultimoGuion + 1)..] : limpio;
        return int.TryParse(candidato, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;
    }
}
