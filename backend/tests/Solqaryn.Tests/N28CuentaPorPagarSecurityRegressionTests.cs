using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N28CuentaPorPagarSecurityRegressionTests
{
    [Theory]
    [InlineData(nameof(CuentasPorPagarController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(CuentasPorPagarController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(CuentasPorPagarController.Generar), AccionPermiso.Crear)]
    [InlineData(nameof(CuentasPorPagarController.Aplicar), AccionPermiso.Editar)]
    [InlineData(nameof(CuentasPorPagarController.RevertirAplicacion), AccionPermiso.Editar)]
    [InlineData(nameof(CuentasPorPagarController.Anular), AccionPermiso.Anular)]
    public void Endpoints_mantienen_permiso_finanzas_exacto(string metodo, AccionPermiso accionEsperada)
    {
        var method = typeof(CuentasPorPagarController).GetMethod(metodo);
        Assert.NotNull(method);

        var permiso = Assert.Single(method!.CustomAttributes.Where(x => x.AttributeType == typeof(RequierePermisoAttribute)));
        Assert.Equal(2, permiso.ConstructorArguments.Count);
        Assert.Equal(ModuloSistema.Finanzas, (ModuloSistema)permiso.ConstructorArguments[0].Value!);
        Assert.Equal(accionEsperada, (AccionPermiso)permiso.ConstructorArguments[1].Value!);
    }

    [Fact]
    public void Controller_y_endpoints_no_permiten_bypass_anonimo()
    {
        var type = typeof(CuentasPorPagarController);

        Assert.NotNull(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.Empty(type.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));

        foreach (var method in type.GetMethods().Where(x => x.DeclaringType == type))
            Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }
}
