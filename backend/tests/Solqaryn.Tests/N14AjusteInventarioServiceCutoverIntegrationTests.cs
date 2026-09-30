using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N14AjusteInventarioServiceCutoverIntegrationTests
{
    [Fact]
    public void Constructor_debe_consumir_la_concurrencia_de_existencia_autoritativa()
    {
        var constructor = typeof(AjusteInventarioService)
            .GetConstructors()
            .Single();

        var tipos = constructor
            .GetParameters()
            .Select(p => p.ParameterType)
            .ToArray();

        Assert.Contains(typeof(IExistenciaVarianteConcurrencyService), tipos);
    }
}
