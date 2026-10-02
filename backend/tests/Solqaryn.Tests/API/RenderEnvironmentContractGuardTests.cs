using Solqaryn.API.Configuration;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class RenderEnvironmentContractGuardTests
{
    [Theory]
    [InlineData("Development", "solqaryn-api-dev-fxx8.onrender.com", "true", "Solqaryn.DEV.API", "Solqaryn.DEV.Frontend", "solqaryn_dev", "https://solqaryn-dev.vercel.app", "https://solqaryn-api-dev-fxx8.onrender.com", "SOLQARYN DEV")]
    [InlineData("Staging", "solqaryn-api-qa.onrender.com", "false", "Solqaryn.QA.API", "Solqaryn.QA.Frontend", "solqaryn_qa", "https://solqaryn-qa.vercel.app", "https://solqaryn-api-qa.onrender.com", "SOLQARYN QA")]
    [InlineData("Production", "solqaryn-api-prod.onrender.com", "false", "Solqaryn.PROD.API", "Solqaryn.PROD.Frontend", "solqaryn_prod", "https://solqaryn-prod.vercel.app", "https://solqaryn-api-prod.onrender.com", "SOLQARYN PROD")]
    public void Validate_AceptaContratoCanonico(
        string environment,
        string allowedHost,
        string applyMigrations,
        string issuer,
        string audience,
        string cloudinaryPrefix,
        string corsOrigin,
        string backendUrl,
        string senderName)
    {
        var variables = Canonical(
            environment,
            allowedHost,
            applyMigrations,
            issuer,
            audience,
            cloudinaryPrefix,
            corsOrigin,
            backendUrl,
            senderName);

        var snapshot = RenderEnvironmentContractGuard.Validate(environment, variables);

        Assert.Equal(RenderEnvironmentContractGuard.ExpectedManagedKeyCount, snapshot.ManagedKeyCount);
        Assert.Equal(environment, snapshot.Environment);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.KeySetFingerprint));
        Assert.False(string.IsNullOrWhiteSpace(snapshot.SharedConfigurationFingerprint));
    }

    [Fact]
    public void Validate_RechazaClaveFaltante()
    {
        var variables = CanonicalDev();
        variables.Remove("Smtp__MaxAttempts");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            RenderEnvironmentContractGuard.Validate("Development", variables));

        Assert.Contains("faltantes=[Smtp__MaxAttempts]", ex.Message);
    }

    [Fact]
    public void Validate_RechazaClaveAdministradaExtra()
    {
        var variables = CanonicalDev();
        variables["Smtp__PasswordSmtp"] = "legacy";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            RenderEnvironmentContractGuard.Validate("Development", variables));

        Assert.Contains("extras=[Smtp__PasswordSmtp]", ex.Message);
    }

    [Fact]
    public void Validate_RechazaSecretoVacioSinExponerValor()
    {
        var variables = CanonicalDev();
        variables["Cloudinary__ApiSecret"] = "";

        var ex = Assert.Throws<InvalidOperationException>(() =>
            RenderEnvironmentContractGuard.Validate("Development", variables));

        Assert.Contains("Cloudinary__ApiSecret", ex.Message);
        Assert.DoesNotContain("secret-value", ex.Message);
    }

    [Fact]
    public void Validate_RechazaCruceDeOrigen()
    {
        var variables = CanonicalDev();
        variables["Cors__AllowedOrigins__0"] = "https://solqaryn-prod.vercel.app";

        Assert.Throws<InvalidOperationException>(() =>
            RenderEnvironmentContractGuard.Validate("Development", variables));
    }

    [Fact]
    public void Validate_ComparteFingerprintPublicoEntreEntornos()
    {
        var dev = RenderEnvironmentContractGuard.Validate("Development", CanonicalDev());
        var qaVariables = Canonical(
            "Staging",
            "solqaryn-api-qa.onrender.com",
            "false",
            "Solqaryn.QA.API",
            "Solqaryn.QA.Frontend",
            "solqaryn_qa",
            "https://solqaryn-qa.vercel.app",
            "https://solqaryn-api-qa.onrender.com",
            "SOLQARYN QA");
        qaVariables["Cloudinary__CloudName"] = "qa-cloud";

        var prodVariables = Canonical(
            "Production",
            "solqaryn-api-prod.onrender.com",
            "false",
            "Solqaryn.PROD.API",
            "Solqaryn.PROD.Frontend",
            "solqaryn_prod",
            "https://solqaryn-prod.vercel.app",
            "https://solqaryn-api-prod.onrender.com",
            "SOLQARYN PROD");
        prodVariables["Cloudinary__CloudName"] = "prod-cloud";

        var qa = RenderEnvironmentContractGuard.Validate("Staging", qaVariables);
        var prod = RenderEnvironmentContractGuard.Validate("Production", prodVariables);

        Assert.Equal(dev.KeySetFingerprint, qa.KeySetFingerprint);
        Assert.Equal(dev.KeySetFingerprint, prod.KeySetFingerprint);
        Assert.Equal(dev.SharedConfigurationFingerprint, qa.SharedConfigurationFingerprint);
        Assert.Equal(dev.SharedConfigurationFingerprint, prod.SharedConfigurationFingerprint);
    }

    private static Dictionary<string, string?> CanonicalDev() =>
        Canonical(
            "Development",
            "solqaryn-api-dev-fxx8.onrender.com",
            "true",
            "Solqaryn.DEV.API",
            "Solqaryn.DEV.Frontend",
            "solqaryn_dev",
            "https://solqaryn-dev.vercel.app",
            "https://solqaryn-api-dev-fxx8.onrender.com",
            "SOLQARYN DEV");

    private static Dictionary<string, string?> Canonical(
        string environment,
        string allowedHost,
        string applyMigrations,
        string issuer,
        string audience,
        string cloudinaryPrefix,
        string corsOrigin,
        string backendUrl,
        string senderName) =>
        new(StringComparer.Ordinal)
        {
            ["ASPNETCORE_ENVIRONMENT"] = environment,
            ["AllowedHosts"] = allowedHost,
            ["Database__ServerVersion"] = "8.4.8",
            ["Database__ApplyMigrationsOnStartup"] = applyMigrations,
            ["ConnectionStrings__DefaultConnection"] = "Server=db;Port=3306;Database=db;User=u;Password=p;SslMode=Required;",
            ["Jwt__Secret"] = "secret-value",
            ["Jwt__Issuer"] = issuer,
            ["Jwt__Audience"] = audience,
            ["Jwt__ExpiraMinutos"] = "120",
            ["Security__LoginRateLimitPerMinute"] = "20",
            ["Cloudinary__CloudName"] = "riyrzmob",
            ["Cloudinary__ApiKey"] = "api-key",
            ["Cloudinary__ApiSecret"] = "secret-value",
            ["Cloudinary__EnvironmentPrefix"] = cloudinaryPrefix,
            ["Cors__AllowedOrigins__0"] = corsOrigin,
            ["AppSettings__BackendPublicUrl"] = backendUrl,
            ["AppSettings__EnlacePublicoFacturaHorasValidez"] = "24",
            ["AppSettings__EnlacePublicoFacturaMaximoAccesos"] = "10",
            ["AppSettings__CorreoFacturaIdempotenciaMinutos"] = "15",
            ["Smtp__Host"] = "smtp-mail.outlook.com",
            ["Smtp__Port"] = "587",
            ["Smtp__UsuarioSmtp"] = "solqaryn.platform@outlook.com",
            ["Smtp__OAuth2ClientId"] = "86122655-f065-413e-87af-56e7a1134d2c",
            ["Smtp__OAuth2RefreshToken"] = "secret-value",
            ["Smtp__NombreRemitente"] = senderName,
            ["Smtp__TimeoutSeconds"] = "60",
            ["Smtp__MaxAttempts"] = "3",
            ["Smtp__RetryBaseDelayMilliseconds"] = "500"
        };
}
