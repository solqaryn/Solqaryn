using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using MySqlConnector;

static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

static (string Provider, string Suffix, string Confidence) DetectProvider(string host)
{
    host = host.Trim().TrimEnd('.').ToLowerInvariant();
    var known = new (string Provider, string Suffix)[]
    {
        ("AIVEN", "aivencloud.com"),
        ("AWS_RDS", "rds.amazonaws.com"),
        ("AZURE_DATABASE_FOR_MYSQL", "mysql.database.azure.com"),
        ("DIGITALOCEAN", "ondigitalocean.com"),
        ("RAILWAY", "proxy.rlwy.net"),
        ("RAILWAY", "railway.internal"),
        ("TIDB_CLOUD", "tidbcloud.com"),
        ("PLANETSCALE", "psdb.cloud"),
        ("SCALEGRID", "scalegrid.io"),
        ("CLEVER_CLOUD", "clever-cloud.com")
    };

    foreach (var item in known)
    {
        if (host == item.Suffix || host.EndsWith("." + item.Suffix, StringComparison.Ordinal))
            return (item.Provider, item.Suffix, "HIGH_PROVIDER_SPECIFIC_HOST_SUFFIX");
    }

    return ("UNKNOWN", "REDACTED_UNKNOWN_SUFFIX", "INSUFFICIENT");
}

static string? PackageVersion(string projectPath, string package)
{
    var document = XDocument.Load(projectPath);
    return document.Descendants("PackageReference")
        .FirstOrDefault(x => string.Equals((string?)x.Attribute("Include"), package, StringComparison.OrdinalIgnoreCase))?
        .Attribute("Version")?.Value;
}

static async Task ExecuteAsync(MySqlConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    command.CommandTimeout = 20;
    await command.ExecuteNonQueryAsync();
}

static async Task<string?> SessionStatusValueAsync(MySqlConnection connection, string variable)
{
    await using var command = connection.CreateCommand();
    command.CommandText = "SHOW SESSION STATUS LIKE @variable;";
    command.Parameters.AddWithValue("@variable", variable);
    command.CommandTimeout = 20;
    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) return null;
    return reader.IsDBNull(1) ? null : reader.GetString(1);
}

var outputDirectory = args.Length > 0 ? args[0] : ".n820c-proof";
var connectionString = Environment.GetEnvironmentVariable("SOLQARYN_DB_CONNECTION");
var repoRoot = Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? Directory.GetCurrentDirectory();
var functionalHead = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "UNKNOWN";
var runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ?? "LOCAL";
var runAttempt = Environment.GetEnvironmentVariable("GITHUB_RUN_ATTEMPT") ?? "1";
var capturedAt = DateTimeOffset.UtcNow.ToString("O");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("N8.20.C probe failed: required DEV DB secret is unavailable.");
    return 21;
}

Directory.CreateDirectory(outputDirectory);

try
{
    var csb = new MySqlConnectionStringBuilder(connectionString)
    {
        SslMode = MySqlSslMode.Required,
        AllowLoadLocalInfile = false,
        ConnectionTimeout = 15,
        DefaultCommandTimeout = 20
    };

    if (string.IsNullOrWhiteSpace(csb.Server))
        throw new InvalidOperationException("SERVER_MISSING");

    var originalHost = csb.Server;
    var provider = DetectProvider(originalHost);

    string serverVersion;
    string versionComment;
    string compileOs;
    string compileMachine;
    string? sslVersion;
    string? sslCipher;
    bool readOnlyTransactionAccepted = false;

    await using (var connection = new MySqlConnection(csb.ConnectionString))
    {
        await connection.OpenAsync();

        await ExecuteAsync(connection, "SET SESSION TRANSACTION READ ONLY;");
        await ExecuteAsync(connection, "START TRANSACTION READ ONLY;");
        readOnlyTransactionAccepted = true;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT VERSION(), @@version_comment, @@version_compile_os, @@version_compile_machine;";
            command.CommandTimeout = 20;
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) throw new InvalidOperationException("VERSION_QUERY_EMPTY");
            serverVersion = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            versionComment = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            compileOs = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            compileMachine = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
        }

        sslVersion = await SessionStatusValueAsync(connection, "Ssl_version");
        sslCipher = await SessionStatusValueAsync(connection, "Ssl_cipher");

        await ExecuteAsync(connection, "ROLLBACK;");
    }

    var engine = serverVersion.Contains("mariadb", StringComparison.OrdinalIgnoreCase) ? "MARIADB" : "MYSQL";
    var infrastructureProject = Path.Combine(repoRoot, "backend", "src", "Infrastructure", "Solqaryn.Infrastructure.csproj");
    var programFile = Path.Combine(repoRoot, "backend", "src", "API", "Program.cs");
    var migrationsRoot = Path.Combine(repoRoot, "backend", "src", "Infrastructure", "Migrations");

    if (!File.Exists(infrastructureProject) || !File.Exists(programFile) || !Directory.Exists(migrationsRoot))
        throw new InvalidOperationException("REPO_PROVIDER_EVIDENCE_MISSING");

    var pomeloVersion = PackageVersion(infrastructureProject, "Pomelo.EntityFrameworkCore.MySql");
    var connectorVersion = PackageVersion(infrastructureProject, "MySqlConnector");
    var programText = await File.ReadAllTextAsync(programFile);
    var usesMySql = programText.Contains("UseMySql(", StringComparison.Ordinal);

    string? migrationEvidence = null;
    foreach (var file in Directory.EnumerateFiles(migrationsRoot, "*.Designer.cs", SearchOption.AllDirectories))
    {
        var text = await File.ReadAllTextAsync(file);
        if (text.Contains("MySqlModelBuilderExtensions", StringComparison.Ordinal) ||
            text.Contains("MySqlPropertyBuilderExtensions", StringComparison.Ordinal))
        {
            migrationEvidence = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            break;
        }
    }

    var tlsActive = !string.IsNullOrWhiteSpace(sslCipher);
    var runtimeVersionProven = !string.IsNullOrWhiteSpace(serverVersion);
    var providerProven = provider.Provider != "UNKNOWN";
    var efProviderProven = usesMySql && !string.IsNullOrWhiteSpace(pomeloVersion) && !string.IsNullOrWhiteSpace(connectorVersion);
    var migrationProviderProven = !string.IsNullOrWhiteSpace(migrationEvidence);

    var pass = engine == "MYSQL" && runtimeVersionProven && providerProven && efProviderProven && migrationProviderProven && tlsActive && readOnlyTransactionAccepted;

    var evidence = new
    {
        schemaVersion = "solqaryn.db-provider-proof.v1",
        authority = "docs/VAEP_AUTHORITY.md",
        task = "N8.20.C",
        status = pass ? "PASS_PROVIDER_IDENTIFIED" : "NOT_CERTIFIED",
        branch = "Desarrollo",
        functionalHead,
        capturedAtUtc = capturedAt,
        github = new { runId, runAttempt },
        runtime = new
        {
            engine,
            serverVersion,
            versionComment,
            compileOs,
            compileMachine,
            readOnlyTransaction = readOnlyTransactionAccepted,
            tls = new { active = tlsActive, version = sslVersion, cipher = sslCipher },
            databaseSelected = !string.IsNullOrWhiteSpace(csb.Database),
            provider = provider.Provider,
            providerConfidence = provider.Confidence,
            providerHostSuffix = provider.Suffix,
            providerHostSha256 = Sha256(originalHost)
        },
        ef = new
        {
            provider = "Pomelo.EntityFrameworkCore.MySql",
            providerVersion = pomeloVersion,
            connector = "MySqlConnector",
            connectorVersion,
            registration = usesMySql ? "UseMySql" : "NOT_FOUND",
            migrationProvider = migrationProviderProven ? "Pomelo.EntityFrameworkCore.MySql" : "NOT_PROVEN",
            migrationProviderEvidence = migrationEvidence
        },
        guardrails = new
        {
            connectionStringPersisted = false,
            usernamePersisted = false,
            passwordPersisted = false,
            databaseNamePersisted = false,
            fullHostPersisted = false,
            writeStatementExecuted = false,
            productionTouched = false,
            mainTouched = false,
            secretsExposed = 0
        },
        acceptance = new
        {
            actualRuntimeVersionProven = runtimeVersionProven,
            actualProviderProven = providerProven,
            efProviderProven,
            migrationProviderProven,
            readOnlyTransactionProven = readOnlyTransactionAccepted,
            tlsProven = tlsActive,
            p0 = pass ? 0 : 1,
            p1 = 0
        }
    };

    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    await File.WriteAllTextAsync(Path.Combine(outputDirectory, "N8.20.C_DB_PROVIDER_EVIDENCE.json"), JsonSerializer.Serialize(evidence, jsonOptions) + Environment.NewLine);

    var readme = $"""
# N8.20.C — DB runtime/provider proof

- Estado: `{(pass ? "PASS_PROVIDER_IDENTIFIED" : "NOT_CERTIFIED")}`
- Rama: `Desarrollo`
- Functional HEAD: `{functionalHead}`
- Capturado UTC: `{capturedAt}`
- GitHub Actions run: `{runId}` attempt `{runAttempt}`
- Motor runtime: `{engine}`
- Versión runtime: `{serverVersion}`
- Proveedor real: `{provider.Provider}`
- Evidencia provider: `{provider.Confidence}` / `{provider.Suffix}`
- Host completo: `REDACTED`
- TLS: `{(tlsActive ? "PASS" : "FAIL")}`
- Transacción read-only: `{(readOnlyTransactionAccepted ? "PASS" : "FAIL")}`
- EF provider: `Pomelo.EntityFrameworkCore.MySql {pomeloVersion}`
- Connector: `MySqlConnector {connectorVersion}`
- Registro runtime: `{(usesMySql ? "UseMySql" : "NOT_FOUND")}`
- Migration provider: `{(migrationProviderProven ? "Pomelo.EntityFrameworkCore.MySql" : "NOT_PROVEN")}`
- Migration evidence: `{migrationEvidence ?? "NONE"}`
- Connection string persistida: `false`
- Usuario persistido: `false`
- Password persistido: `false`
- Database name persistida: `false`
- Production touched: `false`
- main touched: `false`
- Secrets exposed: `0`

## Dictamen

`{(pass ? "N8.20.C tiene evidencia técnica suficiente para que el controller ejecute REVIEW_FIRST, receipt/readback y LISTO_REAL; este probe no se auto-certifica." : "N8.20.C NO puede cerrarse. No fabricar LISTO_REAL.")}`
""";

    await File.WriteAllTextAsync(Path.Combine(outputDirectory, "README.md"), readme + Environment.NewLine);

    var summary = new
    {
        task = "N8.20.C",
        pass,
        engine,
        serverVersion,
        provider = provider.Provider,
        efProvider = $"Pomelo.EntityFrameworkCore.MySql {pomeloVersion}",
        migrationProvider = migrationProviderProven ? "Pomelo.EntityFrameworkCore.MySql" : "NOT_PROVEN",
        tls = tlsActive,
        readOnly = readOnlyTransactionAccepted,
        functionalHead,
        githubRunId = runId
    };

    Console.WriteLine("VAEP_SANITIZED_RESULT=" + JsonSerializer.Serialize(summary));
    return pass ? 0 : 20;
}
catch (Exception ex)
{
    var failure = new
    {
        schemaVersion = "solqaryn.db-provider-proof.v1",
        task = "N8.20.C",
        status = "PROBE_FAILED",
        branch = "Desarrollo",
        functionalHead,
        capturedAtUtc = capturedAt,
        github = new { runId, runAttempt },
        errorType = ex.GetType().Name,
        guardrails = new
        {
            connectionStringPersisted = false,
            usernamePersisted = false,
            passwordPersisted = false,
            databaseNamePersisted = false,
            fullHostPersisted = false,
            productionTouched = false,
            mainTouched = false,
            secretsExposed = 0
        }
    };

    await File.WriteAllTextAsync(Path.Combine(outputDirectory, "N8.20.C_DB_PROVIDER_EVIDENCE.json"), JsonSerializer.Serialize(failure, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
    Console.Error.WriteLine($"N8.20.C probe failed safely: {ex.GetType().Name}. No connection data was printed.");
    return 22;
}
