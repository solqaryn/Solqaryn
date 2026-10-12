using System.Data.Common;

namespace Solqaryn.API.Configuration;

public static class EnvironmentDatabaseGuard
{
    private sealed record ExpectedBinding(string Database, string User);

    private const string ExpectedServer = "solqaryn-mysql-solqaryn.h.aivencloud.com";
    private const int ExpectedPort = 14402;

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

        DbConnectionStringBuilder parsed;
        try
        {
            parsed = new DbConnectionStringBuilder { ConnectionString = connectionString };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Aislamiento de entorno: ConnectionStrings:DefaultConnection no es una cadena MySQL válida.",
                ex);
        }

        static string? Read(DbConnectionStringBuilder builder, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (builder.TryGetValue(key, out var value) && value is not null)
                    return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)?.Trim();
            }

            return null;
        }

        var server = Read(parsed, "Server", "Host", "Data Source");
        var portText = Read(parsed, "Port");
        var database = Read(parsed, "Database", "Initial Catalog");
        var user = Read(parsed, "User ID", "UserID", "User", "Username", "Uid");
        var sslMode = Read(parsed, "SslMode", "Ssl Mode");
        if (!int.TryParse(portText, out var port))
            port = 3306;

        if (!string.Equals(server, ExpectedServer, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Aislamiento de entorno: el host MySQL no coincide con el servicio Aiven corporativo canónico.");
        }

        if (port != ExpectedPort)
        {
            throw new InvalidOperationException(
                "Aislamiento de entorno: el puerto MySQL no coincide con el endpoint Aiven corporativo canónico.");
        }

        if (!string.Equals(sslMode, "Required", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Aislamiento de entorno: la conexión MySQL de Render debe exigir SslMode=Required.");
        }

        if (!string.Equals(database, expected.Database, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Aislamiento de entorno: la base configurada no coincide con {environmentName}. Se esperaba '{expected.Database}'.");
        }

        if (!string.Equals(user, expected.User, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Aislamiento de entorno: el usuario MySQL no coincide con {environmentName}. Se esperaba '{expected.User}'.");
        }
    }
}
