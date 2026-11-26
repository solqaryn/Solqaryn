using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var tx = db.Database.CurrentTransaction;
        if (tx is null || !db.Database.IsRelational() || expected is null ||
            verifyActualResource is null || correlation == Guid.Empty ||
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
            var current = await V5CaseFileOwnershipStore.ReadCandidateInCurrentTransactionAsync(
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
            ValoresNuevos = JsonSerializer.Serialize(new
            {
                SucursalId = expected.BranchId,
                expected.Version
            })
        };
        if (!await AppendAndVerifyAsync(db, receipt, ct) ||
            !await StillAuthorizedAsync())
            return null;
        return receipt.Id;
    }
}
