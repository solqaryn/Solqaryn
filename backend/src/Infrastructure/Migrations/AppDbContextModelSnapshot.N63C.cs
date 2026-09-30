using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Migrations
{
    partial class AppDbContextModelSnapshot
    {
        /// <summary>
        /// N6.3.C — alinea el snapshot efectivo de Sucursal con la persistencia
        /// multiempresa sin reescribir el snapshot histórico canónico. Las filas
        /// con EmpresaId usan namespace E:&lt;EmpresaId&gt; y las legacy permanecen
        /// en namespace LEGACY, igual que SucursalConfiguration y la migración N6.3.C.
        /// </summary>
        private static void ApplyN63CModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity("Solqaryn.Domain.Entities.Sucursal", b =>
            {
                b.Property<string>("CodigoActivoUnico")
                    .ValueGeneratedOnAddOrUpdate()
                    .HasMaxLength(64)
                    .HasColumnType("varchar(64)")
                    .HasComputedColumnSql(
                        "IF(Eliminado = 0, CONCAT(IF(EmpresaId IS NULL, 'LEGACY', CONCAT('E:', EmpresaId)), ':', UPPER(TRIM(Codigo))), NULL)",
                        true);
            });

            // Continúa el encadenamiento aditivo del snapshot efectivo con N6.7.C.
            ApplyN67CModel(modelBuilder);
        }
    }
}
