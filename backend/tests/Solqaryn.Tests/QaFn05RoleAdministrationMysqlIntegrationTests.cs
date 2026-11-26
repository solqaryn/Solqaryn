using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class QaFn05RoleAdministrationMysqlIntegrationTests
{
    [Fact]
    public async Task MySql_RolesYConteoDeMembresiasSeAislanPorEmpresa()
    {
        var database = $"test_role_admin_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database={database};User=root;Password=root;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            var a = new Empresa("Empresa roles A");
            var b = new Empresa("Empresa roles B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var service = new RolService(new RolRepository(db), Mock.Of<IAuditoriaService>(), Mock.Of<ICurrentUserService>());
            var ar = await service.CreateEmpresaAsync(new CrearRolDto { Nombre = "Rol aislado A" }, a.Id);
            var br = await service.CreateEmpresaAsync(new CrearRolDto { Nombre = "Rol aislado B" }, b.Id);
            var plantilla = new Rol { Nombre = "Plantilla prueba QA", NombreNormalizado = "plantilla prueba qa", EsSistema = true };
            db.Roles.Add(plantilla);
            await db.SaveChangesAsync();
            var ua = new Usuario { NombreUsuario = "qa_role_a", NombreCompleto = "Usuario A", PasswordHash = "fixture", RolId = plantilla.Id };
            var ub = new Usuario { NombreUsuario = "qa_role_b", NombreCompleto = "Usuario B", PasswordHash = "fixture", RolId = plantilla.Id };
            db.Usuarios.AddRange(ua, ub);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.AddRange(new UsuarioEmpresa(ua.Id, a.Id, plantilla.Id), new UsuarioEmpresa(ub.Id, b.Id, plantilla.Id));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var aRoles = await service.GetAllEmpresaAsync(a.Id);
            var bRoles = await service.GetAllEmpresaAsync(b.Id);
            Assert.Contains(aRoles, r => r.Id == ar.Id);
            Assert.DoesNotContain(aRoles, r => r.Id == br.Id);
            Assert.Contains(bRoles, r => r.Id == br.Id);
            Assert.DoesNotContain(bRoles, r => r.Id == ar.Id);
            Assert.Equal(1, aRoles.Single(r => r.Id == plantilla.Id).CantidadUsuarios);
            Assert.Equal(1, bRoles.Single(r => r.Id == plantilla.Id).CantidadUsuarios);
            Assert.Null(await service.GetByIdEmpresaAsync(ar.Id, b.Id));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
