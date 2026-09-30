using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N36DevolucionClienteApplicationContractTests
{
    [Fact]
    public void N36D_MaterializaApplicationRepositoryYApi()
    {
        Assert.True(typeof(IDevolucionClienteRepository).IsInterface);
        Assert.True(typeof(IDevolucionClienteService).IsInterface);
        Assert.True(typeof(DevolucionClienteService).IsSealed);
        Assert.True(typeof(DevolucionesClienteController).IsSealed);
        Assert.NotNull(typeof(DevolucionesClienteController).GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());
    }

    [Theory]
    [InlineData("Buscar", AccionPermiso.Ver)]
    [InlineData("GetById", AccionPermiso.Ver)]
    [InlineData("Crear", AccionPermiso.Crear)]
    [InlineData("Confirmar", AccionPermiso.Confirmar)]
    [InlineData("Anular", AccionPermiso.Anular)]
    public void N36D_ControllerMantieneRbacVentas(string metodo, AccionPermiso accion)
    {
        var method = typeof(DevolucionesClienteController).GetMethod(metodo);
        Assert.NotNull(method);
        var permiso = method!.GetCustomAttributes(typeof(RequierePermisoAttribute), true).Cast<RequierePermisoAttribute>().Single();
        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.Instance | BindingFlags.NonPublic);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.Ventas, Assert.IsType<ModuloSistema>(moduloField!.GetValue(permiso)));
        Assert.Equal(accion, Assert.IsType<AccionPermiso>(accionField!.GetValue(permiso)));
    }

    [Fact]
    public void N36D_NoExponeAllowAnonymous()
    {
        Assert.Empty(typeof(DevolucionesClienteController).GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
        Assert.All(typeof(DevolucionesClienteController).GetMethods().Where(x => x.DeclaringType == typeof(DevolucionesClienteController)), method =>
            Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true)));
    }
}
