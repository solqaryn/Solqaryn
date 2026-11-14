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

public sealed class N310CreditoClienteSecurityAuditTests
{
    [Fact]
    public void Controller_exige_autenticacion()
    {
        Assert.Contains(
            typeof(CreditosClienteController).GetCustomAttributes(inherit: true),
            attribute => attribute is AuthorizeAttribute);
    }

    [Theory]
    [InlineData(nameof(CreditosClienteController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(CreditosClienteController.GetByCliente), AccionPermiso.Ver)]
    [InlineData(nameof(CreditosClienteController.Crear), AccionPermiso.Crear)]
    [InlineData(nameof(CreditosClienteController.ActualizarPolitica), AccionPermiso.Editar)]
    [InlineData(nameof(CreditosClienteController.AplicarBloqueoAutomatico), AccionPermiso.Editar)]
    [InlineData(nameof(CreditosClienteController.LiberarBloqueoAutomatico), AccionPermiso.Editar)]
    [InlineData(nameof(CreditosClienteController.AutorizarExcepcion), AccionPermiso.Editar)]
    [InlineData(nameof(CreditosClienteController.RevocarExcepcion), AccionPermiso.Editar)]
    public void Endpoint_declara_permiso_clientes_esperado(string methodName, AccionPermiso accion)
    {
        var method = typeof(CreditosClienteController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"No se encontró {methodName}.");
        var permiso = Assert.Single(
            method.CustomAttributes.Where(attribute => attribute.AttributeType == typeof(RequierePermisoAttribute)));

        Assert.Equal((int)ModuloSistema.Clientes, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)accion, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Theory]
    [InlineData(AccionPermiso.Ver)]
    [InlineData(AccionPermiso.Crear)]
    [InlineData(AccionPermiso.Editar)]
    public async Task Administrador_sin_grant_explicito_no_tiene_bypass(AccionPermiso accion)
    {
        var rolPermisos = new Mock<IRolPermisoRepository>(MockBehavior.Strict);
        rolPermisos
            .Setup(x => x.TienePermisoPorRolIdAsync(99, ModuloSistema.Clientes, accion))
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

        Assert.False(await service.TienePermisoAsync(ModuloSistema.Clientes, accion));
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => service.VerificarPermisoAsync(ModuloSistema.Clientes, accion));
    }

    [Fact]
    public async Task Crear_sin_usuario_valido_falla_antes_de_transaccion_y_auditoria()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns((int?)null);

        var uow = new CountingUnitOfWork();
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var service = new CreditoClienteService(
            Mock.Of<ICreditoClienteRepository>(),
            Mock.Of<IClienteRepository>(),
            auditoria.Object,
            currentUser.Object,
            uow);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.CrearAsync(new CreateCreditoClienteDto
        {
            ClienteId = 1,
            Moneda = "HNL",
            LimiteCredito = 1000m,
            DiasCredito = 30,
            UmbralAlertaPorcentaje = 80m
        }));

        Assert.Equal(0, uow.Calls);
        auditoria.VerifyNoOtherCalls();
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public int Calls { get; private set; }

        public async Task ExecuteInTransactionAsync(Func<Task> operation)
        {
            Calls++;
            await operation();
        }
    }
}
