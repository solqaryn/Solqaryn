using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ReportesInventarioCommonSecurityContractTests
{
    [Fact]
    public void Shell_RequiereAutenticacionRutaCanonicaYPermisoInventarioVer()
    {
        var controllerType = typeof(ReportesInventarioController);
        Assert.NotNull(controllerType.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("inventario/reportes", controllerType.GetCustomAttribute<RouteAttribute>()?.Template);

        var method = controllerType.GetMethod(nameof(ReportesInventarioController.GetEstado));
        Assert.NotNull(method);
        Assert.Equal("estado", method!.GetCustomAttribute<HttpGetAttribute>()?.Template);

        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);
        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Equal(ModuloSistema.Inventario, (ModuloSistema?)moduloField!.GetValue(permiso));
        Assert.Equal(AccionPermiso.Ver, (AccionPermiso?)accionField!.GetValue(permiso));
    }

    [Fact]
    public void Shell_ExigeAuditoriaComoDependencia()
    {
        var constructor = Assert.Single(typeof(ReportesInventarioController).GetConstructors());
        Assert.Contains(constructor.GetParameters(), p => p.ParameterType == typeof(IAuditoriaService));
    }
}
