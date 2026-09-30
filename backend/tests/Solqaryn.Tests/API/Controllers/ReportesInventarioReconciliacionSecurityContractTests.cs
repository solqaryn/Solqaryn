using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ReportesInventarioReconciliacionSecurityContractTests
{
    [Fact]
    public void Controller_RequiereAutenticacionPermisoScopeAuditoriaYCensuraFinanciera()
    {
        var type = typeof(ReportesInventarioReconciliacionController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());

        var method = type.GetMethod(nameof(ReportesInventarioReconciliacionController.Get));
        var permiso = method?.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.Equal(ModuloSistema.Inventario, (ModuloSistema?)moduloField!.GetValue(permiso));
        Assert.Equal(AccionPermiso.Ver, (AccionPermiso?)accionField!.GetValue(permiso));

        var parameters = Assert.Single(type.GetConstructors()).GetParameters().Select(p => p.ParameterType).ToArray();
        Assert.Contains(typeof(IUsuarioScopeService), parameters);
        Assert.Contains(typeof(IPermisoService), parameters);
        Assert.Contains(typeof(IAuditoriaService), parameters);
    }

    [Fact]
    public async Task Get_FiltroInvalido_FallaCerradoAntesDeConsultarDatos()
    {
        await using var context = CreateContext();
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var controller = new ReportesInventarioReconciliacionController(
            context, scope.Object, permisos.Object, auditoria.Object);

        var result = await controller.Get(
            new ReporteInventarioReconciliacionFiltroDto { SortDirection = "invalid" },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("SortDirection", response.Errors!.Single().ToString());
    }

    [Fact]
    public async Task Get_SinFinanzas_CensuraYRegistraAuditoriaSinImportes()
    {
        await using var context = CreateContext();
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(x => x.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(1, 1, "Administrador", true));
        var permisos = new Mock<IPermisoService>();
        permisos.Setup(x => x.TienePermisoAsync(1, ModuloSistema.Finanzas, AccionPermiso.Ver))
            .ReturnsAsync(false);
        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarAsync(
                ModuloSistema.Inventario,
                AccionPermiso.Ver,
                It.Is<string>(descripcion => descripcion.Contains("censurados")),
                null,
                "ReporteInventarioReconciliacion",
                null,
                It.IsAny<object>(),
                null,
                "Exito",
                null))
            .Returns(Task.CompletedTask);

        var controller = new ReportesInventarioReconciliacionController(
            context, scope.Object, permisos.Object, auditoria.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Request.Headers["X-Empresa-Id"] = "1";

        var result = await controller.Get(
            new ReporteInventarioReconciliacionFiltroDto { Page = 1, PageSize = 10 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<PagedResult<ReporteInventarioReconciliacionDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Data!.Items);
        permisos.VerifyAll();
        auditoria.VerifyAll();
    }

    [Fact]
    public async Task ScopeFisicoExplicitoSinScopeResuelto_FallaCerrado()
    {
        var filtro = new ReporteInventarioReconciliacionFiltroDto { AlmacenId = 7 };

        var permitido = await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(
            filtro,
            new NullScopeService());

        Assert.False(permitido);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class NullScopeService : IUsuarioScopeService
    {
        public Task<UsuarioScopeActual?> ObtenerActualAsync() => Task.FromResult<UsuarioScopeActual?>(null);
    }
}
