using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-001.A: cierre atomico de matriz comercial GET/POST/PUT/DELETE.
/// Detecta perdida de autenticacion, ruta o permiso RBAC declarado por accion.
/// La metadata NO certifica ejecucion HTTP/JWT, aislamiento EF/MySQL, ni PROD.
/// Los negativos A/B de ejecucion se mantienen en las pruebas P2-001.E.
/// </summary>
public sealed class P2001AMatrizCrudComercialTests
{
    [Theory]
    [InlineData(typeof(ClientesController), "clientes", nameof(ClientesController.GetAll), "GET", ModuloSistema.Clientes, AccionPermiso.Ver)]
    [InlineData(typeof(ClientesController), "clientes", nameof(ClientesController.Create), "POST", ModuloSistema.Clientes, AccionPermiso.Crear)]
    [InlineData(typeof(ClientesController), "clientes", nameof(ClientesController.Update), "PUT", ModuloSistema.Clientes, AccionPermiso.Editar)]
    [InlineData(typeof(ClientesController), "clientes", nameof(ClientesController.Delete), "DELETE", ModuloSistema.Clientes, AccionPermiso.EliminarLogico)]
    [InlineData(typeof(ProductosController), "productos", nameof(ProductosController.GetPaged), "GET", ModuloSistema.Productos, AccionPermiso.Ver)]
    [InlineData(typeof(ProductosController), "productos", nameof(ProductosController.Create), "POST", ModuloSistema.Productos, AccionPermiso.Crear)]
    [InlineData(typeof(ProductosController), "productos", nameof(ProductosController.Update), "PUT", ModuloSistema.Productos, AccionPermiso.Editar)]
    [InlineData(typeof(ProductosController), "productos", nameof(ProductosController.Delete), "DELETE", ModuloSistema.Productos, AccionPermiso.EliminarLogico)]
    [InlineData(typeof(VentasController), "ventas", nameof(VentasController.GetPaged), "GET", ModuloSistema.Ventas, AccionPermiso.Ver)]
    [InlineData(typeof(VentasController), "ventas", nameof(VentasController.Create), "POST", ModuloSistema.Ventas, AccionPermiso.Crear)]
    [InlineData(typeof(VentasController), "ventas", nameof(VentasController.Update), "PUT", ModuloSistema.Ventas, AccionPermiso.Editar)]
    [InlineData(typeof(VentasController), "ventas", nameof(VentasController.DeleteBorrador), "DELETE", ModuloSistema.Ventas, AccionPermiso.EliminarLogico)]
    [InlineData(typeof(ProveedoresController), "proveedores", nameof(ProveedoresController.GetAll), "GET", ModuloSistema.Proveedores, AccionPermiso.Ver)]
    [InlineData(typeof(ProveedoresController), "proveedores", nameof(ProveedoresController.Create), "POST", ModuloSistema.Proveedores, AccionPermiso.Crear)]
    [InlineData(typeof(ProveedoresController), "proveedores", nameof(ProveedoresController.Update), "PUT", ModuloSistema.Proveedores, AccionPermiso.Editar)]
    [InlineData(typeof(ProveedoresController), "proveedores", nameof(ProveedoresController.Delete), "DELETE", ModuloSistema.Proveedores, AccionPermiso.EliminarLogico)]
    [InlineData(typeof(ComprasController), "compras", nameof(ComprasController.GetPaged), "GET", ModuloSistema.Compras, AccionPermiso.Ver)]
    [InlineData(typeof(ComprasController), "compras", nameof(ComprasController.Create), "POST", ModuloSistema.Compras, AccionPermiso.Crear)]
    [InlineData(typeof(ComprasController), "compras", nameof(ComprasController.Update), "PUT", ModuloSistema.Compras, AccionPermiso.Editar)]
    [InlineData(typeof(ComprasController), "compras", nameof(ComprasController.DeleteBorrador), "DELETE", ModuloSistema.Compras, AccionPermiso.EliminarLogico)]
    public void P2001A_EndpointComercial_NoPierdeVerboRutaNiPermiso(
        Type controlador, string ruta, string nombreMetodo,
        string verbo, ModuloSistema modulo, AccionPermiso accion)
    {
        // Negativo: una accion anonima o sin su filtro RBAC rompe el contrato.
        Assert.NotNull(controlador.GetCustomAttribute<AuthorizeAttribute>(inherit: true));
        Assert.Null(controlador.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true));
        Assert.Equal(ruta, controlador.GetCustomAttribute<RouteAttribute>()?.Template);

        var metodo = controlador.GetMethod(nombreMetodo, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(metodo);
        Assert.Null(metodo!.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true));

        var http = metodo.GetCustomAttributes().OfType<HttpMethodAttribute>().ToArray();
        Assert.Single(http);
        Assert.True(http[0].HttpMethods.Any(
            actual => string.Equals(actual, verbo, StringComparison.OrdinalIgnoreCase)));

        var permiso = metodo.GetCustomAttribute<RequierePermisoAttribute>(inherit: true);
        Assert.NotNull(permiso);
        Assert.Equal(modulo, permiso!.Modulo);
        Assert.Equal(accion, permiso.Accion);
    }
}
