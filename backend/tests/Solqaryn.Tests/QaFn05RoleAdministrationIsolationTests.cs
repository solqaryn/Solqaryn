using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QaFn05RoleAdministrationIsolationTests
{
    [Fact]
    public async Task ControladorMatriz_UsaSoloEmpresaAutorizadaPorElGate()
    {
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var dto = new UpdatePermisoMatrizDto();
        permisos.Setup(x => x.GetMatrizEmpresaAsync(7, 11))
            .ReturnsAsync(new List<PermisoMatrizItemDto>());
        permisos.Setup(x => x.UpdateMatrizEmpresaAsync(7, dto, 11))
            .ReturnsAsync(new List<PermisoMatrizItemDto>());
        var http = new DefaultHttpContext();
        // Simulate the already verified gate context and an unrelated raw header.
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 11;
        http.Request.Headers["X-Empresa-Id"] = "12";
        var controller = new PermisosController(permisos.Object, Mock.Of<IPermisoCatalogoService>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        Assert.IsType<OkObjectResult>(await controller.GetMatriz(7));
        Assert.IsType<OkObjectResult>(await controller.UpdateMatriz(7, dto));
        permisos.Verify(x => x.GetMatrizEmpresaAsync(7, 11), Times.Once);
        permisos.Verify(x => x.UpdateMatrizEmpresaAsync(7, dto, 11), Times.Once);
        permisos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ControladoresAdministrativos_NoAceptanCabeceraSinGateAutorizado()
    {
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var roles = new Mock<IRolService>(MockBehavior.Strict);
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = "11";
        var permisosController = new PermisosController(permisos.Object, Mock.Of<IPermisoCatalogoService>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        var rolesController = new RolesController(roles.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => permisosController.GetMatriz(7));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => permisosController.UpdateMatriz(7, new UpdatePermisoMatrizDto()));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => rolesController.GetAll());
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => rolesController.GetById(7));
        permisos.VerifyNoOtherCalls();
        roles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RolesEmpresariales_NoExponenRolesAjenosNiGlobalesYProtegenPlantillas()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"qa-fn05-role-admin-{Guid.NewGuid():N}").Options;
        await using var db = new AppDbContext(options);
        db.Set<Empresa>().AddRange(new Empresa("A") { Id = 11 }, new Empresa("B") { Id = 12 });
        db.Roles.AddRange(
            new Rol { Id = 1, Nombre = "Plantilla", NombreNormalizado = "plantilla", EsSistema = true },
            new Rol { Id = 2, Nombre = "A", NombreNormalizado = "a", EmpresaId = 11 },
            new Rol { Id = 3, Nombre = "B", NombreNormalizado = "b", EmpresaId = 12 },
            new Rol { Id = 4, Nombre = "Plataforma", NombreNormalizado = "plataforma", Ambito = AmbitoAutorizacion.Plataforma },
            new Rol { Id = 5, Nombre = "Legacy sin dueño", NombreNormalizado = "legacy" });
        await db.SaveChangesAsync();
        var service = new RolService(new RolRepository(db), Mock.Of<IAuditoriaService>(), Mock.Of<ICurrentUserService>());
        Assert.Equal(new[] { 1, 2 }, (await service.GetAllEmpresaAsync(11)).Select(r => r.Id).OrderBy(x => x));
        Assert.Equal(new[] { 1, 3 }, (await service.GetAllEmpresaAsync(12)).Select(r => r.Id).OrderBy(x => x));
        Assert.Null(await service.GetByIdEmpresaAsync(3, 11));
        Assert.Null(await service.GetByIdEmpresaAsync(4, 11));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateEmpresaAsync(3, new ActualizarRolDto { Nombre = "Ajeno" }, 11));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.DesactivarEmpresaAsync(1, 11));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DuplicarEmpresaAsync(4, "No", 11));
        var nuevo = await service.CreateEmpresaAsync(new CrearRolDto { Nombre = "Nuevo A" }, 11);
        var copia = await service.DuplicarEmpresaAsync(1, "Copia A", 11);
        Assert.Equal(11, (await db.Roles.SingleAsync(r => r.Id == nuevo.Id)).EmpresaId);
        Assert.Equal(11, (await db.Roles.SingleAsync(r => r.Id == copia.Id)).EmpresaId);
        Assert.Null(await service.GetByIdEmpresaAsync(nuevo.Id, 12));
        Assert.True((await db.Roles.SingleAsync(r => r.Id == 1)).Activo);
        Assert.Equal("B", (await db.Roles.SingleAsync(r => r.Id == 3)).Nombre);
    }

    [Fact]
    public async Task MatrizEmpresarial_DeniegaPlantillaRolAjenoYPermisoPlataforma()
    {
        var roles = new Mock<IRolRepository>();
        roles.Setup(x => x.GetByIdEmpresaAsync(1, 11)).ReturnsAsync(new Rol { Id = 1, EsSistema = true });
        roles.Setup(x => x.GetByIdEmpresaAsync(2, 11)).ReturnsAsync(new Rol { Id = 2, EmpresaId = 11 });
        roles.Setup(x => x.GetByIdEmpresaAsync(3, 11)).ReturnsAsync((Rol?)null);
        var grants = new Mock<IRolPermisoRepository>();
        var permisos = new Mock<IPermisoRepository>();
        var plataforma = new Permiso { Id = 9, Modulo = ModuloSistema.Usuarios, Accion = AccionPermiso.Ver, Ambito = AmbitoAutorizacion.Plataforma };
        permisos.Setup(x => x.GetAllAsync(false)).ReturnsAsync(new List<Permiso> { plataforma });
        permisos.Setup(x => x.GetByModuloAccionAsync(ModuloSistema.Usuarios, AccionPermiso.Ver)).ReturnsAsync(plataforma);
        var service = new PermisoService(grants.Object, roles.Object, permisos.Object,
            Mock.Of<IAuditoriaService>(), Mock.Of<ICurrentUserService>(), Mock.Of<IUsuarioScopeService>());
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetMatrizEmpresaAsync(3, 11));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.UpdateMatrizEmpresaAsync(1, new UpdatePermisoMatrizDto(), 11));
        var request = new UpdatePermisoMatrizDto
        {
            Permisos = new() { new() { Modulo = ModuloSistema.Usuarios.ToString(), Accion = AccionPermiso.Ver.ToString(), Permitido = true } }
        };
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.UpdateMatrizEmpresaAsync(2, request, 11));
        grants.Verify(x => x.ReemplazarMatrizPorRolIdAsync(It.IsAny<int>(), It.IsAny<List<RolPermiso>>()), Times.Never);
    }
}
