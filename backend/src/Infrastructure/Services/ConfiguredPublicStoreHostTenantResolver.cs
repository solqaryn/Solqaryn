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
    private readonly IReadOnlyDictionary<string, int> _byHost;
    public bool IsEnabled { get; }

    public ConfiguredPublicStoreHostTenantResolver(IConfiguration configuration)
    {
        if (!configuration.GetValue<bool>("PublicStore:EnableMultiEmpresaBinding"))
        {
            _byHost = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in configuration.GetSection("PublicStore:HostBindings").GetChildren())
        {
            var host = NormalizeExactHostname(entry["Host"]);
            if (host is null || !int.TryParse(entry["EmpresaId"], out var empresaId) || empresaId <= 0)
                throw new InvalidOperationException(
                    "PublicStore:HostBindings contains an invalid host or EmpresaId.");

            if (!map.TryAdd(host, empresaId))
                throw new InvalidOperationException(
                    "PublicStore:HostBindings contains a duplicate public host.");
        }

        if (map.Count == 0)
            throw new InvalidOperationException(
                "PublicStore multiempresa cannot be enabled without approved host bindings.");

        _byHost = map;
        IsEnabled = true;
    }

    public int? ResolveEmpresaId(HostString requestHost)
    {
        // Usar únicamente Request.Host.Host; ningún X-Forwarded-Host, path,
        // cabecera de empresa, nombre comercial o subdominio parecido es válido.
        var normalized = NormalizeExactHostname(requestHost.Host);
        return normalized is not null && _byHost.TryGetValue(normalized, out var empresaId)
            ? empresaId : null;
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
