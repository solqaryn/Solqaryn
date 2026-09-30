using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public class UsuariosEmpresaAuthorizationContractTests
{
    private readonly Type _controllerType = typeof(UsuariosController);

    [Fact]
    public void Controller_DebeRequerirAutenticacion_Y_RutaUsuarios()
    {
        Assert.NotNull(_controllerType.GetCustomAttribute<AuthorizeAttribute>());
        var route = _controllerType.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(route);
        Assert.Equal("usuarios", route.Template);
    }

    [Theory]
    [InlineData(nameof(UsuariosController.GetEmpresas), typeof(HttpGetAttribute), "{id:int}/empresas", AccionPermiso.Ver)]
    [InlineData(nameof(UsuariosController.AsignarEmpresa), typeof(HttpPostAttribute), "{id:int}/empresas", AccionPermiso.AsignarRol)]
    [InlineData(nameof(UsuariosController.CambiarRolEmpresa), typeof(HttpPutAttribute), "{id:int}/empresas/{empresaId:int}/rol", AccionPermiso.AsignarRol)]
    public void Endpoints_Empresa_DebenExigirPermisoUsuarios(
        string methodName,
        Type expectedVerb,
        string expectedTemplate,
        AccionPermiso expectedAccion)
    {
        var method = _controllerType.GetMethod(methodName);
        Assert.NotNull(method);

        var verb = method.GetCustomAttributes().Single(attribute => expectedVerb.IsInstanceOfType(attribute));
        var template = verb switch
        {
            HttpGetAttribute get => get.Template,
            HttpPostAttribute post => post.Template,
            HttpPutAttribute put => put.Template,
            _ => null
        };
        Assert.Equal(expectedTemplate, template);

        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.Usuarios, (ModuloSistema?)moduloField.GetValue(permiso));
        Assert.Equal(expectedAccion, (AccionPermiso?)accionField.GetValue(permiso));
    }

    [Fact]
    public async Task CambiarEstadoEmpresa_DebeExigirPermisoActivar_CuandoActivaEsTrue()
    {
        var usuarioService = new Mock<IUsuarioService>();
        var permisoService = new Mock<IPermisoService>();
        var currentUser = new Mock<ICurrentUserService>();
        usuarioService
            .Setup(service => service.CambiarEstadoEmpresaAsync(7, 11, true))
            .ReturnsAsync((UsuarioEmpresaDto)null!);

        var controller = new UsuariosController(usuarioService.Object, permisoService.Object, currentUser.Object);

        await controller.CambiarEstadoEmpresa(7, 11, new UpdateUsuarioEmpresaEstadoDto { Activa = true });

        permisoService.Verify(
            service => service.VerificarPermisoAsync(ModuloSistema.Usuarios, AccionPermiso.Activar),
            Times.Once);
        usuarioService.Verify(service => service.CambiarEstadoEmpresaAsync(7, 11, true), Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoEmpresa_DebeExigirPermisoDesactivar_CuandoActivaEsFalse()
    {
        var usuarioService = new Mock<IUsuarioService>();
        var permisoService = new Mock<IPermisoService>();
        var currentUser = new Mock<ICurrentUserService>();
        usuarioService
            .Setup(service => service.CambiarEstadoEmpresaAsync(7, 11, false))
            .ReturnsAsync((UsuarioEmpresaDto)null!);

        var controller = new UsuariosController(usuarioService.Object, permisoService.Object, currentUser.Object);

        await controller.CambiarEstadoEmpresa(7, 11, new UpdateUsuarioEmpresaEstadoDto { Activa = false });

        permisoService.Verify(
            service => service.VerificarPermisoAsync(ModuloSistema.Usuarios, AccionPermiso.Desactivar),
            Times.Once);
        usuarioService.Verify(service => service.CambiarEstadoEmpresaAsync(7, 11, false), Times.Once);
    }
}
