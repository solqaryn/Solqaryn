using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N29EvaluacionProveedorSecurityRegressionTests
{
    [Theory]
    [InlineData(nameof(EvaluacionesProveedorController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(EvaluacionesProveedorController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(EvaluacionesProveedorController.Generar), AccionPermiso.Crear)]
    public void Endpoints_mantienen_permiso_compras_exacto(string metodo, AccionPermiso accionEsperada)
    {
        var method = typeof(EvaluacionesProveedorController).GetMethod(metodo);
        Assert.NotNull(method);

        var permiso = Assert.Single(method!.CustomAttributes.Where(x => x.AttributeType == typeof(RequierePermisoAttribute)));
        Assert.Equal(2, permiso.ConstructorArguments.Count);
        Assert.Equal(ModuloSistema.Compras, (ModuloSistema)permiso.ConstructorArguments[0].Value!);
        Assert.Equal(accionEsperada, (AccionPermiso)permiso.ConstructorArguments[1].Value!);
    }

    [Fact]
    public void Controller_y_endpoints_no_permiten_bypass_anonimo()
    {
        var type = typeof(EvaluacionesProveedorController);

        Assert.NotNull(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.Empty(type.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));

        foreach (var method in type.GetMethods().Where(x => x.DeclaringType == type))
            Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }
}
