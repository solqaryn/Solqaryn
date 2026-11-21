using System.Data;
using System.Data.Common;
using System.Globalization;
using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.C0401PlanDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0401PlanApplicationService;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C04-01.M03.04 isolated, read-only MySQL adapter for one versioned plan/capacity.
/// Deliberately NOT registered in production DI. Its isolated table exists only
/// where an authorized caller explicitly provisions the versioned snapshot schema.
/// It does not reserve quota, grant RBAC, charge, migrate or modify production data.
/// </summary>
public sealed class C0401IsolatedMySqlSnapshotSource(Func<DbConnection> openConnection)
    : IC0401VerifiedPlanSnapshotSource
{
    private readonly Func<DbConnection> _connections =
        openConnection ?? throw new ArgumentNullException(nameof(openConnection));

    private const string ReadSnapshotSql = """
        SELECT TenantId, PlanId, Codigo, Revision, PlanActivo,
               PlanDesdeUtc, PlanHastaUtc, SubEmpresaId, SubVersion,
               SubActiva, SubInicioUtc, SubFinUtc, CapacidadClave,
               CapacidadMax, Ilimitado, Consumo, PlanHabilitada,
               RubroHabilitada, RolHabilitada, TenantHabilitada
          FROM V22C0401SnapshotIsolated
         WHERE TenantId = @tenant AND PlanId = @plan
        """;

    private static int Int(DbDataReader r, int i) =>
        Convert.ToInt32(r.GetValue(i), CultureInfo.InvariantCulture);
    private static long Long(DbDataReader r, int i) =>
        Convert.ToInt64(r.GetValue(i), CultureInfo.InvariantCulture);
    private static bool Flag(DbDataReader r, int i) => Int(r, i) == 1;
    private static DateTime Utc(DbDataReader r, int i) =>
        DateTime.SpecifyKind(r.GetDateTime(i), DateTimeKind.Utc);
    private static DateTime? NullableUtc(DbDataReader r, int i) =>
        r.IsDBNull(i) ? null : Utc(r, i);

    public async ValueTask<Service.ServerPlanContext?> ReadForTenantAsync(
        int authenticatedTenantId, int planIdHint, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (authenticatedTenantId <= 0 || planIdHint <= 0)
            return null;

        try
        {
            await using var connection = _connections();
            if (connection is null) return null;
            await connection.OpenAsync(cancellationToken);
            // Repeatable-read means plan, membership and quota snapshot are
            // mutually consistent during this ONE read, never an authorization
            // to commit a consumption or to rely on the snapshot for payment.
            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.RepeatableRead, cancellationToken);
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = ReadSnapshotSql;
            var tenant = command.CreateParameter();
            tenant.ParameterName = "@tenant";
            tenant.Value = authenticatedTenantId;
            command.Parameters.Add(tenant);
            var plan = command.CreateParameter();
            plan.ParameterName = "@plan";
            plan.Value = planIdHint;
            command.Parameters.Add(plan);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            // The isolated contract currently represents ONE capacity per
            // tenant/plan. Any ambiguity must fail closed, not choose LIMIT 1.
            if (Int(reader, 0) != authenticatedTenantId ||
                Int(reader, 1) != planIdHint ||
                Int(reader, 7) != authenticatedTenantId)
                return null;

            var planRow = new Rules.PlanSnapshot(
                Int(reader, 1), reader.GetString(2), Long(reader, 3),
                Flag(reader, 4), Utc(reader, 5), NullableUtc(reader, 6));
            var subscription = new Rules.SuscripcionSnapshot(
                Int(reader, 7), Int(reader, 1), Long(reader, 8),
                Flag(reader, 9), Utc(reader, 10), NullableUtc(reader, 11));
            var capacity = new Rules.CapacidadSnapshot(
                reader.GetString(12),
                reader.IsDBNull(13) ? null : Long(reader, 13),
                Flag(reader, 14), Long(reader, 15),
                Flag(reader, 16), Flag(reader, 17),
                Flag(reader, 18), Flag(reader, 19));
            if (await reader.ReadAsync(cancellationToken))
                return null;
            await reader.DisposeAsync();
            await transaction.RollbackAsync(cancellationToken);

            // The caller MUST still run Domain and authenticated RBAC checks,
            // then revalidate all limits in a separate authorized transaction.
            return new Service.ServerPlanContext(
                planRow, subscription, capacity,
                ConsultaFiltradaPorTenant: true,
                CatalogoVerificadoEnServidor: true,
                VersionesFrescas: planRow.Revision > 0 && subscription.Version > 0);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Schema missing, connection errors, duplicate/inconsistent data:
            // never infer a positive plan snapshot or reveal SQL internals.
            return null;
        }
    }
}
