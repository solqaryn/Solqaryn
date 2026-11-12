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
/// I8-007.A: la edición del perfil empresarial propio no habilita edición
/// de una identidad con autoridad global, activa o históricamente revocada.
/// EF InMemory + mocks; no certifica HTTP/JWT, SMTP ni MySQL real.
/// </summary>
public sealed class I8007APerfilEmpresaSinEscaladaGlobalTests
{
    private const int EmpresaA = 11;
    private const int UsuarioA = 71;
    private const int RolEmpresa = 112;
    private const int RolPlataforma = 902;
    private const string HashOriginal = "I8007A-HASH-FICTICIO-NO-MODIFICAR";

    private static (
        AppDbContext Db,
        UsuarioAdministracionEmpresaService Servicio,
        Mock<IUsuarioService> Usuarios,
        Mock<IPermisoService> Permisos,
        Mock<IAuditoriaService> Auditoria)
        Crear(bool grantPlataforma = false, bool grantActivo = true,
            bool rolLegacyPlataforma = false)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("i8007a-" + Guid.NewGuid().ToString("N")).Options);

        db.Roles.AddRange(
            new Rol
            {
                Id = 101, Nombre = "Administrador A", NombreNormalizado = "ADMIN_A",
                Ambito = AmbitoAutorizacion.Empresa, EmpresaId = EmpresaA,
                EsAdministrador = true, Activo = true
            },
            new Rol
            {
                Id = RolEmpresa, Nombre = "Operador A", NombreNormalizado = "OPERADOR_A",
                Ambito = AmbitoAutorizacion.Empresa, EmpresaId = EmpresaA,
                Activo = true
            },
            new Rol
            {
                Id = RolPlataforma, Nombre = "Plataforma", NombreNormalizado = "PLATAFORMA",
                Ambito = AmbitoAutorizacion.Plataforma, EmpresaId = null,
                EsAdministrador = true, EsSistema = true, Activo = true
            });

        db.Usuarios.Add(new Usuario
        {
            Id = UsuarioA, NombreUsuario = "i8007a_usuario",
            NombreCompleto = "Perfil empresa A",
            PasswordHash = HashOriginal,
            RolId = rolLegacyPlataforma ? RolPlataforma : RolEmpresa,
            Activo = true
        });
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(UsuarioA, EmpresaA, RolEmpresa));

        if (grantPlataforma)
        {
            var grant = new UsuarioRolPlataforma(UsuarioA, RolPlataforma);
            if (!grantActivo) grant.Desactivar();
            db.UsuarioRolesPlataforma.Add(grant);
        }
        db.SaveChanges();

        var usuarios = new Mock<IUsuarioService>(MockBehavior.Strict);
        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var actual = new Mock<ICurrentUserService>(MockBehavior.Strict);
        actual.SetupGet(x => x.UsuarioId).Returns(1);
        actual.SetupGet(x => x.EstaAutenticado).Returns(true);
        scope.Setup(x => x.ObtenerActualAsync(EmpresaA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1, EmpresaA, 101,
                "Administrador A", true));
        permisos.Setup(x => x.VerificarPermisoAsync(
                EmpresaA, ModuloSistema.Usuarios, AccionPermiso.Editar))
            .Returns(Task.CompletedTask);
        auditoria.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var servicio = new UsuarioAdministracionEmpresaService(
            db, usuarios.Object, scope.Object, permisos.Object,
            actual.Object, auditoria.Object);
        return (db, servicio, usuarios, permisos, auditoria);
    }

    private static UpdateUsuarioDto Perfil() => new()
    {
        NombreUsuario = "i8007a_usuario",
        NombreCompleto = "Perfil actualizado de empresa A",
        RolId = RolEmpresa
        // Intencionalmente sin NuevaPassword: edición de perfil, no reset.
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GrantPlataformaActivoOInactivo_BloqueaEdicionEmpresarial(
        bool grantActivo)
    {
        var t = Crear(grantPlataforma: true, grantActivo: grantActivo);
        using var db = t.Db;
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            t.Servicio.ActualizarAsync(UsuarioA, Perfil(), EmpresaA));

        Assert.Equal(HashOriginal, db.Usuarios.IgnoreQueryFilters()
            .Single(x => x.Id == UsuarioA).PasswordHash);
        Assert.Equal(RolEmpresa, db.Usuarios.IgnoreQueryFilters()
            .Single(x => x.Id == UsuarioA).RolId);
        var membresia = Assert.Single(db.UsuarioEmpresas.IgnoreQueryFilters());
        Assert.Equal(EmpresaA, membresia.EmpresaId);
        Assert.Equal(RolEmpresa, membresia.RolId);
        Assert.True(membresia.Activa);
        t.Usuarios.VerifyNoOtherCalls();
        t.Auditoria.VerifyNoOtherCalls();
        t.Permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.Editar), Times.Once);
        t.Permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.AsignarRol), Times.Never);
        t.Permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena), Times.Never);
    }

    [Fact]
    public async Task RolGlobalLegacy_SinGrantActual_TambienBloqueaEdicion()
    {
        var t = Crear(rolLegacyPlataforma: true);
        using var db = t.Db;
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            t.Servicio.ActualizarAsync(UsuarioA, Perfil(), EmpresaA));

        Assert.Equal(HashOriginal, db.Usuarios.IgnoreQueryFilters()
            .Single(x => x.Id == UsuarioA).PasswordHash);
        Assert.Equal(RolPlataforma, db.Usuarios.IgnoreQueryFilters()
            .Single(x => x.Id == UsuarioA).RolId);
        t.Usuarios.VerifyNoOtherCalls();
        t.Auditoria.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IdentidadCompartidaConEmpresaB_ActivaOInactiva_DeniegaEdicionDesdeA(bool empresaBActiva)
    {
        // Negativo faltante: incluso una membresia B inactiva mantiene
        // identidad compartida. Una administradora A no debe mutarla.
        const int empresaB = 22;
        const int rolB = 223;
        var t = Crear();
        using var db = t.Db;
        db.Roles.Add(new Rol
        {
            Id = rolB, Nombre = "Operador B", NombreNormalizado = "OPERADOR_B",
            Ambito = AmbitoAutorizacion.Empresa, EmpresaId = empresaB, Activo = true
        });
        var membresiaB = new UsuarioEmpresa(UsuarioA, empresaB, rolB);
        if (!empresaBActiva) membresiaB.Desactivar();
        db.UsuarioEmpresas.Add(membresiaB);
        db.SaveChanges();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            t.Servicio.ActualizarAsync(UsuarioA, Perfil(), EmpresaA));

        var usuario = db.Usuarios.IgnoreQueryFilters().Single(x => x.Id == UsuarioA);
        Assert.Equal(HashOriginal, usuario.PasswordHash);
        Assert.Equal(RolEmpresa, usuario.RolId);
        Assert.Equal(2, db.UsuarioEmpresas.IgnoreQueryFilters()
            .Count(x => x.UsuarioId == UsuarioA));
        Assert.Equal(empresaBActiva, db.UsuarioEmpresas.IgnoreQueryFilters()
            .Single(x => x.EmpresaId == empresaB).Activa);
        t.Usuarios.VerifyNoOtherCalls();
        t.Auditoria.VerifyNoOtherCalls();
        t.Permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.Editar), Times.Once);
    }

    [Fact]
    public async Task PerfilSoloEmpresaA_PuedeDelegarEdicionSinEscalarRolesNiPassword()
    {
        var t = Crear();
        using var db = t.Db;
        t.Usuarios.Setup(x => x.UpdateAsync(
                UsuarioA, It.IsAny<UpdateUsuarioDto>()))
            .ReturnsAsync(new UsuarioDto
            {
                Id = UsuarioA, NombreUsuario = "i8007a_usuario",
                NombreCompleto = "Perfil actualizado de empresa A", RolId = RolEmpresa
            });

        var resultado = await t.Servicio.ActualizarAsync(UsuarioA, Perfil(), EmpresaA);
        Assert.NotNull(resultado);
        Assert.Equal(RolEmpresa, resultado.RolId);
        Assert.Equal(HashOriginal, db.Usuarios.IgnoreQueryFilters()
            .Single(x => x.Id == UsuarioA).PasswordHash);
        Assert.Equal(RolEmpresa, Assert.Single(db.UsuarioEmpresas).RolId);
        t.Usuarios.Verify(x => x.UpdateAsync(
            UsuarioA, It.IsAny<UpdateUsuarioDto>()), Times.Once);
        t.Usuarios.VerifyNoOtherCalls();
        t.Permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.Editar), Times.Once);
        t.Permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.AsignarRol), Times.Never);
        t.Permisos.Verify(x => x.VerificarPermisoAsync(
            EmpresaA, ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena), Times.Never);
        t.Auditoria.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Usuarios, AccionPermiso.Editar,
            It.IsAny<string>(), UsuarioA, "Usuario",
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
        t.Auditoria.VerifyNoOtherCalls();
    }
}
