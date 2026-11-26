using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QaFn05ConfiguracionPlataformaTests
{
    [Fact]
    public async Task AutoridadGlobal_RequiereAsignacionActivaYGrantExactoDelMismoAmbito()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"qa-platform-{Guid.NewGuid():N}").Options;
        await using var db = new AppDbContext(options);
        db.Roles.AddRange(
            new Rol { Id = 1, Nombre = "Administrador tenant", NombreNormalizado = "ADMINISTRADOR TENANT", EsAdministrador = true, EmpresaId = 11 },
            new Rol { Id = 2, Nombre = "Plataforma", NombreNormalizado = "PLATAFORMA", Ambito = AmbitoAutorizacion.Plataforma });
        db.Usuarios.Add(new Usuario { Id = 1, NombreUsuario = "qa", RolId = 1 });
        db.Permisos.AddRange(
            new Permiso { Id = 1, Codigo = "CONFIGURACION.EDITAR", Modulo = ModuloSistema.Configuracion, Accion = AccionPermiso.Editar },
            new Permiso { Id = 2, Codigo = "CONFIGURACIONPLATAFORMA.EDITAR", Modulo = ModuloSistema.ConfiguracionPlataforma, Accion = AccionPermiso.Editar, Ambito = AmbitoAutorizacion.Plataforma });
        db.RolPermisos.AddRange(new RolPermiso { RolId = 1, PermisoId = 1 }, new RolPermiso { RolId = 1, PermisoId = 2 });
        await db.SaveChangesAsync();
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        current.SetupGet(x => x.UsuarioId).Returns(1);
        current.SetupGet(x => x.EsAdministrador).Returns(true);
        var service = new UsuarioScopeService(db, current.Object);
        async Task<bool> Puede() => await service.TienePermisoPlataformaAsync(ModuloSistema.ConfiguracionPlataforma, AccionPermiso.Editar);
        Assert.False(await Puede());
        var incorrecta = new UsuarioRolPlataforma(1, 1);
        db.UsuarioRolesPlataforma.Add(incorrecta);
        var correcta = new UsuarioRolPlataforma(1, 2);
        db.UsuarioRolesPlataforma.Add(correcta);
        await db.SaveChangesAsync();
        Assert.False(await Puede());
        db.RolPermisos.Add(new RolPermiso { RolId = 2, PermisoId = 1 });
        await db.SaveChangesAsync();
        Assert.False(await Puede());
        db.RolPermisos.Add(new RolPermiso { RolId = 2, PermisoId = 2 });
        await db.SaveChangesAsync();
        Assert.True(await Puede());
        Assert.False(await service.TienePermisoPlataformaAsync(ModuloSistema.ConfiguracionPlataforma, AccionPermiso.Ver));
        Assert.False(await service.TienePermisoPlataformaAsync(ModuloSistema.Configuracion, AccionPermiso.Editar));
        correcta.Desactivar();
        await db.SaveChangesAsync();
        Assert.False(await Puede());
    }

    [Fact]
    public async Task CatalogoPlataforma_NoConcedeGrantsNiRolesAutomaticos()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"qa-platform-seed-{Guid.NewGuid():N}").Options;
        await using var db = new AppDbContext(options);
        await new SeedPermisoService(db).SeedDefaultsAsync();
        Assert.Equal(5, await db.Permisos.CountAsync(x => x.Ambito == AmbitoAutorizacion.Plataforma));
        Assert.Empty(await db.Roles.Where(r => r.Ambito == AmbitoAutorizacion.Plataforma).ToListAsync());
        Assert.Empty(await db.UsuarioRolesPlataforma.ToListAsync());
        Assert.False(await db.RolPermisos.AnyAsync(x => x.Permiso.Ambito == AmbitoAutorizacion.Plataforma));
    }

    [Fact]
    public async Task DirectorioEmpresarial_NoEnumeraNiResuelveEmpresasAjenas()
    {
        var service = new Mock<IEmpresaService>(MockBehavior.Strict);
        service.Setup(x => x.GetByIdAsync(11, default)).ReturnsAsync(new EmpresaDto { Id = 11, Nombre = "A", Activa = true });
        var http = new DefaultHttpContext();
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 11;
        http.Request.Headers["X-Empresa-Id"] = "12";
        var controller = new EmpresasController(service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        Assert.IsType<OkObjectResult>(await controller.List(null, default));
        Assert.IsType<NotFoundObjectResult>(await controller.GetById(12, default));
        service.Verify(x => x.ListAsync(It.IsAny<bool?>(), It.IsAny<CancellationToken>()), Times.Never);
        service.Verify(x => x.GetByIdAsync(12, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(typeof(EmpresaConfiguracionController), "Get")]
    [InlineData(typeof(EmpresaConfiguracionController), "Update")]
    [InlineData(typeof(EmpresaConfiguracionController), "UpdateLogo")]
    [InlineData(typeof(EmpresaConfiguracionController), "RestaurarLogo")]
    [InlineData(typeof(TemaVisualController), "Update")]
    [InlineData(typeof(TemaVisualController), "Restaurar")]
    [InlineData(typeof(EmpresasController), "Create")]
    [InlineData(typeof(EmpresasController), "Update")]
    [InlineData(typeof(EmpresasController), "Activar")]
    [InlineData(typeof(EmpresasController), "Desactivar")]
    public void OperacionGlobal_ExigePlanoPlataforma(Type tipo, string metodo)
    {
        var atributo = tipo.GetMethod(metodo)!.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(atributo);
        Assert.True(atributo.Plataforma);
    }
}
