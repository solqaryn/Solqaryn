using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N17ConteosInventarioRbacContractTests
{
    [Fact]
    public void Controller_ExigeAutenticacionGlobal()
    {
        Assert.NotNull(typeof(ConteosInventarioController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Theory]
    [InlineData(nameof(ConteosInventarioController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(ConteosInventarioController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(ConteosInventarioController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(ConteosInventarioController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(ConteosInventarioController.Iniciar), AccionPermiso.CambiarEstado)]
    [InlineData(nameof(ConteosInventarioController.Capturar), AccionPermiso.Editar)]
    [InlineData(nameof(ConteosInventarioController.Cerrar), AccionPermiso.Cerrar)]
    [InlineData(nameof(ConteosInventarioController.Aprobar), AccionPermiso.Aprobar)]
    [InlineData(nameof(ConteosInventarioController.GenerarAjuste), AccionPermiso.Crear)]
    [InlineData(nameof(ConteosInventarioController.Cancelar), AccionPermiso.Anular)]
    public void Endpoint_UsaPermisoEspecificoMovimientosInventario(string metodo, AccionPermiso accion)
    {
        var method = typeof(ConteosInventarioController).GetMethods()
            .Single(x => x.Name == metodo);
        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();

        Assert.NotNull(permiso);
        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.Instance | BindingFlags.NonPublic);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.MovimientosInventario, moduloField!.GetValue(permiso));
        Assert.Equal(accion, accionField!.GetValue(permiso));
    }
}
