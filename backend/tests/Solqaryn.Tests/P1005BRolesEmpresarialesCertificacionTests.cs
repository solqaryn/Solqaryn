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
/// P1-005.B: contrato xUnit causal que exige el gate comercial dirigido.
/// La prueba LIVE con HTTP/JWT y MySQL DEV sigue siendo independiente.
/// Nunca concede acceso empresarial a un rol global ni al tenant B.
/// </summary>
public sealed class P1005BRolesEmpresarialesCertificacionTests
{
    private static (
        AppDbContext Db,
        UsuarioAdministracionEmpresaService Sut,
        Mock<IUsuarioService> Users,
        Mock<IPermisoService> Perms,
        Mock<IAuditoriaService> Audit) Setup(bool administrador = true)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p1005b-" + Guid.NewGuid().ToString("N")).Options);
        db.Roles.AddRange(
            new Rol { Id = 110, Nombre = "Admin A", NombreNormalizado = "admin_a",
                EmpresaId = 11, Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false, EsAdministrador = true },
            new Rol { Id = 111, Nombre = "Operador A", NombreNormalizado = "operador_a",
                EmpresaId = 11, Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false },
            new Rol { Id = 112, Nombre = "Supervisor A", NombreNormalizado = "supervisor_a",
                EmpresaId = 11, Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false },
            new Rol { Id = 220, Nombre = "Operador B", NombreNormalizado = "operador_b",
                EmpresaId = 22, Ambito = AmbitoAutorizacion.Empresa,
                EsSistema = false },
            new Rol { Id = 300, Nombre = "Admin plataforma", NombreNormalizado = "plataforma",
                EmpresaId = null, Ambito = AmbitoAutorizacion.Plataforma,
                EsSistema = true, EsAdministrador = true });
        db.SaveChanges();

        var users = new Mock<IUsuarioService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>();
        var current = new Mock<ICurrentUserService>();
        var perms = new Mock<IPermisoService>();
        var audit = new Mock<IAuditoriaService>();

        current.SetupGet(x => x.UsuarioId).Returns(1);
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        scope.Setup(x => x.ObtenerActualAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(
                1, 11, 110, "Admin A", administrador));
        perms.Setup(x => x.VerificarPermisoAsync(
                11, It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>()))
            .Returns(Task.CompletedTask);
        audit.Setup(x => x.RegistrarEstrictoAsync(
                It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        return (db, new UsuarioAdministracionEmpresaService(
            db, users.Object, scope.Object, perms.Object, current.Object,
            audit.Object), users, perms, audit);
    }

    private static CreateUsuarioDto Nuevo(int rolId) => new()
    {
        NombreUsuario = "qa_p1005b_unico",
        NombreCompleto = "Fixture QA de A",
        Password = "SoloContratoQA-2026!",
        RolId = rolId
    };

    private static UpdateUsuarioDto Editar(int rolId) => new()
    {
        NombreUsuario = "qa_p1005b_usuario",
        NombreCompleto = "Usuario QA A",
        RolId = rolId
    };

    private static async Task UsuarioPropioAsync(AppDbContext db)
    {
        db.Usuarios.Add(new Usuario
        {
            Id = 71, NombreUsuario = "qa_p1005b_usuario",
            NombreCompleto = "Usuario QA A", PasswordHash = "solo-fixture",
            RolId = 111, Activo = true
        });
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(71, 11, 111));
        await db.SaveChangesAsync();
    }


    [Fact]
    public void P1005B_PlantillaEmpresarialEsCompartiblePeroNoPrivilegioGlobal()
    {
        var plantilla = new Rol
        {
            Id = 170, Nombre = "Vendedor plantilla",
            EmpresaId = null, EsSistema = true,
            Ambito = AmbitoAutorizacion.Empresa, Activo = true
        };
        Assert.True(plantilla.EsAsignableAEmpresa(11));
        Assert.True(plantilla.EsAsignableAEmpresa(22));

        var legacySinDueno = new Rol
        {
            Id = 171, EmpresaId = null, EsSistema = false,
            Ambito = AmbitoAutorizacion.Empresa, Activo = true
        };
        Assert.False(legacySinDueno.EsAsignableAEmpresa(11));

        var privadaB = new Rol
        {
            Id = 172, EmpresaId = 22, EsSistema = false,
            Ambito = AmbitoAutorizacion.Empresa, Activo = true
        };
        Assert.True(privadaB.EsAsignableAEmpresa(22));
        Assert.False(privadaB.EsAsignableAEmpresa(11));

        plantilla.Ambito = AmbitoAutorizacion.Plataforma;
        Assert.False(plantilla.EsAsignableAEmpresa(11));
    }

    [Fact]
    public async Task P1005B_Alta_InstalaUnaSolaMembresiaDeA_YAudita()
    {
        var t = Setup();
        using var db = t.Db;
        t.Users.Setup(x => x.CreateAsync(It.IsAny<CreateUsuarioDto>()))
            .ReturnsAsync(new UsuarioDto { Id = 71, NombreUsuario = "qa_p1005b_unico", RolId = 111 });
        var response = await t.Sut.CrearAsync(Nuevo(111), 11);
        Assert.Equal(71, response.Id);
        var membership = Assert.Single(db.UsuarioEmpresas);
        Assert.Equal(71, membership.UsuarioId);
        Assert.Equal(11, membership.EmpresaId);
        Assert.Equal(111, membership.RolId);
        Assert.True(membership.Activa);
        t.Audit.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Usuarios, AccionPermiso.AsignarRol,
            It.IsAny<string>(), 71, "UsuarioEmpresa",
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Theory]
    [InlineData(220)]
    [InlineData(300)]
    public async Task P1005B_Alta_NoPuedeAsignarRolDeBoPlataforma(int roleId)
    {
        var t = Setup();
        using var db = t.Db;
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.CrearAsync(Nuevo(roleId), 11));
        t.Users.VerifyNoOtherCalls();
        Assert.Empty(db.UsuarioEmpresas);
    }

    [Fact]
    public async Task P1005B_OperadorSinRolAdministradorVivo_NoPuedeCrear()
    {
        var t = Setup(administrador: false);
        using var db = t.Db;
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.CrearAsync(Nuevo(111), 11));
        t.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task P1005B_UsuarioCompartidoAB_NoPuedeEditarIdentidadGlobal()
    {
        var t = Setup();
        using var db = t.Db;
        db.UsuarioEmpresas.AddRange(new UsuarioEmpresa(71, 11, 111),
            new UsuarioEmpresa(71, 22, 220));
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.ActualizarAsync(71, Editar(111), 11));
        t.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task P1005B_UsuarioSoloDeB_NoPuedeEditarDesdeA()
    {
        var t = Setup();
        using var db = t.Db;
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(71, 22, 220));
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.ActualizarAsync(71, Editar(111), 11));
        t.Users.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(220)]
    [InlineData(300)]
    public async Task P1005B_Edicion_NoPuedeAsignarRolAjenoNiGlobal(int roleId)
    {
        var t = Setup();
        using var db = t.Db;
        await UsuarioPropioAsync(db);
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.ActualizarAsync(71, Editar(roleId), 11));
        t.Users.VerifyNoOtherCalls();
        Assert.Equal(111, Assert.Single(db.UsuarioEmpresas).RolId);
    }

    [Fact]
    public async Task P1005B_CambioDeRolPropioYRestauracion_PersistenEnMembresiaYAuditoria()
    {
        var t = Setup();
        using var db = t.Db;
        await UsuarioPropioAsync(db);

        // Simula el flujo de compatibilidad UsuarioService.UpdateAsync: cambia
        // Usuario.RolId; el servicio de administración debe cambiar además
        // UsuarioEmpresa.RolId y realizar auditoría estricta.
        t.Users.Setup(x => x.UpdateAsync(71, It.IsAny<UpdateUsuarioDto>()))
            .Returns<int, UpdateUsuarioDto>((id, dto) =>
            {
                var usuario = db.Usuarios.Single(u => u.Id == id);
                usuario.RolId = dto.RolId!.Value;
                db.SaveChanges();
                return Task.FromResult<UsuarioDto?>(new UsuarioDto
                {
                    Id = id, RolId = dto.RolId,
                    NombreUsuario = dto.NombreUsuario,
                    NombreCompleto = dto.NombreCompleto
                });
            });

        var assigned = await t.Sut.ActualizarAsync(71, Editar(112), 11);
        Assert.Equal(112, assigned?.RolId);
        Assert.Equal(112, db.Usuarios.Single(u => u.Id == 71).RolId);
        Assert.Equal(112, Assert.Single(db.UsuarioEmpresas).RolId);

        var restored = await t.Sut.ActualizarAsync(71, Editar(111), 11);
        Assert.Equal(111, restored?.RolId);
        Assert.Equal(111, db.Usuarios.Single(u => u.Id == 71).RolId);
        Assert.Equal(111, Assert.Single(db.UsuarioEmpresas).RolId);
        Assert.Single(db.UsuarioEmpresas);
        t.Audit.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Usuarios, AccionPermiso.Editar,
            It.IsAny<string>(), 71, "Usuario",
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Exactly(2));
    }

    [Fact]
    public async Task P1005B_CambioDeRolExigePermisoAsignarRol_EnBackend()
    {
        var t = Setup();
        using var db = t.Db;
        await UsuarioPropioAsync(db);
        t.Perms.Setup(x => x.VerificarPermisoAsync(
                11, ModuloSistema.Usuarios, AccionPermiso.AsignarRol))
            .ThrowsAsync(new ForbiddenAccessException("Sin permiso de asignación."));
        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.ActualizarAsync(71, Editar(112), 11));
        Assert.Equal(111, Assert.Single(db.UsuarioEmpresas).RolId);
        t.Users.VerifyNoOtherCalls();
    }
    [Fact]
    public async Task P1005B_EliminarUsuarioPropio_RevocaLoginMembresiaYAudiTa()
    {
        var t = Setup();
        using var db = t.Db;
        await UsuarioPropioAsync(db);

        var eliminado = await t.Sut.EliminarAsync(71, 11);

        Assert.True(eliminado);
        var user = Assert.Single(db.Usuarios);
        Assert.True(user.Eliminado);
        Assert.False(user.Activo);
        Assert.NotNull(user.FechaEliminacion);
        Assert.Equal(1, user.EliminadoPorUsuarioId);
        Assert.False(Assert.Single(db.UsuarioEmpresas).Activa);
        t.Audit.Verify(x => x.RegistrarEstrictoAsync(
            ModuloSistema.Usuarios, AccionPermiso.EliminarLogico,
            It.IsAny<string>(), 71, "Usuario",
            It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
        t.Users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task P1005B_EliminarCompartidoAB_DenegadoSinMutar()
    {
        var t = Setup();
        using var db = t.Db;
        await UsuarioPropioAsync(db);
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(71, 22, 220));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.EliminarAsync(71, 11));
        Assert.False(db.Usuarios.Single(u => u.Id == 71).Eliminado);
        Assert.True(db.Usuarios.Single(u => u.Id == 71).Activo);
        Assert.Equal(2, db.UsuarioEmpresas.Count());
        Assert.All(db.UsuarioEmpresas, x => Assert.True(x.Activa));
    }

    [Fact]
    public async Task P1005B_EliminarBDesdeA_DenegadoSinMutar()
    {
        var t = Setup();
        using var db = t.Db;
        db.Usuarios.Add(new Usuario { Id = 71, NombreUsuario = "solo_b",
            NombreCompleto = "Solo B", PasswordHash = "fixture",
            RolId = 220, Activo = true });
        db.UsuarioEmpresas.Add(new UsuarioEmpresa(71, 22, 220));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.EliminarAsync(71, 11));
        Assert.False(Assert.Single(db.Usuarios).Eliminado);
        Assert.True(Assert.Single(db.UsuarioEmpresas).Activa);
    }

    [Fact]
    public async Task P1005B_EliminarPlataformaInactiva_DenegadoPorProcedencia()
    {
        var t = Setup();
        using var db = t.Db;
        await UsuarioPropioAsync(db);
        var grant = new UsuarioRolPlataforma(71, 300);
        grant.Desactivar();
        db.UsuarioRolesPlataforma.Add(grant);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.EliminarAsync(71, 11));
        Assert.False(Assert.Single(db.Usuarios).Eliminado);
        Assert.True(Assert.Single(db.UsuarioEmpresas).Activa);
    }

    [Fact]
    public async Task P1005B_EliminarRolGlobalLegacy_DenegadoSinMutar()
    {
        var t = Setup();
        using var db = t.Db;
        await UsuarioPropioAsync(db);
        db.Usuarios.Single(u => u.Id == 71).RolId = 300;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.EliminarAsync(71, 11));
        Assert.False(Assert.Single(db.Usuarios).Eliminado);
    }

    [Fact]
    public async Task P1005B_EliminarPropiaCuentaOActorSinAdmin_Denegado()
    {
        var t = Setup();
        using (t.Db)
        {
            await Assert.ThrowsAsync<BusinessRuleException>(
                () => t.Sut.EliminarAsync(1, 11));
        }
        var nonAdmin = Setup(administrador: false);
        using (nonAdmin.Db)
        {
            await Assert.ThrowsAsync<ForbiddenAccessException>(
                () => nonAdmin.Sut.EliminarAsync(71, 11));
        }
    }

    [Fact]
    public async Task P1005B_EliminarSinPermiso_RespetaRBAC()
    {
        var t = Setup();
        using var db = t.Db;
        await UsuarioPropioAsync(db);
        t.Perms.Setup(x => x.VerificarPermisoAsync(
                11, ModuloSistema.Usuarios, AccionPermiso.EliminarLogico))
            .ThrowsAsync(new ForbiddenAccessException("Sin permiso para eliminar."));

        await Assert.ThrowsAsync<ForbiddenAccessException>(
            () => t.Sut.EliminarAsync(71, 11));
        Assert.False(Assert.Single(db.Usuarios).Eliminado);
        Assert.True(Assert.Single(db.UsuarioEmpresas).Activa);
    }

}
