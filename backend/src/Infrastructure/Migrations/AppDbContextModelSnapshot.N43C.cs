using System;
using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Solqaryn.Infrastructure.Migrations
{
    partial class AppDbContextModelSnapshot
    {
        private IModel? _n43cModel;

        public override IModel Model => _n43cModel ??= CreateN43CModel();

        private IModel CreateN43CModel()
        {
            var modelBuilder = new ModelBuilder();
            modelBuilder.Entity<Sucursal>();
            BuildModel(modelBuilder);
            ApplyN43CModel(modelBuilder);
            ApplyN46CModel(modelBuilder);
            ApplyN53CModel(modelBuilder);
            ApplyN57CModel(modelBuilder);
            return (IModel)modelBuilder.Model;
        }

        private static void ApplyN43CModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity("Solqaryn.Domain.Entities.Bancos.ConciliacionBancaria", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int");
                MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));
                b.Property<string>("ActualizadoPorNombreUsuario").HasMaxLength(150).HasColumnType("varchar(150)");
                b.Property<int?>("ActualizadoPorUsuarioId").HasColumnType("int");
                b.Property<string>("CreadoPorNombreUsuario").HasMaxLength(150).HasColumnType("varchar(150)");
                b.Property<int?>("CreadoPorUsuarioId").HasColumnType("int");
                b.Property<int>("CuentaBancariaId").HasColumnType("int");
                b.Property<int>("Estado").HasColumnType("int");
                b.Property<DateTime>("FechaActualizacion").HasColumnType("datetime(6)");
                b.Property<DateTime>("FechaCreacion").HasColumnType("datetime(6)");
                b.Property<DateTime>("FechaFin").HasColumnType("datetime(6)");
                b.Property<DateTime>("FechaInicio").HasColumnType("datetime(6)");
                b.Property<string>("Observaciones").HasMaxLength(500).HasColumnType("varchar(500)");
                b.Property<decimal>("SaldoFinalBanco").HasColumnType("decimal(18,2)");
                b.Property<decimal>("SaldoInicialBanco").HasColumnType("decimal(18,2)");
                b.HasKey("Id");
                b.HasIndex("CuentaBancariaId").HasDatabaseName("IX_ConciliacionesBancarias_CuentaBancariaId");
                b.HasIndex("Estado").HasDatabaseName("IX_ConciliacionesBancarias_Estado");
                b.HasIndex("CuentaBancariaId", "FechaInicio", "FechaFin").HasDatabaseName("IX_ConciliacionesBancarias_Cuenta_Periodo");
                b.ToTable("ConciliacionesBancarias", null, t =>
                {
                    t.HasCheckConstraint("CK_ConciliacionesBancarias_CuentaBancariaId", "`CuentaBancariaId` > 0");
                    t.HasCheckConstraint("CK_ConciliacionesBancarias_SaldoFinalBanco", "`SaldoFinalBanco` >= 0");
                    t.HasCheckConstraint("CK_ConciliacionesBancarias_SaldoInicialBanco", "`SaldoInicialBanco` >= 0");
                });
            });

            modelBuilder.Entity("Solqaryn.Domain.Entities.Bancos.MatchConciliacion", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int");
                MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));
                b.Property<string>("ActualizadoPorNombreUsuario").HasMaxLength(150).HasColumnType("varchar(150)");
                b.Property<int?>("ActualizadoPorUsuarioId").HasColumnType("int");
                b.Property<string>("CreadoPorNombreUsuario").HasMaxLength(150).HasColumnType("varchar(150)");
                b.Property<int?>("CreadoPorUsuarioId").HasColumnType("int");
                b.Property<DateTime>("FechaActualizacion").HasColumnType("datetime(6)");
                b.Property<DateTime>("FechaCreacion").HasColumnType("datetime(6)");
                b.Property<decimal>("MontoAplicado").HasColumnType("decimal(18,2)");
                b.Property<int>("MovimientoEstadoCuentaId").HasColumnType("int");
                b.Property<int>("MovimientoFinancieroId").HasColumnType("int");
                b.Property<int>("TipoMatch").HasColumnType("int");
                b.HasKey("Id");
                b.HasIndex("MovimientoFinancieroId").HasDatabaseName("IX_MatchesConciliacion_MovimientoFinancieroId");
                b.HasIndex("MovimientoEstadoCuentaId", "MovimientoFinancieroId").IsUnique().HasDatabaseName("UX_MatchesConciliacion_MovimientoEstadoCuenta_MovimientoFinanciero");
                b.ToTable("MatchesConciliacion", null, t =>
                {
                    t.HasCheckConstraint("CK_MatchesConciliacion_MontoAplicado", "`MontoAplicado` > 0");
                    t.HasCheckConstraint("CK_MatchesConciliacion_MovimientoEstadoCuentaId", "`MovimientoEstadoCuentaId` > 0");
                    t.HasCheckConstraint("CK_MatchesConciliacion_MovimientoFinancieroId", "`MovimientoFinancieroId` > 0");
                });
            });

            modelBuilder.Entity("Solqaryn.Domain.Entities.Bancos.MovimientoEstadoCuenta", b =>
            {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("int");
                MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));
                b.Property<string>("ActualizadoPorNombreUsuario").HasMaxLength(150).HasColumnType("varchar(150)");
                b.Property<int?>("ActualizadoPorUsuarioId").HasColumnType("int");
                b.Property<string>("Concepto").IsRequired().HasMaxLength(250).HasColumnType("varchar(250)");
                b.Property<int>("ConciliacionBancariaId").HasColumnType("int");
                b.Property<string>("CreadoPorNombreUsuario").HasMaxLength(150).HasColumnType("varchar(150)");
                b.Property<int?>("CreadoPorUsuarioId").HasColumnType("int");
                b.Property<int>("Estado").HasColumnType("int");
                b.Property<DateTime>("FechaActualizacion")
                    .HasColumnType("datetime(6)");
                b.Property<DateTime>("FechaCreacion")
                    .HasColumnType("datetime(6)");
                b.Property<DateTime>("FechaMovimiento")
                    .HasColumnType("datetime(6)");
                b.Property<string>("IdempotencyKey").IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");
                b.Property<decimal>("Monto").HasColumnType("decimal(18,2)");
                b.Property<string>("Referencia").IsRequired().HasMaxLength(100).HasColumnType("varchar(100)");
                b.Property<int>("Tipo").HasColumnType("int");
                b.HasKey("Id");
                b.HasIndex("Estado").HasDatabaseName("IX_MovimientosEstadoCuenta_Estado");
                b.HasIndex("ConciliacionBancariaId", "FechaMovimiento").HasDatabaseName("IX_MovimientosEstadoCuenta_Conciliacion_Fecha");
                b.HasIndex("ConciliacionBancariaId", "IdempotencyKey").IsUnique().HasDatabaseName("UX_MovimientosEstadoCuenta_Conciliacion_IdempotencyKey");
                b.ToTable("MovimientosEstadoCuenta", null, t =>
                {
                    t.HasCheckConstraint("CK_MovimientosEstadoCuenta_ConciliacionBancariaId", "`ConciliacionBancariaId` > 0");
                    t.HasCheckConstraint("CK_MovimientosEstadoCuenta_Monto", "`Monto` > 0");
                });
            });

            modelBuilder.Entity("Solqaryn.Domain.Entities.Bancos.ConciliacionBancaria", b =>
            {
                b.HasOne("Solqaryn.Domain.Entities.Bancos.CuentaBancaria", "CuentaBancaria").WithMany().HasForeignKey("CuentaBancariaId").OnDelete(DeleteBehavior.Restrict).IsRequired();
                b.Navigation("CuentaBancaria");
            });

            modelBuilder.Entity("Solqaryn.Domain.Entities.Bancos.MatchConciliacion", b =>
            {
                b.HasOne("Solqaryn.Domain.Entities.Bancos.MovimientoEstadoCuenta", "MovimientoEstadoCuenta").WithMany("Matches").HasForeignKey("MovimientoEstadoCuentaId").OnDelete(DeleteBehavior.Cascade).IsRequired();
                b.HasOne("Solqaryn.Domain.Entities.MovimientoFinanciero", null).WithMany().HasForeignKey("MovimientoFinancieroId").OnDelete(DeleteBehavior.Restrict).IsRequired();
                b.Navigation("MovimientoEstadoCuenta");
            });

            modelBuilder.Entity("Solqaryn.Domain.Entities.Bancos.MovimientoEstadoCuenta", b =>
            {
                b.HasOne("Solqaryn.Domain.Entities.Bancos.ConciliacionBancaria", null).WithMany("Movimientos").HasForeignKey("ConciliacionBancariaId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            });

            modelBuilder.Entity("Solqaryn.Domain.Entities.Bancos.ConciliacionBancaria", b => b.Navigation("Movimientos"));
            modelBuilder.Entity("Solqaryn.Domain.Entities.Bancos.MovimientoEstadoCuenta", b => b.Navigation("Matches"));
        }
    }
}
