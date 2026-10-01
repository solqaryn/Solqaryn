using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class EmpresaConfiguracionConfiguration : IEntityTypeConfiguration<EmpresaConfiguracion>
{
    public void Configure(EntityTypeBuilder<EmpresaConfiguracion> builder)
    {
        builder.ToTable("EmpresaConfiguraciones");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.NombreComercial).IsRequired().HasMaxLength(200);
        builder.Property(e => e.RazonSocial).HasMaxLength(250);
        builder.Property(e => e.Eslogan).HasMaxLength(200);
        builder.Property(e => e.RTN).HasMaxLength(50);
        builder.Property(e => e.Telefono).HasMaxLength(30);
        builder.Property(e => e.Correo).HasMaxLength(150);
        builder.Property(e => e.Direccion).HasMaxLength(500);
        builder.Property(e => e.SitioWeb).HasMaxLength(250);
        builder.Property(e => e.Facebook).HasMaxLength(250);
        builder.Property(e => e.Instagram).HasMaxLength(250);
        builder.Property(e => e.WhatsApp).HasMaxLength(80);
        builder.Property(e => e.LogoUrl).HasMaxLength(1000);
        builder.Property(e => e.LogoPublicId).HasMaxLength(250);
        builder.Property(e => e.NombreVisibleSistema).IsRequired().HasMaxLength(120);
        builder.Property(e => e.DescripcionSistema).HasMaxLength(250);
        builder.Property(e => e.MensajeLogin).HasMaxLength(300);
        builder.Property(e => e.Copyright).HasMaxLength(300);
        builder.Property(e => e.EncabezadoTexto).HasMaxLength(500);
        builder.Property(e => e.PiePaginaTexto).HasMaxLength(500);
        builder.Property(e => e.Moneda).HasMaxLength(10);
        builder.Property(e => e.ZonaHoraria).HasMaxLength(80);
        builder.Property(e => e.FormatoFecha).HasMaxLength(30);
        builder.Property(e => e.InformacionFiscal).HasMaxLength(1000);
        builder.Property(e => e.TextoLegal).HasMaxLength(1000);
        builder.Property(e => e.TextoFactura).HasMaxLength(1000);
        builder.Property(e => e.TextoReportes).HasMaxLength(1000);
        builder.Property(e => e.ActivaUnica)
            .HasMaxLength(10)
            .HasComputedColumnSql("IF(Activa = 1, 'ACTIVE', NULL)", stored: true);

        builder.HasIndex(e => e.Activa);
        builder.HasIndex(e => e.ActivaUnica)
            .IsUnique()
            .HasDatabaseName("UX_EmpresaConfiguraciones_Activa");
    }
}
