using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N15KardexApiContractTests
{
    [Fact]
    public void Controller_ExigeAutenticacion_Y_Ruta_Canonica()
    {
        var tipo = typeof(MovimientosInventarioController);

        Assert.NotNull(tipo.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(tipo.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal("inventario/movimientos", tipo.GetCustomAttribute<RouteAttribute>()?.Template);

        foreach (var metodo in MetodosHttp(tipo))
            Assert.Null(metodo.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(MovimientosInventarioController.GetFiltered), null)]
    [InlineData(nameof(MovimientosInventarioController.GetPaged), "paged")]
    public void Endpoints_De_Lectura_Exigen_Permiso_Relacional_Ver(string nombreMetodo, string? plantilla)
    {
        var metodo = typeof(MovimientosInventarioController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Single(m => m.Name == nombreMetodo);

        var httpGet = Assert.Single(metodo.GetCustomAttributes<HttpGetAttribute>());
        Assert.Equal(plantilla, httpGet.Template);

        var permiso = Assert.Single(metodo.GetCustomAttributes<RequierePermisoAttribute>());
        Assert.Equal(ModuloSistema.MovimientosInventario, LeerCampoPrivado<ModuloSistema>(permiso, "_modulo"));
        Assert.Equal(AccionPermiso.Ver, LeerCampoPrivado<AccionPermiso>(permiso, "_accion"));
    }

    [Fact]
    public async Task GetPaged_Conserva_Envelope_ApiResponse_Y_Metadatos_De_Paginacion()
    {
        var query = new MovimientoInventarioQueryDto { Page = 2, PageSize = 25 };
        var esperado = new PagedResult<MovimientoInventarioDto>
        {
            Page = 2,
            PageSize = 25,
            TotalCount = 51,
            Items = new List<MovimientoInventarioDto>
            {
                new() { Id = 7, ProductoId = 3, CorrelationId = "venta:9:confirmar" }
            }
        };
        var service = new Mock<IMovimientoInventarioService>();
        service.Setup(s => s.GetPagedAsync(query)).ReturnsAsync(esperado);
        var controller = new MovimientosInventarioController(service.Object);

        var action = await controller.GetPaged(query);

        var ok = Assert.IsType<OkObjectResult>(action);
        var envelope = Assert.IsType<ApiResponse<PagedResult<MovimientoInventarioDto>>>(ok.Value);
        Assert.True(envelope.Success);
        Assert.Same(esperado, envelope.Data);
        Assert.Equal(3, envelope.Data!.TotalPages);
    }

    private static IEnumerable<MethodInfo> MetodosHttp(Type tipo) =>
        tipo.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());

    private static T LeerCampoPrivado<T>(object instancia, string nombre)
    {
        var campo = instancia.GetType().GetField(nombre, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(campo);
        return Assert.IsType<T>(campo!.GetValue(instancia));
    }
}
