using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace Solqaryn.API.Configuration;

public sealed record RenderEnvironmentContractSnapshot(
    string Environment,
    int ManagedKeyCount,
    string KeySetFingerprint,
    string SharedConfigurationFingerprint);

public static class RenderEnvironmentContractGuard
{
    public const int ExpectedManagedKeyCount = 28;

    private static readonly string[] RequiredKeys =
    [
        "ASPNETCORE_ENVIRONMENT",
        "AllowedHosts",
        "Database__ServerVersion",
        "Database__ApplyMigrationsOnStartup",
        "ConnectionStrings__DefaultConnection",
        "Jwt__Secret",
        "Jwt__Issuer",
        "Jwt__Audience",
        "Jwt__ExpiraMinutos",
        "Security__LoginRateLimitPerMinute",
        "Cloudinary__CloudName",
        "Cloudinary__ApiKey",
        "Cloudinary__ApiSecret",
        "Cloudinary__EnvironmentPrefix",
        "Cors__AllowedOrigins__0",
        "AppSettings__BackendPublicUrl",
        "AppSettings__EnlacePublicoFacturaHorasValidez",
        "AppSettings__EnlacePublicoFacturaMaximoAccesos",
        "AppSettings__CorreoFacturaIdempotenciaMinutos",
        "Smtp__Host",
        "Smtp__Port",
        "Smtp__UsuarioSmtp",
        "Smtp__OAuth2ClientId",
        "Smtp__OAuth2RefreshToken",
        "Smtp__NombreRemitente",
        "Smtp__TimeoutSeconds",
        "Smtp__MaxAttempts",
        "Smtp__RetryBaseDelayMilliseconds"
    ];

    private static readonly string[] ManagedPrefixes =
    [
        "Database__",
        "ConnectionStrings__",
        "Jwt__",
        "Security__",
        "Cloudinary__",
        "Cors__",
        "AppSettings__",
        "Smtp__"
    ];

    private static readonly string[] SharedConfigurationKeys =
    [
        "Database__ServerVersion",
        "Jwt__ExpiraMinutos",
        "Security__LoginRateLimitPerMinute",
        "Cloudinary__CloudName",
        "AppSettings__EnlacePublicoFacturaHorasValidez",
        "AppSettings__EnlacePublicoFacturaMaximoAccesos",
        "AppSettings__CorreoFacturaIdempotenciaMinutos",
        "Smtp__Host",
        "Smtp__Port",
        "Smtp__UsuarioSmtp",
        "Smtp__OAuth2ClientId",
        "Smtp__TimeoutSeconds",
        "Smtp__MaxAttempts",
        "Smtp__RetryBaseDelayMilliseconds"
    ];

    private sealed record ExpectedEnvironment(
        string AspNetEnvironment,
        string AllowedHost,
        string ApplyMigrationsOnStartup,
        string JwtIssuer,
        string JwtAudience,
        string CloudinaryPrefix,
        string CorsOrigin,
        string BackendPublicUrl,
        string SmtpSenderName);

    public static RenderEnvironmentContractSnapshot ValidateProcessEnvironment(string? environmentName)
    {
        var variables = Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Where(entry => entry.Key is string)
            .ToDictionary(
                entry => (string)entry.Key,
                entry => entry.Value?.ToString(),
                StringComparer.Ordinal);

        return Validate(environmentName, variables);
    }

    public static RenderEnvironmentContractSnapshot Validate(
        string? environmentName,
        IReadOnlyDictionary<string, string?> variables)
    {
        var expected = ResolveExpected(environmentName);

        var managedKeys = variables.Keys
            .Where(IsManagedKey)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        var missing = RequiredKeys
            .Except(managedKeys, StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        var extra = managedKeys
            .Except(RequiredKeys, StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        if (missing.Length > 0 || extra.Length > 0 || managedKeys.Length != ExpectedManagedKeyCount)
        {
            throw new InvalidOperationException(
                $"Contrato Render inválido. Esperadas {ExpectedManagedKeyCount} claves administradas; " +
                $"observadas {managedKeys.Length}; faltantes=[{string.Join(",", missing)}]; extras=[{string.Join(",", extra)}].");
        }

        var empty = RequiredKeys
            .Where(key => !variables.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        if (empty.Length > 0)
        {
            throw new InvalidOperationException(
                $"Contrato Render inválido. Hay claves requeridas sin valor: [{string.Join(",", empty)}].");
        }

        Expect(variables, "ASPNETCORE_ENVIRONMENT", expected.AspNetEnvironment);
        Expect(variables, "AllowedHosts", expected.AllowedHost);
        Expect(variables, "Database__ServerVersion", "8.4.8");
        Expect(variables, "Database__ApplyMigrationsOnStartup", expected.ApplyMigrationsOnStartup);
        Expect(variables, "Jwt__Issuer", expected.JwtIssuer);
        Expect(variables, "Jwt__Audience", expected.JwtAudience);
        Expect(variables, "Jwt__ExpiraMinutos", "120");
        Expect(variables, "Security__LoginRateLimitPerMinute", "20");
        Expect(variables, "Cloudinary__CloudName", "riyrzmob");
        Expect(variables, "Cloudinary__EnvironmentPrefix", expected.CloudinaryPrefix);
        Expect(variables, "Cors__AllowedOrigins__0", expected.CorsOrigin);
        Expect(variables, "AppSettings__BackendPublicUrl", expected.BackendPublicUrl);
        Expect(variables, "AppSettings__EnlacePublicoFacturaHorasValidez", "24");
        Expect(variables, "AppSettings__EnlacePublicoFacturaMaximoAccesos", "10");
        Expect(variables, "AppSettings__CorreoFacturaIdempotenciaMinutos", "15");
        Expect(variables, "Smtp__Host", "smtp-mail.outlook.com");
        Expect(variables, "Smtp__Port", "587");
        Expect(variables, "Smtp__UsuarioSmtp", "solqaryn.platform@outlook.com");
        Expect(variables, "Smtp__OAuth2ClientId", "86122655-f065-413e-87af-56e7a1134d2c");
        Expect(variables, "Smtp__NombreRemitente", expected.SmtpSenderName);
        Expect(variables, "Smtp__TimeoutSeconds", "60");
        Expect(variables, "Smtp__MaxAttempts", "3");
        Expect(variables, "Smtp__RetryBaseDelayMilliseconds", "500");

        var keyFingerprint = Fingerprint(string.Join("\n", managedKeys));
        var sharedPayload = string.Join(
            "\n",
            SharedConfigurationKeys
                .OrderBy(key => key, StringComparer.Ordinal)
                .Select(key => $"{key}={variables[key]!.Trim()}"));

        return new RenderEnvironmentContractSnapshot(
            expected.AspNetEnvironment,
            managedKeys.Length,
            keyFingerprint,
            Fingerprint(sharedPayload));
    }

    private static bool IsManagedKey(string key) =>
        string.Equals(key, "ASPNETCORE_ENVIRONMENT", StringComparison.Ordinal) ||
        string.Equals(key, "AllowedHosts", StringComparison.Ordinal) ||
        ManagedPrefixes.Any(prefix => key.StartsWith(prefix, StringComparison.Ordinal));

    private static ExpectedEnvironment ResolveExpected(string? environmentName) =>
        environmentName?.Trim() switch
        {
            "Development" => new ExpectedEnvironment(
                "Development",
                "solqaryn-api-dev-fxx8.onrender.com",
                "true",
                "Solqaryn.DEV.API",
                "Solqaryn.DEV.Frontend",
                "solqaryn_dev",
                "https://solqaryn-dev.vercel.app",
                "https://solqaryn-api-dev-fxx8.onrender.com",
                "SOLQARYN DEV"),
            "Staging" => new ExpectedEnvironment(
                "Staging",
                "solqaryn-api-qa.onrender.com",
                "false",
                "Solqaryn.QA.API",
                "Solqaryn.QA.Frontend",
                "solqaryn_qa",
                "https://solqaryn-qa.vercel.app",
                "https://solqaryn-api-qa.onrender.com",
                "SOLQARYN QA"),
            "Production" => new ExpectedEnvironment(
                "Production",
                "solqaryn-api-prod.onrender.com",
                "false",
                "Solqaryn.PROD.API",
                "Solqaryn.PROD.Frontend",
                "solqaryn_prod",
                "https://solqaryn-prod.vercel.app",
                "https://solqaryn-api-prod.onrender.com",
                "SOLQARYN PROD"),
            _ => throw new InvalidOperationException(
                "Contrato Render: ASPNETCORE_ENVIRONMENT debe ser Development, Staging o Production.")
        };

    private static void Expect(
        IReadOnlyDictionary<string, string?> variables,
        string key,
        string expected)
    {
        var actual = variables[key]?.Trim();
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Contrato Render inválido para {key}. El valor no coincide con el contrato canónico del entorno.");
        }
    }

    private static string Fingerprint(string value)
    {
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(digest.AsSpan(0, 8));
    }
}
