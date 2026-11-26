using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.AutomationV5;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Security;

/// <summary>
/// Signed tenant-session consumer of the existing transactional P01/P02 and
/// P12/API12 primitives. The first supported resource is Almacen, not a client
/// supplied tenant/type/owner. No automatic role grants or deletion policy.
/// </summary>
public sealed class V5CaseFileEnterpriseService(AppDbContext db,
    V5CaseFileAuditCorrelationAuthority authority)
{
    public async Task<V5CaseFileAuditCorrelationAuthority.Session?> BindTenantAsync(
        HttpContext http, AccionPermiso operation, CancellationToken ct)
    {
        var session = await authority.ResolveForOperationAsync(operation, ct);
        if (session is null ||
            (VerifiedCommercialTenant.Resolve(http) is int previous && previous != session.TenantId))
            return null;
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = session.TenantId;
        return db.AuthorizedCommercialEmpresaId == session.TenantId ? session : null;
    }

    public async Task<V5CaseFileOwnershipStore.Result> AppendVersionAsync(HttpContext http,
        int caseId, int warehouseId, long? expectedVersion, CancellationToken ct)
    {
        if (caseId <= 0 || warehouseId <= 0 || expectedVersion is <= 0)
            return new("CASEFILE_WRITE_DENIED");
        var session = await BindTenantAsync(http, AccionPermiso.WriteCaseFileVersion, ct);
        if (session is null) return new("CASEFILE_WRITE_DENIED");

        // P02 describes the versioned resource binding, not a warehouse snapshot
        // or an arbitrary client digest. The canonical descriptor is server-owned.
        var resourceRef = warehouseId.ToString(CultureInfo.InvariantCulture);
        var descriptor = FormattableString.Invariant(
            $"warehouse-case:v1:{session.TenantId}:{session.BranchId}:{caseId}:{warehouseId}:{expectedVersion ?? 0}");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(descriptor))).ToLowerInvariant();
        var selection = new V5CaseFileOwnershipStore.Selection(caseId,
            V5CaseFileWarehouseResource.ResourceType, resourceRef, expectedVersion, hash,
            expectedVersion.HasValue ? "append" : "create");
        var store = new V5CaseFileOwnershipStore(db, new WriteAuthority(authority, session));
        return await store.AppendAsync(selection, ct);
    }

    public sealed record CorrelationResult(string Code, Guid? Correlation = null, long? TargetVersion = null);

    public async Task<CorrelationResult> RecordOrReadAsync(HttpContext http, int caseId,
        Guid? target, CancellationToken ct)
    {
        if (caseId <= 0 || target == Guid.Empty) return new("CASEFILE_AUDIT_DENIED");
        var session = await BindTenantAsync(http, AccionPermiso.ReadAuditCorrelation, ct);
        if (session is null) return new("CASEFILE_AUDIT_DENIED");
        var operation = Guid.NewGuid();
        async ValueTask<bool> VerifyResource(AppDbContext context,
            V5CaseFileOwnershipStore.Candidate candidate, CancellationToken token)
        {
            if (!ReferenceEquals(context, db) || candidate.TenantId != session.TenantId ||
                candidate.BranchId != session.BranchId || candidate.CaseId != caseId ||
                await authority.ResolveAsync(token) != session)
                return false;
            return await V5CaseFileWarehouseResource.TryLockAsync(context,
                session.TenantId, session.BranchId, session.TenantRoleId,
                candidate.ResourceType, candidate.ResourceRef, token) &&
                await authority.ResolveAsync(token) == session;
        }

        var result = target is Guid linked
            ? await authority.ReadAuthorizedReceiptCorrelationAsync(db, caseId, linked, operation, VerifyResource, ct)
            : await authority.AppendAuthorizedReceiptAsync(db, caseId, operation, VerifyResource, ct);
        // The internal numeric audit receipt ID is deliberately never exposed.
        return new(result.Code, result.ReceiptId.HasValue || result.Code == "CASEFILE_AUDIT_COMMIT_UNCERTAIN"
            ? operation : null, result.TargetVersion);
    }

    private sealed class WriteAuthority(V5CaseFileAuditCorrelationAuthority authority,
        V5CaseFileAuditCorrelationAuthority.Session expected) : V5CaseFileOwnershipStore.IWriteAuthority
    {
        public async ValueTask<V5CaseFileOwnershipStore.WriteAuthority?> ResolveAsync(
            AppDbContext context, V5CaseFileOwnershipStore.Selection selection, CancellationToken ct)
        {
            var session = await authority.ResolveForOperationAsync(AccionPermiso.WriteCaseFileVersion, ct);
            if (session != expected || context.AuthorizedCommercialEmpresaId != expected.TenantId ||
                !await V5CaseFileAuditGrantLock.TryAcquireWriteAsync(context,
                    expected.ActorId, expected.BaseRoleId, expected.TenantId, expected.BranchId,
                    expected.TenantRoleId, ct) ||
                !await V5CaseFileWarehouseResource.TryLockAsync(context, expected.TenantId,
                    expected.BranchId, expected.TenantRoleId, selection.ResourceType, selection.ResourceRef, ct) ||
                await authority.ResolveForOperationAsync(AccionPermiso.WriteCaseFileVersion, ct) != expected)
                return null;
            return new(expected.ActorId, expected.TenantId, expected.BranchId,
                selection.CaseId, selection.ResourceType, selection.ResourceRef);
        }
    }
}
