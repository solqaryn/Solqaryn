using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Infrastructure.Persistence;

/// <summary>
/// Filtro de defensa en profundidad para lecturas EF de todas las entidades
/// que el modelo vigente declara propiedad de una Empresa. Conserva los filtros
/// existentes (por ejemplo, borrado lógico) y usa el contexto RBAC verificado
/// por solicitud; nunca acepta EmpresaId enviado en query, cuerpo o cabecera.
/// La ausencia de marca NO autoriza multiempresa: permanece la cuarentena HTTP
/// P0 hasta cubrir explícitamente tienda pública, jobs y accesos sin EF.
/// </summary>
public static class CommercialTenantReadFilterModel
{
    public static void Apply(ModelBuilder modelBuilder, AppDbContext db)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes().ToArray())
        {
            var owner = entity.FindProperty("EmpresaId");
            if (entity.BaseType is not null ||
                entity.IsOwned() ||
                owner is null ||
                (owner.ClrType != typeof(int) && owner.ClrType != typeof(int?)))
                continue;

            var record = Expression.Parameter(entity.ClrType, "record");
            Expression ownerValue = Expression.Call(
                typeof(EF), nameof(EF.Property), new[] { owner.ClrType },
                record, Expression.Constant("EmpresaId"));

            if (owner.ClrType == typeof(int))
                ownerValue = Expression.Convert(ownerValue, typeof(int?));

            // EF Core parametriza esta propiedad del DbContext por instancia.
            // No capturar el tenant como un valor fijo en el modelo cacheado.
            var tenant = Expression.Property(
                Expression.Constant(db), nameof(AppDbContext.AuthorizedReadEmpresaId));
            var scopeAbsent = Expression.Equal(
                tenant, Expression.Constant(null, typeof(int?)));
            var ownedByTenant = Expression.Equal(ownerValue, tenant);

            Expression scopePredicate = Expression.OrElse(scopeAbsent, ownedByTenant);

            // Los roles empresariales de sistema y sus grants no tienen owner.
            // Mantener su visibilidad, sin abrir roles/grants de empresas ajenas.
            if (entity.ClrType == typeof(Rol) || entity.ClrType == typeof(RolPermiso))
            {
                scopePredicate = Expression.OrElse(
                    scopePredicate,
                    Expression.Equal(ownerValue, Expression.Constant(null, typeof(int?))));
            }

            var existing = entity.GetQueryFilter();
            if (existing is not null)
            {
                var softDeletePredicate =
                    new ReplaceParameterVisitor(existing.Parameters.Single(), record)
                        .Visit(existing.Body)!;
                scopePredicate = Expression.AndAlso(softDeletePredicate, scopePredicate);
            }

            modelBuilder.Entity(entity.ClrType)
                .HasQueryFilter(Expression.Lambda(scopePredicate, record));
        }
    }

    /// <summary>
    /// Usuarios son identidades compartidas y NO tienen EmpresaId. En rutas de
    /// administración con tenant RBAC se exponen solo las identidades con
    /// membresía en esa empresa; login y renovación sin marca conservan el
    /// lookup global necesario para autenticar sin cambiar el contrato JWT.
    /// </summary>
    public static void ApplySharedIdentityMembershipFilter(ModelBuilder modelBuilder, AppDbContext db)
    {
        modelBuilder.Entity<Usuario>().HasQueryFilter(user =>
            db.AuthorizedReadEmpresaId == null ||
            db.UsuarioEmpresas.Any(member =>
                member.UsuarioId == user.Id &&
                member.EmpresaId == db.AuthorizedReadEmpresaId));
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _original;
        private readonly ParameterExpression _replacement;

        public ReplaceParameterVisitor(
            ParameterExpression original, ParameterExpression replacement)
        {
            _original = original;
            _replacement = replacement;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _original ? _replacement : base.VisitParameter(node);
    }
}
