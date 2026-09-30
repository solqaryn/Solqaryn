using Solqaryn.Domain.Common;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N15KardexDomainContractTests
{
    [Fact]
    public void Crear_ConContextoFisicoValido_NormalizaCorrelationId()
    {
        var contexto = ContextoFisicoMovimientoInventario.Crear(
            productoVarianteId: 15,
            almacenId: 4,
            ubicacionAlmacenId: 9,
            correlationId: "  venta:2026_08-15.abc  ");

        Assert.Equal(15, contexto.ProductoVarianteId);
        Assert.Equal(4, contexto.AlmacenId);
        Assert.Equal(9, contexto.UbicacionAlmacenId);
        Assert.Equal("venta:2026_08-15.abc", contexto.CorrelationId);
    }

    [Fact]
    public void Crear_SinUbicacion_PermiteExistenciaRaizDeAlmacen()
    {
        var contexto = ContextoFisicoMovimientoInventario.Crear(15, 4, null, "kardex-abc123");

        Assert.Null(contexto.UbicacionAlmacenId);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(-1, 4)]
    [InlineData(15, 0)]
    [InlineData(15, -1)]
    public void Crear_ConVarianteOAlmacenNoPositivo_FallaCerrado(int varianteId, int almacenId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ContextoFisicoMovimientoInventario.Crear(varianteId, almacenId, null, "corr-1"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Crear_ConUbicacionNoPositiva_FallaCerrado(int ubicacionId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ContextoFisicoMovimientoInventario.Crear(15, 4, ubicacionId, "corr-1"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("corr con espacios")]
    [InlineData("corr<script>")]
    public void Crear_ConCorrelationIdInvalido_FallaCerrado(string correlationId)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            ContextoFisicoMovimientoInventario.Crear(15, 4, null, correlationId));
    }

    [Fact]
    public void Crear_ConCorrelationIdMayorAlLimite_FallaCerrado()
    {
        var correlationId = new string('a', ContextoFisicoMovimientoInventario.MaxCorrelationIdLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ContextoFisicoMovimientoInventario.Crear(15, 4, null, correlationId));
    }
}
