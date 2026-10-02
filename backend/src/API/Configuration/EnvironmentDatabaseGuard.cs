using MySqlConnector;

namespace Solqaryn.API.Configuration;

public static class EnvironmentDatabaseGuard
{
    private sealed record ExpectedBinding(string Database, string User);

    private const string ExpectedServer = "solqaryn-mysql-solqaryn.h.aivencloud.com";
    private const uint ExpectedPort = 14402;

    public static void ValidateRenderBinding(string? environmentName, string connectionString)
    {
        var expected = environmentName?.Trim() switch
        {
            "Development" => new ExpectedBinding("solqaryn_dev", "solqaryn_dev_user"),
            "Staging" => new ExpectedBinding("solqaryn_qa", "solqaryn_qa_user"),
            "Production" => new ExpectedBinding("solqaryn_prod", "solqaryn_prod_user"),
            _ => throw new InvalidOperationException(
                "Aislamiento de entorno: ASPNETCORE_ENVIRONMENT de Render debe ser Development, Staging o Production.")
        };

        MySqlConnectionStringBuilder parsed;
        try
        {
            parsed = new MySqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Aislamiento de entorno: ConnectionStrings:DefaultConnection no es una cadena MySQL válida.",
                ex);
        }

        if (!string.Equals(parsed.Server?.Trim(), ExpectedServer, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Aislamiento de entorno: el host MySQL no coincide con el servicio Aiven corporativo canónico.");
        }

        if (parsed.Port != ExpectedPort)
        {
            throw new InvalidOperationException(
                "Aislamiento de entorno: el puerto MySQL no coincide con el endpoint Aiven corporativo canónico.");
        }

        if (parsed.SslMode != MySqlSslMode.Required)
        {
            throw new InvalidOperationException(
                "Aislamiento de entorno: la conexión MySQL de Render debe exigir SslMode=Required.");
        }

        if (!string.Equals(parsed.Database, expected.Database, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Aislamiento de entorno: la base configurada no coincide con {environmentName}. Se esperaba '{expected.Database}'.");
        }

        if (!string.Equals(parsed.UserID, expected.User, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Aislamiento de entorno: el usuario MySQL no coincide con {environmentName}. Se esperaba '{expected.User}'.");
        }
    }
}
