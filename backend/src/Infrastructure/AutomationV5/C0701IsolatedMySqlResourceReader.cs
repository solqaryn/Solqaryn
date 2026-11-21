using System.Data;
using System.Data.Common;
using System.Globalization;
using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.C0701CatalogDomainRules;
using Service = Solqaryn.Application.AutomationV5.C0701CatalogApplicationService;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// C07-01.M03.04: isolated MySQL read-only catalog adapter, not production DI.
/// Only a caller-provided connection to a deliberately provisioned table.
/// This does not grant JWT/RBAC, perform mutations, or migrate production.
/// </summary>
public sealed class C0701IsolatedMySqlResourceReader(Func<DbConnection> connectionFactory)
    : IC0701VerifiedCatalogResourceReader
{
    private readonly Func<DbConnection> _connections =
        connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

    private const string Sql = """
        SELECT TenantId, ProductoId, Version, SucursalId,
               CategoriaEmpresaId, VarianteEmpresaId, Activo, Eliminado
          FROM V22C0701CatalogSnapshotIsolated
         WHERE TenantId = @tenant AND ProductoId = @product
        """;

    public async ValueTask<Service.VerifiedResource?> ReadOwnedAsync(
        int authenticatedTenantId, int productoId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (authenticatedTenantId <= 0 || productoId <= 0) return null;
        try
        {
            await using var db = _connections();
            if (db is null) return null;
            await db.OpenAsync(cancellationToken);
            await using var transaction = await db.BeginTransactionAsync(
                IsolationLevel.RepeatableRead, cancellationToken);
            await using var cmd = db.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = Sql;
            var tenant = cmd.CreateParameter();
            tenant.ParameterName = "@tenant";
            tenant.Value = authenticatedTenantId;
            cmd.Parameters.Add(tenant);
            var product = cmd.CreateParameter();
            product.ParameterName = "@product";
            product.Value = productoId;
            cmd.Parameters.Add(product);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;

            static int Int(DbDataReader r, int i) =>
                Convert.ToInt32(r.GetValue(i), CultureInfo.InvariantCulture);
            static long Long(DbDataReader r, int i) =>
                Convert.ToInt64(r.GetValue(i), CultureInfo.InvariantCulture);
            static int? NullableInt(DbDataReader r, int i) =>
                r.IsDBNull(i) ? null : Int(r, i);

            var tenantId = NullableInt(reader, 0);
            var productId = Int(reader, 1);
            var version = Long(reader, 2);
            var branch = NullableInt(reader, 3);
            var category = NullableInt(reader, 4);
            var variant = NullableInt(reader, 5);
            var active = Int(reader, 6) == 1;
            var deleted = Int(reader, 7) == 1;
            // NULL legacy ownership, stale or ambiguous versions, foreign
            // relations and soft deletion must never authorize a read.
            if (tenantId != authenticatedTenantId || productId != productoId ||
                version <= 0 || !active || deleted || branch is <= 0 ||
                (category.HasValue && category != authenticatedTenantId) ||
                (variant.HasValue && variant != authenticatedTenantId) ||
                await reader.ReadAsync(cancellationToken))
                return null;

            var snapshot = new Rules.ProductSnapshot(
                tenantId, productId, version, branch, category, variant, active, deleted);
            await reader.DisposeAsync();
            await transaction.RollbackAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            // Requires independent signed JWT, fresh membership/RBAC and
            // revalidation of version/ownership inside a REAL write transaction.
            return new Service.VerifiedResource(snapshot, true, true);
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
}
