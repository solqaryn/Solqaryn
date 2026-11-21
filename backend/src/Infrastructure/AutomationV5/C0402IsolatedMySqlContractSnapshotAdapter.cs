using System.Data;
using System.Data.Common;
using System.Globalization;
using Solqaryn.Application.AutomationV5;
using MySql.Data.MySqlClient;
using Rules = Solqaryn.Domain.AutomationV5.C0402ContractDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0402ContractApplicationService;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C04-02.M03.04: isolated read-only physical MySQL contract snapshot.
/// Requires an explicitly created isolated schema. Never registered in API DI,
/// never a legal policy, contract mutation, payment or production capability.
/// </summary>
public sealed class C0402IsolatedMySqlContractSnapshotAdapter(Func<DbConnection> connectionFactory)
    : IC0402VerifiedContractSnapshotSource
{
    private readonly Func<DbConnection> _factory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private const string Query = """
        SELECT TenantId, ContractId, StateId, Version, StartUtc, EndUtc
          FROM V22C0402ContractSnapshotIsolated
         WHERE TenantId = @tenant AND ContractId = @contract
        """;

    public async ValueTask<Service.VerifiedContractContext?> ReadOwnedAsync(
        int authenticatedTenantId, long contractIdHint, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (authenticatedTenantId <= 0 || contractIdHint <= 0)
            return null;
        try
        {
            await using var connection = _factory();
            if (connection is null || !IsEphemeralContractSchemaConnection(connection)) return null;
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.RepeatableRead, cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = Query;
            var tenant = command.CreateParameter();
            tenant.ParameterName = "@tenant";
            tenant.Value = authenticatedTenantId;
            command.Parameters.Add(tenant);
            var contract = command.CreateParameter();
            contract.ParameterName = "@contract";
            contract.Value = contractIdHint;
            command.Parameters.Add(contract);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;
            var rowTenant = Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
            var rowId = Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture);
            var stateId = Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture);
            var version = Convert.ToInt64(reader.GetValue(3), CultureInfo.InvariantCulture);
            var start = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc);
            DateTime? end = reader.IsDBNull(5) ? null :
                DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc);
            if (rowTenant != authenticatedTenantId || rowId != contractIdHint ||
                version <= 0 || !Enum.IsDefined(typeof(Rules.ContractState), stateId) ||
                (end.HasValue && end.Value <= start) ||
                await reader.ReadAsync(cancellationToken))
                return null;

            await reader.DisposeAsync();
            await transaction.RollbackAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // No verified D01-D08 policy exists in this isolated physical source.
            // Physical rows never authorize commercial effects or grant private read.
            return new Service.VerifiedContractContext(
                new Rules.ContractSnapshot(rowTenant, rowId,
                    (Rules.ContractState)stateId, version, start, end),
                new Rules.CommercialEvidence(false, false, false, false, false, false, false),
                ServerTenantFiltered: true,
                ResourceOwnedByActorTenant: true,
                SnapshotVersionFresh: true,
                PolicyProvenanceVerified: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }
    }

    // This adapter MUST NOT be allowed to read Aiven, the live schema or an
    // arbitrary caller-selected database, even if a future DI registration
    // accidentally supplies a production connection factory.
    private static bool IsEphemeralContractSchemaConnection(DbConnection connection)
    {
        if (connection is not MySqlConnection ||
            connection.State != ConnectionState.Closed)
            return false;
        try
        {
            var settings = new MySqlConnectionStringBuilder(connection.ConnectionString);
            const string prefix = "c0402_m0304_";
            var schema = settings.Database;
            return (settings.Server is "127.0.0.1" or "localhost") &&
                   settings.Port == 3306 &&
                   settings.UserID == "root" &&
                   schema is not null &&
                   schema.Length == prefix.Length + 32 &&
                   schema.StartsWith(prefix, StringComparison.Ordinal) &&
                   schema.Substring(prefix.Length).All(Uri.IsHexDigit);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return false;
        }
    }
}
