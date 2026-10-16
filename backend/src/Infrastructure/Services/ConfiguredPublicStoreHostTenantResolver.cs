using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Solqaryn.Application.Interfaces;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Registro host exacto -> EmpresaId aprobado en configuración del servidor.
/// No deriva empresas del nombre, de cabeceras forwarded ni de valores públicos.
/// La activación es opt-in y separada de la cuarentena general de rutas privadas.
/// </summary>
public sealed class ConfiguredPublicStoreHostTenantResolver : IPublicStoreHostTenantResolver
{
    private readonly IReadOnlyDictionary<string, int> _byOriginAndKey;
    public bool IsEnabled { get; }

    public ConfiguredPublicStoreHostTenantResolver(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("PublicStore:EnableMultiEmpresaBinding"))
        {
            _byOriginAndKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in configuration.GetSection("PublicStore:HostBindings").GetChildren())
        {
            var host = NormalizeExactHostname(entry["Host"]);
            if (host is null || !int.TryParse(entry["EmpresaId"], out var empresaId) || empresaId <= 0)
                throw new InvalidOperationException(
                    "PublicStore:HostBindings contains an invalid host or EmpresaId.");

            // Varias tiendas comparten Host, nunca la pareja Host + StoreKey.
            // La entrada sin StoreKey representa exclusivamente /tienda legacy.
            var configuredKey = entry["StoreKey"];
            var storeKey = configuredKey is null ? string.Empty : NormalizeStoreKey(configuredKey);
            if (storeKey is null)
                throw new InvalidOperationException("PublicStore:HostBindings contains an invalid StoreKey.");
            if (!map.TryAdd(ComposeKey(host, storeKey), empresaId))
                throw new InvalidOperationException(
                    "PublicStore:HostBindings contains a duplicate public host.");
        }

        if (map.Count == 0)
            throw new InvalidOperationException(
                "PublicStore multiempresa cannot be enabled without approved host bindings.");

        _byOriginAndKey = map;
        IsEnabled = true;
    }

    public int? ResolveEmpresaId(HostString requestHost) =>
        ResolveEmpresaId(requestHost, null);

    public int? ResolveEmpresaId(HostString requestHost, string? storeKey)
    {
        // Los valores recibidos deben ser verificados por el middleware.
        // El resolvedor SOLO consulta la allowlist de servidor.
        var normalizedHost = NormalizeExactHostname(requestHost.Host);
        var normalizedKey = storeKey is null ? string.Empty : NormalizeStoreKey(storeKey);
        return normalizedHost is not null && normalizedKey is not null &&
               _byOriginAndKey.TryGetValue(ComposeKey(normalizedHost, normalizedKey), out var empresaId)
            ? empresaId : null;
    }

    private static string ComposeKey(string host, string storeKey) => host + "\n" + storeKey;

    private static string? NormalizeStoreKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 2 or > 48 ||
            value != value.Trim() || value != value.ToLowerInvariant())
            return null;
        if (!(value[0] is >= 'a' and <= 'z') ||
            !(value[^1] is >= 'a' and <= 'z' or >= '0' and <= '9') ||
            value.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
            return null;
        return value;
    }

    private static string? NormalizeExactHostname(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var host = value.Trim().ToLowerInvariant();
        if (host.Length is < 4 or > 253 || !host.Contains('.') ||
            IPAddress.TryParse(host, out _))
            return null;

        var labels = host.Split('.');
        foreach (var label in labels)
        {
            if (label.Length is < 1 or > 63 ||
                label[0] == '-' || label[^1] == '-' ||
                label.Any(c => !((c >= 'a' && c <= 'z') ||
                                 (c >= '0' && c <= '9') || c == '-')))
                return null;
        }

        return host;
    }
}
