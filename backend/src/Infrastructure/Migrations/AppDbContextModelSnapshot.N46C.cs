using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Solqaryn.Infrastructure.Migrations
{
    partial class AppDbContextModelSnapshot
    {
        /// <summary>
        /// N4.6.C — alinea el modelo efectivo del snapshot EF con CuentaContable sin reescribir
        /// el snapshot canónico histórico. Mantiene el mismo patrón aditivo usado por N4.3.C.
        /// CentroCosto ya forma parte del snapshot canónico y no debe volver a registrarse aquí
        /// como entidad CLR, porque eso colisiona con la representación histórica del snapshot.
        /// </summary>
        private static void ApplyN46CModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity("Solqaryn.Domain.Entities.CuentaContable", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");
                MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                b.Property<bool>("AceptaMovimientos")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(true);

                b.Property<bool>("Activa")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("tinyint(1)")
                    .HasDefaultValue(true);

                b.Property<string>("ActualizadoPorNombreUsuario")
                    .HasMaxLength(150)
                    .HasColumnType("varchar(150)");

                b.Property<int?>("ActualizadoPorUsuarioId")
                    .HasColumnType("int");

                b.Property<string>("Codigo")
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnType("varchar(50)");

                b.Property<string>("CreadoPorNombreUsuario")
                    .HasMaxLength(150)
                    .HasColumnType("varchar(150)");

                b.Property<int?>("CreadoPorUsuarioId")
                    .HasColumnType("int");

                b.Property<int?>("CuentaPadreId")
                    .HasColumnType("int");

                b.Property<string>("Descripcion")
                    .HasMaxLength(1000)
                    .HasColumnType("varchar(1000)");

                b.Property<DateTime>("FechaActualizacion")
                    .HasColumnType("datetime(6)");

                b.Property<DateTime>("FechaCreacion")
                    .HasColumnType("datetime(6)");

                b.Property<string>("Nombre")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("varchar(200)");

                b.Property<int>("Tipo")
                    .HasColumnType("int");

                b.HasKey("Id");

                b.HasIndex("CuentaPadreId")
                    .HasDatabaseName("IX_CuentasContables_CuentaPadreId");

                b.HasIndex("Tipo", "Activa")
                    .HasDatabaseName("IX_CuentasContables_Tipo_Activa");

                b.HasIndex("Codigo")
                    .IsUnique()
                    .HasDatabaseName("UX_CuentasContables_Codigo");

                b.ToTable("CuentasContables", null, t =>
                {
                    t.HasCheckConstraint("CK_CuentasContables_Codigo", "CHAR_LENGTH(TRIM(`Codigo`)) > 0");
                    t.HasCheckConstraint("CK_CuentasContables_Nombre", "CHAR_LENGTH(TRIM(`Nombre`)) > 0");
                    t.HasCheckConstraint("CK_CuentasContables_Tipo", "`Tipo` BETWEEN 1 AND 6");
                });
            });

            modelBuilder.Entity("Solqaryn.Domain.Entities.CuentaContable", b =>
            {
                b.HasOne("Solqaryn.Domain.Entities.CuentaContable", "CuentaPadre")
                    .WithMany("Subcuentas")
                    .HasForeignKey("CuentaPadreId")
                    .OnDelete(DeleteBehavior.Restrict);

                b.Navigation("CuentaPadre");
            });

            modelBuilder.Entity("Solqaryn.Domain.Entities.CuentaContable", b =>
            {
                b.Navigation("Subcuentas");
            });
        }
    }
}
