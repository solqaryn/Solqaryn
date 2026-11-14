using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N110CosteoInventarioRbacContractTests
{
    [Fact]
    public void Controller_requiere_usuario_autenticado()
    {
        var authorize = typeof(CosteoInventarioController)
            .GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
    }

    [Theory]
    [InlineData(nameof(CosteoInventarioController.GetVigente), AccionPermiso.Ver)]
    [InlineData(nameof(CosteoInventarioController.GetHistorial), AccionPermiso.Ver)]
    [InlineData(nameof(CosteoInventarioController.GetMetodos), AccionPermiso.Ver)]
    [InlineData(nameof(CosteoInventarioController.Cambiar), AccionPermiso.Editar)]
    public void Endpoints_exigen_permiso_relacional_de_movimientos_inventario(
        string metodo,
        AccionPermiso accionEsperada)
    {
        var action = typeof(CosteoInventarioController)
            .GetMethod(metodo, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(action);
        var permiso = action!.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute)
            .GetField("_modulo", BindingFlags.Instance | BindingFlags.NonPublic);
        var accionField = typeof(RequierePermisoAttribute)
            .GetField("_accion", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.MovimientosInventario, moduloField!.GetValue(permiso));
        Assert.Equal(accionEsperada, accionField!.GetValue(permiso));
    }
}
