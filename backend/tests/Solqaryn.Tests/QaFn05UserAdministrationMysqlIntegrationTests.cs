using MySql.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class QaFn05UserAdministrationMysqlIntegrationTests
{
    [Fact]
    public async Task MySql_IdentidadesYRolesEfectivos_Aislados_ConMutacionesAtomicas()
    {
        var database = $"test_user_admin_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database={database};User=root;Password=root;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var a = new Empresa("Usuarios A");
            var b = new Empresa("Usuarios B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var ar = new Rol { Nombre = "Admin A", NombreNormalizado = "admin a qa", EmpresaId = a.Id, EsAdministrador = true };
            var av = new Rol { Nombre = "Operador A", NombreNormalizado = "operador a qa", EmpresaId = a.Id };
            var br = new Rol { Nombre = "Operador B", NombreNormalizado = "operador b qa", EmpresaId = b.Id };
            db.Roles.AddRange(ar, av, br);
            await db.SaveChangesAsync();
            var actor = new Usuario { NombreUsuario = "qa_actor_a", NombreCompleto = "Administrador A", PasswordHash = "fixture", RolId = ar.Id };
            var shared = new Usuario { NombreUsuario = "qa_shared", NombreCompleto = "Identidad compartida", PasswordHash = "fixture", RolId = br.Id };
            var foreign = new Usuario { NombreUsuario = "qa_foreign", NombreCompleto = "Identidad ajena", PasswordHash = "fixture", RolId = br.Id };
            db.Usuarios.AddRange(actor, shared, foreign);
            await db.SaveChangesAsync();
            var ownShared = new UsuarioEmpresa(shared.Id, a.Id, av.Id);
            var otherShared = new UsuarioEmpresa(shared.Id, b.Id, br.Id);
            otherShared.Desactivar();
            db.UsuarioEmpresas.AddRange(new UsuarioEmpresa(actor.Id, a.Id, ar.Id), ownShared,
                otherShared, new UsuarioEmpresa(foreign.Id, b.Id, br.Id));
            await db.SaveChangesAsync();
            var current = new Mock<ICurrentUserService>();
            current.SetupGet(x => x.EstaAutenticado).Returns(true);
            current.SetupGet(x => x.UsuarioId).Returns(actor.Id);
            current.SetupGet(x => x.NombreUsuario).Returns(actor.NombreUsuario);
            var scope = new UsuarioScopeService(db, current.Object);
            var permisos = new Mock<IPermisoService>();
            var service = new UsuarioAdministracionEmpresaService(db, scope, permisos.Object, current.Object);

            var list = await service.GetAllAsync(a.Id);
            Assert.Contains(list, u => u.Id == shared.Id && u.RolId == av.Id);
            Assert.DoesNotContain(list, u => u.Id == foreign.Id);
            Assert.Null(await service.GetByIdAsync(a.Id, foreign.Id));
            var page = await service.GetPagedAsync(a.Id, new PagedRequest { Search = "qa_", PageSize = 1 });
            Assert.Equal(2, page.TotalCount);
            Assert.Single(page.Items);
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetAllAsync(b.Id));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ActualizarAsync(a.Id, shared.Id, new UpdateUsuarioDto { RolId = av.Id }));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.BloquearAsync(a.Id, shared.Id, "No autorizado"));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.EliminarAsync(a.Id, foreign.Id));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.AsignarEmpresaAsync(a.Id, foreign.Id, av.Id));
            var platformRole = new Rol { Nombre = "Plataforma QA", NombreNormalizado = "plataforma qa", Ambito = AmbitoAutorizacion.Plataforma };
            db.Roles.Add(platformRole);
            await db.SaveChangesAsync();
            var platformUser = new Usuario { NombreUsuario = "qa_platform", NombreCompleto = "Identidad plataforma", PasswordHash = "fixture", RolId = av.Id };
            db.Usuarios.Add(platformUser);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.Add(new UsuarioEmpresa(platformUser.Id, a.Id, av.Id));
            var platformGrant = new UsuarioRolPlataforma(platformUser.Id, platformRole.Id);
            platformGrant.Desactivar();
            db.UsuarioRolesPlataforma.Add(platformGrant);
            await db.SaveChangesAsync();
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.EliminarAsync(a.Id, platformUser.Id));
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.UpdateEstadoAsync(a.Id, platformUser.Id, false));
            await Assert.ThrowsAsync<BusinessRuleException>(() => service.CambiarEstadoEmpresaAsync(a.Id, actor.Id, false));
            await Assert.ThrowsAsync<BusinessRuleException>(() => service.CambiarRolEmpresaAsync(a.Id, actor.Id, av.Id));

            var created = await service.CrearAsync(a.Id, new CreateUsuarioDto
            {
                NombreUsuario = "qa_nuevo_a", NombreCompleto = "Usuario nuevo A", RolId = av.Id,
                Password = $"Qa!{Guid.NewGuid():N}7"
            });
            var updated = await service.ActualizarAsync(a.Id, created.Id, new UpdateUsuarioDto
            {
                NombreUsuario = "qa_nuevo_a", NombreCompleto = "Usuario editado A", RolId = av.Id
            });
            Assert.Equal("Usuario editado A", updated!.NombreCompleto);
            Assert.True((await service.BloquearAsync(a.Id, created.Id, "Prueba aislada")).Bloqueado);
            Assert.False((await service.DesbloquearAsync(a.Id, created.Id)).Bloqueado);
            Assert.False((await service.UpdateEstadoAsync(a.Id, created.Id, false)).Activo);
            Assert.True((await service.UpdateEstadoAsync(a.Id, created.Id, true)).Activo);
            await service.CambiarEstadoEmpresaAsync(a.Id, shared.Id, false);
            await service.AsignarEmpresaAsync(a.Id, shared.Id, av.Id);
            await service.EliminarAsync(a.Id, created.Id);

            db.ChangeTracker.Clear();
            Assert.True((await db.Usuarios.SingleAsync(u => u.Id == created.Id)).Eliminado);
            Assert.False((await db.UsuarioEmpresas.SingleAsync(m => m.UsuarioId == created.Id)).Activa);
            Assert.False((await db.UsuarioEmpresas.SingleAsync(m => m.UsuarioId == shared.Id && m.EmpresaId == b.Id)).Activa);
            Assert.Equal(br.Id, (await db.Usuarios.SingleAsync(u => u.Id == shared.Id)).RolId);
            Assert.False((await db.Usuarios.SingleAsync(u => u.Id == foreign.Id)).Eliminado);
            var audit = await db.RegistrosAuditoria.AsNoTracking().Where(r => r.Modulo == ModuloSistema.Usuarios).ToListAsync();
            Assert.Equal(9, audit.Count);
            Assert.All(audit, r =>
            {
                Assert.Equal(a.Id, r.EmpresaId);
                Assert.Equal(AmbitoAutorizacion.Empresa, r.AmbitoAutoridad);
                Assert.Equal(OrigenAutorizacion.MembresiaEmpresa, r.OrigenAutorizacion);
            });
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
