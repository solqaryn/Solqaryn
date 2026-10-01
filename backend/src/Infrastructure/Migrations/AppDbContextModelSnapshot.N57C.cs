using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Solqaryn.Infrastructure.Migrations
{
    partial class AppDbContextModelSnapshot
    {
        /// <summary>
        /// N5.7.C — alinea el snapshot efectivo con la persistencia de configuración
        /// de KPIs usando el patrón aditivo ya establecido por N4.3.C/N4.6.C/N5.3.C,
        /// sin reescribir el snapshot histórico canónico.
        /// </summary>
        private static void ApplyN57CModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity("Solqaryn.Domain.Entities.DashboardKpiConfiguracion", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");
                MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                b.Property<string>("MetricKey")
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnType("varchar(64)");

                b.Property<int?>("UsuarioId")
                    .HasColumnType("int");

                b.Property<int?>("RolId")
                    .HasColumnType("int");

                b.Property<bool>("Habilitado")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(true);

                b.Property<int>("Orden")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int")
                    .HasDefaultValue(0);

                b.Property<string>("EtiquetaVisible")
                    .HasMaxLength(150)
                    .HasColumnType("varchar(150)");

                b.HasKey("Id");

                b.HasIndex("RolId", "MetricKey")
                    .IsUnique()
                    .HasDatabaseName("UX_DashboardKpiConfiguraciones_Rol_MetricKey");

                b.HasIndex("UsuarioId", "MetricKey")
                    .IsUnique()
                    .HasDatabaseName("UX_DashboardKpiConfiguraciones_Usuario_MetricKey");

                b.ToTable("DashboardKpiConfiguraciones", null, t =>
                {
                    t.HasCheckConstraint(
                        "CK_DashboardKpiConfiguraciones_Owner",
                        "(`UsuarioId` IS NOT NULL AND `RolId` IS NULL) OR (`UsuarioId` IS NULL AND `RolId` IS NOT NULL)");
                });
            });

            modelBuilder.Entity("Solqaryn.Domain.Entities.DashboardKpiConfiguracion", b =>
            {
                b.HasOne("Solqaryn.Domain.Entities.Rol", "Rol")
                    .WithMany()
                    .HasForeignKey("RolId")
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne("Solqaryn.Domain.Entities.Usuario", "Usuario")
                    .WithMany()
                    .HasForeignKey("UsuarioId")
                    .OnDelete(DeleteBehavior.Cascade);

                b.Navigation("Rol");
                b.Navigation("Usuario");
            });

            // Mantiene el encadenamiento aditivo del snapshot efectivo y aplica la
            // reconciliación N6.3.C después de los overlays históricos existentes.
            ApplyN63CModel(modelBuilder);
        }
    }
}
