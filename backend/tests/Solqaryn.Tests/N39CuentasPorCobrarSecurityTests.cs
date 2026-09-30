using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N39CuentasPorCobrarSecurityTests
{
    [Fact]
    public void Controller_exige_autenticacion()
    {
        Assert.Contains(
            typeof(CuentasPorCobrarController).GetCustomAttributes(inherit: true),
            attribute => attribute is AuthorizeAttribute);
    }

    [Fact]
    public void GetAll_exige_Facturacion_Ver()
    {
        var method = typeof(CuentasPorCobrarController).GetMethod(nameof(CuentasPorCobrarController.GetAll))
            ?? throw new InvalidOperationException("No se encontró GetAll.");
        var permiso = Assert.Single(
            method.CustomAttributes.Where(attribute => attribute.AttributeType == typeof(RequierePermisoAttribute)));

        Assert.Equal((int)ModuloSistema.Facturacion, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)AccionPermiso.Ver, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Fact]
    public void Controller_expone_solo_GET_read_only()
    {
        var actions = typeof(CuentasPorCobrarController)
            .GetMethods()
            .Where(method => method.DeclaringType == typeof(CuentasPorCobrarController))
            .ToArray();

        var action = Assert.Single(actions);
        Assert.Equal(nameof(CuentasPorCobrarController.GetAll), action.Name);
        Assert.Contains(action.GetCustomAttributes(inherit: true), attribute => attribute is HttpGetAttribute);
        Assert.DoesNotContain(action.GetCustomAttributes(inherit: true), attribute =>
            attribute is HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute);
    }

    [Fact]
    public async Task Administrador_sin_grant_explicito_no_tiene_bypass_Facturacion_Ver()
    {
        var rolPermisos = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        rolPermisos
            .Setup(x => x.TienePermisoPorRolIdAsync(99, ModuloSistema.Facturacion, AccionPermiso.Ver))
            .ReturnsAsync(false);

        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(7, 99, "Administrador", EsAdministrador: true));

        var service = new PermisoService(
            rolPermisos.Object,
            Mock.Of<IRolRepository>(),
            Mock.Of<IPermisoRepository>(),
            Mock.Of<IAuditoriaService>(),
            Mock.Of<ICurrentUserService>(),
            scope.Object);

        Assert.False(await service.TienePermisoAsync(ModuloSistema.Facturacion, AccionPermiso.Ver));
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.VerificarPermisoAsync(ModuloSistema.Facturacion, AccionPermiso.Ver));
    }
}
