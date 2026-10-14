using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Gate de SQL de bloqueo que NO compone EF query filters. El owner debe
/// aparecer dentro del SELECT ... FOR UPDATE y no depender de LINQ posterior.
/// Sin RBAC privado, 2+ empresas permanecen fail-closed, aunque estén inactivas.
/// En 0-1 empresas devuelve null para preservar el contrato legacy pre-cutover.
/// </summary>
public static class CommercialTenantSqlLockScope
{
    public static async Task<int?> RequireAsync(
        AppDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var empresaId = context.AuthorizedCommercialEmpresaId;
        if (empresaId is > 0)
            return empresaId.Value;

        var total = await context.Set<Empresa>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Take(2)
            .CountAsync(cancellationToken);

        if (total > 1)
            throw new ForbiddenAccessException(
                "El SQL de bloqueo requiere una empresa autorizada cuando existen varias empresas.");

        return null;
    }
}
