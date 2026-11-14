using Solqaryn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solqaryn.Infrastructure.Persistence.Configurations;

public class TemaVisualConfiguration : IEntityTypeConfiguration<TemaVisual>
{
    public void Configure(EntityTypeBuilder<TemaVisual> builder)
    {
        builder.ToTable("TemaVisual");
        builder.HasKey(t => t.Id);

        foreach (var propiedad in new[]
        {
            nameof(TemaVisual.ColorPrimario), nameof(TemaVisual.ColorSecundario), nameof(TemaVisual.ColorAcento),
            nameof(TemaVisual.FondoPrincipal), nameof(TemaVisual.FondoTarjetas), nameof(TemaVisual.MenuLateral),
            nameof(TemaVisual.BarraSuperior), nameof(TemaVisual.Encabezados), nameof(TemaVisual.BotonesPrincipales),
            nameof(TemaVisual.TextoPrincipal), nameof(TemaVisual.TextoSecundario), nameof(TemaVisual.ColorExito),
            nameof(TemaVisual.ColorAdvertencia), nameof(TemaVisual.ColorError), nameof(TemaVisual.ColorInformacion)
        })
        {
            builder.Property(propiedad).HasMaxLength(9); // #RRGGBBAA como máximo
        }

        // Seed inicial (Id=1) con los mismos valores que ya vive hoy en
        // frontend/src/styles.scss — para que el primer GET no devuelva
        // vacío ni rompa la UI antes de que un admin guarde cambios.
        builder.HasData(new TemaVisual { Id = 1 });
    }
}
