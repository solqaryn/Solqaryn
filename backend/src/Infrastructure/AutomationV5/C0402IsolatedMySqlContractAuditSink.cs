using System.Data;
using System.Data.Common;
using MySql.Data.MySqlClient;
using Solqaryn.Application.AutomationV5;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C04-02.M03.06: isolated durable preflight audit only. It is not registered
/// in production DI, cannot reach Aiven, and never authorizes contract writes.
/// </summary>
public sealed class C0402IsolatedMySqlContractAuditSink(Func<DbConnection> factory)
    : IC0402ContractAuditSink
{
    private readonly Func<DbConnection> _factory = factory ??
        throw new ArgumentNullException(nameof(factory));

    public async ValueTask<bool> AppendAsync(
        C0402ContractAuditEntry entry, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (entry is null ||
            entry.EventCode != "C0402_CONTRACT_PREFLIGHT" ||
            !Guid.TryParseExact(entry.EventId, "N", out var eventId) || eventId == Guid.Empty ||
            !IsAllowedOutcome(entry.OutcomeCode))
            return false;

        // The audit outcome is server-controlled; a hostile or buggy caller
        // cannot persist a synthetic positive candidate for a denied request,
        // or disguise a candidate as a denial. Audit data is not authorization.
        const string candidateCode =
            "REAL_AUTHORIZED_ATOMIC_TRANSACTION_AND_POLICY_STILL_REQUIRED";
        if (entry.IsDomainCandidateOnly !=
            string.Equals(entry.OutcomeCode, candidateCode, StringComparison.Ordinal))
            return false;

        try
        {
            await using var db = _factory();
            if (db is not MySqlConnection mysql ||
                mysql.State != ConnectionState.Closed ||
                !IsIsolated(mysql.ConnectionString))
                return false;
            await db.OpenAsync(token);
            await using var tx = await db.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, token);
            await using var cmd = db.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO V23C0402ContractAuditIsolated
                (EventId, EventCode, OutcomeCode, IsDomainCandidateOnly, OccurredUtc)
                VALUES (@id, @event, @outcome, @candidate, @utc)
                """;
            void Add(string name, object value)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = name;
                p.Value = value;
                cmd.Parameters.Add(p);
            }
            Add("@id", entry.EventId);
            Add("@event", entry.EventCode);
            Add("@outcome", entry.OutcomeCode);
            Add("@candidate", entry.IsDomainCandidateOnly);
            Add("@utc", DateTime.UtcNow);
            if (await cmd.ExecuteNonQueryAsync(token) != 1)
                return false;
            await tx.CommitAsync(token);
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never reveal SQL, connection strings, tenant, actor or raw payload.
            return false;
        }
    }

    // Formatting alone does not prevent caller data from entering durable audit.
    // Only the fixed server preflight outcomes are permitted.
    private static bool IsAllowedOutcome(string? code) => code is
        "CLIENT_AUTHORITY_OR_SIDE_EFFECT_DENIED" or
        "CLIENT_CONTRACT_INTENT_INVALID" or
        "CLIENT_TENANT_HINT_MISMATCH" or
        "D01_D02_APPROVAL_REQUIRED" or
        "D04_RENEWAL_POLICY_OR_FUTURE_PERIOD_REQUIRED" or
        "D07_D08_APPROVAL_REQUIRED" or
        "EXPIRY_NOT_SUPPORTED_BY_PERIOD" or
        "INVALID_STATE_TRANSITION" or
        "JWT_RBAC_MEMBERSHIP_DENIED" or
        "LIVE_SERVER_JWT_RBAC_MEMBERSHIP_REQUIRED" or
        "MOROSITY_OR_TIMEOUT_NOT_VERIFIED" or
        "REACTIVATION_RECONCILIATION_REQUIRED" or
        "REAL_AUTHORIZED_ATOMIC_TRANSACTION_AND_POLICY_STILL_REQUIRED" or
        "RETROACTIVE_TRANSITION_DENIED" or
        "SAFE_CAUSE_REQUIRED" or
        "SERVER_ACTOR_SOURCE_UNAVAILABLE" or
        "SERVER_CONTRACT_SOURCE_UNAVAILABLE" or
        "SERVER_PREFLIGHT_UNAVAILABLE" or
        "STALE_OR_INVALID_CONTRACT_VERSION" or
        "TENANT_OR_RESOURCE_OWNERSHIP_DENIED" or
        "TERMINAL_CONTRACT_DENIED" or
        "UNKNOWN_STATE_OR_OPERATION_DENIED" or
        "UTC_PERIOD_EVIDENCE_INVALID" or
        "VERIFIED_OWNED_CONTRACT_VERSION_AND_POLICY_REQUIRED" or
        "VERIFIED_SERVER_CONTEXT_REQUIRED";

    private static bool IsIsolated(string connectionString)
    {
        try
        {
            var info = new MySqlConnectionStringBuilder(connectionString);
            const string prefix = "c0402_m0306_";
            var schema = info.Database;
            return (info.Server is "127.0.0.1" or "localhost") &&
                info.Port == 3306 && info.UserID == "root" &&
                schema is not null && schema.Length == prefix.Length + 32 &&
                schema.StartsWith(prefix, StringComparison.Ordinal) &&
                schema[prefix.Length..].All(Uri.IsHexDigit);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return false;
        }
    }
}
