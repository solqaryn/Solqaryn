using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QaFn05ReportesTenantTests
{
    [Fact]
    public async Task Controlador_ExigeAdministradorEfectivoEnTodosLosEndpoints()
    {
        var service = new Mock<IReporteAdministrativoService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var http = new DefaultHttpContext();
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 12;
        http.Request.Headers["X-Empresa-Id"] = "11";
        var controller = new ReportesAdministrativosController(service.Object, scope.Object, Mock.Of<IAuditoriaService>())
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        var filtro = new ReporteAdministrativoFiltroDto();
        scope.Setup(x => x.ObtenerActualAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, 12, 2, "Operador", false));
        Assert.IsType<ForbidResult>(await controller.Resumen(filtro, default));
        Assert.IsType<ForbidResult>(await controller.UsuariosAccesos(default));
        Assert.IsType<ForbidResult>(await controller.RolesPermisos(default));
        Assert.IsType<ForbidResult>(await controller.AuditoriaResumen(filtro, default));
        Assert.IsType<ForbidResult>(await controller.Exportar("auditoria", "csv", filtro, default));
        service.VerifyNoOtherCalls();
        scope.Setup(x => x.ObtenerActualAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, 12, 3, "Administrador B", true));
        service.Setup(x => x.ObtenerResumenAsync(filtro, default)).ReturnsAsync(new ResumenAdministrativoDto());
        Assert.IsType<OkObjectResult>(await controller.Resumen(filtro, default));
        http.Items.Clear();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.Resumen(filtro, default));
    }

    [Fact]
    public async Task ReportesYExportaciones_AislanEmpresasYUsanRolDeMembresia()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"qa-reportes-{Guid.NewGuid():N}").Options;
        await using var setup = new AppDbContext(options);
        setup.Set<Empresa>().AddRange(new Empresa("A") { Id = 11 }, new Empresa("B") { Id = 12 });
        setup.Roles.AddRange(
            new Rol { Id = 21, Nombre = "ROL-A", NombreNormalizado = "ROL-A", EmpresaId = 11 },
            new Rol { Id = 22, Nombre = "ROL-B", NombreNormalizado = "ROL-B", EmpresaId = 12 },
            new Rol { Id = 23, Nombre = "ROL-PLATAFORMA", NombreNormalizado = "ROL-PLATAFORMA", Ambito = AmbitoAutorizacion.Plataforma });
        setup.Usuarios.AddRange(
            new Usuario { Id = 31, NombreUsuario = "COMPARTIDO", RolId = 22 },
            new Usuario { Id = 32, NombreUsuario = "SOLO-B", RolId = 22 },
            new Usuario { Id = 33, NombreUsuario = "INACTIVO-A", RolId = 21 });
        var inactiva = new UsuarioEmpresa(33, 11, 21);
        inactiva.Desactivar();
        setup.UsuarioEmpresas.AddRange(new UsuarioEmpresa(31, 11, 21), new UsuarioEmpresa(31, 12, 22),
            new UsuarioEmpresa(32, 12, 22), inactiva);
        setup.Permisos.AddRange(
            new Permiso { Id = 41, Codigo = "A-VER", Modulo = ModuloSistema.Auditoria, Accion = AccionPermiso.Ver },
            new Permiso { Id = 42, Codigo = "PLATAFORMA", Modulo = ModuloSistema.Configuracion, Accion = AccionPermiso.Administrar, Ambito = AmbitoAutorizacion.Plataforma });
        setup.RolPermisos.AddRange(new RolPermiso { RolId = 21, PermisoId = 41 }, new RolPermiso { RolId = 21, PermisoId = 42 },
            new RolPermiso { RolId = 22, PermisoId = 41 });
        setup.RegistrosAuditoria.AddRange(
            new RegistroAuditoria { EmpresaId = 11, AmbitoAutoridad = AmbitoAutorizacion.Empresa, NombreUsuario = "COMPARTIDO", Descripcion = "EVENTO-A" },
            new RegistroAuditoria { EmpresaId = 12, AmbitoAutoridad = AmbitoAutorizacion.Empresa, NombreUsuario = "SOLO-B", Descripcion = "EVENTO-B" },
            new RegistroAuditoria { EmpresaId = 11, AmbitoAutoridad = AmbitoAutorizacion.Plataforma, Descripcion = "EVENTO-PLATAFORMA" },
            new RegistroAuditoria { Descripcion = "EVENTO-LEGACY" });
        await setup.SaveChangesAsync();

        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = "12";
        await using var db = new AppDbContext(options, httpContextAccessor: new HttpContextAccessor { HttpContext = http });
        var service = new ReporteAdministrativoService(db);
        var filtro = new ReporteAdministrativoFiltroDto();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ObtenerUsuariosAccesoAsync());
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ObtenerRolesPermisosAsync());
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ObtenerResumenAuditoriaAsync(filtro));
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 11;
        var usuarios = await service.ObtenerUsuariosAccesoAsync();
        Assert.Equal(2, usuarios.Count);
        Assert.DoesNotContain(usuarios, u => u.NombreUsuario == "SOLO-B");
        Assert.Equal(21, usuarios.Single(u => u.UsuarioId == 31).RolId);
        Assert.Equal(1, usuarios.Single(u => u.UsuarioId == 31).PermisosEfectivos);
        Assert.Equal(0, usuarios.Single(u => u.UsuarioId == 33).PermisosEfectivos);
        Assert.False(usuarios.Single(u => u.UsuarioId == 33).Activo);
        var rol = Assert.Single(await service.ObtenerRolesPermisosAsync());
        Assert.Equal(21, rol.RolId);
        Assert.Equal(1, rol.UsuariosAsignados);
        Assert.Equal(1, rol.PermisosAsignados);
        var resumen = await service.ObtenerResumenAsync(filtro);
        Assert.Equal(1, resumen.EventosAuditoria);
        Assert.Equal(1, resumen.PermisosCatalogados);
        Assert.Equal(1, resumen.UsuariosActivos);
        foreach (var tipo in new[] { "usuarios", "roles", "auditoria" })
        {
            var csv = Encoding.UTF8.GetString((await service.ExportarAsync(tipo, "csv", filtro)).Contenido);
            Assert.DoesNotContain("SOLO-B", csv);
            Assert.DoesNotContain("ROL-B", csv);
            Assert.DoesNotContain("EVENTO-B", csv);
            Assert.DoesNotContain("PLATAFORMA", csv);
            Assert.DoesNotContain("LEGACY", csv);
        }
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 12;
        usuarios = await service.ObtenerUsuariosAccesoAsync();
        Assert.Equal(2, usuarios.Count);
        Assert.All(usuarios, u => Assert.Equal(22, u.RolId));
        Assert.Equal(22, Assert.Single(await service.ObtenerRolesPermisosAsync()).RolId);
        var auditoria = Encoding.UTF8.GetString((await service.ExportarAsync("auditoria", "csv", filtro)).Contenido);
        Assert.Contains("EVENTO-B", auditoria);
        Assert.DoesNotContain("EVENTO-A", auditoria);
    }
}
