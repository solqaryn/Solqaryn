using System;
using System.Linq;
using Xunit;

namespace Solqaryn.Tests;

public class N14AjusteInventarioExistenciaAuthorityContractTests
{
    [Fact]
    public void AjusteInventarioService_DebeDependerDelServicioDeConcurrenciaDeExistencias()
    {
        var applicationAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => assembly.GetName().Name?.Contains("Application", StringComparison.OrdinalIgnoreCase) == true);

        Assert.NotNull(applicationAssembly);

        var serviceType = applicationAssembly!
            .GetTypes()
            .FirstOrDefault(type => type.Name == "AjusteInventarioService");

        Assert.NotNull(serviceType);

        var constructor = serviceType!
            .GetConstructors()
            .OrderByDescending(candidate => candidate.GetParameters().Length)
            .FirstOrDefault();

        Assert.NotNull(constructor);
        Assert.Contains(
            constructor!.GetParameters(),
            parameter => parameter.ParameterType.Name == "IExistenciaVarianteConcurrencyService");
    }
}
