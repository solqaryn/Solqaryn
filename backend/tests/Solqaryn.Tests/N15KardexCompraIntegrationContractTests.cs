using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N15KardexCompraIntegrationContractTests
{
    [Fact]
    public void CompraService_debe_consumir_writer_canonico_de_Kardex()
    {
        var constructor = typeof(CompraService)
            .GetConstructors()
            .Single();

        var tipos = constructor
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToArray();

        Assert.Contains(typeof(IKardexMovimientoWriter), tipos);
    }

    [Theory]
    [InlineData(23, "compra:23:confirmar")]
    [InlineData(23, "compra:23:anular")]
    public void Correlacion_compra_permanece_deterministica_y_segura(int compraId, string esperado)
    {
        var actual = esperado.EndsWith(":confirmar", StringComparison.Ordinal)
            ? KardexCorrelationId.CompraConfirmar(compraId)
            : KardexCorrelationId.CompraAnular(compraId);

        Assert.Equal(esperado, actual);
    }
}
