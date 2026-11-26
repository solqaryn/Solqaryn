using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Persistent P01/P02 primitive. Not a public endpoint, grant or registered DI service.
/// The resource resolver must establish actual ownership and a separate write grant
/// inside the same transaction; API12 is read-only and cannot authorize this writer.
/// </summary>
public sealed class V5CaseFileOwnershipStore
{
    public sealed record Selection(int CaseId, string ResourceType, string ResourceRef,
        long? ExpectedVersion, string ContentHash, string ReasonCode);
    public sealed record WriteAuthority(int ActorId, int TenantId, int BranchId,
        int CaseId, string ResourceType, string ResourceRef);
    public sealed record Candidate(int TenantId, int CaseId, int BranchId,
        string ResourceType, string ResourceRef, long Version);
    public sealed record Result(string Code, long? Version = null);

    public interface IWriteAuthority
    {
        // Server-side only: resolve active membership, exact write operation, allowed
        // resource type and persistent resource/branch ownership; lock authoritative rows
        // in db.Database.CurrentTransaction. Never derive authority from Selection.
        ValueTask<WriteAuthority?> ResolveAsync(AppDbContext db, Selection selection,
            CancellationToken cancellationToken);
    }

    private readonly AppDbContext _db;
    private readonly IWriteAuthority _authority;
    private static readonly Regex Token = new(@"\A[A-Za-z0-9_.:-]+\z", RegexOptions.CultureInvariant);
    private static readonly Regex Hash = new(@"\A[0-9a-f]{64}\z", RegexOptions.CultureInvariant);

    public V5CaseFileOwnershipStore(AppDbContext db, IWriteAuthority authority)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _authority = authority ?? throw new ArgumentNullException(nameof(authority));
    }

    private static bool ValidToken(string? value, int max) =>
        value is not null && value.Length > 0 && value.Length <= max && Token.IsMatch(value);

    private static bool Valid(Selection? s) => s is not null && s.CaseId > 0 &&
        (s.ExpectedVersion is null or > 0) && ValidToken(s.ResourceType, 64) &&
        ValidToken(s.ResourceRef, 128) && ValidToken(s.ReasonCode, 64) &&
        s.ContentHash is { Length: 64 } && Hash.IsMatch(s.ContentHash);

    private bool Matches(WriteAuthority? a, Selection s, int tenant) => a is not null &&
        a.ActorId > 0 && a.TenantId == tenant && a.BranchId > 0 &&
        a.CaseId == s.CaseId && a.ResourceType == s.ResourceType &&
        a.ResourceRef == s.ResourceRef && _db.AuthorizedCommercialEmpresaId == tenant;

    private DbCommand Command(IDbContextTransaction tx, string sql,
        params (string Name, object Value)[] parameters)
    {
        var cmd = _db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = tx.GetDbTransaction();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
        return cmd;
    }

    private async Task<bool> ActiveBranchAsync(IDbContextTransaction tx, int tenant,
        int branch, CancellationToken ct)
    {
        await using var cmd = Command(tx, """
            SELECT s.Id FROM Sucursales s INNER JOIN Empresas e ON e.Id=s.EmpresaId
            WHERE s.EmpresaId=@tenant AND s.Id=@branch AND s.Activa=1
              AND s.Eliminado=0 AND e.Activa=1 FOR SHARE
            """, ("@tenant", tenant), ("@branch", branch));
        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    public async Task<Result> AppendAsync(Selection selection, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!Valid(selection) || _db.AuthorizedCommercialEmpresaId is not int tenant ||
            tenant <= 0 || _db.Database.CurrentTransaction is not null)
            return new("CASEFILE_WRITE_DENIED");
        var commitStarted = false;
        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            var a = await _authority.ResolveAsync(_db, selection, ct);
            ct.ThrowIfCancellationRequested();
            if (a is null || !Matches(a, selection, tenant) ||
                !await ActiveBranchAsync(tx, tenant, a.BranchId, ct))
                return new("CASEFILE_WRITE_DENIED");

            bool exists;
            await using (var cmd = Command(tx, """
                SELECT SucursalId, RecursoTipo, RecursoRef FROM V5CaseFileOwnerships
                WHERE EmpresaId=@tenant AND Id=@case FOR UPDATE
                """, ("@tenant", tenant), ("@case", selection.CaseId)))
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
            {
                exists = await reader.ReadAsync(ct);
                if (exists && (reader.GetInt32(0) != a.BranchId ||
                    reader.GetString(1) != selection.ResourceType ||
                    reader.GetString(2) != selection.ResourceRef))
                    return new("CASEFILE_WRITE_DENIED");
            }

            long next;
            if (!exists)
            {
                if (selection.ExpectedVersion is not null) return new("CASEFILE_VERSION_CONFLICT");
                await using var insert = Command(tx, """
                    INSERT INTO V5CaseFileOwnerships (EmpresaId,Id,SucursalId,RecursoTipo,RecursoRef)
                    VALUES (@tenant,@case,@branch,@type,@ref)
                    """, ("@tenant", tenant), ("@case", selection.CaseId),
                    ("@branch", a.BranchId), ("@type", selection.ResourceType),
                    ("@ref", selection.ResourceRef));
                if (await insert.ExecuteNonQueryAsync(ct) != 1)
                    return new("CASEFILE_WRITE_FAILED");
                next = 1;
            }
            else
            {
                await using var version = Command(tx, """
                    SELECT MAX(Version) FROM V5CaseFileVersions
                    WHERE EmpresaId=@tenant AND ExpedienteId=@case
                    """, ("@tenant", tenant), ("@case", selection.CaseId));
                var value = await version.ExecuteScalarAsync(ct);
                if (value is null or DBNull) return new("CASEFILE_WRITE_DENIED");
                var current = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                if (current <= 0 || current == long.MaxValue ||
                    selection.ExpectedVersion != current)
                    return new("CASEFILE_VERSION_CONFLICT");
                next = current + 1;
            }

            await using (var write = Command(tx, """
                INSERT INTO V5CaseFileVersions
                    (EmpresaId,ExpedienteId,Version,ActorId,ContentHash,ReasonCode,FechaUtc)
                VALUES (@tenant,@case,@version,@actor,@hash,@reason,UTC_TIMESTAMP(6))
                """, ("@tenant", tenant), ("@case", selection.CaseId), ("@version", next),
                ("@actor", a.ActorId), ("@hash", selection.ContentHash), ("@reason", selection.ReasonCode)))
            {
                if (await write.ExecuteNonQueryAsync(ct) != 1) return new("CASEFILE_WRITE_FAILED");
            }
            ct.ThrowIfCancellationRequested();

            // Read back the exact joined P01/P02 receipt before authorization recheck.
            await using (var receipt = Command(tx, """
                SELECT COUNT(*) FROM V5CaseFileOwnerships o
                INNER JOIN V5CaseFileVersions v ON v.EmpresaId=o.EmpresaId AND v.ExpedienteId=o.Id
                WHERE o.EmpresaId=@tenant AND o.Id=@case AND o.SucursalId=@branch
                  AND o.RecursoTipo=@type AND o.RecursoRef=@ref AND v.Version=@version
                  AND v.ActorId=@actor AND v.ContentHash=@hash AND v.ReasonCode=@reason
                """, ("@tenant", tenant), ("@case", selection.CaseId), ("@branch", a.BranchId),
                ("@type", selection.ResourceType), ("@ref", selection.ResourceRef),
                ("@version", next), ("@actor", a.ActorId),
                ("@hash", selection.ContentHash), ("@reason", selection.ReasonCode)))
            {
                if (Convert.ToInt64(await receipt.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture) != 1)
                    return new("CASEFILE_WRITE_FAILED");
            }
            ct.ThrowIfCancellationRequested();

            // Re-resolve the independent write authority after I/O. A changed grant,
            // resource or actor rolls back both P01 and P02. Never retry a commit blindly.
            var latest = await _authority.ResolveAsync(_db, selection, ct);
            if (latest != a || !Matches(latest, selection, tenant) ||
                !await ActiveBranchAsync(tx, tenant, a.BranchId, ct))
                return new("CASEFILE_WRITE_DENIED");
            ct.ThrowIfCancellationRequested();
            if (_db.AuthorizedCommercialEmpresaId != tenant)
                return new("CASEFILE_WRITE_DENIED");
            commitStarted = true;
            await tx.CommitAsync(ct);
            return new("CASEFILE_VERSION_WRITTEN", next);
        }
        catch (OperationCanceledException) when (!commitStarted) { throw; }
        catch (Exception)
        {
            // No SQL exception details, resource identifiers or PII returned to callers.
            return new(commitStarted ? "CASEFILE_COMMIT_UNCERTAIN" : "CASEFILE_WRITE_FAILED");
        }
    }

    // Internal storage candidate, NOT sufficient to authorize API12 or attest source SHA.
    public async Task<Candidate?> ReadCandidateAsync(int tenant, int caseId, int branch,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (tenant <= 0 || caseId <= 0 || branch <= 0 ||
            _db.AuthorizedCommercialEmpresaId != tenant || _db.Database.CurrentTransaction is not null)
            return null;
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        if (!await ActiveBranchAsync(tx, tenant, branch, ct)) return null;
        await using var cmd = Command(tx, """
            SELECT o.RecursoTipo,o.RecursoRef,v.Version FROM V5CaseFileOwnerships o
            INNER JOIN V5CaseFileVersions v ON v.EmpresaId=o.EmpresaId AND v.ExpedienteId=o.Id
            WHERE o.EmpresaId=@tenant AND o.Id=@case AND o.SucursalId=@branch
            ORDER BY v.Version DESC LIMIT 1
            """, ("@tenant", tenant), ("@case", caseId), ("@branch", branch));
        Candidate? result;
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            result = await reader.ReadAsync(ct)
                ? new(tenant, caseId, branch, reader.GetString(0), reader.GetString(1), reader.GetInt64(2))
                : null;
        }
        ct.ThrowIfCancellationRequested();
        if (_db.AuthorizedCommercialEmpresaId != tenant) return null;
        await tx.CommitAsync(ct);
        return result;
    }
}
