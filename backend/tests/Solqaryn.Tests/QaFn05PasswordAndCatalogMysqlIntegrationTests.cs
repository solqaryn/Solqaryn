using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class QaFn05PasswordAndCatalogMysqlIntegrationTests
{
    [Fact]
    public async Task MySql_ResetExigeGrantRealYCambiaLoginSinExponerCredenciales()
    {
        var options = Options("password");
        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var a = new Empresa("Contraseña A");
            var b = new Empresa("Contraseña B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var admin = Role("Admin prueba contraseña", a.Id, true);
            var role = Role("Operador prueba contraseña", a.Id);
            var foreignRole = Role("Ajeno prueba contraseña", b.Id);
            db.Roles.AddRange(admin, role, foreignRole);
            await db.SaveChangesAsync();

            var anterior = $"Qa!{Guid.NewGuid():N}7";
            var nueva = $"Qa!{Guid.NewGuid():N}8";
            var hashAnterior = BCrypt.Net.BCrypt.HashPassword(anterior, workFactor: 12);
            var actor = new Usuario { NombreUsuario = "qa_reset_actor", NombreCompleto = "Actor QA", RolId = admin.Id, PasswordHash = hashAnterior };
            var target = new Usuario { NombreUsuario = "qa_reset_target", NombreCompleto = "Destino QA", RolId = role.Id, PasswordHash = hashAnterior };
            var shared = new Usuario { NombreUsuario = "qa_reset_shared", NombreCompleto = "Compartido QA", RolId = role.Id, PasswordHash = hashAnterior };
            var foreign = new Usuario { NombreUsuario = "qa_reset_foreign", NombreCompleto = "Ajeno QA", RolId = foreignRole.Id, PasswordHash = hashAnterior };
            db.Usuarios.AddRange(actor, target, shared, foreign);
            await db.SaveChangesAsync();
            var sharedB = new UsuarioEmpresa(shared.Id, b.Id, foreignRole.Id);
            sharedB.Desactivar();
            db.UsuarioEmpresas.AddRange(new UsuarioEmpresa(actor.Id, a.Id, admin.Id),
                new UsuarioEmpresa(target.Id, a.Id, role.Id),
                new UsuarioEmpresa(shared.Id, a.Id, role.Id), sharedB,
                new UsuarioEmpresa(foreign.Id, b.Id, foreignRole.Id));
            var edit = Definition(ModuloSistema.Usuarios, AccionPermiso.Editar);
            var reset = Definition(ModuloSistema.Usuarios, AccionPermiso.RestablecerContrasena);
            db.Permisos.AddRange(edit, reset);
            await db.SaveChangesAsync();
            db.RolPermisos.Add(new RolPermiso { RolId = admin.Id, PermisoId = edit.Id });
            await db.SaveChangesAsync();

            var current = Actor(actor);
            var scope = new UsuarioScopeService(db, current.Object);
            var grants = new RolPermisoRepository(db);
            var roleRepo = new RolRepository(db);
            var permissions = new PermisoService(grants, roleRepo, new PermisoRepository(db),
                Mock.Of<IAuditoriaService>(), current.Object, scope);
            var service = new UsuarioAdministracionEmpresaService(db, scope, permissions, current.Object);
            UpdateUsuarioDto Dto(Usuario user, string password) => new()
            {
                NombreUsuario = user.NombreUsuario, NombreCompleto = user.NombreCompleto,
                RolId = user.RolId, NuevaPassword = password
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                ["Jwt:Issuer"] = "qa-fixture", ["Jwt:Audience"] = "qa-fixture"
            }).Build();
            var jwt = new JwtService(config);
            var auth = new AuthService(new UsuarioRepository(db), roleRepo, jwt);
            Assert.NotNull(await auth.LoginAsync(new LoginRequestDto { NombreUsuario = target.NombreUsuario, Password = anterior }));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ActualizarAsync(a.Id, target.Id, Dto(target, nueva)));
            Assert.Equal(hashAnterior, (await db.Usuarios.AsNoTracking().SingleAsync(u => u.Id == target.Id)).PasswordHash);
            Assert.False(await db.RegistrosAuditoria.AnyAsync(r => r.Modulo == ModuloSistema.Usuarios));

            db.RolPermisos.Add(new RolPermiso { RolId = admin.Id, PermisoId = reset.Id });
            await db.SaveChangesAsync();
            await Assert.ThrowsAsync<BusinessRuleException>(() => service.ActualizarAsync(a.Id, target.Id, Dto(target, anterior)));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ActualizarAsync(a.Id, shared.Id, Dto(shared, nueva)));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ActualizarAsync(a.Id, foreign.Id, Dto(foreign, nueva)));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ActualizarAsync(b.Id, foreign.Id, Dto(foreign, nueva)));
            await service.ActualizarAsync(a.Id, target.Id, Dto(target, nueva));

            await using var persisted = new AppDbContext(options);
            var updated = await persisted.Usuarios.AsNoTracking().SingleAsync(u => u.Id == target.Id);
            Assert.NotEqual(hashAnterior, updated.PasswordHash);
            Assert.False(BCrypt.Net.BCrypt.Verify(anterior, updated.PasswordHash));
            Assert.True(BCrypt.Net.BCrypt.Verify(nueva, updated.PasswordHash));
            var realLogin = new AuthService(new UsuarioRepository(persisted), new RolRepository(persisted), jwt);
            Assert.Null(await realLogin.LoginAsync(new LoginRequestDto { NombreUsuario = target.NombreUsuario, Password = anterior }));
            var login = await realLogin.LoginAsync(new LoginRequestDto { NombreUsuario = target.NombreUsuario, Password = nueva });
            Assert.NotNull(login);
            Assert.False(string.IsNullOrWhiteSpace(login!.Token));
            Assert.Equal(hashAnterior, (await persisted.Usuarios.AsNoTracking().SingleAsync(u => u.Id == shared.Id)).PasswordHash);
            Assert.Equal(hashAnterior, (await persisted.Usuarios.AsNoTracking().SingleAsync(u => u.Id == foreign.Id)).PasswordHash);
            var entry = Assert.Single(await persisted.RegistrosAuditoria.AsNoTracking()
                .Where(r => r.Modulo == ModuloSistema.Usuarios).ToListAsync());
            Assert.Equal(a.Id, entry.EmpresaId);
            Assert.Equal(actor.Id, entry.UsuarioId);
            Assert.Equal(target.Id, entry.ReferenciaId);
            Assert.Contains("PasswordRestablecida", entry.ValoresNuevos!);
            Assert.DoesNotContain(anterior, entry.ValoresNuevos!);
            Assert.DoesNotContain(nueva, entry.ValoresNuevos!);
            Assert.DoesNotContain(hashAnterior, entry.ValoresNuevos!);
            Assert.DoesNotContain(updated.PasswordHash, entry.ValoresNuevos!);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task MySql_CatalogoSoloLectura_ConteoTenant_YGrantPlataformaNoConcedeAutoridad()
    {
        var options = Options("catalog");
        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var a = new Empresa("Catálogo A");
            var b = new Empresa("Catálogo B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var ar = Role("Catálogo rol A", a.Id);
            var br = Role("Catálogo rol B", b.Id);
            var br2 = Role("Catálogo segundo rol B", b.Id);
            var platformRole = Role("Catálogo plataforma", null);
            platformRole.Ambito = AmbitoAutorizacion.Plataforma;
            var template = Role("Catálogo plantilla", null);
            template.EsSistema = true;
            db.Roles.AddRange(ar, br, br2, platformRole, template);
            var permission = Definition(ModuloSistema.Permisos, AccionPermiso.Ver);
            var platformPermission = Definition(ModuloSistema.Permisos, AccionPermiso.Editar);
            platformPermission.Ambito = AmbitoAutorizacion.Plataforma;
            db.Permisos.AddRange(permission, platformPermission);
            await db.SaveChangesAsync();
            db.RolPermisos.AddRange(new RolPermiso { RolId = ar.Id, PermisoId = permission.Id },
                new RolPermiso { RolId = br.Id, PermisoId = permission.Id },
                new RolPermiso { RolId = br2.Id, PermisoId = permission.Id },
                new RolPermiso { RolId = template.Id, PermisoId = permission.Id },
                new RolPermiso { RolId = platformRole.Id, PermisoId = permission.Id },
                new RolPermiso { RolId = ar.Id, PermisoId = platformPermission.Id });
            var user = new Usuario { NombreUsuario = "qa_catalog_actor", NombreCompleto = "Actor QA", RolId = ar.Id, PasswordHash = "fixture" };
            db.Usuarios.Add(user);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.Add(new UsuarioEmpresa(user.Id, a.Id, ar.Id));
            await db.SaveChangesAsync();

            var current = Actor(user);
            var repository = new PermisoRepository(db);
            var catalog = new PermisoCatalogoService(repository, Mock.Of<IAuditoriaService>(), current.Object);
            var scope = new UsuarioScopeService(db, current.Object);
            var grants = new RolPermisoRepository(db);
            var service = new PermisoService(grants, new RolRepository(db), repository,
                Mock.Of<IAuditoriaService>(), current.Object, scope);
            var controller = new PermisosController(service, catalog)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
            controller.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = a.Id;
            controller.HttpContext.Request.Headers["X-Empresa-Id"] = b.Id.ToString();

            var list = Assert.IsType<ApiResponse<List<PermisoCatalogoDto>>>(
                Assert.IsType<OkObjectResult>(await controller.GetCatalogo()).Value).Data!;
            Assert.DoesNotContain(list, p => p.Id == platformPermission.Id);
            Assert.Equal(2, list.Single(p => p.Id == permission.Id).CantidadRolesAsignados);
            Assert.Equal(3, (await catalog.GetByIdEmpresaAsync(permission.Id, b.Id))!.CantidadRolesAsignados);
            Assert.IsType<NotFoundObjectResult>(await controller.GetCatalogoById(platformPermission.Id));
            Assert.True(await service.TienePermisoAsync(a.Id, ModuloSistema.Permisos, AccionPermiso.Ver));
            Assert.False(await service.TienePermisoAsync(a.Id, ModuloSistema.Permisos, AccionPermiso.Editar));
            Assert.DoesNotContain("Permisos:Editar", (await service.GetMisPermisosAsync(a.Id)).Permisos);

            await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.CreateCatalogo(new CrearPermisoDto()));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.UpdateCatalogo(permission.Id, new ActualizarPermisoDto()));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.ActivarCatalogo(permission.Id));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.DesactivarCatalogo(permission.Id));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.EliminarLogicoCatalogo(permission.Id));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.EliminarPermanenteCatalogo(permission.Id));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.DuplicarCatalogo(permission.Id, new DuplicarPermisoRequest()));
            controller.HttpContext.Items.Clear();
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.GetCatalogo());
            await using var persisted = new AppDbContext(options);
            Assert.True((await persisted.Permisos.SingleAsync(p => p.Id == permission.Id)).Activo);
            Assert.False((await persisted.Permisos.SingleAsync(p => p.Id == permission.Id)).Eliminado);
            Assert.Equal(6, await persisted.RolPermisos.CountAsync(rp =>
                rp.PermisoId == permission.Id || rp.PermisoId == platformPermission.Id));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static DbContextOptions<AppDbContext> Options(string suffix) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database=test_{suffix}_{Guid.NewGuid():N};User=root;Password=root;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;

    private static Rol Role(string name, int? owner, bool admin = false) => new()
    {
        Nombre = name, NombreNormalizado = name.ToLowerInvariant(), EmpresaId = owner, EsAdministrador = admin
    };

    private static Permiso Definition(ModuloSistema module, AccionPermiso action) => new()
    {
        Modulo = module, Accion = action, Codigo = $"{module}.{action}".ToUpperInvariant(),
        Nombre = $"{module}: {action}"
    };

    private static Mock<ICurrentUserService> Actor(Usuario u)
    {
        var actor = new Mock<ICurrentUserService>();
        actor.SetupGet(x => x.EstaAutenticado).Returns(true);
        actor.SetupGet(x => x.UsuarioId).Returns(u.Id);
        actor.SetupGet(x => x.NombreUsuario).Returns(u.NombreUsuario);
        return actor;
    }
}
