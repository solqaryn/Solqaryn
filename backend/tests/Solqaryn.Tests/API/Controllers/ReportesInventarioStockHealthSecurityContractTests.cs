using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ReportesInventarioStockHealthSecurityContractTests
{
    [Fact]
    public void Controller_FallaCerrado_ConAutenticacionPermisoYAuditoria()
    {
        var type = typeof(ReportesInventarioStockHealthController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("inventario/reportes/stock-health", type.GetCustomAttribute<RouteAttribute>()?.Template);

        var method = type.GetMethod(nameof(ReportesInventarioStockHealthController.Get));
        Assert.NotNull(method);
        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Equal(ModuloSistema.Inventario, (ModuloSistema?)moduloField?.GetValue(permiso));
        Assert.Equal(AccionPermiso.Ver, (AccionPermiso?)accionField?.GetValue(permiso));

        var ctor = Assert.Single(type.GetConstructors());
        Assert.Contains(ctor.GetParameters(), parameter => parameter.ParameterType == typeof(IAuditoriaService));
    }

    [Fact]
    public void QueryRules_RechazanVentanaNoAcotada()
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
    public void QueryRules_NormalizanVentanaPermitida()
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
    public void QueryRules_RechazanSortFueraDeAllowlist()
    {
        var filtro = new ReporteInventarioStockHealthFiltroDto
        {
            Dias = 30,
            SortBy = "CostoUnitario",
            SortDirection = "desc"
        };

        var error = ReporteInventarioQueryRules.ValidateStockHealth(filtro);

        Assert.NotNull(error);
        Assert.Contains("SortBy no permitido", error);
    }
}
