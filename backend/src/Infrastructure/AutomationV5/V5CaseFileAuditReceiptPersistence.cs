using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>
/// Durable audit receipt primitive. It does not authenticate a request or authorize
/// a commercial resource by itself. The caller owns commit/rollback.
/// </summary>
public static class V5CaseFileAuditReceiptPersistence
{
    public static async Task<bool> AppendAndVerifyAsync(AppDbContext db,
        RegistroAuditoria expected, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (expected.Id != 0 ||
            (db.Database.IsRelational() && db.Database.CurrentTransaction is null))
            return false;
        // Oracle MySQL stores Fecha as datetime(6). Normalize before INSERT so
        // the durable readback can distinguish timestamp tampering from precision loss.
        expected.Fecha = expected.Fecha.AddTicks(-(expected.Fecha.Ticks % TimeSpan.TicksPerMicrosecond));
        db.Set<RegistroAuditoria>().Add(expected);
        if (await db.SaveChangesAsync(cancellationToken) != 1 || expected.Id <= 0)
            return false;

        var actual = await db.Set<RegistroAuditoria>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == expected.Id, cancellationToken);
        if (actual is null ||
            actual.Fecha != expected.Fecha ||
            actual.UsuarioId != expected.UsuarioId ||
            actual.NombreUsuario != expected.NombreUsuario ||
            actual.EmpresaId != expected.EmpresaId ||
            actual.AmbitoAutoridad != expected.AmbitoAutoridad ||
            actual.OrigenAutorizacion != expected.OrigenAutorizacion ||
            actual.Modulo != expected.Modulo || actual.Accion != expected.Accion ||
            actual.Entidad != expected.Entidad || actual.ReferenciaId != expected.ReferenciaId ||
            actual.Descripcion != expected.Descripcion ||
            actual.CorrelationId != expected.CorrelationId ||
            actual.Resultado != expected.Resultado || actual.Error != expected.Error ||
            actual.Motivo != expected.Motivo || actual.Ip != expected.Ip ||
            actual.UserAgent != expected.UserAgent ||
            actual.ValoresAnteriores != expected.ValoresAnteriores ||
            actual.ValoresNuevos is null || expected.ValoresNuevos is null)
            return false;
        try
        {
            // MySQL JSON may normalize property ordering and whitespace.
            using var actualJson = JsonDocument.Parse(actual.ValoresNuevos);
            using var expectedJson = JsonDocument.Parse(expected.ValoresNuevos);
            if (!JsonElement.DeepEquals(actualJson.RootElement, expectedJson.RootElement))
                return false;
        }
        catch (JsonException)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return true;
    }

    // Narrow P12 source: only durable API12 receipts already bound to the exact
    // case/tenant/branch. The authenticated caller must hold grant, P01/P02 and
    // actual-resource locks. Do not return raw audit JSON, IDs or actor details.
    public static async Task<long?> ReadReceiptVersionInCurrentTransactionAsync(
        AppDbContext db, V5CaseFileOwnershipStore.Candidate expected, Guid target,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var tx = db.Database.CurrentTransaction;
        if (tx is null || !db.Database.IsRelational() || expected is null || target == Guid.Empty ||
            expected.TenantId <= 0 || expected.CaseId <= 0 || expected.BranchId <= 0 || expected.Version <= 0 ||
            db.AuthorizedCommercialEmpresaId != expected.TenantId || db.ChangeTracker.HasChanges())
            return null;
        var canonical = target.ToString("N");
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = tx.GetDbTransaction();
        cmd.CommandText = """
            SELECT CorrelationId, ValoresNuevos FROM RegistrosAuditoria
            WHERE EmpresaId=@tenant AND ReferenciaId=@case AND AmbitoAutoridad=@scope AND OrigenAutorizacion=@origin
              AND CAST(Modulo AS BINARY)=CAST(@module AS BINARY)
              AND CAST(Accion AS BINARY)=CAST(@action AS BINARY)
              AND CAST(Entidad AS BINARY)=CAST(@entity AS BINARY)
              AND CAST(Resultado AS BINARY)=CAST(@result AS BINARY)
              AND CorrelationId=@target LIMIT 2 FOR SHARE
            """;
        foreach (var (name, value) in new (string, object)[]
        {
            ("@tenant", expected.TenantId), ("@case", expected.CaseId),
            ("@scope", (int)AmbitoAutorizacion.Empresa), ("@origin", (int)OrigenAutorizacion.MembresiaEmpresa),
            ("@module", nameof(ModuloSistema.Auditoria)), ("@action", nameof(AccionPermiso.ReadAuditCorrelation)),
            ("@entity", "ExpedienteUniversal"), ("@result", "Exito"), ("@target", canonical)
        })
        {
            var p = cmd.CreateParameter(); p.ParameterName = name; p.Value = value; cmd.Parameters.Add(p);
        }
        string payload;
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct) || reader.IsDBNull(0) || reader.IsDBNull(1) ||
                !string.Equals(reader.GetString(0), canonical, StringComparison.Ordinal)) return null;
            payload = reader.GetString(1);
            if (await reader.ReadAsync(ct)) return null; // ambiguous target: deny, not first match
        }
        long version;
        try
        {
            using var json = JsonDocument.Parse(payload);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("SucursalId", out var branch) || branch.ValueKind != JsonValueKind.Number ||
                !branch.TryGetInt32(out var branchId) || branchId != expected.BranchId ||
                !root.TryGetProperty("Version", out var storedVersion) || storedVersion.ValueKind != JsonValueKind.Number ||
                !storedVersion.TryGetInt64(out version) || version <= 0 || version > expected.Version) return null;
            var properties = root.EnumerateObject().ToArray();
            if (properties.Length is < 2 or > 3 || properties.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length ||
                properties.Any(p => p.Name is not ("SucursalId" or "Version" or "TargetCorrelation"))) return null;
            if (root.TryGetProperty("TargetCorrelation", out var linked) &&
                (linked.ValueKind != JsonValueKind.String || !Guid.TryParseExact(linked.GetString(), "N", out var previous) ||
                 previous == Guid.Empty || linked.GetString() != previous.ToString("N"))) return null;
        }
        catch (JsonException) { return null; }
        ct.ThrowIfCancellationRequested();
        return ReferenceEquals(tx, db.Database.CurrentTransaction) &&
            db.AuthorizedCommercialEmpresaId == expected.TenantId && !db.ChangeTracker.HasChanges() ? version : null;
    }

    /// <summary>
    /// API12 receipt only, never an ownership/version writer or a route. A future
    /// authenticated caller MUST supply an authoritative resource resolver, which
    /// checks the actual allowed resource and holds its ownership locks in this
    /// same transaction. No default resolver or grant is installed.
    /// On null or exception the caller must roll back the entire transaction.
    /// </summary>
    public static async Task<int?> AppendEnterpriseInCurrentTransactionAsync(
        AppDbContext db, V5CaseFileOwnershipStore.Candidate expected,
        int actor, int baseRole, int tenantRole, Guid correlation,
        Func<AppDbContext, V5CaseFileOwnershipStore.Candidate, CancellationToken, ValueTask<bool>> verifyActualResource,
        CancellationToken ct = default, Guid? targetCorrelation = null)
    {
        ct.ThrowIfCancellationRequested();
        var tx = db.Database.CurrentTransaction;
        if (tx is null || !db.Database.IsRelational() || expected is null ||
            verifyActualResource is null || correlation == Guid.Empty ||
            targetCorrelation == Guid.Empty || targetCorrelation == correlation ||
            actor <= 0 || baseRole <= 0 || tenantRole <= 0 || expected.Version <= 0 ||
            expected.TenantId <= 0 || expected.BranchId <= 0 || expected.CaseId <= 0 ||
            db.AuthorizedCommercialEmpresaId != expected.TenantId ||
            db.ChangeTracker.HasChanges())
            return null;

        async Task<bool> StillAuthorizedAsync()
        {
            ct.ThrowIfCancellationRequested();
            if (!ReferenceEquals(tx, db.Database.CurrentTransaction) ||
                db.AuthorizedCommercialEmpresaId != expected.TenantId ||
                !await V5CaseFileAuditGrantLock.TryAcquireAsync(db, actor, baseRole,
                    expected.TenantId, expected.BranchId, tenantRole, ct))
                return false;
            var current = await V5CaseFileOwnershipStore.ReadCandidateForAuditReceiptInCurrentTransactionAsync(
                db, expected.TenantId, expected.CaseId, expected.BranchId, ct);
            if (current != expected ||
                !await verifyActualResource(db, current!, ct))
                return false;
            ct.ThrowIfCancellationRequested();
            return ReferenceEquals(tx, db.Database.CurrentTransaction) &&
                db.AuthorizedCommercialEmpresaId == expected.TenantId &&
                !db.ChangeTracker.HasChanges();
        }

        if (!await StillAuthorizedAsync()) return null;
        // A linked receipt must name an existing, unambiguous receipt in the
        // same tenant/case/branch. Enforce this at the durable boundary as
        // well as the authenticated orchestration boundary.
        long? linkedVersion = null;
        if (targetCorrelation is Guid linked)
        {
            linkedVersion = await ReadReceiptVersionInCurrentTransactionAsync(db, expected, linked, ct);
            if (linkedVersion is null) return null;
        }
        // The ownership row is held exclusively until commit/rollback. Reject
        // a previously persisted operation GUID in this narrow API12 case scope;
        // do not return or overwrite a prior receipt or grant. Ordinary audit
        // correlation can legitimately span multiple events and is unchanged.
        await using (var replay = db.Database.GetDbConnection().CreateCommand())
        {
            replay.Transaction = tx.GetDbTransaction();
            replay.CommandText = """
                SELECT 1 FROM RegistrosAuditoria
                WHERE EmpresaId=@tenant AND ReferenciaId=@case AND AmbitoAutoridad=@scope AND OrigenAutorizacion=@origin
                  AND CAST(Modulo AS BINARY)=CAST(@module AS BINARY)
                  AND CAST(Accion AS BINARY)=CAST(@action AS BINARY)
                  AND CAST(Entidad AS BINARY)=CAST(@entity AS BINARY)
                  AND CorrelationId=@operation LIMIT 1 FOR SHARE
                """;
            foreach (var (name, value) in new (string, object)[]
            {
                ("@tenant", expected.TenantId), ("@case", expected.CaseId),
                ("@scope", (int)AmbitoAutorizacion.Empresa), ("@origin", (int)OrigenAutorizacion.MembresiaEmpresa),
                ("@module", nameof(ModuloSistema.Auditoria)), ("@action", nameof(AccionPermiso.ReadAuditCorrelation)),
                ("@entity", "ExpedienteUniversal"), ("@operation", correlation.ToString("N"))
            })
            {
                var p = replay.CreateParameter(); p.ParameterName = name; p.Value = value; replay.Parameters.Add(p);
            }
            if (await replay.ExecuteScalarAsync(ct) is not null) return null;
        }
        ct.ThrowIfCancellationRequested();
        // Persist only opaque correlation and minimal server-side attribution.
        // Do not serialize resource ref, token, display name, IP or request payload.
        var receipt = new RegistroAuditoria
        {
            Fecha = DateTime.UtcNow,
            UsuarioId = actor,
            NombreUsuario = "actor-autenticado",
            EmpresaId = expected.TenantId,
            AmbitoAutoridad = AmbitoAutorizacion.Empresa,
            OrigenAutorizacion = OrigenAutorizacion.MembresiaEmpresa,
            Modulo = ModuloSistema.Auditoria,
            Accion = AccionPermiso.ReadAuditCorrelation,
            Entidad = "ExpedienteUniversal",
            ReferenciaId = expected.CaseId,
            Descripcion = "Consulta de correlacion de auditoria",
            CorrelationId = correlation.ToString("N"),
            Resultado = "Exito",
            ValoresNuevos = targetCorrelation is Guid target
                ? JsonSerializer.Serialize(new { SucursalId = expected.BranchId, expected.Version, TargetCorrelation = target.ToString("N") })
                : JsonSerializer.Serialize(new { SucursalId = expected.BranchId, expected.Version })
        };
        if (!await AppendAndVerifyAsync(db, receipt, ct) ||
            !await StillAuthorizedAsync())
            return null;
        if (targetCorrelation is Guid linkedAfter &&
            await ReadReceiptVersionInCurrentTransactionAsync(db, expected, linkedAfter, ct) != linkedVersion)
            return null;
        return receipt.Id;
    }
}
