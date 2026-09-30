using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N49FPeriodoContableRbacContractTests
{
    [Fact]
    public void Controller_ExigeAutenticacionGlobal()
    {
        Assert.NotNull(typeof(PeriodosContablesController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Theory]
    [InlineData(nameof(PeriodosContablesController.GetAll), AccionPermiso.Ver)]
    [InlineData(nameof(PeriodosContablesController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(PeriodosContablesController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(PeriodosContablesController.Cerrar), AccionPermiso.Cerrar)]
    public void Endpoint_UsaPermisoEspecificoConfiguracion(string metodo, AccionPermiso accion)
    {
        var method = typeof(PeriodosContablesController).GetMethods()
            .Single(x => x.Name == metodo);
        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();

        Assert.NotNull(permiso);
        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.Instance | BindingFlags.NonPublic);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.Configuracion, moduloField!.GetValue(permiso));
        Assert.Equal(accion, accionField!.GetValue(permiso));
    }
}
