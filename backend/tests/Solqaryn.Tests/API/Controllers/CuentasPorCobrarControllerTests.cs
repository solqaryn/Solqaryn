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

namespace Solqaryn.Tests.API.Controllers;

public sealed class CuentasPorCobrarControllerTests
{
    [Fact]
    public void Controller_DebeExigirAutenticacionRutaYPermisoVer()
    {
        var type = typeof(CuentasPorCobrarController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("cuentas-por-cobrar", type.GetCustomAttribute<RouteAttribute>()?.Template);

        var method = type.GetMethod(nameof(CuentasPorCobrarController.GetAll));
        Assert.NotNull(method);
        var get = method!.GetCustomAttribute<HttpGetAttribute>();
        Assert.NotNull(get);
        Assert.Null(get!.Template);

        var permiso = Assert.Single(method.CustomAttributes.Where(a => a.AttributeType == typeof(RequierePermisoAttribute)));
        Assert.Equal((int)ModuloSistema.Facturacion, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)AccionPermiso.Ver, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Fact]
    public async Task GetAll_FiltraSaldosNoCobrablesYOrdenaPorVencimiento()
    {
        var service = new Mock<IFacturaService>();
        service.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<FacturaDto>
        {
            new() { NumeroFactura = "FAC-003", SaldoPendiente = 100, Estado = "Emitida", FechaVencimiento = new DateTime(2026, 9, 20) },
            new() { NumeroFactura = "FAC-001", SaldoPendiente = 50, Estado = "Emitida", FechaVencimiento = new DateTime(2026, 9, 10) },
            new() { NumeroFactura = "FAC-000", SaldoPendiente = 0, Estado = "Pagada", FechaVencimiento = new DateTime(2026, 9, 1) },
            new() { NumeroFactura = "FAC-002", SaldoPendiente = 75, Estado = EstadoFactura.Cancelada.ToString(), FechaVencimiento = new DateTime(2026, 9, 5) },
            new() { NumeroFactura = "FAC-004", SaldoPendiente = 90, Estado = EstadoFactura.Anulada.ToString(), FechaVencimiento = new DateTime(2026, 9, 6) }
        });

        var controller = new CuentasPorCobrarController(service.Object);
        var result = await controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<List<FacturaDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(new[] { "FAC-001", "FAC-003" }, response.Data!.Select(x => x.NumeroFactura).ToArray());
        Assert.All(response.Data, x => Assert.True(x.SaldoPendiente > 0));
    }
}
