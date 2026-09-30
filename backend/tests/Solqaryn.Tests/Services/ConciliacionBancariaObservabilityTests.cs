using System.Reflection;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Services;

public class ConciliacionBancariaObservabilityTests
{
    [Theory]
    [InlineData(nameof(IAuditoriaService.RegistrarAsync))]
    [InlineData(nameof(IAuditoriaService.RegistrarEstrictoAsync))]
    public void AuditoriaExponeContratosDeRegistro(string methodName)
    {
        var method = typeof(IAuditoriaService)
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method!.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(10, parameters.Length);
        Assert.Equal(typeof(ModuloSistema), parameters[0].ParameterType);
        Assert.Equal(typeof(AccionPermiso), parameters[1].ParameterType);
        Assert.Equal(typeof(string), parameters[2].ParameterType);
        Assert.Equal(typeof(int?), parameters[3].ParameterType);
        Assert.Equal(typeof(string), parameters[4].ParameterType);
        Assert.Equal(typeof(object), parameters[5].ParameterType);
        Assert.Equal(typeof(object), parameters[6].ParameterType);
        Assert.Equal(typeof(string), parameters[7].ParameterType);
        Assert.Equal(typeof(string), parameters[8].ParameterType);
        Assert.Equal(typeof(string), parameters[9].ParameterType);
    }
}
