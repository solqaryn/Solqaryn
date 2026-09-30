using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N37NotaCreditoClienteSecurityAuditTests
{
    [Fact]
    public void Controller_exige_autenticacion()
    {
        Assert.Contains(
            typeof(NotasCreditoClienteController).GetCustomAttributes(inherit: true),
            attribute => attribute is AuthorizeAttribute);
    }

    [Theory]
    [InlineData(nameof(NotasCreditoClienteController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(NotasCreditoClienteController.Create), AccionPermiso.Crear)]
    public void Endpoint_declara_permiso_ventas_esperado(string methodName, AccionPermiso accion)
    {
        var method = typeof(NotasCreditoClienteController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"No se encontró {methodName}.");
        var permiso = Assert.Single(
            method.CustomAttributes.Where(attribute => attribute.AttributeType == typeof(RequierePermisoAttribute)));

        Assert.Equal((int)ModuloSistema.Ventas, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)accion, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Theory]
    [InlineData(AccionPermiso.Ver)]
    [InlineData(AccionPermiso.Crear)]
    public async Task Administrador_sin_grant_explicito_no_tiene_bypass(AccionPermiso accion)
    {
        var rolPermisos = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        rolPermisos
            .Setup(x => x.TienePermisoPorRolIdAsync(99, ModuloSistema.Ventas, accion))
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

        Assert.False(await service.TienePermisoAsync(ModuloSistema.Ventas, accion));
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.VerificarPermisoAsync(ModuloSistema.Ventas, accion));
    }

    [Fact]
    public async Task Create_sin_usuario_autenticado_falla_antes_de_transaccion_y_auditoria()
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        currentUser.SetupGet(x => x.EstaAutenticado).Returns(false);
        currentUser.SetupGet(x => x.UsuarioId).Returns((int?)null);

        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var service = new NotaCreditoClienteService(
            Mock.Of<INotaCreditoClienteRepository>(),
            Mock.Of<IFacturaRepository>(),
            currentUser.Object,
            unitOfWork.Object,
            auditoria.Object);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CreateAsync(new CreateNotaCreditoClienteDto
        {
            FacturaId = 1,
            MontoCredito = 1m,
            Motivo = "QA seguridad"
        }));

        unitOfWork.VerifyNoOtherCalls();
        auditoria.VerifyNoOtherCalls();
    }
}
