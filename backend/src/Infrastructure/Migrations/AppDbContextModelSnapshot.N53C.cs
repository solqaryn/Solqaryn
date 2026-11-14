using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Migrations
{
    partial class AppDbContextModelSnapshot
    {
        /// <summary>
        /// N5.3.C — alinea el snapshot efectivo con los índices de reportes de ventas
        /// aceptados por REVIEW_FIRST sin reescribir el snapshot histórico canónico.
        /// ProductoId ya existe físicamente desde la migración base y aquí sólo se
        /// normaliza el nombre del índice en el modelo efectivo.
        /// </summary>
        private static void ApplyN53CModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity("Solqaryn.Domain.Entities.Venta", b =>
            {
                b.HasIndex("Fecha")
                    .HasDatabaseName("IX_Ventas_Fecha");

                b.HasIndex("CreadoPorUsuarioId")
                    .HasDatabaseName("IX_Ventas_CreadoPorUsuarioId");
            });

            modelBuilder.Entity("Solqaryn.Domain.Entities.VentaDetalle", b =>
            {
                b.HasIndex("ProductoId")
                    .HasDatabaseName("IX_VentaDetalles_ProductoId");
            });
        }
    }
}
