using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-001.E: matriz negativa de los cinco endpoints GET comerciales.
/// Ejercita el filtro real con un servicio RBAC simulado; no valida
/// JWT/middleware HTTP, membresias persistidas ni MySQL real.
/// </summary>
public sealed class P2001EPermisoTenantLecturaTests
{
    [Theory]
    [InlineData(typeof(ClientesController), nameof(ClientesController.GetAll), ModuloSistema.Clientes)]
    [InlineData(typeof(ProductosController), nameof(ProductosController.GetPaged), ModuloSistema.Productos)]
    [InlineData(typeof(VentasController), nameof(VentasController.GetPaged), ModuloSistema.Ventas)]
    [InlineData(typeof(ProveedoresController), nameof(ProveedoresController.GetAll), ModuloSistema.Proveedores)]
    [InlineData(typeof(ComprasController), nameof(ComprasController.GetPaged), ModuloSistema.Compras)]
    public async Task RBAC_denegado_impide_lectura_y_solo_autoriza_tenant_verificado(
        Type tipoController, string nombreAccion, ModuloSistema modulo)
    {
        var metodo = tipoController.GetMethod(
            nombreAccion, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(metodo);
        var filtro = Assert.IsType<RequierePermisoAttribute>(
            metodo!.GetCustomAttribute<RequierePermisoAttribute>());
        Assert.Equal(modulo, filtro.Modulo);
        Assert.Equal(AccionPermiso.Ver, filtro.Accion);

        const int empresaPermitida = 101;
        const int empresaAjena = 202;
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(p => p.VerificarPermisoAsync(
                empresaAjena, modulo, AccionPermiso.Ver))
            .ThrowsAsync(new ForbiddenAccessException(
                "La empresa solicitada carece de membresia autorizada."));
        permisos.Setup(p => p.VerificarPermisoAsync(
                empresaPermitida, modulo, AccionPermiso.Ver))
            .Returns(Task.CompletedTask);

        using var services = new ServiceCollection()
            .AddSingleton(permisos.Object)
            .BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        http.Request.Method = HttpMethods.Get;
        http.Request.Headers["X-Empresa-Id"] = empresaAjena.ToString();

        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var execution = new ActionExecutingContext(
            action, filters, new Dictionary<string, object?>(), new object());
        var accionesEjecutadas = 0;
        ActionExecutionDelegate next = () =>
        {
            accionesEjecutadas++;
            return Task.FromResult(new ActionExecutedContext(
                execution, filters, execution.Controller));
        };

        // La cabecera B es solo una solicitud; un permiso denegado no puede
        // ejecutar la accion ni marcar B como tenant autorizado.
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            filtro.OnActionExecutionAsync(execution, next));
        Assert.Equal(0, accionesEjecutadas);
        Assert.False(http.Items.ContainsKey(
            VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey));

        // Control positivo: el mismo filtro permite A despues de que
        // IPermisoService verifica expresamente la empresa y el modulo.
        http.Request.Headers["X-Empresa-Id"] = empresaPermitida.ToString();
        await filtro.OnActionExecutionAsync(execution, next);
        Assert.Equal(1, accionesEjecutadas);
        Assert.Equal(empresaPermitida,
            Assert.IsType<int>(http.Items[
                VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey]));

        permisos.Verify(p => p.VerificarPermisoAsync(
            empresaAjena, modulo, AccionPermiso.Ver), Times.Once);
        permisos.Verify(p => p.VerificarPermisoAsync(
            empresaPermitida, modulo, AccionPermiso.Ver), Times.Once);
        permisos.VerifyNoOtherCalls();
    }
}
