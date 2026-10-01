using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public class N411FCentrosCostoAuthorizationTests
{
    [Fact]
    public void Controller_Requires_Authentication()
    {
        var controllerType = typeof(CentrosCostoController);
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>(inherit: true));
        Assert.Null(controllerType.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true));
    }

    [Theory]
    [InlineData("Buscar", AccionPermiso.Ver)]
    [InlineData("GetActivos", AccionPermiso.Ver)]
    [InlineData("GetById", AccionPermiso.Ver)]
    [InlineData("Create", AccionPermiso.Crear)]
    [InlineData("Update", AccionPermiso.Editar)]
    [InlineData("Activar", AccionPermiso.Activar)]
    [InlineData("Desactivar", AccionPermiso.Desactivar)]
    [InlineData("Delete", AccionPermiso.EliminarLogico)]
    public void Acciones_Requieren_Permisos_Especificos(string methodName, AccionPermiso accionEsperada)
    {
        var controllerType = typeof(CentrosCostoController);
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);
        Assert.Null(method!.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true));

        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>(inherit: true);
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute)
            .GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute)
            .GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);

        Assert.Equal(ModuloSistema.Finanzas, moduloField!.GetValue(permiso));
        Assert.Equal(accionEsperada, accionField!.GetValue(permiso));
    }
}
