using Solqaryn.Application.DTOs;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N58DExportPdfServiceTests
{
    [Fact]
    public async Task ExportarAsync_UsuariosPdf_GeneraPdfRealConContratoDescargable()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n58d-pdf-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        var service = new ReporteAdministrativoService(db);

        var archivo = await service.ExportarAsync(
            "usuarios",
            "pdf",
            new ReporteAdministrativoFiltroDto());

        Assert.Equal("application/pdf", archivo.ContentType);
        Assert.Equal("usuarios-accesos.pdf", archivo.NombreArchivo);
        Assert.True(archivo.Contenido.Length > 4);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(archivo.Contenido, 0, 4));
    }

    [Fact]
    public async Task ExportarAsync_FormatoNoPermitido_FallaCerrado()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n58d-invalid-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options);
        var service = new ReporteAdministrativoService(db);

        var ex = await Assert.ThrowsAsync<Solqaryn.Application.Exceptions.BusinessRuleException>(() =>
            service.ExportarAsync("usuarios", "html", new ReporteAdministrativoFiltroDto()));

        Assert.Contains("csv, xlsx o pdf", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
