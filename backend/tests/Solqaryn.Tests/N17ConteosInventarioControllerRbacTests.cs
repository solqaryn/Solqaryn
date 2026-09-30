using System.Globalization;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public class N17ConteosInventarioControllerRbacTests
{
    [Fact]
    public void Controller_ExigeAutenticacionGlobal()
    {
        Assert.Contains(
            typeof(ConteosInventarioController).CustomAttributes,
            atributo => atributo.AttributeType == typeof(AuthorizeAttribute));
    }

    [Fact]
    public void Endpoints_NoPermitenBypassAnonimo()
    {
        Assert.DoesNotContain(
            typeof(ConteosInventarioController).CustomAttributes,
            atributo => atributo.AttributeType == typeof(AllowAnonymousAttribute));

        var endpoints = typeof(ConteosInventarioController)
            .GetMethods()
            .Where(m => m.DeclaringType == typeof(ConteosInventarioController));

        Assert.DoesNotContain(endpoints, method =>
            method.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(AllowAnonymousAttribute)));
    }

    [Theory]
    [InlineData(nameof(ConteosInventarioController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(ConteosInventarioController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(ConteosInventarioController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(ConteosInventarioController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(ConteosInventarioController.Iniciar), AccionPermiso.CambiarEstado)]
    [InlineData(nameof(ConteosInventarioController.Capturar), AccionPermiso.Editar)]
    [InlineData(nameof(ConteosInventarioController.CapturarLote), AccionPermiso.Editar)]
    [InlineData(nameof(ConteosInventarioController.Cerrar), AccionPermiso.Cerrar)]
    [InlineData(nameof(ConteosInventarioController.Aprobar), AccionPermiso.Aprobar)]
    [InlineData(nameof(ConteosInventarioController.GenerarAjuste), AccionPermiso.Crear)]
    [InlineData(nameof(ConteosInventarioController.Cancelar), AccionPermiso.Anular)]
    public void Endpoints_ExigenPermisoEspecificoDeMovimientosInventario(
        string metodo,
        AccionPermiso accionEsperada)
    {
        var methodInfo = typeof(ConteosInventarioController).GetMethod(metodo);
        Assert.NotNull(methodInfo);

        var permiso = Assert.Single(methodInfo!.CustomAttributes.Where(a =>
            a.AttributeType == typeof(RequierePermisoAttribute)));

        var modulo = (ModuloSistema)Convert.ToInt32(
            permiso.ConstructorArguments[0].Value,
            CultureInfo.InvariantCulture);
        var accion = (AccionPermiso)Convert.ToInt32(
            permiso.ConstructorArguments[1].Value,
            CultureInfo.InvariantCulture);

        Assert.Equal(ModuloSistema.MovimientosInventario, modulo);
        Assert.Equal(accionEsperada, accion);
    }
}