using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Regresiones P1-005.A. InMemory no sustituye MySQL LIVE de P1-005.B.
/// </summary>
public sealed class P1005AEmpresaAdminScopeTests
{
    private static (AppDbContext Db, UsuarioAdministracionEmpresaService Sut,
        Mock<IUsuarioService> Users, Mock<IAuditoriaService> Audit,
        Mock<IPermisoService> Perms)
        Setup(bool administrador = true)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p1005a-" + Guid.NewGuid().ToString("N")).Options);
        var users = new Mock<IUsuarioService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>();
        var current = new Mock<ICurrentUserService>();
        var audit = new Mock<IAuditoriaService>();
        var perms = new Mock<IPermisoService>();
        current.SetupGet(x => x.UsuarioId).Returns(1);
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        scope.Setup(x => x.ObtenerActualAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, 11, 101, "Admin A", administrador));
        perms.Setup(x => x.VerificarPermisoAsync(
                11, It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
            .Returns(Task.CompletedTask);
        audit.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        db.Roles.AddRange(
            new Rol { Id=101, Nombre="Admin A", NombreNormalizado="admin_a",
                EmpresaId=11, Ambito=AmbitoAutorizacion.Empresa, EsSistema=false, EsAdministrador=true },
            new Rol { Id=102, Nombre="B", NombreNormalizado="b",
                EmpresaId=22, Ambito=AmbitoAutorizacion.Empresa, EsSistema=false },
            new Rol { Id=103, Nombre="Platform", NombreNormalizado="platform",
                EmpresaId=null, Ambito=AmbitoAutorizacion.Plataforma, EsSistema=true, EsAdministrador=true },
            new Rol { Id=104, Nombre="System tenant", NombreNormalizado="system_tenant",
                EmpresaId=null, Ambito=AmbitoAutorizacion.Empresa, EsSistema=true });
        db.SaveChanges();
        return (db, new UsuarioAdministracionEmpresaService(
                db, users.Object, scope.Object, perms.Object, current.Object, audit.Object),
            users, audit, perms);
    }

    private static CreateUsuarioDto Nuevo(int rolId) => new()
    {
        NombreUsuario="qa_p1005_admin",
        NombreCompleto="Usuario temporal QA",
        Password="Secreto-temporal-2026!",
        RolId=rolId
    };

    [Fact]
    public async Task AltaA_SellaEmpresaYRolPropios()
    {
        var t=Setup();
        using var db=t.Db;
        t.Users.Setup(x => x.CreateAsync(It.IsAny<CreateUsuarioDto>()))
            .ReturnsAsync(new UsuarioDto { Id=91, NombreUsuario="qa_p1005_admin", RolId=101 });
        var creado=await t.Sut.CrearAsync(Nuevo(101),11);
        Assert.Equal(91,creado.Id);
        var row=Assert.Single(db.UsuarioEmpresas);
        Assert.Equal(11,row.EmpresaId);
        Assert.Equal(101,row.RolId);
        Assert.Equal(91,row.UsuarioId);
        t.Audit.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Usuarios,AccionPermiso.AsignarRol,
            It.IsAny<string>(),91, "UsuarioEmpresa",
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>()),Times.Once);
    }

    [Theory]
    [InlineData(102)]
    [InlineData(103)]
    public async Task AltaA_NoPuedeAsignarRolAjenoNiPlataforma(int rolId)
    {
        var t=Setup();
        using var db=t.Db;
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            t.Sut.CrearAsync(Nuevo(rolId),11));
        t.Users.VerifyNoOtherCalls();
        Assert.Empty(db.UsuarioEmpresas);
    }

    [Fact]
    public async Task AltaA_AceptaPlantillaEmpresarialPeroNoAutoridadGlobal()
    {
        var t=Setup();
        using var db=t.Db;
        t.Users.Setup(x => x.CreateAsync(It.IsAny<CreateUsuarioDto>()))
            .ReturnsAsync(new UsuarioDto { Id=92, RolId=104 });
        await t.Sut.CrearAsync(Nuevo(104),11);
        Assert.Equal(104,Assert.Single(db.UsuarioEmpresas).RolId);
    }

    [Fact]
    public async Task AltaSinAdminReal_DenegadaAunquePaseCabecera()
    {
        var t=Setup(administrador:false);
        using var db=t.Db;
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            t.Sut.CrearAsync(Nuevo(101),11));
        t.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EdicionUsuarioCompartidoAB_Denegada()
    {
        var t=Setup();
        using var db=t.Db;
        db.UsuarioEmpresas.AddRange(new UsuarioEmpresa(61,11,101),
            new UsuarioEmpresa(61,22,102));
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            t.Sut.ActualizarAsync(61,new UpdateUsuarioDto { RolId=101 },11));
        t.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EdicionUsuarioSoloB_Denegada()
    {
        var t=Setup();
        using var db=t.Db;
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(61,22,102));
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            t.Sut.ActualizarAsync(61,new UpdateUsuarioDto { RolId=101 },11));
        t.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EdicionA_ConRolB_DenegadaAntesDeModificar()
    {
        var t=Setup();
        using var db=t.Db;
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            t.Sut.ActualizarAsync(61,new UpdateUsuarioDto { RolId=102 },11));
        t.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EdicionA_CambiaRolEfectivoDeMembresiaAdemasDelLegacy()
    {
        var t=Setup();
        using var db=t.Db;
        db.Usuarios.Add(new Usuario
        {
            Id=61, NombreUsuario="usuarioa", NombreCompleto="Usuario Empresa A",
            PasswordHash="solo-fixture", RolId=101, Activo=true
        });
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(61,11,101));
        await db.SaveChangesAsync();
        t.Users.Setup(x=>x.UpdateAsync(61,It.IsAny<UpdateUsuarioDto>()))
            .ReturnsAsync(new UsuarioDto { Id=61, RolId=104 });
        var result=await t.Sut.ActualizarAsync(61,
            new UpdateUsuarioDto { RolId=104, NombreUsuario="usuarioa", NombreCompleto="Usuario Empresa A" },11);
        Assert.Equal(104,result?.RolId);
        Assert.Equal(104,Assert.Single(db.UsuarioEmpresas).RolId);
        t.Audit.Verify(x=>x.RegistrarEstrictoAsync(
            ModuloSistema.Usuarios,AccionPermiso.Editar,
            It.IsAny<string>(),61,"Usuario",
            It.IsAny<object?>(),It.IsAny<object?>(),It.IsAny<string?>(),
            It.IsAny<string>(),It.IsAny<string?>()),Times.Once);
    }

}
