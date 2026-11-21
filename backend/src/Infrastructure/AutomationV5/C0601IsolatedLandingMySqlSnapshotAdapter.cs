using System.Data;
using System.Data.Common;
using System.Globalization;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C06-01.M03.04: read-only, isolated draft metadata snapshot. The tenant must
/// already be validated by the server; this adapter never supplies authorization.
/// Not registered in API DI and never connected to a product database.
/// </summary>
public sealed class C0601IsolatedLandingMySqlSnapshotAdapter(Func<DbConnection> connectionFactory)
{
    private readonly Func<DbConnection> _factory =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private const string Query = """
        SELECT TenantId, PageSlug, Revision, DraftState, EditedUtc
          FROM V22C0601LandingDraftIsolated
         WHERE TenantId = @tenant AND PageSlug = @slug
         LIMIT 2
        """;

    public sealed record DraftSnapshot(
        int TenantId, string PageSlug, long Revision, int DraftState, DateTime EditedUtc)
    {
        public bool AuthorizesPublication => false;
        public bool GrantsPrivateOrCrossTenantRead => false;
        public bool EstablishesLegalOrSeoApproval => false;
        public bool PermitsBusinessMutation => false;
    }

    public async ValueTask<DraftSnapshot?> ReadDraftAsync(
        int serverVerifiedTenantId, string? pageSlug, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (serverVerifiedTenantId <= 0 || !IsCanonicalSlug(pageSlug))
            return null;

        try
        {
            await using var connection = _factory();
            if (connection is null) return null;
            await connection.OpenAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.RepeatableRead, cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = Query;

            var tenant = command.CreateParameter();
            tenant.ParameterName = "@tenant";
            tenant.Value = serverVerifiedTenantId;
            command.Parameters.Add(tenant);

            var slug = command.CreateParameter();
            slug.ParameterName = "@slug";
            slug.Value = pageSlug!;
            command.Parameters.Add(slug);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var rowTenant = Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture);
            var rowSlug = reader.GetString(1);
            var revision = Convert.ToInt64(reader.GetValue(2), CultureInfo.InvariantCulture);
            var state = Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture);
            var edited = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc);
            if (rowTenant != serverVerifiedTenantId ||
                !string.Equals(rowSlug, pageSlug, StringComparison.Ordinal) ||
                revision <= 0 || (state != 0 && state != 1) ||
                await reader.ReadAsync(cancellationToken))
                return null;

            cancellationToken.ThrowIfCancellationRequested();
            await reader.DisposeAsync();
            await transaction.RollbackAsync(cancellationToken);

            return new DraftSnapshot(rowTenant, rowSlug, revision, state, edited);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // No SQL, tenant, host, or draft detail may leak into the caller.
            return null;
        }
    }

    private static bool IsCanonicalSlug(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 63 ||
            value[0] == '-' || value[^1] == '-')
            return false;
        foreach (var ch in value)
        {
            if (!((ch >= 'a' && ch <= 'z') ||
                  (ch >= '0' && ch <= '9') || ch == '-'))
                return false;
        }
        return true;
    }
}
