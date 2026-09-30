using System.Linq;
using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ReportesInventarioAuthorizationContractTests
{
    [Theory]
    [InlineData(typeof(ReportesInventarioValorizacionController), "inventario/reportes/valorizacion")]
    [InlineData(typeof(ReportesInventarioKardexController), "inventario/reportes/kardex")]
    [InlineData(typeof(ReportesInventarioStockHealthController), "inventario/reportes/stock-health")]
    [InlineData(typeof(ReportesInventarioReconciliacionController), "inventario/reportes/reconciliacion")]
    public void ReportController_RequiereAutenticacionYRutaCanonica(Type controllerType, string expectedRoute)
    {
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());
        var route = controllerType.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(route);
        Assert.Equal(expectedRoute, route.Template);
    }

    [Fact]
    public void Valorizacion_RequiereInventarioVer()
        => AssertPermission(typeof(ReportesInventarioValorizacionController), nameof(ReportesInventarioValorizacionController.GetResumen), ModuloSistema.Inventario, AccionPermiso.Ver);

    [Fact]
    public void Kardex_RequiereMovimientosConsultarHistorial()
        => AssertPermission(typeof(ReportesInventarioKardexController), nameof(ReportesInventarioKardexController.Get), ModuloSistema.MovimientosInventario, AccionPermiso.ConsultarHistorial);

    [Fact]
    public void StockHealth_RequiereInventarioVer()
        => AssertPermission(typeof(ReportesInventarioStockHealthController), nameof(ReportesInventarioStockHealthController.Get), ModuloSistema.Inventario, AccionPermiso.Ver);

    [Fact]
    public void Reconciliacion_RequiereInventarioVer()
        => AssertPermission(typeof(ReportesInventarioReconciliacionController), nameof(ReportesInventarioReconciliacionController.Get), ModuloSistema.Inventario, AccionPermiso.Ver);

    [Fact]
    public async Task Reportes_SinPermiso_FallanCerradoAntesDeEjecutarAccion()
    {
        const int empresaId = 31;
        var matrix = new (Type Controller, string Method, ModuloSistema Module, AccionPermiso Action)[]
        {
            (typeof(ReportesInventarioValorizacionController), nameof(ReportesInventarioValorizacionController.GetResumen), ModuloSistema.Inventario, AccionPermiso.Ver),
            (typeof(ReportesInventarioKardexController), nameof(ReportesInventarioKardexController.Get), ModuloSistema.MovimientosInventario, AccionPermiso.ConsultarHistorial),
            (typeof(ReportesInventarioStockHealthController), nameof(ReportesInventarioStockHealthController.Get), ModuloSistema.Inventario, AccionPermiso.Ver),
            (typeof(ReportesInventarioReconciliacionController), nameof(ReportesInventarioReconciliacionController.Get), ModuloSistema.Inventario, AccionPermiso.Ver)
        };

        foreach (var entry in matrix)
        {
            var method = entry.Controller.GetMethod(entry.Method);
            Assert.NotNull(method);
            var filter = method.GetCustomAttribute<RequierePermisoAttribute>();
            Assert.NotNull(filter);

            var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
            permisos
                .Setup(service => service.VerificarPermisoAsync(empresaId, entry.Module, entry.Action))
                .ThrowsAsync(new ForbiddenAccessException("denegado"));

            using var services = new ServiceCollection()
                .AddSingleton(permisos.Object)
                .BuildServiceProvider();
            var httpContext = new DefaultHttpContext { RequestServices = services };
            httpContext.Request.Headers["X-Empresa-Id"] = empresaId.ToString();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var executingContext = new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                new object());

            var nextCalled = false;
            ActionExecutionDelegate next = () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(
                    actionContext,
                    new List<IFilterMetadata>(),
                    new object()));
            };

            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => filter.OnActionExecutionAsync(executingContext, next));
            Assert.False(nextCalled);
            permisos.VerifyAll();
        }
    }

    [Fact]
    public void QueryRules_RechazaVentanaMayorA366Dias()
    {
        var filtro = new ReporteInventarioKardexFiltroDto
        {
            Desde = new DateTime(2026, 1, 1),
            Hasta = new DateTime(2027, 1, 3),
            SortBy = "Fecha",
            SortDirection = "desc"
        };

        var error = ReporteInventarioQueryRules.Validate(filtro, "Fecha");
        Assert.NotNull(error);
        Assert.Contains("366", error);
    }

    [Fact]
    public void QueryRules_SinPeriodo_NormalizaVentanaHistoricaAcotada()
    {
        var filtro = new ReporteInventarioKardexFiltroDto
        {
            SortBy = "Fecha",
            SortDirection = "desc"
        };

        var error = ReporteInventarioQueryRules.Validate(filtro, "Fecha");

        Assert.Null(error);
        Assert.True(filtro.Desde.HasValue);
        Assert.True(filtro.Hasta.HasValue);
        Assert.Equal(
            TimeSpan.FromDays(ReporteInventarioQueryRules.MaxHistoricalDays),
            filtro.Hasta.Value - filtro.Desde.Value);
    }

    [Fact]
    public void QueryRules_DesdeAbiertoMayorAlLimite_FallaCerrado()
    {
        var filtro = new ReporteInventarioKardexFiltroDto
        {
            Desde = DateTime.UtcNow.AddDays(-(ReporteInventarioQueryRules.MaxHistoricalDays + 2)),
            SortBy = "Fecha",
            SortDirection = "desc"
        };

        var error = ReporteInventarioQueryRules.Validate(filtro, "Fecha");

        Assert.NotNull(error);
        Assert.Contains("366", error);
    }

    [Fact]
    public void QueryRules_RechazaDireccionYSortNoPermitidos()
    {
        var direccion = new ReporteInventarioKardexFiltroDto { SortBy = "Fecha", SortDirection = "sideways" };
        Assert.NotNull(ReporteInventarioQueryRules.Validate(direccion, "Fecha"));

        var sort = new ReporteInventarioKardexFiltroDto { SortBy = "CostoSecreto", SortDirection = "desc" };
        Assert.NotNull(ReporteInventarioQueryRules.Validate(sort, "Fecha"));
    }

    [Fact]
    public void QueryRules_StockHealthMantieneDiasAcotados()
    {
        var filtro = new ReporteInventarioStockHealthFiltroDto
        {
            Dias = ReporteInventarioQueryRules.MaxHistoricalDays + 1,
            SortBy = "Fecha",
            SortDirection = "desc"
        };

        var error = ReporteInventarioQueryRules.ValidateStockHealth(filtro);
        Assert.NotNull(error);
        Assert.Contains("Dias", error);
    }

    [Fact]
    public void QueryRules_StockHealthSinPeriodo_NormalizaConDiasSolicitados()
    {
        var filtro = new ReporteInventarioStockHealthFiltroDto
        {
            Dias = 30,
            SortBy = "Fecha",
            SortDirection = "desc"
        };

        var error = ReporteInventarioQueryRules.ValidateStockHealth(filtro);

        Assert.Null(error);
        Assert.True(filtro.Desde.HasValue);
        Assert.True(filtro.Hasta.HasValue);
        Assert.Equal(TimeSpan.FromDays(30), filtro.Hasta.Value - filtro.Desde.Value);
    }

    [Fact]
    public async Task ScopeGuard_SinFiltroFisico_ConservaScopeServerSideExistente()
    {
        var filtro = new ReporteInventarioStockHealthFiltroDto();
        var permitido = await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(
            filtro,
            new FakeScopeService(null));

        Assert.True(permitido);
    }

    [Fact]
    public async Task ScopeGuard_FiltroFisicoExplicito_NoAdmin_FallaCerrado()
    {
        var filtro = new ReporteInventarioStockHealthFiltroDto { AlmacenId = 7 };
        var permitido = await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(
            filtro,
            new FakeScopeService(new UsuarioScopeActual(41, 3, "Operador", false)));

        Assert.False(permitido);
    }

    [Fact]
    public async Task ScopeGuard_FiltroFisicoExplicito_Admin_Permitido()
    {
        var filtro = new ReporteInventarioReconciliacionFiltroDto { SucursalId = 2 };
        var permitido = await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(
            filtro,
            new FakeScopeService(new UsuarioScopeActual(1, 1, "Administrador", true)));

        Assert.True(permitido);
    }

    [Fact]
    public async Task ScopeGuard_FiltroFisicoExplicito_ScopeNoResuelto_FallaCerrado()
    {
        var filtro = new ReporteInventarioKardexFiltroDto { UbicacionAlmacenId = 9 };
        var permitido = await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(
            filtro,
            new FakeScopeService(null));

        Assert.False(permitido);
    }

    private static void AssertPermission(Type controllerType, string methodName, ModuloSistema expectedModule, AccionPermiso expectedAction)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);
        Assert.NotNull(method.GetCustomAttribute<HttpGetAttribute>());

        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);

        Assert.Equal(expectedModule, (ModuloSistema?)moduloField.GetValue(permiso));
        Assert.Equal(expectedAction, (AccionPermiso?)accionField.GetValue(permiso));

        var definicion = Assert.Single(CatalogoPermisosBase.Definicion.Where(item => item.Modulo == expectedModule));
        Assert.Contains(expectedAction, definicion.Acciones);
    }

    private sealed class FakeScopeService : IUsuarioScopeService
    {
        private readonly UsuarioScopeActual? _scope;

        public FakeScopeService(UsuarioScopeActual? scope) => _scope = scope;

        public Task<UsuarioScopeActual?> ObtenerActualAsync() => Task.FromResult(_scope);
    }
}
