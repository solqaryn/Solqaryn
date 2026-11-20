using Microsoft.EntityFrameworkCore;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Repositories;

/// <summary>
/// C11-03.M03.04 isolated, tenant-bound evidence adapter over the existing outbox.
/// Deliberately NOT registered in DI. No HTTP, authorization grant, persistence,
/// enqueue, provider dispatch or ACK; the authorized server must gate all consumers.
/// </summary>
public sealed class C1103OutboxTenantEvidenceAdapter(AppDbContext database)
{
    public sealed record Snapshot(
        int EmpresaId, Guid EventoId, EstadoMensajeOutbox Estado, int Intentos);

    public async Task<Snapshot?> ReadByEventAsync(
        int requestedTenantId, Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (requestedTenantId <= 0 || eventId == Guid.Empty)
            return null;

        // Tenant equality is part of the SQL predicate, not a client-side filter.
        // No payload, idempotency key, correlation ID, provider data or PII escapes.
        var result = await database.MensajesOutbox.AsNoTracking()
            .Where(row => row.EmpresaId == requestedTenantId && row.EventoId == eventId)
            .Select(row => new Snapshot(row.EmpresaId, row.EventoId, row.Estado, row.Intentos))
            .SingleOrDefaultAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
