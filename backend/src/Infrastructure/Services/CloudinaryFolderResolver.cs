using Microsoft.Extensions.Configuration;

namespace Solqaryn.Infrastructure.Services;

internal static class CloudinaryFolderResolver
{
    public static string Resolve(IConfiguration configuration, string baseFolder)
    {
        var environmentPrefix = GetEnvironmentPrefix(configuration);

        return string.IsNullOrWhiteSpace(environmentPrefix)
            ? baseFolder
            : $"{environmentPrefix}/{baseFolder}";
    }

    public static string? GetEnvironmentPrefix(IConfiguration configuration)
    {
        var prefix = configuration["Cloudinary:EnvironmentPrefix"]?
            .Trim()
            .Trim('/');

        return string.IsNullOrWhiteSpace(prefix) ? null : prefix;
    }

    public static bool CanDelete(string? environmentPrefix, string publicId)
    {
        if (string.IsNullOrWhiteSpace(environmentPrefix))
            return true;

        if (string.IsNullOrWhiteSpace(publicId))
            return false;

        return publicId.StartsWith($"{environmentPrefix}/", StringComparison.Ordinal);
    }
}
