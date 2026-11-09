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
/// P2-005.A: cierre de superficie de permisos sobre borrador, confirmacion,
/// recepcion y anulacion de compras. Complementa las regresiones de aislamiento
/// P2-005.C/D/E; es prueba de metadata, NO ejecucion HTTP/JWT/RBAC/MySQL.
/// </summary>
public sealed class P2005ACierreComprasPermisosTests
{
    [Theory]
    [InlineData(typeof(ComprasController), nameof(ComprasController.GetById), "{id:int}", "GET", AccionPermiso.Ver)]
    [InlineData(typeof(ComprasController), nameof(ComprasController.Create), "", "POST", AccionPermiso.Crear)]
    [InlineData(typeof(ComprasController), nameof(ComprasController.Update), "{id:int}", "PUT", AccionPermiso.Editar)]
    [InlineData(typeof(ComprasController), nameof(ComprasController.Confirmar), "{id:int}/confirmar", "POST", AccionPermiso.Confirmar)]
    [InlineData(typeof(ComprasController), nameof(ComprasController.Anular), "{id:int}/anular", "POST", AccionPermiso.Anular)]
    [InlineData(typeof(ComprasController), nameof(ComprasController.DeleteBorrador), "{id:int}", "DELETE", AccionPermiso.EliminarLogico)]
    [InlineData(typeof(RecepcionesCompraController), nameof(RecepcionesCompraController.Create), "", "POST", AccionPermiso.Crear)]
    [InlineData(typeof(RecepcionesCompraController), nameof(RecepcionesCompraController.Update), "{id:int}", "PUT", AccionPermiso.Editar)]
    [InlineData(typeof(RecepcionesCompraController), nameof(RecepcionesCompraController.Confirmar), "{id:int}/confirmar", "POST", AccionPermiso.Confirmar)]
    [InlineData(typeof(RecepcionesCompraController), nameof(RecepcionesCompraController.Anular), "{id:int}/anular", "POST", AccionPermiso.Anular)]
    public void La_operacion_de_compra_no_puede_perder_autenticacion_ruta_ni_permiso(
        Type controllerType, string actionName, string expectedRoute, string expectedVerb,
        AccionPermiso expectedPermission)
    {
        // Rechazo causal: quitar Authorize, colocar AllowAnonymous, cambiar ruta,
        // quitar RequierePermiso o sustituir la accion RBAC hace fallar la prueba.
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>(inherit: true));
        Assert.Null(controllerType.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true));

        var action = controllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(action);
        Assert.Null(action!.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true));

        var route = action.GetCustomAttributes(inherit: true)
            .OfType<HttpMethodAttribute>().SingleOrDefault();
        Assert.NotNull(route);
        Assert.Equal(expectedRoute, route!.Template ?? "");
        Assert.Contains(route.HttpMethods, verb =>
            string.Equals(verb, expectedVerb, StringComparison.OrdinalIgnoreCase));

        var permission = action.GetCustomAttribute<RequierePermisoAttribute>(inherit: true);
        Assert.NotNull(permission);
        Assert.Equal(ModuloSistema.Compras, permission!.Modulo);
        Assert.Equal(expectedPermission, permission.Accion);
    }
}
