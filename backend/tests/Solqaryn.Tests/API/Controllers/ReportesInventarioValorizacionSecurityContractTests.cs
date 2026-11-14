using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ReportesInventarioValorizacionSecurityContractTests
{
    [Fact]
    public void Controller_RequiereAutenticacionYRutaCanonica()
    {
        var controllerType = typeof(ReportesInventarioValorizacionController);
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("inventario/reportes/valorizacion", controllerType.GetCustomAttribute<RouteAttribute>()?.Template);
    }

    [Fact]
    public void GetResumen_RequierePermisoInventarioVer()
    {
        var method = typeof(ReportesInventarioValorizacionController).GetMethod(nameof(ReportesInventarioValorizacionController.GetResumen));
        Assert.NotNull(method);

        var permiso = method!.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.Inventario, (ModuloSistema?)moduloField!.GetValue(permiso));
        Assert.Equal(AccionPermiso.Ver, (AccionPermiso?)accionField!.GetValue(permiso));

        Assert.Equal("resumen", method.GetCustomAttribute<HttpGetAttribute>()?.Template);
    }

    [Fact]
    public void Constructor_ExigeServicios()
    {
        var constructor = Assert.Single(typeof(ReportesInventarioValorizacionController).GetConstructors());
        var parameterTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Contains(typeof(IFinanzasService), parameterTypes);
        Assert.Contains(typeof(IPermisoService), parameterTypes);
        Assert.Contains(typeof(IAuditoriaService), parameterTypes);
    }
}
