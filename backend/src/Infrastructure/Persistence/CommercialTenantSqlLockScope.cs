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

        // Un SELECT ordinario aquí establece el read view REPEATABLE READ
        // ANTES del FOR UPDATE del documento. Con dos confirmaciones simultáneas,
        // el segundo hilo podría ver un snapshot anterior al primer commit
        // y crear dos variantes técnicas del mismo producto. En MySQL usar
        // current/locking read FOR SHARE, compatible con lectores concurrentes,
        // sin establecer ese snapshot obsoleto.
        int total;
        if (context.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) == true)
        {
            var ids = await context.Database
                .SqlQueryRaw<int>("SELECT e.Id AS Value FROM Empresas e ORDER BY e.Id LIMIT 2 FOR SHARE")
                .ToArrayAsync(cancellationToken);
            total = ids.Length;
        }
        else
        {
            // InMemory y otros proveedores sólo se utilizan en tests del gate.
            total = await context.Set<Empresa>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Take(2)
                .CountAsync(cancellationToken);
        }

        if (total > 1)
            throw new ForbiddenAccessException(
                "El SQL de bloqueo requiere una empresa autorizada cuando existen varias empresas.");

        return null;
    }
}
