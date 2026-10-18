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
            "Solqaryn.Domain.Entities.AjusteInventario",
            "Solqaryn.Domain.Entities.Almacen",
            "Solqaryn.Domain.Entities.Contabilidad.AsientoDetalle",
            "Solqaryn.Domain.Entities.Contabilidad.AsientoContable",
            "Solqaryn.Domain.Entities.Cajas.CajaMovimiento",
            "Solqaryn.Domain.Entities.Cajas.Caja",
            "Solqaryn.Domain.Entities.Cajas.CajaSesion",
            "Solqaryn.Domain.Entities.CargaMasivaError",
            "Solqaryn.Domain.Entities.CargaMasiva",
            "Solqaryn.Domain.Entities.Categoria",
            "Solqaryn.Domain.Entities.Contabilidad.CentroCosto",
            "Solqaryn.Domain.Entities.Color",
            "Solqaryn.Domain.Entities.Bancos.ConciliacionBancaria",
            "Solqaryn.Domain.Entities.Contabilidad.ConfiguracionContable",
            "Solqaryn.Domain.Entities.ConsumoInsumo",
            "Solqaryn.Domain.Entities.ConteoInventario",
            "Solqaryn.Domain.Entities.CostoEnvio",
            "Solqaryn.Domain.Entities.Bancos.CuentaBancaria",
            "Solqaryn.Domain.Entities.CuentaContable",
            "Solqaryn.Domain.Entities.DashboardKpiConfiguracion",
            "Solqaryn.Domain.Entities.DescuentoCategoria",
            "Solqaryn.Domain.Entities.DescuentoRol",
            "Solqaryn.Domain.Entities.Descuento",
            "Solqaryn.Domain.Entities.EmpresaConfiguracion",
            "Solqaryn.Domain.Entities.EnlacePublicoFactura",
            "Solqaryn.Domain.Entities.HistorialAplicacionImpuesto",
            "Solqaryn.Domain.Entities.HistorialEnvioFactura",
            "Solqaryn.Domain.Entities.ImpuestoCategoria",
            "Solqaryn.Domain.Entities.ImpuestoOperacion",
            "Solqaryn.Domain.Entities.Impuesto",
            "Solqaryn.Domain.Entities.Marca",
            "Solqaryn.Domain.Entities.Modelo",
            "Solqaryn.Domain.Entities.Bancos.MovimientoEstadoCuenta",
            "Solqaryn.Domain.Entities.Contabilidad.PeriodoContable",
            "Solqaryn.Domain.Entities.PoliticaCosteoInventario",
            "Solqaryn.Domain.Entities.RevisionFinanciera",
            "Solqaryn.Domain.Entities.Talla",
            "Solqaryn.Domain.Entities.TemaVisual",
            "Solqaryn.Domain.Entities.TipoCliente",
            "Solqaryn.Domain.Entities.TransferenciaInventario",
            "Solqaryn.Domain.Entities.UbicacionAlmacen",
            "Solqaryn.Domain.Entities.RolPermiso",
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

        // RolPermiso de roles plantilla (EmpresaId NULL + ámbito Empresa) es una
        // matriz compartida del sistema. No asignar a una empresa arbitraria:
        // las FKs compuestas siguen aplicando sin excepciones a roles con dueño.
        // Rol de Plataforma nunca se modifica desde una sesión empresarial.
        var sharedGrants = new HashSet<object>();
        foreach (var entry in entries)
        {
            if (await IsSharedRoleTemplateGrantAsync(db, entry, authorized, ct))
                sharedGrants.Add(entry.Entity);
        }
        entries = entries.Where(e => !sharedGrants.Contains(e.Entity)).ToArray();
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
            // OriginalValue no basta para Update/Remove de entidades desconectadas.
            // La comparación autoritativa en MySQL evita falsificar un tenant.
            if (entry.State is EntityState.Modified or EntityState.Deleted && db.Database.IsRelational())
                await VerifyPersistedOwnerAsync(db, entry, ct);
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

    private static async Task<bool> IsSharedRoleTemplateGrantAsync(
        AppDbContext db, EntityEntry entry, int? authorized, CancellationToken ct)
    {
        if (entry.Entity is not Solqaryn.Domain.Entities.RolPermiso grant)
            return false;

        var role = grant.RolEntidad
            ?? db.Roles.Local.FirstOrDefault(r => r.Id == grant.RolId)
            ?? await db.Roles.IgnoreQueryFilters().AsNoTracking()
                .SingleOrDefaultAsync(r => r.Id == grant.RolId, ct);

        if (role is null)
            throw new BusinessRuleException("El rol del permiso no existe.");
        if (role.EmpresaId is > 0)
            return false; // Rol empresarial personalizado: guard completo A/B.

        // Un rol legacy sin propietario NO es plantilla: un grant insertado
        // directamente en EF tampoco puede legitimarlo como autoridad global.
        if (role.Ambito == Solqaryn.Domain.Enums.AmbitoAutorizacion.Empresa &&
            !role.EsSistema)
            throw new BusinessRuleException(
                "Un rol empresarial sin dueño y sin condición de sistema no puede recibir grants.");

        if (role.Ambito == Solqaryn.Domain.Enums.AmbitoAutorizacion.Plataforma &&
            authorized is > 0)
            throw new ForbiddenAccessException(
                "La matriz de un rol de plataforma no se modifica desde una empresa.");

        var owner = entry.Property("EmpresaId");
        if (owner.CurrentValue is not null ||
            (entry.State is EntityState.Modified or EntityState.Deleted &&
             owner.OriginalValue is not null))
            throw new BusinessRuleException(
                "Los permisos de un rol plantilla no pueden atribuirse a una empresa arbitraria.");

        if (entry.State is EntityState.Modified or EntityState.Deleted &&
            db.Database.IsRelational())
            await VerifyPersistedOwnerAsync(db, entry, ct);
        return true;
    }

    private static async Task VerifyPersistedOwnerAsync(
        AppDbContext db, EntityEntry entry, CancellationToken ct)
    {
        var table = entry.Metadata.GetTableName()
            ?? throw new BusinessRuleException("Entidad comercial sin tabla verificada.");
        var property = entry.Metadata.FindProperty("EmpresaId")
            ?? throw new BusinessRuleException("Entidad comercial sin propiedad empresarial.");
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null || key.Properties.Count != 1 || key.Properties[0].Name != "Id")
            throw new BusinessRuleException("Clave comercial no certificada para verificar tenant.");
        var id = entry.Property("Id").CurrentValue;
        if (id is null)
            throw new BusinessRuleException("Identidad comercial ausente.");
        static string Quote(string name) => "`" + name.Replace("`", "``", StringComparison.Ordinal) + "`";
        var ownerColumn = property.GetColumnName(StoreObjectIdentifier.Table(table, entry.Metadata.GetSchema()))
            ?? "EmpresaId";
        var idColumn = key.Properties[0].GetColumnName(
            StoreObjectIdentifier.Table(table, entry.Metadata.GetSchema())) ?? "Id";
        var sql = $"SELECT {Quote(ownerColumn)} AS `Value` FROM {Quote(table)} WHERE {Quote(idColumn)} = {{0}} LIMIT 1";
        var saved = await db.Database.SqlQueryRaw<int?>(sql, id).SingleOrDefaultAsync(ct);
        if (!Equals(saved, entry.Property("EmpresaId").CurrentValue))
            throw new BusinessRuleException(
                "La propiedad empresarial almacenada difiere de la operación solicitada.");
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
