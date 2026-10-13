using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Solqaryn.Application.Exceptions;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Guard uniforme para los 46 descendientes comerciales. Deriva EmpresaId de padres
/// EF con ownership conocido, impide modificaciones de owner y cruces A/B antes
/// de SaveChanges. No modifica filas legacy ni abre la cuarentena multiempresa.
/// </summary>
public static class CommercialTenantOwnershipGraphWriteGuard
{
    private static readonly HashSet<string> OwnedGraphTypes =
        new(StringComparer.Ordinal)
        {
            "Solqaryn.Domain.Entities.AjusteInventarioDetalle",
            "Solqaryn.Domain.Entities.AplicacionCuentaPorPagar",
            "Solqaryn.Domain.Entities.AsignacionCostoMovimientoInventario",
            "Solqaryn.Domain.Entities.CapaCostoInventario",
            "Solqaryn.Domain.Entities.ConsumoInsumoDetalle",
            "Solqaryn.Domain.Entities.ConteoInventarioDetalle",
            "Solqaryn.Domain.Entities.CostoEstandarInventario",
            "Solqaryn.Domain.Entities.CreditoCliente",
            "Solqaryn.Domain.Entities.CuentaPorPagar",
            "Solqaryn.Domain.Entities.DescuentoCliente",
            "Solqaryn.Domain.Entities.DescuentoProducto",
            "Solqaryn.Domain.Entities.DevolucionClienteDetalle",
            "Solqaryn.Domain.Entities.DevolucionCliente",
            "Solqaryn.Domain.Entities.DevolucionProveedor",
            "Solqaryn.Domain.Entities.DevolucionProveedorDetalle",
            "Solqaryn.Domain.Entities.EvaluacionProveedor",
            "Solqaryn.Domain.Entities.ExistenciaVariante",
            "Solqaryn.Domain.Entities.FacturaProveedorDetalle",
            "Solqaryn.Domain.Entities.FacturaProveedor",
            "Solqaryn.Domain.Entities.ImpuestoCliente",
            "Solqaryn.Domain.Entities.ImpuestoProducto",
            "Solqaryn.Domain.Entities.ImpuestoProveedor",
            "Solqaryn.Domain.Entities.LoteInventario",
            "Solqaryn.Domain.Entities.Bancos.MatchConciliacion",
            "Solqaryn.Domain.Entities.MovimientoFinanciero",
            "Solqaryn.Domain.Entities.MovimientoInventario",
            "Solqaryn.Domain.Entities.NotaCreditoCliente",
            "Solqaryn.Domain.Entities.NotaCreditoProveedor",
            "Solqaryn.Domain.Entities.OrdenCompraDetalle",
            "Solqaryn.Domain.Entities.OrdenCompra",
            "Solqaryn.Domain.Entities.PreparacionPedidoVenta",
            "Solqaryn.Domain.Entities.PreparacionPedidoVentaDetalle",
            "Solqaryn.Domain.Entities.RecepcionCompraDetalle",
            "Solqaryn.Domain.Entities.RecepcionCompra",
            "Solqaryn.Domain.Entities.ReservaInventarioDetalle",
            "Solqaryn.Domain.Entities.ReservaInventario",
            "Solqaryn.Domain.Entities.SerieInventario",
            "Solqaryn.Domain.Entities.SolicitudCompraDetalle",
            "Solqaryn.Domain.Entities.SolicitudCompra",
            "Solqaryn.Domain.Entities.ThreeWayMatchResult",
            "Solqaryn.Domain.Entities.TiendaCuentaCliente",
            "Solqaryn.Domain.Entities.TiendaDireccionCliente",
            "Solqaryn.Domain.Entities.TiendaFavoritoCliente",
            "Solqaryn.Domain.Entities.TiendaSesionCliente",
            "Solqaryn.Domain.Entities.TransferenciaInventarioDetalle",
            "Solqaryn.Domain.Entities.VariacionCostoEstandarInventario",
        };

    public static bool Applies(string entityName) => OwnedGraphTypes.Contains(entityName);

    public static async Task ValidateAsync(AppDbContext db, int? authorized, CancellationToken ct)
    {
        var entries = db.ChangeTracker.Entries()
            .Where(e => Applies(e.Metadata.Name) &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();
        if (entries.Length == 0)
            return;

        foreach (var entry in entries)
        {
            var company = entry.Property("EmpresaId");
            if (company.CurrentValue is int current && current <= 0)
                throw new BusinessRuleException("EmpresaId no válido para una operación comercial.");
            if (entry.State is EntityState.Modified or EntityState.Deleted &&
                !Equals(company.CurrentValue, company.OriginalValue))
                throw new BusinessRuleException("No se puede reasignar el dueño empresarial de un registro.");
            if (authorized is > 0 && company.CurrentValue is int owner && owner != authorized)
                throw new ForbiddenAccessException("Registro comercial de una empresa no autorizada.");
        }

        // Una nueva jerarquía completa puede añadirse en un mismo SaveChanges.
        // Propagar ownership hasta converger antes de validar todas sus aristas.
        for (var pass = 0; pass < entries.Length; pass++)
        {
            var changed = false;
            foreach (var entry in entries.Where(e => e.State == EntityState.Added))
            {
                var company = entry.Property("EmpresaId");
                if (company.CurrentValue is int)
                    continue;
                int? candidate = authorized is > 0 ? authorized : null;
                foreach (var edge in TenantEdges(entry))
                {
                    var parent = await ResolveParentAsync(db, entry, edge, ct);
                    if (!parent.Found || parent.Owner is not > 0)
                        continue;
                    if (candidate is > 0 && candidate != parent.Owner)
                        throw new ForbiddenAccessException(
                            "Los documentos relacionados no pertenecen a la empresa autorizada.");
                    candidate = parent.Owner;
                }
                if (candidate is > 0)
                {
                    company.CurrentValue = candidate;
                    changed = true;
                }
            }
            if (!changed)
                break;
        }

        foreach (var entry in entries)
        {
            var company = entry.Property("EmpresaId");
            var current = company.CurrentValue as int?;
            if (authorized is > 0 && current != authorized)
                throw new ForbiddenAccessException(
                    "Empresa verificada obligatoria para guardar un descendiente comercial.");

            if (entry.State == EntityState.Deleted)
                continue;

            foreach (var edge in TenantEdges(entry))
            {
                var parent = await ResolveParentAsync(db, entry, edge, ct);
                if (parent.Found && parent.Owner != current &&
                    (parent.Owner is > 0 || current is > 0))
                    throw new BusinessRuleException(
                        "Relación comercial entre empresas distintas o sin dueño comprobado.");
            }
        }
    }

    private static IEnumerable<IForeignKey> TenantEdges(EntityEntry entry) =>
        entry.Metadata.GetForeignKeys().Where(fk =>
            fk.PrincipalEntityType.FindProperty("EmpresaId") is not null &&
            fk.PrincipalEntityType.ClrType != typeof(Solqaryn.Domain.Entities.Empresa));

    private static async Task<(bool Found, int? Owner)> ResolveParentAsync(
        AppDbContext db, EntityEntry child, IForeignKey fk, CancellationToken ct)
    {
        if (fk.DependentToPrincipal is not null)
        {
            var navigation = child.Navigation(fk.DependentToPrincipal.Name).CurrentValue;
            if (navigation is not null)
            {
                var related = db.Entry(navigation);
                return (true, related.Property("EmpresaId").CurrentValue as int?);
            }
        }

        var principalKey = fk.PrincipalEntityType.FindPrimaryKey();
        if (principalKey is null ||
            !principalKey.Properties.Select(p => p.Name)
                .SequenceEqual(fk.PrincipalKey.Properties.Select(p => p.Name)))
            return (false, null);

        var keys = fk.Properties.Select(x => child.Property(x.Name).CurrentValue).ToArray();
        if (keys.Any(x => x is null || x is int number && number <= 0))
            return (false, null);

        // FindAsync resuelve primero entidades en seguimiento (incluidas las del
        // SaveChanges actual), luego registros persistidos del modelo EF.
        var principal = await db.FindAsync(fk.PrincipalEntityType.ClrType, keys, ct);
        if (principal is null)
            return (false, null);
        return (true, db.Entry(principal).Property("EmpresaId").CurrentValue as int?);
    }
}
