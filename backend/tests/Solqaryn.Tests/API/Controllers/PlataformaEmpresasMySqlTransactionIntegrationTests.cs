using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

[Trait("Category", "Integration")]
public sealed class PlataformaEmpresasMySqlTransactionIntegrationTests
{
    [Fact]
    public async Task AltaA_B_EsTransaccional_ConAuditoriaYRollbackEnMySqlReal()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL efímero CI no configurado.");
        var cs = new MySqlConnectionStringBuilder(raw)
        {
            Database = "p4_platform_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(cs, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        var optFail = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(cs, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .AddInterceptors(new SimulatedAuditFailure())
            .Options;
        var optFailConfig = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(cs, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .AddInterceptors(new SimulatedConfigFailure())
            .Options;
        var optFailMembership = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(cs, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .AddInterceptors(new SimulatedMembershipFailure())
            .Options;

        try
        {
            int actorId, adminId, pendienteId, tenantAId;
            await using (var setup = new AppDbContext(opt))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);

                var platformRole = new Rol
                {
                    Nombre = "P4 Plataforma Administrador",
                    NombreNormalizado = "P4_PLAT_ADMIN",
                    Ambito = AmbitoAutorizacion.Plataforma,
                    EsSistema = true, EsAdministrador = true, Activo = true
                };
                var enterpriseRole = new Rol
                {
                    Nombre = "P4 Empresa Administrador",
                    NombreNormalizado = "P4_EMP_ADMIN",
                    Ambito = AmbitoAutorizacion.Empresa,
                    EsSistema = true, EsAdministrador = true, Activo = true
                };
                var baseRole = new Rol
                {
                    Nombre = "P4 Empresa Rol Base",
                    NombreNormalizado = "P4_EMP_BASE",
                    Ambito = AmbitoAutorizacion.Empresa,
                    EsSistema = true, EsAdministrador = false, Activo = true
                };
                setup.Roles.AddRange(platformRole, enterpriseRole, baseRole);
                await setup.SaveChangesAsync();

                var actor = new Usuario
                {
                    NombreUsuario = "p4-platform-transact",
                    NombreCompleto = "Admin Plataforma",
                    PasswordHash = "hash-efimero",
                    RolId = platformRole.Id, Activo = true
                };
                var admin = new Usuario
                {
                    NombreUsuario = "p4-tenant-transact",
                    NombreCompleto = "Admin de Empresa",
                    PasswordHash = "hash-efimero",
                    RolId = enterpriseRole.Id, Activo = true
                };
                setup.Usuarios.AddRange(actor, admin);
                await setup.SaveChangesAsync();
                // Esta cuenta permanece inactiva hasta el commit del alta.
                // Ambas fallas posteriores deben restaurarla a inactiva.
                var pendiente = new Usuario
                {
                    NombreUsuario = "p4-tenant-pendiente-transact",
                    NombreCompleto = "Admin Pendiente",
                    PasswordHash = "hash-efimero",
                    RolId = enterpriseRole.Id,
                    Activo = false,
                    CreadoPorUsuarioId = actor.Id
                };
                setup.Usuarios.Add(pendiente);
                await setup.SaveChangesAsync();
                setup.Set<UsuarioRolPlataforma>().Add(
                    new UsuarioRolPlataforma(actor.Id, platformRole.Id));
                var tenantA = new Empresa("Empresa A");
                setup.Set<Empresa>().Add(tenantA);
                await setup.SaveChangesAsync();
                actorId = actor.Id;
                adminId = admin.Id;
                pendienteId = pendiente.Id;
                tenantAId = tenantA.Id;
            }

            // En B se hace un único commit de empresa, ConfigEmpresa, membresía y auditoría.
            await using (var successDb = new AppDbContext(opt))
            {
                var controller = Controller(successDb, actorId);
                var result = await controller.AltaControlada(new()
                {
                    Nombre = "Empresa B",
                    AdministradorUsuarioId = adminId
                }, CancellationToken.None);
                Assert.IsType<CreatedResult>(result);
            }

            await using (var verify = new AppDbContext(opt))
            {
                var companies = await verify.Set<Empresa>()
                    .IgnoreQueryFilters().OrderBy(x => x.Id).ToListAsync();
                Assert.Equal(2, companies.Count);
                Assert.Contains(companies, x => x.Id == tenantAId);
                var b = Assert.Single(companies.Where(x => x.Nombre == "Empresa B"));
                var membership = Assert.Single(await verify.Set<UsuarioEmpresa>()
                    .IgnoreQueryFilters().ToListAsync());
                Assert.Equal(b.Id, membership.EmpresaId);
                Assert.Equal(adminId, membership.UsuarioId);
                var config = Assert.Single(await verify.Set<ConfigEmpresa>()
                    .IgnoreQueryFilters().ToListAsync());
                Assert.Equal(b.Id, config.EmpresaId);
                Assert.Equal(actorId, config.CreadoPorUsuarioId);
                Assert.Equal("p4-platform-transact", config.CreadoPorNombreUsuario);
                Assert.Equal("HNL", config.Moneda);
                Assert.Equal("America/Tegucigalpa", config.ZonaHoraria);
                Assert.Equal("{}", config.ImpuestosJson);
                Assert.Equal("{}", config.EmisionJson);
                Assert.Equal(1L, config.Version);
                Assert.False(config.CorreoConfigurado);
                var audit = Assert.Single(await verify.Set<RegistroAuditoria>()
                    .IgnoreQueryFilters().Where(x => x.Entidad == "Empresa").ToListAsync());
                Assert.Equal(b.Id, audit.EmpresaId);
                Assert.Equal(OrigenAutorizacion.RolPlataforma, audit.OrigenAutorizacion);
                Assert.Contains("\"ConfigEmpresaId\"", audit.ValoresNuevos ?? "");
            }

            // Fallar la auditoría después de persistir empresa, ConfigEmpresa
            // y membresía dentro de la transacción, antes de su único commit.
            await using (var failedDb = new AppDbContext(optFail))
            {
                var controller = Controller(failedDb, actorId);
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    controller.AltaControlada(new()
                    {
                        Nombre = "Empresa C - debe revertirse",
                        AdministradorUsuarioId = pendienteId
                    }, CancellationToken.None));
            }

            // Fallar la inserción de ConfigEmpresa después de la primera
            // escritura de Empresa y activación temporal del administrador.
            await using (var failedConfigDb = new AppDbContext(optFailConfig))
            {
                var controller = Controller(failedConfigDb, actorId);
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    controller.AltaControlada(new()
                    {
                        Nombre = "Empresa D - config fallida",
                        AdministradorUsuarioId = pendienteId
                    }, CancellationToken.None));
            }

            await using (var verify = new AppDbContext(opt))
            {
                Assert.False(await verify.Set<Empresa>().IgnoreQueryFilters()
                    .AnyAsync(x => x.Nombre == "Empresa C - debe revertirse"));
                Assert.False(await verify.Set<Empresa>().IgnoreQueryFilters()
                    .AnyAsync(x => x.Nombre == "Empresa D - config fallida"));
                Assert.Equal(2, await verify.Set<Empresa>().IgnoreQueryFilters().CountAsync());
                Assert.Single(await verify.Set<ConfigEmpresa>()
                    .IgnoreQueryFilters().ToListAsync());
                Assert.Single(await verify.Set<UsuarioEmpresa>()
                    .IgnoreQueryFilters().ToListAsync());
                Assert.Single(await verify.Set<RegistroAuditoria>()
                    .IgnoreQueryFilters().Where(x => x.Entidad == "Empresa").ToListAsync());
                // No queda administrador activado sin empresa tras un rollback.
                var pendiente = await verify.Usuarios.IgnoreQueryFilters()
                    .SingleAsync(x => x.Id == pendienteId);
                Assert.False(pendiente.Activo);
            }

            // Nuevo modo: usuario + contraseña BCrypt + empresa + config +
            // membresía admin + auditoría, bajo un ÚNICO commit MySQL real.
            const string passwordNuevo = "ClaveSegura!2026";
            await using (var successNew = new AppDbContext(opt))
            {
                var response = await Controller(successNew, actorId).AltaControlada(new()
                {
                    Nombre = "Empresa con alta nueva",
                    AdministradorNuevo = new()
                    {
                        NombreUsuario = "p4-usuario-atomico",
                        NombreCompleto = "Admin Empresarial Atómico",
                        Password = passwordNuevo
                    }
                }, CancellationToken.None);
                Assert.IsType<CreatedResult>(response);
            }

            await using (var verified = new AppDbContext(opt))
            {
                var usuario = await verified.Usuarios.IgnoreQueryFilters()
                    .SingleAsync(u => u.NombreUsuario == "p4-usuario-atomico");
                Assert.True(usuario.Activo);
                Assert.True(BCrypt.Net.BCrypt.Verify(passwordNuevo, usuario.PasswordHash));
                Assert.StartsWith("$2", usuario.PasswordHash);
                Assert.NotEqual(passwordNuevo, usuario.PasswordHash);
                var roleBase = await verified.Roles.IgnoreQueryFilters()
                    .SingleAsync(r => r.Id == usuario.RolId);
                Assert.False(roleBase.EsAdministrador);
                Assert.Equal(AmbitoAutorizacion.Empresa, roleBase.Ambito);
                Assert.False(await verified.Set<UsuarioRolPlataforma>()
                    .IgnoreQueryFilters().AnyAsync(x => x.UsuarioId == usuario.Id));

                var empresaNueva = await verified.Set<Empresa>()
                    .IgnoreQueryFilters().SingleAsync(x => x.Nombre == "Empresa con alta nueva");
                var config = await verified.Set<ConfigEmpresa>().IgnoreQueryFilters()
                    .SingleAsync(x => x.EmpresaId == empresaNueva.Id);
                Assert.Equal("HNL", config.Moneda);
                var m = await verified.Set<UsuarioEmpresa>().IgnoreQueryFilters()
                    .SingleAsync(x => x.UsuarioId == usuario.Id);
                Assert.Equal(empresaNueva.Id, m.EmpresaId);
                var adminRole = await verified.Roles.IgnoreQueryFilters()
                    .SingleAsync(r => r.Id == m.RolId);
                Assert.True(adminRole.EsAdministrador);
                Assert.Equal(AmbitoAutorizacion.Empresa, adminRole.Ambito);
                var audit = await verified.Set<RegistroAuditoria>().IgnoreQueryFilters()
                    .SingleAsync(a => a.Entidad == "Empresa" && a.EmpresaId == empresaNueva.Id);
                Assert.Equal(actorId, audit.UsuarioId);
                Assert.Contains("\"UsuarioCreadoEnAlta\":true", audit.ValoresNuevos ?? "");
                Assert.DoesNotContain(passwordNuevo, audit.ValoresNuevos ?? "");
                Assert.DoesNotContain(usuario.PasswordHash, audit.ValoresNuevos ?? "");
            }

            // Los tres fallos ocurren DESPUÉS de insertar usuario y empresa.
            // No deben sobrevivir ni contraseñas ni asignaciones parciales.
            await using (var failed = new AppDbContext(optFailConfig))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    Controller(failed, actorId).AltaControlada(new()
                    {
                        Nombre = "Empresa rollback config nueva",
                        AdministradorNuevo = new() {
                            NombreUsuario = "p4-rollback-config",
                            NombreCompleto = "Administrador Config Fallida",
                            Password = passwordNuevo
                        }
                    }, CancellationToken.None));
            }
            await using (var failed = new AppDbContext(optFailMembership))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    Controller(failed, actorId).AltaControlada(new()
                    {
                        Nombre = "Empresa rollback membresia nueva",
                        AdministradorNuevo = new() {
                            NombreUsuario = "p4-rollback-miembro",
                            NombreCompleto = "Administrador Miembro Fallido",
                            Password = passwordNuevo
                        }
                    }, CancellationToken.None));
            }
            await using (var failed = new AppDbContext(optFail))
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    Controller(failed, actorId).AltaControlada(new()
                    {
                        Nombre = "Empresa rollback auditoria nueva",
                        AdministradorNuevo = new() {
                            NombreUsuario = "p4-rollback-audit",
                            NombreCompleto = "Administrador Auditoria Fallida",
                            Password = passwordNuevo
                        }
                    }, CancellationToken.None));
            }
            await using (var verified = new AppDbContext(opt))
            {
                foreach (var nombreNoPersistido in new[] {
                    "p4-rollback-config", "p4-rollback-miembro", "p4-rollback-audit"
                })
                    Assert.False(await verified.Usuarios.IgnoreQueryFilters()
                        .AnyAsync(x => x.NombreUsuario == nombreNoPersistido));
                foreach (var empresaNoPersistida in new[] {
                    "Empresa rollback config nueva", "Empresa rollback membresia nueva",
                    "Empresa rollback auditoria nueva"
                })
                    Assert.False(await verified.Set<Empresa>().IgnoreQueryFilters()
                        .AnyAsync(x => x.Nombre == empresaNoPersistida));
                Assert.Equal(3, await verified.Set<Empresa>().IgnoreQueryFilters().CountAsync());
                Assert.Equal(4, await verified.Usuarios.IgnoreQueryFilters().CountAsync());
                Assert.Equal(2, await verified.Set<ConfigEmpresa>().IgnoreQueryFilters().CountAsync());
                Assert.Equal(2, await verified.Set<UsuarioEmpresa>().IgnoreQueryFilters().CountAsync());
                Assert.Equal(2, await verified.Set<RegistroAuditoria>().IgnoreQueryFilters()
                    .CountAsync(x => x.Entidad == "Empresa"));
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(opt);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static PlataformaEmpresasController Controller(AppDbContext db, int actorId)
    {
        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(actorId);
        current.SetupGet(x => x.NombreUsuario).Returns("p4-platform-transact");
        current.SetupGet(x => x.EstaAutenticado).Returns(true);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["MultiEmpresa:PlatformOnboardingEnabled"] = "true",
                ["MultiEmpresa:CommercialIsolationCertified"] = "true"
            }).Build();
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, actorId.ToString()) }, "TestJwt"));
        return new PlataformaEmpresasController(db, current.Object, configuration,
            new PlataformaScopeService(db, current.Object))
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    private sealed class SimulatedConfigFailure : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<ConfigEmpresa>()
                    .Any(x => x.State == EntityState.Added) == true)
                throw new InvalidOperationException("Alta: falla simulada en ConfigEmpresa.");
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }

    private sealed class SimulatedMembershipFailure : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<UsuarioEmpresa>()
                    .Any(x => x.State == EntityState.Added) == true)
                throw new InvalidOperationException("Alta: falla simulada en membresía.");
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }

    private sealed class SimulatedAuditFailure : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<RegistroAuditoria>()
                    .Any(x => x.State == EntityState.Added) == true)
                throw new InvalidOperationException("P4: falla simulada de auditoría, rollback obligatorio.");
            return new ValueTask<InterceptionResult<int>>(result);
        }
    }
}
