using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Las consultas que necesitan IgnoreQueryFilters para consultar borrados
/// historicos NO pueden omitir la empresa autorizada en el mismo request.
/// El caso sin marca se conserva exclusivamente mientras opere el
/// middleware de cuarentena (una empresa); no es una autorizacion multiempresa.
/// </summary>
public static class TenantQueryBypassGuard
{
    public static IQueryable<TEntity> RespectCurrentTenant<TEntity>(
        IQueryable<TEntity> query, AppDbContext db)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(db);

        var empresaId = db.AuthorizedReadEmpresaId;
        if (empresaId is not > 0)
            return query;

        return query.Where(entity =>
            EF.Property<int?>(entity, "EmpresaId") == empresaId.Value);
    }
}
