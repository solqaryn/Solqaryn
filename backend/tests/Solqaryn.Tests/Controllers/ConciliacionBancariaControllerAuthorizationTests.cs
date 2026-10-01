using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Controllers;

public class ConciliacionBancariaControllerAuthorizationTests
{
    [Theory]
    [InlineData(nameof(ConciliacionBancariaController.ImportarEstadoCuenta), AccionPermiso.Importar)]
    [InlineData(nameof(ConciliacionBancariaController.ConciliarMovimientos), AccionPermiso.Crear)]
    [InlineData(nameof(ConciliacionBancariaController.SolicitarAjuste), AccionPermiso.Crear)]
    [InlineData(nameof(ConciliacionBancariaController.CerrarPeriodo), AccionPermiso.Cerrar)]
    [InlineData(nameof(ConciliacionBancariaController.ReabrirPeriodo), AccionPermiso.Reabrir)]
    public void OperacionesExigenPermisoFinanzas(string methodName, AccionPermiso expectedAction)
    {
        var method = typeof(ConciliacionBancariaController)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var requierePermiso = method!.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(requierePermiso);

        var moduloField = typeof(RequierePermisoAttribute)
            .GetField("_modulo", BindingFlags.Instance | BindingFlags.NonPublic);
        var accionField = typeof(RequierePermisoAttribute)
            .GetField("_accion", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.Finanzas, (ModuloSistema)moduloField!.GetValue(requierePermiso)!);
        Assert.Equal(expectedAction, (AccionPermiso)accionField!.GetValue(requierePermiso)!);
    }
}
