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
/// I8-007.H: regresion positiva de edicion del perfil de A. Una cabecera que
/// reclama A no sustituye el contexto autorizado y la identidad del actor.
/// EF InMemory + mocks; no acredita middleware HTTP/JWT ni MySQL real.
/// </summary>
public sealed class I8007HRegresionPerfilEmpresaContextoVerificadoTests
{
    private const int EmpresaA = 11;
    private const int UsuarioObjetivo = 71;
    private const int RolEmpresa = 112;
    private const string HashOriginal = "I8007H-hash-sintetico-inmutable";

    private static (
        AppDbContext Db,
        UsuarioAdministracionEmpresaService Sut,
        Mock<IUsuarioService> Usuarios,
        Mock<IPermisoService> Permisos,
        Mock<IAuditoriaService> Auditoria,
        Mock<IUsuarioScopeService> Scope)
        Crear(int empresaEnScope, int actorEnScope)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("i8007h-" + Guid.NewGuid().ToString("N")).Options);
        db.Roles.AddRange(
            new Rol
            {
                Id = 101, Nombre = "Admin empresa A", NombreNormalizado = "ADMIN_A",
                EmpresaId = EmpresaA, Ambito = AmbitoAutorizacion.Empresa,
                Activo = true, EsAdministrador = true
            },
            new Rol
            {
                Id = RolEmpresa, Nombre = "Operador empresa A",
                NombreNormalizado = "OPERADOR_A",
                EmpresaId = EmpresaA, Ambito = AmbitoAutorizacion.Empresa,
                Activo = true
            });
        db.Usuarios.Add(new Usuario
        {
            Id = UsuarioObjetivo, NombreUsuario = "usuario_empresa_a",
            NombreCompleto = "Nombre antes de editar", PasswordHash = HashOriginal,
            RolId = RolEmpresa, Activo = true
        });
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(UsuarioObjetivo, EmpresaA, RolEmpresa));
        db.SaveChanges();

        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var actual = new Mock<ICurrentUserService>(MockBehavior.Strict);
        actual.SetupGet(x => x.UsuarioId).Returns(1);
        actual.SetupGet(x => x.EstaAutenticado).Returns(true);
        scope.Setup(x => x.ObtenerActualAsync(EmpresaA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(
                actorEnScope, empresaEnScope, 101, "Admin empresa A", true));

        var sut = new UsuarioAdministracionEmpresaService(
            db, usuarios.Object, scope.Object, permisos.Object, actual.Object,
            auditoria.Object);
        return (db, sut, usuarios, permisos, auditoria, scope);
    }

    private static UpdateUsuarioDto Perfil() => new()
    {
        NombreUsuario = "usuario_empresa_a",
        NombreCompleto = "Perfil autorizado en empresa A",
        RolId = RolEmpresa
        // Sin NuevaPassword, sin rol global y sin cambio de membresia.
    };

    [Fact]
    public async Task AdminConScopeVigenteDeA_DelegaEdicionSinTocarClaveNiRoles()
    {
        var t = Crear(EmpresaA, actorEnScope: 1);
        using var db = t.Db;
        t.Permisos.Setup(x => x.VerificarPermisoAsync(
                EmpresaA, ModuloSistema.Usuarios, AccionPermiso.Editar))
            .Returns(Task.CompletedTask);
        t.Usuarios.Setup(x => x.UpdateAsync(
                UsuarioObjetivo, It.Is<UpdateUsuarioDto>(d =>
                    d.RolId == RolEmpresa &&
                    d.NombreCompleto == "Perfil autorizado en empresa A" &&
                    string.IsNullOrEmpty(d.NuevaPassword))))
            .ReturnsAsync(new UsuarioDto
            {
                Id = UsuarioObjetivo, NombreUsuario = "usuario_empresa_a",
                NombreCompleto = "Perfil autorizado en empresa A",
                RolId = RolEmpresa
            });
        t.Auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var resultado = await t.Sut.ActualizarAsync(
            UsuarioObjetivo, Perfil(), EmpresaA);

        Assert.NotNull(resultado);
        Assert.Equal("Perfil autorizado en empresa A", resultado.NombreCompleto);
        var usuario = db.Usuarios.IgnoreQueryFilters().Single(x => x.Id == UsuarioObjetivo);
        Assert.Equal(HashOriginal, usuario.PasswordHash);
        Assert.Equal(RolEmpresa, usuario.RolId);
        var membresia = Assert.Single(db.UsuarioEmpresas.IgnoreQueryFilters());
        Assert.Equal(EmpresaA, membresia.EmpresaId);
        Assert.Equal(RolEmpresa, membresia.RolId);
        Assert.True(membresia.Activa);
        t.Usuarios.Verify(x => x.UpdateAsync(
            UsuarioObjetivo, It.IsAny<UpdateUsuarioDto>()), Times.Once);
        t.Usuarios.VerifyNoOtherCalls();
        t.Permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.Editar), Times.Once);
        t.Permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.AsignarRol), Times.Never);
        t.Permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena), Times.Never);
        t.Auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Usuarios, AccionPermiso.Editar,
            It.IsAny<string>(), UsuarioObjetivo, "Usuario",
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
        t.Auditoria.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(22, 1)]
    [InlineData(11, 999)]
    public async Task CabeceraDiceA_PeroScopeNoCoincide_DeniegaSinEfectos(
        int empresaEnScope, int actorEnScope)
    {
        var t = Crear(empresaEnScope, actorEnScope);
        using var db = t.Db;

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            t.Sut.ActualizarAsync(UsuarioObjetivo, Perfil(), EmpresaA));

        var usuario = db.Usuarios.IgnoreQueryFilters().Single(x => x.Id == UsuarioObjetivo);
        Assert.Equal(HashOriginal, usuario.PasswordHash);
        Assert.Equal(RolEmpresa, usuario.RolId);
        Assert.Equal("Nombre antes de editar", usuario.NombreCompleto);
        var membresia = Assert.Single(db.UsuarioEmpresas.IgnoreQueryFilters());
        Assert.Equal(EmpresaA, membresia.EmpresaId);
        Assert.True(membresia.Activa);
        t.Usuarios.VerifyNoOtherCalls();
        t.Permisos.VerifyNoOtherCalls();
        t.Auditoria.VerifyNoOtherCalls();
        t.Scope.Verify(x => x.ObtenerActualAsync(
            EmpresaA, It.IsAny<CancellationToken>()), Times.Once);
    }
}
