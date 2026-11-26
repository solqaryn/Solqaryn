using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Infrastructure.AutomationV5;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;

namespace Solqaryn.API.Security;

// API12 session authority only. Never attests case ownership, source SHA or CI.
// Consumers must independently resolve persisted resource/version and revalidate
// this authority before and after I/O and within the persistence boundary.
public sealed class V5CaseFileAuditCorrelationAuthority(
    IHttpContextAccessor accessor, IPermisoService permissions)
{
    public sealed record Session(int ActorId, int BaseRoleId, int TenantId,
        int BranchId, int TenantRoleId, long ExpiresUnix);

    public async Task<Session?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var http = accessor.HttpContext;
        if (http is null) return null;

        var auth = await http.AuthenticateAsync(TenantSessionToken.Scheme);
        cancellationToken.ThrowIfCancellationRequested();
        if (!auth.Succeeded || auth.Ticket?.AuthenticationScheme != TenantSessionToken.Scheme ||
            auth.Principal is null) return null;
        var session = Read(auth.Principal);
        if (session is null || !Matches(http, session)) return null;

        // No administrator, generic Auditoria:Ver or platform fallback.
        var grant = $"{ModuloSistema.Auditoria}:{AccionPermiso.ReadAuditCorrelation}";
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var current = await permissions.GetMisPermisosContextoAsync(
                session.ActorId, session.BaseRoleId, session.TenantId,
                session.BranchId, session.TenantRoleId, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (current?.Permisos is null ||
                !current.Permisos.Contains(grant, StringComparer.Ordinal) ||
                !Matches(http, session)) return null;
        }
        return session;
    }

    public async Task<bool> RevalidateAsync(Session expected,
        CancellationToken cancellationToken = default) =>
        expected is not null && await ResolveAsync(cancellationToken) == expected;

    // Call in the same transaction that locks P01/P02 and writes the audit.
    // This is only session authority; actual resource ownership must still be verified.
    public async Task<Session?> ResolveInCurrentTransactionAsync(AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tx = db.Database.CurrentTransaction;
        if (tx is null) return null;
        var session = await ResolveAsync(cancellationToken);
        if (session is null ||
            !ReferenceEquals(tx, db.Database.CurrentTransaction) ||
            !await V5CaseFileAuditGrantLock.TryAcquireAsync(db, session.ActorId,
                session.BaseRoleId, session.TenantId, session.BranchId,
                session.TenantRoleId, cancellationToken))
            return null;
        // Repeat the signed-session/context checks while the relational locks are held.
        if (!await RevalidateAsync(session, cancellationToken) ||
            !ReferenceEquals(tx, db.Database.CurrentTransaction) ||
            db.AuthorizedCommercialEmpresaId != session.TenantId)
            return null;
        return session;
    }

    // Internal orchestration boundary, not an HTTP endpoint or an ownership writer.
    // The caller supplies a dedicated context and a server-owned allowlisted resource
    // resolver. The operation correlation is generated server-side, not the target
    // correlation being queried. A receipt ID is internal persistence evidence only.
    public sealed record ReceiptResult(string Code, int? ReceiptId = null)
    {
        public long? TargetVersion { get; init; }
    }

    public Task<ReceiptResult> AppendAuthorizedReceiptAsync(AppDbContext db, int caseId,
        Guid operationCorrelation,
        Func<AppDbContext, V5CaseFileOwnershipStore.Candidate, CancellationToken, ValueTask<bool>> verifyActualResource,
        CancellationToken ct = default) => ExecuteReceiptAsync(db, caseId, operationCorrelation, null, verifyActualResource, ct);

    // Internal P12 consumer for already case-bound receipts only, not a generic
    // audit search or a route. The operation GUID must be issued server-side.
    public Task<ReceiptResult> ReadAuthorizedReceiptCorrelationAsync(AppDbContext db, int caseId,
        Guid targetCorrelation, Guid operationCorrelation,
        Func<AppDbContext, V5CaseFileOwnershipStore.Candidate, CancellationToken, ValueTask<bool>> verifyActualResource,
        CancellationToken ct = default) => ExecuteReceiptAsync(db, caseId, operationCorrelation, targetCorrelation, verifyActualResource, ct);

    private async Task<ReceiptResult> ExecuteReceiptAsync(AppDbContext db, int caseId,
        Guid operationCorrelation, Guid? targetCorrelation,
        Func<AppDbContext, V5CaseFileOwnershipStore.Candidate, CancellationToken, ValueTask<bool>> verifyActualResource,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);
        ct.ThrowIfCancellationRequested();
        if (caseId <= 0 || operationCorrelation == Guid.Empty || targetCorrelation == Guid.Empty ||
            targetCorrelation == operationCorrelation || verifyActualResource is null ||
            !db.Database.IsRelational() || db.Database.CurrentTransaction is not null ||
            db.ChangeTracker.HasChanges())
            return new("CASEFILE_AUDIT_DENIED");

        var transactionStarted = false;
        var commitStarted = false;
        var committed = false;
        try
        {
            var expected = await ResolveAsync(ct);
            if (expected is null || db.AuthorizedCommercialEmpresaId != expected.TenantId)
                return new("CASEFILE_AUDIT_DENIED");

            await using var tx = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, ct);
            transactionStarted = true;
            async Task<ReceiptResult> DenyAsync()
            {
                await tx.RollbackAsync(CancellationToken.None);
                return new("CASEFILE_AUDIT_DENIED");
            }

            if (await ResolveInCurrentTransactionAsync(db, ct) != expected)
                return await DenyAsync();
            var candidate = await V5CaseFileOwnershipStore.ReadCandidateInCurrentTransactionAsync(
                db, expected.TenantId, caseId, expected.BranchId, ct);
            if (candidate is null)
                return await DenyAsync();

            long? targetVersion = null;
            if (targetCorrelation is Guid target)
            {
                if (!await verifyActualResource(db, candidate, ct) ||
                    await ResolveInCurrentTransactionAsync(db, ct) != expected) return await DenyAsync();
                targetVersion = await V5CaseFileAuditReceiptPersistence.ReadReceiptVersionInCurrentTransactionAsync(db, candidate, target, ct);
                if (targetVersion is null) return await DenyAsync();
            }

            var receipt = await V5CaseFileAuditReceiptPersistence.AppendEnterpriseInCurrentTransactionAsync(
                db, candidate, expected.ActorId, expected.BaseRoleId, expected.TenantRoleId,
                operationCorrelation, verifyActualResource, ct, targetCorrelation);
            // Relational locks protect the grant and source; recheck the signed
            // session after all I/O as its expiry/context can change independently.
            if (receipt is null || await ResolveInCurrentTransactionAsync(db, ct) != expected ||
                !ReferenceEquals(tx, db.Database.CurrentTransaction) ||
                db.AuthorizedCommercialEmpresaId != expected.TenantId ||
                db.ChangeTracker.HasChanges() ||
                expected.ExpiresUnix <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                return await DenyAsync();

            if (targetCorrelation is Guid linked &&
                (await V5CaseFileAuditReceiptPersistence.ReadReceiptVersionInCurrentTransactionAsync(db, candidate, linked, ct) != targetVersion ||
                 await ResolveInCurrentTransactionAsync(db, ct) != expected)) return await DenyAsync();
            ct.ThrowIfCancellationRequested();
            commitStarted = true;
            await tx.CommitAsync(ct);
            committed = true;
            return new(targetCorrelation.HasValue ? "CASEFILE_AUDIT_CORRELATION_READ" : "CASEFILE_AUDIT_RECEIPT_WRITTEN", receipt)
                { TargetVersion = targetVersion };
        }
        catch (OperationCanceledException) when (!commitStarted) { throw; }
        catch (Exception)
        {
            // A commit error is not proof of rollback. Reconcile the server-issued
            // operation correlation before considering any recovery; never retry here.
            return new(commitStarted ? "CASEFILE_AUDIT_COMMIT_UNCERTAIN" : "CASEFILE_AUDIT_FAILED");
        }
        finally
        {
            // Only a dedicated context may be passed. Do not leave an EF receipt
            // tracked after rollback or an uncertain result for a later SaveChanges.
            if (transactionStarted && !committed)
                db.ChangeTracker.Clear();
        }
    }

    private bool Matches(HttpContext http, Session session) =>
        ReferenceEquals(accessor.HttpContext, http) &&
        Read(http.User) == session &&
        session.ExpiresUnix > DateTimeOffset.UtcNow.ToUnixTimeSeconds() &&
        HeaderMatches(http, "X-Empresa-Id", session.TenantId) &&
        HeaderMatches(http, "X-Sucursal-Id", session.BranchId);

    private static Session? Read(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true ||
            !Positive(principal, "id", out var actor) ||
            !Positive(principal, "rolId", out var baseRole) ||
            !Positive(principal, "tenantId", out var tenant) ||
            !Positive(principal, "branchId", out var branch) ||
            !Positive(principal, "tenantRoleId", out var role) ||
            !PositiveLong(principal, "exp", out var expiry)) return null;
        return new Session(actor, baseRole, tenant, branch, role, expiry);
    }

    private static bool Positive(ClaimsPrincipal principal, string name, out int value)
    {
        value = 0;
        var claims = principal.FindAll(name).ToArray();
        return claims.Length == 1 &&
            int.TryParse(claims[0].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out value) && value > 0 &&
            claims[0].Value == value.ToString(CultureInfo.InvariantCulture);
    }

    private static bool PositiveLong(ClaimsPrincipal principal, string name, out long value)
    {
        value = 0;
        var claims = principal.FindAll(name).ToArray();
        return claims.Length == 1 &&
            long.TryParse(claims[0].Value, NumberStyles.None,
                CultureInfo.InvariantCulture, out value) && value > 0 &&
            claims[0].Value == value.ToString(CultureInfo.InvariantCulture);
    }

    private static bool HeaderMatches(HttpContext http, string name, int id) =>
        !http.Request.Headers.TryGetValue(name, out var values) ||
        (values.Count == 1 && string.Equals(values[0],
            id.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
}
