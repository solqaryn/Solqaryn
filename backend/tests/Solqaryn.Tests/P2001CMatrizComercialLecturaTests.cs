using System;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-001.C: matriz mínima de lectura comercial por endpoint.
/// Comprueba metadata del contrato HTTP, no sustituye pruebas de JWT,
/// membresía/tenant real ni decisiones del middleware de autorización.
/// </summary>
public sealed class P2001CMatrizComercialLecturaTests
{
    [Theory]
    [InlineData(typeof(ClientesController), nameof(ClientesController.GetAll), "clientes", ModuloSistema.Clientes)]
    [InlineData(typeof(ProductosController), nameof(ProductosController.GetPaged), "productos", ModuloSistema.Productos)]
    [InlineData(typeof(VentasController), nameof(VentasController.GetPaged), "ventas", ModuloSistema.Ventas)]
    public void Lectura_comercial_no_puede_perder_autenticacion_o_permiso(
        Type controllerType, string actionName, string route, ModuloSistema modulo)
    {
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>(inherit: true));
        Assert.Null(controllerType.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true));
        Assert.Equal(route, controllerType.GetCustomAttribute<RouteAttribute>()?.Template);

        var action = controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(action);
        Assert.NotNull(action!.GetCustomAttribute<HttpGetAttribute>());
        Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>());
        var permiso = action.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);
        Assert.Equal(modulo, permiso!.Modulo);
        Assert.Equal(AccionPermiso.Ver, permiso.Accion);
    }
}
