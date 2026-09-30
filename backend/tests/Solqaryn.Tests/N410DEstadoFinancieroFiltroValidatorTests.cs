using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Validators;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N410DEstadoFinancieroFiltroValidatorTests
{
    private readonly EstadoFinancieroFiltroValidator _validator = new();

    [Fact]
    public async Task AceptaPeriodoORangoPeroNoAmbos()
    {
        Assert.True((await _validator.ValidateAsync(new EstadoFinancieroFiltroDto { PeriodoContableId = 1 })).IsValid);
        Assert.True((await _validator.ValidateAsync(new EstadoFinancieroFiltroDto
        {
            FechaDesde = DateTime.UtcNow.AddDays(-1),
            FechaHasta = DateTime.UtcNow
        })).IsValid);
        Assert.False((await _validator.ValidateAsync(new EstadoFinancieroFiltroDto())).IsValid);
        Assert.False((await _validator.ValidateAsync(new EstadoFinancieroFiltroDto
        {
            PeriodoContableId = 1,
            FechaDesde = DateTime.UtcNow.AddDays(-1),
            FechaHasta = DateTime.UtcNow
        })).IsValid);
    }

    [Fact]
    public async Task RechazaRangoInvertido()
    {
        var resultado = await _validator.ValidateAsync(new EstadoFinancieroFiltroDto
        {
            FechaDesde = DateTime.UtcNow,
            FechaHasta = DateTime.UtcNow.AddDays(-1)
        });
        Assert.False(resultado.IsValid);
    }
}
