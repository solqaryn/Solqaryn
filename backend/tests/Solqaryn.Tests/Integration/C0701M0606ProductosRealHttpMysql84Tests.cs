using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Controllers;
using Solqaryn.API.Middleware;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.Integration;

/// <summary>
/// C07-01.M06.06: endpoint de produccion /productos, JWT firmado y
/// RBAC/membresias reales en Oracle EF/MySQL 8.4 efimero.
/// Esta lectura no certifica Angular, CRUD, variantes ni rollback.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0701M0606ProductosRealHttpMysql84Tests
{
    private const string Issuer = "c0701-m0606-ephemeral-issuer";
    private const string Audience = "c0701-m0606-ephemeral-audience";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0701-m0606-local-test-signing-key-isolated-2026"));

    [Fact]
    public async Task RealProductosGetUsesSignedJwtMysqlTenantABAndRevocation()
    {
        var raw = Environment.GetEnvironmentVariable("C0701_M0606_MYSQL_TEST")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") ||
            origin.Port != 3306 || origin.UserID != "root" ||
            origin.Database != "c0701_m0606_bootstrap")
            throw new InvalidOperationException("EPHEMERAL_LOOPBACK_ONLY");

        var isolated = new MySqlConnectionStringBuilder(raw)
        {
            Database = "c0701_m0606_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated, mysql => mysql.MigrationsAssembly(
                "Solqaryn.Infrastructure.Migrations")).Options;
        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            await db.Database.OpenConnectionAsync();
            await using (var command = db.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "SELECT VERSION()";
                Assert.StartsWith("8.4.", Convert.ToString(await command.ExecuteScalarAsync()));
            }
            await db.Database.CloseConnectionAsync();

            var a = new Empresa("C0701M0606 Tenant A");
            var b = new Empresa("C0701M0606 Tenant B");
            db.Set<Empresa>().AddRange(a, b);
            await db.SaveChangesAsync();
            var roleA = new Rol
            {
                Nombre = "C0701M0606 role A", NombreNormalizado = "C0701M0606_A",
                EmpresaId = a.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "C0701M0606 role B", NombreNormalizado = "C0701M0606_B",
                EmpresaId = b.Id, Ambito = AmbitoAutorizacion.Empresa, Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();
            var userA = NewUser("a", roleA.Id);
            var userB = NewUser("b", roleB.Id);
            db.Usuarios.AddRange(userA, userB);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.AddRange(
                new UsuarioEmpresa(userA.Id, a.Id, roleA.Id),
                new UsuarioEmpresa(userB.Id, b.Id, roleB.Id));
            await db.SaveChangesAsync();

            var permission = await db.Permisos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Modulo == ModuloSistema.Productos &&
                    p.Accion == AccionPermiso.Ver &&
                    p.Ambito == AmbitoAutorizacion.Empresa && p.Activo && !p.Eliminado);
            if (permission is null)
            {
                permission = new Permiso
                {
                    Codigo = "C0701M0606.PRODUCTOS.VER",
                    Nombre = "C0701 M0606 isolated permission",
                    Modulo = ModuloSistema.Productos, Accion = AccionPermiso.Ver,
                    Ambito = AmbitoAutorizacion.Empresa, Activo = true
                };
                db.Permisos.Add(permission);
                await db.SaveChangesAsync();
            }
            db.RolPermisos.AddRange(
                new RolPermiso { RolId = roleA.Id, PermisoId = permission.Id },
                new RolPermiso { RolId = roleB.Id, PermisoId = permission.Id });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            // R2 category read contract: the existing EF shadow EmpresaId is
            // physical in MySQL; authorization is still SQL-backed.
            var categoryReadPermission = await db.Permisos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Modulo == ModuloSistema.Categorias &&
                    p.Accion == AccionPermiso.Ver &&
                    p.Ambito == AmbitoAutorizacion.Empresa && p.Activo && !p.Eliminado);
            if (categoryReadPermission is null)
            {
                categoryReadPermission = new Permiso
                {
                    Codigo = "C0701M0606.CATEGORIAS.VER",
                    Nombre = "C0701 isolated permission for category reads",
                    Modulo = ModuloSistema.Categorias, Accion = AccionPermiso.Ver,
                    Ambito = AmbitoAutorizacion.Empresa, Activo = true
                };
                db.Permisos.Add(categoryReadPermission);
                await db.SaveChangesAsync();
            }
            db.RolPermisos.AddRange(
                new RolPermiso { RolId = roleA.Id, PermisoId = categoryReadPermission.Id },
                new RolPermiso { RolId = roleB.Id, PermisoId = categoryReadPermission.Id });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            // Real Impuestos.Ver grants on isolated SQL-backed roles.
            var taxReadPermission = await db.Permisos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Modulo == ModuloSistema.Impuestos &&
                    p.Accion == AccionPermiso.Ver &&
                    p.Ambito == AmbitoAutorizacion.Empresa && p.Activo && !p.Eliminado);
            if (taxReadPermission is null)
            {
                taxReadPermission = new Permiso
                {
                    Codigo = "C0701M0606.IMPUESTOS.VER",
                    Nombre = "C0701 isolated fiscal read permission",
                    Modulo = ModuloSistema.Impuestos, Accion = AccionPermiso.Ver,
                    Ambito = AmbitoAutorizacion.Empresa, Activo = true
                };
                db.Permisos.Add(taxReadPermission);
                await db.SaveChangesAsync();
            }
            db.RolPermisos.AddRange(
                new RolPermiso { RolId = roleA.Id, PermisoId = taxReadPermission.Id },
                new RolPermiso { RolId = roleB.Id, PermisoId = taxReadPermission.Id });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // SQL-backed fiscal grants for both isolated tenants.
            foreach (var action in new[] { AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.Crear, AccionPermiso.Editar })
            {
                var fiscalPermission = await db.Permisos.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.Modulo == ModuloSistema.Impuestos &&
                        p.Accion == action && p.Ambito == AmbitoAutorizacion.Empresa &&
                        p.Activo && !p.Eliminado);
                if (fiscalPermission is null)
                {
                    fiscalPermission = new Permiso
                    {
                        Codigo = "C0701M0606.IMPUESTOS." + action.ToString().ToUpperInvariant(),
                        Nombre = "C0701 fiscal state grant " + action,
                        Modulo = ModuloSistema.Impuestos, Accion = action,
                        Ambito = AmbitoAutorizacion.Empresa, Activo = true
                    };
                    db.Permisos.Add(fiscalPermission);
                    await db.SaveChangesAsync();
                }
                db.RolPermisos.AddRange(
                    new RolPermiso { RolId = roleA.Id, PermisoId = fiscalPermission.Id },
                    new RolPermiso { RolId = roleB.Id, PermisoId = fiscalPermission.Id });
            }
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Test-only RBAC: the valid A role has Crear, while B does not.
            var createPermission = await db.Permisos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Modulo == ModuloSistema.Productos
                    && x.Accion == AccionPermiso.Crear
                    && x.Ambito == AmbitoAutorizacion.Empresa && x.Activo && !x.Eliminado);
            if (createPermission is null)
            {
                createPermission = new Permiso
                {
                    Codigo = "C0701M0606.PRODUCTOS.CREAR",
                    Nombre = "C0701 isolated permission for product creation",
                    Modulo = ModuloSistema.Productos, Accion = AccionPermiso.Crear,
                    Ambito = AmbitoAutorizacion.Empresa, Activo = true
                };
                db.Permisos.Add(createPermission);
                await db.SaveChangesAsync();
            }
            db.RolPermisos.Add(new RolPermiso
            {
                RolId = roleA.Id, PermisoId = createPermission.Id
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Both tenants may Editar within their verified enterprise; B still
            // has no Crear grant. This distinguishes IDOR from missing permission.
            var editPermission = await db.Permisos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Modulo == ModuloSistema.Productos
                    && x.Accion == AccionPermiso.Editar
                    && x.Ambito == AmbitoAutorizacion.Empresa && x.Activo && !x.Eliminado);
            if (editPermission is null)
            {
                editPermission = new Permiso
                {
                    Codigo = "C0701M0606.PRODUCTOS.EDITAR",
                    Nombre = "C0701 isolated edit permission",
                    Modulo = ModuloSistema.Productos, Accion = AccionPermiso.Editar,
                    Ambito = AmbitoAutorizacion.Empresa, Activo = true
                };
                db.Permisos.Add(editPermission);
                await db.SaveChangesAsync();
            }
            db.RolPermisos.AddRange(
                new RolPermiso { RolId = roleA.Id, PermisoId = editPermission.Id },
                new RolPermiso { RolId = roleB.Id, PermisoId = editPermission.Id });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Real tenant-owned products, seeded only using a server-side verified
            // commercial authority, never by trusting the request header.
            async Task<int> SeedOwnedProductAsync(int tenantId, string name)
            {
                var request = new DefaultHttpContext();
                request.Items[
                    Solqaryn.Application.Security.VerifiedCommercialTenant
                        .AuthorizedEmpresaIdItemKey] = tenantId;
                var accessor = new HttpContextAccessor { HttpContext = request };
                await using var owned = new AppDbContext(options,
                    tenantHttpContextAccessor: accessor);
                var category = new Categoria { Nombre = "Category " + name };
                owned.Categorias.Add(category);
                await owned.SaveChangesAsync();
                var product = new Producto
                {
                    Nombre = name, Marca = "", Modelo = "",
                    Precio = 100m, Costo = 50m, CategoriaId = category.Id
                };
                owned.Productos.Add(product);
                await owned.SaveChangesAsync();
                Assert.Equal(tenantId, product.EmpresaId);
                return product.Id;
            }
            var productAId = await SeedOwnedProductAsync(a.Id, "C0701RealProductoA");
            var productBId = await SeedOwnedProductAsync(b.Id, "C0701RealProductoB");

            // Real Oracle EF/MySQL 8.4 tax owners, injected server-side only.
            async Task<int> SeedOwnedTaxAsync(int tenantId, string code, bool active = true)
            {
                var context = new DefaultHttpContext();
                context.Items[
                    Solqaryn.Application.Security.VerifiedCommercialTenant
                        .AuthorizedEmpresaIdItemKey] = tenantId;
                var accessor = new HttpContextAccessor { HttpContext = context };
                await using var owned = new AppDbContext(options,
                    tenantHttpContextAccessor: accessor);
                var tax = new Impuesto
                {
                    Nombre = "C0701Fiscal" + code, Codigo = code,
                    Tipo = TipoImpuesto.Porcentaje, Tasa = 15m, Activo = active
                };
                owned.Impuestos.Add(tax);
                await owned.SaveChangesAsync();
                return tax.Id;
            }
            var taxAId = await SeedOwnedTaxAsync(a.Id, "C0701TAXA");
            var taxBId = await SeedOwnedTaxAsync(b.Id, "C0701TAXB");
            var inactiveTaxAId = await SeedOwnedTaxAsync(a.Id, "C0701TAXAINACTIVE", false);
            Assert.True(inactiveTaxAId > 0);

            // P1: una escritura de producto no puede sobrevivir si la bitacora
            // critica no se persiste. Unidad de trabajo REAL + Oracle EF/MySQL.
            // Esto prueba rollback de ProductService.CreateAsync, no habilita
            // escrituras HTTP comerciales ni certifica UI/variantes.
            var auditRollbackName = "C0701AuditRollback" + Guid.NewGuid().ToString("N");
            var auditContext = new DefaultHttpContext();
            auditContext.Items[
                Solqaryn.Application.Security.VerifiedCommercialTenant
                    .AuthorizedEmpresaIdItemKey] = a.Id;
            var auditAccessor = new HttpContextAccessor { HttpContext = auditContext };
            await using (var auditDb = new AppDbContext(options,
                tenantHttpContextAccessor: auditAccessor))
            {
                var categoryId = await auditDb.Categorias
                    .Where(c => c.Nombre == "Category C0701RealProductoA")
                    .Select(c => c.Id).SingleAsync();
                var actor = new Mock<ICurrentUserService>();
                actor.SetupGet(x => x.UsuarioId).Returns(userA.Id);
                actor.SetupGet(x => x.NombreUsuario).Returns(userA.NombreUsuario);
                var unavailableAudit = new Mock<IAuditoriaService>();
                unavailableAudit.Setup(x => x.RegistrarEstrictoAsync(
                        It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                        It.IsAny<string>(), It.IsAny<int?>(),
                        It.IsAny<string?>(), It.IsAny<object?>(),
                        It.IsAny<object?>(), It.IsAny<string?>(),
                        It.IsAny<string>(), It.IsAny<string?>()))
                    .ThrowsAsync(new InvalidOperationException("AUDIT_PERSISTENCE_UNAVAILABLE"));
                var service = new ProductoService(
                    new ProductoRepository(auditDb),
                    new CategoriaRepository(auditDb),
                    Mock.Of<IImageStorageService>(), actor.Object,
                    unavailableAudit.Object, httpContextAccessor: auditAccessor);
                var uow = new UnitOfWork(auditDb);
                var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => uow.ExecuteInTransactionAsync(async () =>
                    {
                        await service.CreateAsync(new CreateProductoDto
                        {
                            Nombre = auditRollbackName,
                            CategoriaId = categoryId
                        });
                    }));
                Assert.Equal("AUDIT_PERSISTENCE_UNAVAILABLE", thrown.Message);
                unavailableAudit.Verify(x => x.RegistrarEstrictoAsync(
                    ModuloSistema.Productos, AccionPermiso.Crear,
                    It.IsAny<string>(), It.IsAny<int?>(),
                    It.IsAny<string?>(), It.IsAny<object?>(),
                    It.IsAny<object?>(), It.IsAny<string?>(),
                    It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
            }
            await using (var auditReadback = new AppDbContext(options))
            {
                Assert.Equal(0, await auditReadback.Productos
                    .IgnoreQueryFilters().AsNoTracking()
                    .CountAsync(p => p.Nombre == auditRollbackName));
            }


            // Strict audit is required for every material product write,
            // not only Create. A failed injected audit must roll back the
            // actual Oracle EF/MySQL transaction in the same isolated schema.
            // This is a NEGATIVE real-DB proof, not a positive durable audit
            // or permission to bypass private HTTP write quarantine.
            async Task AssertStrictAuditRollbackAsync(
                AccionPermiso action, Func<ProductoService, Task> mutation)
            {
                await using (var changeDb = new AppDbContext(options,
                    tenantHttpContextAccessor: auditAccessor))
                {
                    var actor = new Mock<ICurrentUserService>();
                    actor.SetupGet(x => x.UsuarioId).Returns(userA.Id);
                    actor.SetupGet(x => x.NombreUsuario).Returns(userA.NombreUsuario);
                    var unavailableAudit = new Mock<IAuditoriaService>();
                    unavailableAudit.Setup(x => x.RegistrarEstrictoAsync(
                        ModuloSistema.Productos, action,
                            It.IsAny<string>(), It.IsAny<int?>(),
                            It.IsAny<string?>(), It.IsAny<object?>(),
                            It.IsAny<object?>(), It.IsAny<string?>(),
                            It.IsAny<string>(), It.IsAny<string?>()))
                        .ThrowsAsync(new InvalidOperationException(
                            "AUDIT_PERSISTENCE_UNAVAILABLE"));
                    var service = new ProductoService(
                        new ProductoRepository(changeDb),
                        new CategoriaRepository(changeDb),
                        Mock.Of<IImageStorageService>(), actor.Object,
                        unavailableAudit.Object, httpContextAccessor: auditAccessor);
                    var failure = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => new UnitOfWork(changeDb).ExecuteInTransactionAsync(
                            async () => await mutation(service)));
                    Assert.Equal("AUDIT_PERSISTENCE_UNAVAILABLE", failure.Message);
                    unavailableAudit.Verify(x => x.RegistrarEstrictoAsync(
                        ModuloSistema.Productos, action,
                        It.IsAny<string>(), It.IsAny<int?>(),
                        It.IsAny<string?>(), It.IsAny<object?>(),
                        It.IsAny<object?>(), It.IsAny<string?>(),
                        It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
                }
                await using (var readback = new AppDbContext(options))
                {
                    var untouched = await readback.Productos
                        .IgnoreQueryFilters().AsNoTracking()
                        .SingleAsync(p => p.Id == productAId);
                    Assert.Equal("C0701RealProductoA", untouched.Nombre);
                    Assert.True(untouched.Activo);
                    Assert.False(untouched.Eliminado);
                    Assert.Equal(100m, untouched.Precio);
                }
            }

            await AssertStrictAuditRollbackAsync(AccionPermiso.Editar,
                service => service.UpdateAsync(productAId,
                    new UpdateProductoDto
                    {
                        Nombre = "C0701AuditRejectedEdit" + Guid.NewGuid().ToString("N")
                    }));
            await AssertStrictAuditRollbackAsync(AccionPermiso.Desactivar,
                service => service.CambiarEstadoAsync(productAId, false));
            await AssertStrictAuditRollbackAsync(AccionPermiso.EliminarLogico,
                service => service.DeleteAsync(productAId));

            // Positive durable audit proof: unlike the injected-failure cases
            // above, use the real AuditoriaService/AuditoriaRepository and the
            // SAME Oracle EF DbContext/transaction as the product creation.
            // Reopen from a fresh context to prove both product and audit row
            // survive the commit and retain server-verified tenant ownership.
            // This does not certify the quarantined private HTTP write path.
            var auditedProductName = "C0701DurableAudit" + Guid.NewGuid().ToString("N");
            await using (var durableDb = new AppDbContext(options,
                tenantHttpContextAccessor: auditAccessor))
            {
                var categoryId = await durableDb.Categorias
                    .Where(c => c.Nombre == "Category C0701RealProductoA")
                    .Select(c => c.Id).SingleAsync();
                var actor = new Mock<ICurrentUserService>();
                actor.SetupGet(x => x.UsuarioId).Returns(userA.Id);
                actor.SetupGet(x => x.NombreUsuario).Returns(userA.NombreUsuario);
                var audit = new AuditoriaService(
                    new AuditoriaRepository(durableDb), actor.Object,
                    auditAccessor,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditoriaService>.Instance);
                var service = new ProductoService(
                    new ProductoRepository(durableDb),
                    new CategoriaRepository(durableDb),
                    Mock.Of<IImageStorageService>(), actor.Object,
                    audit, httpContextAccessor: auditAccessor);
                await new UnitOfWork(durableDb).ExecuteInTransactionAsync(async () =>
                {
                    var created = await service.CreateAsync(new CreateProductoDto
                    {
                        Nombre = auditedProductName,
                        CategoriaId = categoryId
                    });
                    Assert.True(created.Id > 0);
                });
            }
            await using (var durableReadback = new AppDbContext(options))
            {
                var stored = await durableReadback.Productos
                    .IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(p => p.Nombre == auditedProductName);
                Assert.Equal(a.Id, stored.EmpresaId);
                var receipt = await durableReadback.RegistrosAuditoria
                    .IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(log => log.Modulo == ModuloSistema.Productos
                        && log.Accion == AccionPermiso.Crear
                        && log.ReferenciaId == stored.Id
                        && log.Entidad == "Producto");
                Assert.Equal(a.Id, receipt.EmpresaId);
                Assert.Equal(userA.Id, receipt.UsuarioId);
                Assert.Equal(userA.NombreUsuario, receipt.NombreUsuario);
                Assert.Equal("Exito", receipt.Resultado);
            }

            // V23 R2: positive commercial state transitions persist BOTH the
            // product state and strict audit receipts in isolated Oracle EF /
            // MySQL 8.4. This remains a service-level proof, not an HTTP-write
            // permission or a replacement for browser-to-API acceptance.
            int auditedProductId;
            await using (var productLookup = new AppDbContext(options))
            {
                auditedProductId = await productLookup.Productos
                    .IgnoreQueryFilters().AsNoTracking()
                    .Where(p => p.Nombre == auditedProductName)
                    .Select(p => p.Id).SingleAsync();
            }
            foreach (var (active, action) in new[]
            {
                (active: false, action: AccionPermiso.Desactivar),
                (active: true, action: AccionPermiso.Activar)
            })
            {
                await using (var transitionDb = new AppDbContext(options,
                    tenantHttpContextAccessor: auditAccessor))
                {
                    var actor = new Mock<ICurrentUserService>();
                    actor.SetupGet(x => x.UsuarioId).Returns(userA.Id);
                    actor.SetupGet(x => x.NombreUsuario).Returns(userA.NombreUsuario);
                    var audit = new AuditoriaService(
                        new AuditoriaRepository(transitionDb), actor.Object,
                        auditAccessor,
                        Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditoriaService>.Instance);
                    var service = new ProductoService(
                        new ProductoRepository(transitionDb),
                        new CategoriaRepository(transitionDb),
                        Mock.Of<IImageStorageService>(), actor.Object,
                        audit, httpContextAccessor: auditAccessor);
                    await new UnitOfWork(transitionDb).ExecuteInTransactionAsync(async () =>
                    {
                        var changed = await service.CambiarEstadoAsync(auditedProductId, active);
                        Assert.NotNull(changed);
                        Assert.Equal(active, changed!.Activo);
                    });
                }
                await using (var committedReadback = new AppDbContext(options))
                {
                    var product = await committedReadback.Productos
                        .IgnoreQueryFilters().AsNoTracking()
                        .SingleAsync(p => p.Id == auditedProductId);
                    Assert.Equal(a.Id, product.EmpresaId);
                    Assert.Equal(active, product.Activo);
                    Assert.False(product.Eliminado);
                    var receipt = await committedReadback.RegistrosAuditoria
                        .IgnoreQueryFilters().AsNoTracking()
                        .SingleAsync(log => log.Entidad == "Producto"
                            && log.ReferenciaId == auditedProductId
                            && log.Modulo == ModuloSistema.Productos
                            && log.Accion == action);
                    Assert.Equal(a.Id, receipt.EmpresaId);
                    Assert.Equal(userA.Id, receipt.UsuarioId);
                    Assert.Equal("Exito", receipt.Resultado);
                }
            }

            // Negative A/B: a tenant B service context cannot change the same
            // product or create a tenant B audit receipt for an A-owned record.
            var foreignHttp = new DefaultHttpContext();
            foreignHttp.Items[
                Solqaryn.Application.Security.VerifiedCommercialTenant
                    .AuthorizedEmpresaIdItemKey] = b.Id;
            var foreignAccessor = new HttpContextAccessor { HttpContext = foreignHttp };
            await using (var foreignDb = new AppDbContext(options,
                tenantHttpContextAccessor: foreignAccessor))
            {
                var foreignActor = new Mock<ICurrentUserService>();
                foreignActor.SetupGet(x => x.UsuarioId).Returns(userB.Id);
                foreignActor.SetupGet(x => x.NombreUsuario).Returns(userB.NombreUsuario);
                var foreignAudit = new AuditoriaService(
                    new AuditoriaRepository(foreignDb), foreignActor.Object,
                    foreignAccessor,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditoriaService>.Instance);
                var foreignService = new ProductoService(
                    new ProductoRepository(foreignDb),
                    new CategoriaRepository(foreignDb),
                    Mock.Of<IImageStorageService>(), foreignActor.Object,
                    foreignAudit, httpContextAccessor: foreignAccessor);
                Assert.Null(await foreignService.CambiarEstadoAsync(
                    auditedProductId, false));
            }
            await using (var foreignReadback = new AppDbContext(options))
            {
                var untouched = await foreignReadback.Productos
                    .IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(p => p.Id == auditedProductId);
                Assert.Equal(a.Id, untouched.EmpresaId);
                Assert.True(untouched.Activo);
                Assert.Equal(0, await foreignReadback.RegistrosAuditoria
                    .IgnoreQueryFilters().AsNoTracking()
                    .CountAsync(log => log.ReferenciaId == auditedProductId
                        && log.Modulo == ModuloSistema.Productos
                        && log.EmpresaId == b.Id));
            }


            // Real Oracle EF/MySQL transaction rollback at the tenant-bound
            // persistence layer. This is NOT HTTP CRUD acceptance: writes
            // through ProductosController still require separate coverage.
            var rollbackName = "C0701RollbackProducto" + Guid.NewGuid().ToString("N");
            var rollbackSku = "C0701RollbackVariante" + Guid.NewGuid().ToString("N");
            var rollbackHttp = new DefaultHttpContext();
            rollbackHttp.Items[
                Solqaryn.Application.Security.VerifiedCommercialTenant
                    .AuthorizedEmpresaIdItemKey] = a.Id;
            var rollbackAccessor = new HttpContextAccessor { HttpContext = rollbackHttp };
            await using (var rollbackDb = new AppDbContext(options,
                tenantHttpContextAccessor: rollbackAccessor))
            {
                var categoryId = await rollbackDb.Categorias
                    .Where(c => c.Nombre == "Category C0701RealProductoA")
                    .Select(c => c.Id).SingleAsync();
                await using var transaction = await rollbackDb.Database.BeginTransactionAsync();
                var temporary = new Producto
                {
                    Nombre = rollbackName, Marca = "", Modelo = "",
                    Precio = 321m, Costo = 123m, CategoriaId = categoryId
                };
                rollbackDb.Productos.Add(temporary);
                await rollbackDb.SaveChangesAsync();
                Assert.Equal(a.Id, temporary.EmpresaId);
                Assert.Equal(1, await rollbackDb.Productos
                    .CountAsync(p => p.Nombre == rollbackName));

                // El rollback debe cubrir también modificaciones de catálogo
                // y una variante del mismo tenant, no solo el INSERT de Producto.
                var persistedA = await rollbackDb.Productos
                    .SingleAsync(p => p.Id == productAId);
                persistedA.Precio = 654m;
                var temporaryVariant = new ProductoVariante
                {
                    ProductoId = temporary.Id,
                    EmpresaId = a.Id,
                    Sku = rollbackSku,
                    Cantidad = 1,
                    Precio = 87m,
                    Costo = 53m
                };
                rollbackDb.ProductoVariantes.Add(temporaryVariant);
                await rollbackDb.SaveChangesAsync();
                Assert.Equal(a.Id, temporaryVariant.EmpresaId);
                Assert.Equal(1, await rollbackDb.ProductoVariantes
                    .CountAsync(v => v.Sku == rollbackSku));
                Assert.Equal(654m, await rollbackDb.Productos
                    .Where(p => p.Id == productAId)
                    .Select(p => p.Precio).SingleAsync());
                await transaction.RollbackAsync();
            }
            await using (var verified = new AppDbContext(options,
                tenantHttpContextAccessor: rollbackAccessor))
            {
                Assert.Equal(0, await verified.Productos
                    .CountAsync(p => p.Nombre == rollbackName));
                Assert.Equal(0, await verified.ProductoVariantes
                    .CountAsync(v => v.Sku == rollbackSku));
                Assert.Equal(100m, await verified.Productos
                    .Where(p => p.Id == productAId)
                    .Select(p => p.Precio).SingleAsync());
            }




            // V24 DEV01: una mutacion de variante debe revertir TAMBIEN la
            // proyeccion del producto si el recibo de auditoria durable falla.
            // MySQL Oracle EF es real y efimero; no habilita HTTP productivo.
            var variantAuditSku = "C0701AUDITVAR" + Guid.NewGuid().ToString("N")[..12];
            int variantAuditId;
            await using (var variantSeedDb = new AppDbContext(options,
                tenantHttpContextAccessor: auditAccessor))
            {
                var seeded = new ProductoVariante
                {
                    EmpresaId = a.Id, ProductoId = auditedProductId,
                    Sku = variantAuditSku, Cantidad = 0,
                    Precio = 21m, Costo = 11m, UmbralStockBajo = 2,
                    EsTecnica = false, Activo = true, Eliminado = false
                };
                variantSeedDb.ProductoVariantes.Add(seeded);
                await variantSeedDb.SaveChangesAsync();
                variantAuditId = seeded.Id;
                Assert.True(variantAuditId > 0);
            }

            async Task AssertVariantAuditFailureRollsBackAsync(
                AccionPermiso action, Func<ProductoVarianteService, Task> mutate)
            {
                await using (var variantDb = new AppDbContext(options,
                    tenantHttpContextAccessor: auditAccessor))
                {
                    var actor = new Mock<ICurrentUserService>();
                    actor.SetupGet(x => x.UsuarioId).Returns(userA.Id);
                    actor.SetupGet(x => x.NombreUsuario).Returns(userA.NombreUsuario);
                    var failure = new Mock<IAuditoriaService>();
                    failure.Setup(x => x.RegistrarEstrictoAsync(
                            ModuloSistema.Productos, action,
                            It.IsAny<string>(), It.IsAny<int?>(),
                            It.IsAny<string?>(), It.IsAny<object?>(),
                            It.IsAny<object?>(), It.IsAny<string?>(),
                            It.IsAny<string>(), It.IsAny<string?>()))
                        .ThrowsAsync(new InvalidOperationException(
                            "VARIANTE_AUDIT_DURABLE_UNAVAILABLE"));
                    var service = new ProductoVarianteService(
                        new ProductoVarianteRepository(variantDb),
                        new ProductoRepository(variantDb),
                        Mock.Of<ICatalogoProductoService>(), actor.Object,
                        new UnitOfWork(variantDb), failure.Object);
                    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                        () => mutate(service));
                    Assert.Equal("VARIANTE_AUDIT_DURABLE_UNAVAILABLE", exception.Message);
                    failure.Verify(x => x.RegistrarEstrictoAsync(
                        ModuloSistema.Productos, action,
                        It.IsAny<string>(), It.IsAny<int?>(),
                        It.IsAny<string?>(), It.IsAny<object?>(),
                        It.IsAny<object?>(), It.IsAny<string?>(),
                        It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
                }
                await using (var check = new AppDbContext(options))
                {
                    var unchanged = await check.ProductoVariantes.IgnoreQueryFilters()
                        .AsNoTracking().SingleAsync(v => v.Id == variantAuditId);
                    Assert.True(unchanged.Activo);
                    Assert.False(unchanged.Eliminado);
                    Assert.Equal(a.Id, unchanged.EmpresaId);
                    Assert.Equal(0, await check.RegistrosAuditoria.IgnoreQueryFilters()
                        .CountAsync(x => x.Entidad == "ProductoVariante"
                            && x.ReferenciaId == variantAuditId));
                }
            }

            await AssertVariantAuditFailureRollsBackAsync(
                AccionPermiso.Desactivar,
                async service => { await service.CambiarEstadoAsync(
                    auditedProductId, variantAuditId, false); });
            await AssertVariantAuditFailureRollsBackAsync(
                AccionPermiso.EliminarLogico,
                async service => { await service.DeleteAsync(
                    auditedProductId, variantAuditId); });

            // Positivo: variante y auditoria real se consolidan conjuntamente
            // con mismo contexto verificado y se leen desde contexto nuevo.
            await using (var commitDb = new AppDbContext(options,
                tenantHttpContextAccessor: auditAccessor))
            {
                var actor = new Mock<ICurrentUserService>();
                actor.SetupGet(x => x.UsuarioId).Returns(userA.Id);
                actor.SetupGet(x => x.NombreUsuario).Returns(userA.NombreUsuario);
                var audit = new AuditoriaService(
                    new AuditoriaRepository(commitDb), actor.Object, auditAccessor,
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditoriaService>.Instance);
                var service = new ProductoVarianteService(
                    new ProductoVarianteRepository(commitDb),
                    new ProductoRepository(commitDb),
                    Mock.Of<ICatalogoProductoService>(), actor.Object,
                    new UnitOfWork(commitDb), audit);
                var changed = await service.CambiarEstadoAsync(
                    auditedProductId, variantAuditId, false);
                Assert.NotNull(changed);
                Assert.False(changed!.Activo);
            }
            await using (var committed = new AppDbContext(options))
            {
                var variant = await committed.ProductoVariantes.IgnoreQueryFilters()
                    .AsNoTracking().SingleAsync(v => v.Id == variantAuditId);
                Assert.False(variant.Activo);
                Assert.False(variant.Eliminado);
                Assert.Equal(a.Id, variant.EmpresaId);
                var receipt = await committed.RegistrosAuditoria.IgnoreQueryFilters()
                    .AsNoTracking().SingleAsync(x => x.Entidad == "ProductoVariante"
                        && x.ReferenciaId == variantAuditId
                        && x.Accion == AccionPermiso.Desactivar);
                Assert.Equal(a.Id, receipt.EmpresaId);
                Assert.Equal(userA.Id, receipt.UsuarioId);
                Assert.Equal("Exito", receipt.Resultado);
            }

            // C14-02.M06.06: exercise the REAL Oracle EF/MySQL transaction of
            // ProductosController.Delete when DeleteAsync returns false AFTER a
            // persisted variant change. The injected variant-service boundary
            // performs a physical SQL write; this is not a full production JWT
            // HTTP DELETE or a certification of the commercial variant service.
            await using (var productDeleteDb = new AppDbContext(options,
                tenantHttpContextAccessor: auditAccessor))
            {
                var product = new Mock<IProductoService>(MockBehavior.Strict);
                var variants = new Mock<IProductoVarianteService>(MockBehavior.Strict);
                // The productive DELETE locks the tenant-owned product before EF reads.
                product.Setup(x => x.LockForDeleteAsync(auditedProductId))
                    .ReturnsAsync(true);
                product.Setup(x => x.GetByIdAsync(auditedProductId))
                    .ReturnsAsync(new ProductoDto { Id = auditedProductId });
                product.Setup(x => x.DeleteAsync(auditedProductId))
                    .ReturnsAsync(false);
                variants.Setup(x => x.EliminarTecnicaConProductoAsync(auditedProductId))
                    .Returns(async () =>
                    {
                        var row = await productDeleteDb.ProductoVariantes
                            .SingleAsync(v => v.Id == variantAuditId);
                        Assert.False(row.Eliminado);
                        row.Eliminado = true;
                        await productDeleteDb.SaveChangesAsync();
                        Assert.True(row.Eliminado);
                    });
                var controller = new ProductosController(
                    product.Object, variants.Object, new UnitOfWork(productDeleteDb),
                    Mock.Of<IImageStorageService>(), Mock.Of<IAuditoriaService>());
                var failure = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => controller.Delete(auditedProductId));
                Assert.Equal("PRODUCT_DELETE_ATOMICITY_ROLLBACK_REQUIRED", failure.Message);
                product.Verify(x => x.LockForDeleteAsync(auditedProductId), Times.Once);
                product.Verify(x => x.GetByIdAsync(auditedProductId), Times.Once);
                product.Verify(x => x.DeleteAsync(auditedProductId), Times.Once);
                variants.Verify(x => x.EliminarTecnicaConProductoAsync(auditedProductId),
                    Times.Once);
            }
            // Fresh DbContext readback: no partial SQL mutation was committed.
            await using (var deleteReadback = new AppDbContext(options))
            {
                var variant = await deleteReadback.ProductoVariantes
                    .IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(v => v.Id == variantAuditId);
                Assert.False(variant.Eliminado);
                Assert.False(variant.Activo);
                Assert.Equal(a.Id, variant.EmpresaId);
                var own = await deleteReadback.Productos.IgnoreQueryFilters()
                    .AsNoTracking().SingleAsync(p => p.Id == auditedProductId);
                Assert.Equal(a.Id, own.EmpresaId);
                Assert.False(own.Eliminado);
                var foreign = await deleteReadback.Productos.IgnoreQueryFilters()
                    .AsNoTracking().SingleAsync(p => p.Id == productBId);
                Assert.Equal(b.Id, foreign.EmpresaId);
                Assert.False(foreign.Eliminado);
            }

            // Product and RBAC services/repositories are real. Mocked dependencies
            // belong only to other (uninvoked) CRUD methods of the controller.
            // The multiempresa quarantine is OFF by default. This in-memory
            // test-only pilot flag is deliberately never deployed to environments.
            var pilotConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MultiEmpresa:PrivateReadPilotEnabled"] = "false"
                })
                .Build();
            using var host = await new HostBuilder()
                .ConfigureLogging(log => log.AddConsole())
                .ConfigureWebHost(web => web.UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddDbContext<AppDbContext>(o =>
                            o.UseMySQL(isolated, mysql => mysql.MigrationsAssembly(
                                "Solqaryn.Infrastructure.Migrations")));
                        services.AddHttpContextAccessor();
                        // Reflection-based UseMiddleware resolves InvokeAsync
                        // parameters from DI even for a path not using public store.
                        // These inert public-host collaborators are not catalog
                        // authorization bypasses and are never called by GET /productos.
                        services.AddSingleton(Mock.Of<IPublicStoreHostTenantResolver>());
                        services.AddSingleton(Mock.Of<IPublicStoreProofReplayStore>());
                        services.AddSingleton<IConfiguration>(pilotConfig);
                        services.AddScoped<ICurrentUserService, CurrentUserService>();
                        services.AddScoped<IUsuarioScopeService, UsuarioScopeService>();
                        services.AddScoped<IRolPermisoRepository, RolPermisoRepository>();
                        services.AddScoped<IRolRepository, RolRepository>();
                        services.AddScoped<IPermisoRepository, PermisoRepository>();
                        services.AddScoped<IPermisoService, PermisoService>();
                        services.AddScoped<IProductoRepository, ProductoRepository>();
                        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
                        services.AddScoped<ICategoriaService, CategoriaService>();
                        services.AddScoped<IImpuestoRepository, ImpuestoRepository>();
                        services.AddScoped<IImpuestoService, ImpuestoService>();
                        services.AddScoped<IProductoService, ProductoService>();
                        // Real technical variant and durable audit share the EF transaction.
                        services.AddScoped<IProductoVarianteRepository, ProductoVarianteRepository>();
                        services.AddScoped<ICatalogoProductoRepository, CatalogoProductoRepository>();
                        services.AddScoped<ICatalogoProductoService, CatalogoProductoService>();
                        services.AddScoped<IProductoVarianteService, ProductoVarianteService>();
                        services.AddScoped<IUnitOfWork, UnitOfWork>();
                        services.AddScoped<IAuditoriaRepository, AuditoriaRepository>();
                        services.AddScoped<IAuditoriaService, AuditoriaService>();
                        services.AddSingleton(Mock.Of<IImageStorageService>());
                        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(o => o.TokenValidationParameters =
                                new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidateAudience = true,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    ValidIssuer = Issuer,
                                    ValidAudience = Audience,
                                    IssuerSigningKey = Key,
                                    ClockSkew = TimeSpan.Zero
                                });
                        services.AddAuthorization();
                        services.AddControllers().AddApplicationPart(
                            typeof(ProductosController).Assembly);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseMiddleware<ExceptionHandlingMiddleware>();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseMiddleware<UnscopedCommercialTenantGuardMiddleware>();
                        app.UseEndpoints(routes => routes.MapControllers());
                    }))
                .StartAsync();
            using var client = host.GetTestClient();

            using (var anonymous = await client.GetAsync("/productos"))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

            var jwtA = Sign(userA.Id, roleA.Id);
            var segments = jwtA.Split('.');
            segments[2] = (segments[2][0] == 'A' ? 'B' : 'A') + segments[2][1..];
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", string.Join('.', segments));
            using (var forged = await client.GetAsync("/productos"))
                Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);

            // Real negative JWT boundary: a signature from the local test issuer
            // must not authenticate when issuer, audience or lifetime is wrong.
            // Every request includes the valid A selector so a 401 is attributable
            // to JwtBearer validation, not the downstream tenant quarantine.
            async Task AssertInvalidSignedJwtRejectedAsync(string invalidJwt)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/productos");
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", invalidJwt);
                request.Headers.TryAddWithoutValidation(
                    "X-Empresa-Id", a.Id.ToString());
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                var body = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("C0701RealProductoA", body, StringComparison.Ordinal);
                Assert.DoesNotContain("C0701RealProductoB", body, StringComparison.Ordinal);
            }
            await AssertInvalidSignedJwtRejectedAsync(Sign(
                userA.Id, roleA.Id, issuer: "c0701-untrusted-issuer"));
            await AssertInvalidSignedJwtRejectedAsync(Sign(
                userA.Id, roleA.Id, audience: "c0701-other-audience"));
            await AssertInvalidSignedJwtRejectedAsync(Sign(
                userA.Id, roleA.Id,
                notBefore: DateTime.UtcNow.AddMinutes(-30),
                expires: DateTime.UtcNow.AddMinutes(-20)));

            // Production quarantine must reject even an authorized JWT until
            // the explicit server-side pilot flag is enabled.
            Assert.Equal(HttpStatusCode.ServiceUnavailable,
                await ReadStatusAsync(client, a.Id, jwtA));
            pilotConfig["MultiEmpresa:PrivateReadPilotEnabled"] = "true";
            Assert.Equal(HttpStatusCode.OK, await ReadStatusAsync(client, a.Id, jwtA));

            // Physical product rows in the same MySQL schema must remain isolated
            // by the real controller, RBAC filter, service and EF query filter.
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwtA);
            client.DefaultRequestHeaders.Remove("X-Empresa-Id");
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Empresa-Id", a.Id.ToString());
            using (var ownA = await client.GetAsync("/productos?page=1&pageSize=10"))
            {
                Assert.Equal(HttpStatusCode.OK, ownA.StatusCode);
                var json = await ownA.Content.ReadAsStringAsync();
                Assert.Contains("C0701RealProductoA", json, StringComparison.Ordinal);
                Assert.DoesNotContain("C0701RealProductoB", json, StringComparison.Ordinal);
            }
            // Reales y adversariales: la seleccion de empresa nunca se infiere
            // de un JWT valido cuando el selector HTTP es ambiguo o invalido.
            // Mantener el request aislado evita contaminar cabeceras del cliente.
            async Task AssertRejectedTenantSelectorAsync(params string[] selectors)
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Get, "/productos?page=1&pageSize=10");
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtA);
                request.Headers.TryAddWithoutValidation(
                    "X-Empresa-Id", selectors);
                using var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                var rejectedBody = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain("C0701RealProductoA", rejectedBody, StringComparison.Ordinal);
                Assert.DoesNotContain("C0701RealProductoB", rejectedBody, StringComparison.Ordinal);
            }
            await AssertRejectedTenantSelectorAsync(a.Id.ToString(), b.Id.ToString());
            await AssertRejectedTenantSelectorAsync("0");
            await AssertRejectedTenantSelectorAsync("invalid-tenant-id");

            // A read-only tenant pilot must never activate commercial writes.
            // These are real signed-JWT HTTP requests through production routing
            // and tenant guard, followed by independent physical MySQL readback.
            var deniedName = "C0701ReadPilotDeniedWrite" + Guid.NewGuid().ToString("N");
            using (var createForm = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Nombre"] = deniedName,
                    ["Precio"] = "120",
                    ["Costo"] = "50"
                }))
            using (var deniedCreate = await client.PostAsync("/productos", createForm))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, deniedCreate.StatusCode);

            using (var updateForm = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["Nombre"] = deniedName,
                    ["Precio"] = "120"
                }))
            using (var deniedForeignUpdate = await client.PutAsync(
                $"/productos/{productBId}", updateForm))
                Assert.Equal(HttpStatusCode.ServiceUnavailable,
                    deniedForeignUpdate.StatusCode);

            using (var deniedDelete = await client.DeleteAsync(
                $"/productos/{productAId}"))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, deniedDelete.StatusCode);

            await using (var negativeReadback = new AppDbContext(options))
            {
                var persisted = await negativeReadback.Productos
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(p => p.Id == productAId || p.Id == productBId)
                    .Select(p => new { p.Id, p.Nombre, p.Eliminado, p.Precio })
                    .ToArrayAsync();
                Assert.Equal(2, persisted.Length);
                Assert.Contains(persisted, p => p.Id == productAId &&
                    p.Nombre == "C0701RealProductoA" && !p.Eliminado && p.Precio == 100m);
                Assert.Contains(persisted, p => p.Id == productBId &&
                    p.Nombre == "C0701RealProductoB" && !p.Eliminado && p.Precio == 100m);
                Assert.Equal(0, await negativeReadback.Productos
                    .IgnoreQueryFilters()
                    .CountAsync(p => p.Nombre == deniedName));
            }


            // Positive HTTP path: pilot is enabled only in this isolated TestServer.
            // The production PrivateDraftPilotEnabled remains false and unchanged.
            pilotConfig["MultiEmpresa:PrivateDraftPilotEnabled"] = "true";
            var writtenName = "C0701HttpSignedWrite" + Guid.NewGuid().ToString("N");
            // Categoria has no EmpresaId: the verified tenant-bound Producto owns CategoriaId.
            // Select through persisted Producto/EmpresaId; never invent a category tenant column.
            var categoryAId = await db.Productos.IgnoreQueryFilters().AsNoTracking()
                .Where(p => p.Id == productAId && p.EmpresaId == a.Id)
                .Select(p => p.CategoriaId).SingleAsync()
                ?? throw new InvalidOperationException(
                    "El producto del tenant A no tiene CategoriaId persistida.");
            // Tenant B's IDOR request must contain a B-owned category.
            // An A-owned category may be rejected as malformed before the
            // controller verifies ownership of the target product in A.
            var categoryBId = await db.Productos.IgnoreQueryFilters().AsNoTracking()
                .Where(p => p.Id == productBId && p.EmpresaId == b.Id)
                .Select(p => p.CategoriaId).SingleAsync()
                ?? throw new InvalidOperationException(
                    "El producto del tenant B no tiene CategoriaId persistida.");
            Assert.NotEqual(categoryAId, categoryBId);
            using (var postA = new HttpRequestMessage(HttpMethod.Post, "/productos"))
            {
                postA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtA);
                postA.Headers.TryAddWithoutValidation("X-Empresa-Id", a.Id.ToString());
                postA.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Nombre"] = writtenName,
                    ["CategoriaId"] = categoryAId.ToString(),
                    ["Cantidad"] = "0",
                    ["Costo"] = "60",
                    ["Precio"] = "127"
                });
                using var reply = await client.SendAsync(postA);
                Assert.Equal(HttpStatusCode.Created, reply.StatusCode);
            }
            int httpProductId;
            await using (var verification = new AppDbContext(options))
            {
                var product = await verification.Productos.IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(x => x.Nombre == writtenName);
                httpProductId = product.Id;
                Assert.Equal(a.Id, product.EmpresaId);
                Assert.True(product.Activo);
                var variant = await verification.ProductoVariantes.IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(v => v.ProductoId == httpProductId && !v.Eliminado);
                Assert.Equal(a.Id, variant.EmpresaId);
                Assert.True(variant.EsTecnica);
                Assert.Equal(127m, variant.Precio);
                var audit = await verification.RegistrosAuditoria.IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(l => l.Modulo == ModuloSistema.Productos
                        && l.Accion == AccionPermiso.Crear
                        && l.ReferenciaId == httpProductId && l.Entidad == "Producto");
                Assert.Equal(a.Id, audit.EmpresaId);
                Assert.Equal(userA.Id, audit.UsuarioId);
                Assert.Equal("Exito", audit.Resultado);
            }

            // Signed JWT HTTP PUT through the *real* ProductosController,
            // Oracle EF/MySQL 8.4, technical variant and durable audit.
            // A valid signed JWT and the draft POST opt-in cannot bypass the
            // independent files/edit PUT gate. Verify fail-closed first.
            using (var deniedPutWithoutFilesPilot = new HttpRequestMessage(
                HttpMethod.Put, $"/productos/{httpProductId}"))
            {
                deniedPutWithoutFilesPilot.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtA);
                deniedPutWithoutFilesPilot.Headers.TryAddWithoutValidation(
                    "X-Empresa-Id", a.Id.ToString());
                deniedPutWithoutFilesPilot.Content = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["Nombre"] = "C0701DeniedPutWithoutSeparateOptIn",
                        ["Precio"] = "1"
                    });
                using var denied = await client.SendAsync(deniedPutWithoutFilesPilot);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, denied.StatusCode);
            }
            await using (var offReadback = new AppDbContext(options))
            {
                Assert.Equal(writtenName, await offReadback.Productos
                    .IgnoreQueryFilters().AsNoTracking()
                    .Where(p => p.Id == httpProductId)
                    .Select(p => p.Nombre).SingleAsync());
            }

            // Existing production guard authorizes PUT /productos/{id} ONLY via
            // PrivateCommercialFilesPilotEnabled, NOT PrivateDraftPilotEnabled.
            // Switch this independent flag on in isolated TestServer memory ONLY;
            // no environment, runtime guard, or real commercial setting changes.
            pilotConfig["MultiEmpresa:PrivateCommercialFilesPilotEnabled"] = "true";
            var updatedName = "C0701HttpUpdated" + Guid.NewGuid().ToString("N");
            using (var putA = new HttpRequestMessage(
                HttpMethod.Put, $"/productos/{httpProductId}"))
            {
                putA.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtA);
                putA.Headers.TryAddWithoutValidation("X-Empresa-Id", a.Id.ToString());
                putA.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Nombre"] = updatedName,
                    ["CategoriaId"] = categoryAId.ToString(),
                    ["Cantidad"] = "0",
                    ["Costo"] = "64",
                    ["Precio"] = "139"
                });
                using var reply = await client.SendAsync(putA);
                Assert.Equal(HttpStatusCode.OK, reply.StatusCode);
            }
            await using (var updatedDb = new AppDbContext(options))
            {
                var updated = await updatedDb.Productos.IgnoreQueryFilters().AsNoTracking()
                    .SingleAsync(p => p.Id == httpProductId);
                Assert.Equal(a.Id, updated.EmpresaId);
                Assert.Equal(updatedName, updated.Nombre);
                var variant = await updatedDb.ProductoVariantes.IgnoreQueryFilters()
                    .AsNoTracking().SingleAsync(v => v.ProductoId == httpProductId && !v.Eliminado);
                Assert.Equal(a.Id, variant.EmpresaId);
                Assert.True(variant.EsTecnica);
                Assert.Equal(139m, variant.Precio);
                var receipt = await updatedDb.RegistrosAuditoria.IgnoreQueryFilters()
                    .AsNoTracking().SingleAsync(x => x.Modulo == ModuloSistema.Productos
                        && x.Accion == AccionPermiso.Editar
                        && x.ReferenciaId == httpProductId && x.Entidad == "Producto");
                Assert.Equal(a.Id, receipt.EmpresaId);
                Assert.Equal(userA.Id, receipt.UsuarioId);
                Assert.Equal("Exito", receipt.Resultado);
            }

            // A valid tenant B JWT has Editar on B, not authorization over A.
            // Physical readback must demonstrate zero mutation after IDOR denial.
            using (var crossTenant = new HttpRequestMessage(
                HttpMethod.Put, $"/productos/{httpProductId}"))
            {
                crossTenant.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer", Sign(userB.Id, roleB.Id));
                crossTenant.Headers.TryAddWithoutValidation("X-Empresa-Id", b.Id.ToString());
                // The request must be model-valid. Incomplete forms yielded HTTP 400
                // before ownership was evaluated and could mask a missing IDOR guard.
                // Exercise actual tenant-B authorization against tenant-A's existing ID.
                crossTenant.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Nombre"] = "CrossTenantMutationDenied",
                    ["CategoriaId"] = categoryBId.ToString(),
                    ["Cantidad"] = "0",
                    ["Costo"] = "64",
                    ["Precio"] = "139"
                });
                using var rejected = await client.SendAsync(crossTenant);
                Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
            }
            await using (var deniedDb = new AppDbContext(options))
            {
                Assert.Equal(updatedName, await deniedDb.Productos.IgnoreQueryFilters()
                    .AsNoTracking().Where(p => p.Id == httpProductId)
                    .Select(p => p.Nombre).SingleAsync());
            }

            // Removing Editar must revoke writes despite an unexpired signed A JWT.
            var revokedEdit = await db.RolPermisos.SingleAsync(x =>
                x.RolId == roleA.Id && x.PermisoId == editPermission.Id);
            db.RolPermisos.Remove(revokedEdit);
            await db.SaveChangesAsync();
            using (var revokedPut = new HttpRequestMessage(
                HttpMethod.Put, $"/productos/{httpProductId}"))
            {
                revokedPut.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtA);
                revokedPut.Headers.TryAddWithoutValidation("X-Empresa-Id", a.Id.ToString());
                revokedPut.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Nombre"] = "RevokedMutationDenied",
                    ["Precio"] = "1"
                });
                using var rejected = await client.SendAsync(revokedPut);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, rejected.StatusCode);
            }
            await using (var revokedDb = new AppDbContext(options))
            {
                Assert.Equal(updatedName, await revokedDb.Productos.IgnoreQueryFilters()
                    .AsNoTracking().Where(p => p.Id == httpProductId)
                    .Select(p => p.Nombre).SingleAsync());
            }
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwtA);
            client.DefaultRequestHeaders.Remove("X-Empresa-Id");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Empresa-Id", a.Id.ToString());
            using (var ownRead = await client.GetAsync($"/productos/{httpProductId}"))
            {
                Assert.Equal(HttpStatusCode.OK, ownRead.StatusCode);
                Assert.Contains(updatedName, await ownRead.Content.ReadAsStringAsync(),
                    StringComparison.Ordinal);
            }
            // B is an authenticated tenant with a current membership but no Crear grant.
            var bDeniedName = "C0701DeniedBWrite" + Guid.NewGuid().ToString("N");
            using (var postB = new HttpRequestMessage(HttpMethod.Post, "/productos"))
            {
                postB.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", Sign(userB.Id, roleB.Id));
                postB.Headers.TryAddWithoutValidation("X-Empresa-Id", b.Id.ToString());
                postB.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Nombre"] = bDeniedName,
                    ["CategoriaId"] = categoryAId.ToString(),
                    ["Precio"] = "127"
                });
                using var reply = await client.SendAsync(postB);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, reply.StatusCode);
            }
            await using (var rejectedReadback = new AppDbContext(options))
            {
                Assert.Equal(0, await rejectedReadback.Productos.IgnoreQueryFilters()
                    .CountAsync(x => x.Nombre == bDeniedName));
            }
            // A formerly authorized actor keeps an unexpired signed JWT,
            // but revoking Crear in real Oracle EF must invalidate POST now.
            // TestServer pilots remain test-only and the MySQL readback must
            // prove no product or durable audit receipt was committed.
            var createGrantToRevoke = await db.RolPermisos.SingleAsync(x =>
                x.RolId == roleA.Id && x.PermisoId == createPermission.Id);
            db.RolPermisos.Remove(createGrantToRevoke);
            await db.SaveChangesAsync();
            var rejectedCreateName = "C0701RevokedCrear" + Guid.NewGuid().ToString("N");
            var auditCountBeforeRevokedPost = await db.RegistrosAuditoria
                .IgnoreQueryFilters().CountAsync(x =>
                    x.EmpresaId == a.Id && x.UsuarioId == userA.Id
                    && x.Modulo == ModuloSistema.Productos
                    && x.Accion == AccionPermiso.Crear);
            using (var revokedCreate = new HttpRequestMessage(HttpMethod.Post, "/productos"))
            {
                revokedCreate.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtA);
                revokedCreate.Headers.TryAddWithoutValidation(
                    "X-Empresa-Id", a.Id.ToString());
                revokedCreate.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Nombre"] = rejectedCreateName,
                    ["CategoriaId"] = categoryAId.ToString(),
                    ["Cantidad"] = "0",
                    ["Costo"] = "60",
                    ["Precio"] = "128"
                });
                using var rejected = await client.SendAsync(revokedCreate);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, rejected.StatusCode);
            }
            await using (var revokedCreateDb = new AppDbContext(options))
            {
                Assert.Equal(0, await revokedCreateDb.Productos.IgnoreQueryFilters()
                    .CountAsync(p => p.Nombre == rejectedCreateName));
                Assert.Equal(auditCountBeforeRevokedPost,
                    await revokedCreateDb.RegistrosAuditoria.IgnoreQueryFilters()
                        .CountAsync(x => x.EmpresaId == a.Id
                            && x.UsuarioId == userA.Id
                            && x.Modulo == ModuloSistema.Productos
                            && x.Accion == AccionPermiso.Crear));
                Assert.Equal(updatedName,
                    await revokedCreateDb.Productos.IgnoreQueryFilters()
                        .Where(p => p.Id == httpProductId)
                        .Select(p => p.Nombre).SingleAsync());
            }

            pilotConfig["MultiEmpresa:PrivateCommercialFilesPilotEnabled"] = "false";
            pilotConfig["MultiEmpresa:PrivateDraftPilotEnabled"] = "false";
            using (var postDefaultOff = new HttpRequestMessage(HttpMethod.Post, "/productos"))
            {
                postDefaultOff.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwtA);
                postDefaultOff.Headers.TryAddWithoutValidation("X-Empresa-Id", a.Id.ToString());
                postDefaultOff.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["Nombre"] = "C0701DefaultOff" + Guid.NewGuid().ToString("N"),
                    ["Precio"] = "127"
                });
                using var reply = await client.SendAsync(postDefaultOff);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, reply.StatusCode);
            }

            using (var foreignA = await client.GetAsync($"/productos/{productBId}"))
                Assert.Equal(HttpStatusCode.NotFound, foreignA.StatusCode);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, await ReadStatusAsync(client, b.Id, jwtA));
            var jwtB = Sign(userB.Id, roleB.Id);
            Assert.Equal(HttpStatusCode.OK, await ReadStatusAsync(client, b.Id, jwtB));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", jwtB);
            client.DefaultRequestHeaders.Remove("X-Empresa-Id");
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "X-Empresa-Id", b.Id.ToString());
            using (var ownB = await client.GetAsync("/productos?page=1&pageSize=10"))
            {
                Assert.Equal(HttpStatusCode.OK, ownB.StatusCode);
                var json = await ownB.Content.ReadAsStringAsync();
                Assert.Contains("C0701RealProductoB", json, StringComparison.Ordinal);
                Assert.DoesNotContain("C0701RealProductoA", json, StringComparison.Ordinal);
            }
            using (var foreignB = await client.GetAsync($"/productos/{productAId}"))
                Assert.Equal(HttpStatusCode.NotFound, foreignB.StatusCode);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, await ReadStatusAsync(client, a.Id, jwtB));

            // V30: fiscal GET now has a verified tenant/RBAC pilot (covered by
            // positive and negative physical MySQL assertions below); do not
            // repeat the obsolete blanket-503 expectation. Fiscal writes stay
            // quarantined and must remain unchanged in the snapshot below.

            // V29 R2: Impuesto already has physical EF shadow EmpresaId;
            // the positive fiscal HTTP/write contract remains uncertified.
            // Exercise every real mutator without granting any fiscal pilot.
            // A row-count-only check can miss an UPDATE or logical DELETE on
            // an existing legacy fiscal row. Seed a real sentinel in the
            // disposable Oracle EF/MySQL 8.4 schema, then compare its content.
            var fiscalSentinelCode = "C0701S_" + Guid.NewGuid().ToString("N");
            int fiscalSentinelId;
            await using (var fiscalSeed = new AppDbContext(options))
            {
                var fiscalSentinel = new Impuesto
                {
                    Nombre = "C0701 fiscal quarantine sentinel",
                    Codigo = fiscalSentinelCode,
                    Tasa = 15m,
                    Activo = true,
                    Eliminado = false
                };
                fiscalSeed.Set<Impuesto>().Add(fiscalSentinel);
                await fiscalSeed.SaveChangesAsync();
                fiscalSentinelId = fiscalSentinel.Id;
                Assert.True(fiscalSentinelId > 0);
            }
            List<string> fiscalSnapshotBefore;
            int fiscalAuditCountBefore;
            await using (var fiscalBefore = new AppDbContext(options))
            {
                fiscalSnapshotBefore = await fiscalBefore.Set<Impuesto>()
                    .IgnoreQueryFilters().AsNoTracking()
                    .OrderBy(i => i.Id)
                    .Select(i => i.Id + "|" + i.Codigo + "|" + i.Nombre + "|" +
                                 i.Tasa + "|" + i.Activo + "|" + i.Eliminado)
                    .ToListAsync();
                fiscalAuditCountBefore = await fiscalBefore.RegistrosAuditoria
                    .IgnoreQueryFilters().AsNoTracking()
                    .CountAsync(x => x.Modulo == ModuloSistema.Impuestos);
            }
            Assert.Contains(fiscalSnapshotBefore,
                row => row.Contains(fiscalSentinelCode, StringComparison.Ordinal));

            // Untrusted tenant selector may be spoofed, but must not open
            // any fiscal mutator or produce a durable change or audit record.
            foreach (var (signedJwt, tenantId) in new[]
            {
                (jwtA, a.Id), (jwtB, b.Id),
                (jwtA, b.Id), (jwtB, a.Id)
            })
            {
                // Exercise the persisted sentinel ID, not a guessed ID=1.
                // Valid payloads detect gate bypass despite model validation.
                const string validFiscalJson =
                    "{\"nombre\":\"C0701 Fiscal Guard\",\"codigo\":\"C0701NEW\"," +
                    "\"tipo\":\"Porcentaje\",\"tasa\":15," +
                    "\"operaciones\":[\"Venta\"]}";
                foreach (var (method, path) in new[]
                {
                    (HttpMethod.Post, "/impuestos"),
                    (HttpMethod.Put, $"/impuestos/{fiscalSentinelId}"),
                    (HttpMethod.Patch, $"/impuestos/{fiscalSentinelId}/activar"),
                    (HttpMethod.Patch, $"/impuestos/{fiscalSentinelId}/desactivar"),
                    (HttpMethod.Delete, $"/impuestos/{fiscalSentinelId}"),
                    (HttpMethod.Delete, $"/impuestos/{fiscalSentinelId}/permanente"),
                    (HttpMethod.Post, $"/impuestos/{fiscalSentinelId}/duplicar")
                })
                {
                    using var request = new HttpRequestMessage(method, path);
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", signedJwt);
                    request.Headers.TryAddWithoutValidation(
                        "X-Empresa-Id", tenantId.ToString());
                    if (method == HttpMethod.Post || method == HttpMethod.Put)
                    {
                        var json = path.EndsWith("/duplicar", StringComparison.Ordinal)
                            ? "{\"nuevoNombre\":\"C0701 Duplicate\",\"nuevoCodigo\":\"C0701DUP\"}"
                            : validFiscalJson;
                        request.Content = new StringContent(
                            json, Encoding.UTF8, "application/json");
                    }
                    using var response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                    Assert.Contains("TENANT_DATA_ISOLATION_REQUIRED",
                        await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
                }
            }
            await using (var fiscalAfter = new AppDbContext(options))
            {
                var fiscalSnapshotAfter = await fiscalAfter.Set<Impuesto>()
                    .IgnoreQueryFilters().AsNoTracking()
                    .OrderBy(i => i.Id)
                    .Select(i => i.Id + "|" + i.Codigo + "|" + i.Nombre + "|" +
                                 i.Tasa + "|" + i.Activo + "|" + i.Eliminado)
                    .ToListAsync();
                Assert.Equal(fiscalSnapshotBefore, fiscalSnapshotAfter);
                Assert.Equal(fiscalAuditCountBefore,
                    await fiscalAfter.RegistrosAuditoria.IgnoreQueryFilters()
                        .AsNoTracking().CountAsync(x => x.Modulo == ModuloSistema.Impuestos));
            }

            // V29 R2: Categoria has EF shadow EmpresaId persisted by Oracle
            // migrations. First prove default-off HTTP quarantine and no writes.
            pilotConfig["MultiEmpresa:PrivateReadPilotEnabled"] = "false";
            var categoryRejectedName = "C0701CategoryDenied" + Guid.NewGuid().ToString("N");
            var categorySnapshotBefore = await db.Categorias.IgnoreQueryFilters()
                .AsNoTracking().OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.Nombre, c.Descripcion, c.Activa, c.Eliminada })
                .ToArrayAsync();
            Assert.Contains(categorySnapshotBefore,
                c => c.Nombre == "Category C0701RealProductoA");
            Assert.Contains(categorySnapshotBefore,
                c => c.Nombre == "Category C0701RealProductoB");
            var categoryAuditBefore = await db.RegistrosAuditoria.IgnoreQueryFilters()
                .CountAsync(x => x.Modulo == ModuloSistema.Categorias);
            foreach (var (signedJwt, selectedTenant) in new[]
            {
                (jwtA, a.Id), (jwtB, b.Id), (jwtA, b.Id), (jwtB, a.Id)
            })
            {
                foreach (var (method, path) in new[]
                {
                    (HttpMethod.Get, "/categorias"),
                    (HttpMethod.Get, "/categorias/activas"),
                    (HttpMethod.Get, "/categorias/1"),
                    (HttpMethod.Post, "/categorias"),
                    (HttpMethod.Put, "/categorias/1"),
                    (HttpMethod.Patch, "/categorias/1/activar"),
                    (HttpMethod.Patch, "/categorias/1/desactivar"),
                    (HttpMethod.Delete, "/categorias/1")
                })
                {
                    using var request = new HttpRequestMessage(method, path);
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", signedJwt);
                    request.Headers.TryAddWithoutValidation(
                        "X-Empresa-Id", selectedTenant.ToString());
                    if (method == HttpMethod.Post || method == HttpMethod.Put)
                        request.Content = new StringContent(
                            "{\"nombre\":\"" + categoryRejectedName + "\"}",
                            Encoding.UTF8, "application/json");
                    using var response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                    var deniedBody = await response.Content.ReadAsStringAsync();
                    Assert.Contains("TENANT_DATA_ISOLATION_REQUIRED",
                        deniedBody, StringComparison.Ordinal);
                    Assert.DoesNotContain("Category C0701RealProductoA",
                        deniedBody, StringComparison.Ordinal);
                    Assert.DoesNotContain("Category C0701RealProductoB",
                        deniedBody, StringComparison.Ordinal);
                }
            }
            await using (var categoryReadback = new AppDbContext(options))
            {
                var categorySnapshotAfter = await categoryReadback.Categorias
                    .IgnoreQueryFilters().AsNoTracking().OrderBy(c => c.Id)
                    .Select(c => new { c.Id, c.Nombre, c.Descripcion, c.Activa, c.Eliminada })
                    .ToArrayAsync();
                Assert.Equal(categorySnapshotBefore, categorySnapshotAfter);
                Assert.DoesNotContain(categorySnapshotAfter,
                    c => c.Nombre == categoryRejectedName);
                Assert.Equal(categoryAuditBefore,
                    await categoryReadback.RegistrosAuditoria.IgnoreQueryFilters()
                        .AsNoTracking().CountAsync(x => x.Modulo == ModuloSistema.Categorias));
            }

            // V29 R2: positive authenticated GET through real CategoriesController,
            // CategoriaService, EF shadow ownership and MySQL 8.4. The production
            // default remains OFF; no category mutation route is enabled.
            var pilotCategoryAId = categorySnapshotBefore.Single(
                c => c.Nombre == "Category C0701RealProductoA").Id;
            var pilotCategoryBId = categorySnapshotBefore.Single(
                c => c.Nombre == "Category C0701RealProductoB").Id;
            await using (var categoryOwnersSql = new AppDbContext(options))
            {
                var owners = await categoryOwnersSql.Categorias
                    .IgnoreQueryFilters().AsNoTracking()
                    .Where(c => c.Id == pilotCategoryAId || c.Id == pilotCategoryBId)
                    .Select(c => new
                    {
                        c.Id,
                        EmpresaId = EF.Property<int?>(c, "EmpresaId")
                    }).ToListAsync();
                Assert.Equal(a.Id, owners.Single(c => c.Id == pilotCategoryAId).EmpresaId);
                Assert.Equal(b.Id, owners.Single(c => c.Id == pilotCategoryBId).EmpresaId);
            }

            pilotConfig["MultiEmpresa:PrivateReadPilotEnabled"] = "true";
            async Task<HttpResponseMessage> CategoryRequestAsync(
                string jwt, int selectedTenant, string path)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
                request.Headers.TryAddWithoutValidation(
                    "X-Empresa-Id", selectedTenant.ToString());
                return await client.SendAsync(request);
            }

            foreach (var (jwt, tenantId, ownName, foreignName, ownId, foreignId) in new[]
            {
                (jwtA, a.Id, "Category C0701RealProductoA",
                    "Category C0701RealProductoB", pilotCategoryAId, pilotCategoryBId),
                (jwtB, b.Id, "Category C0701RealProductoB",
                    "Category C0701RealProductoA", pilotCategoryBId, pilotCategoryAId)
            })
            {
                foreach (var path in new[] {
                    "/categorias", "/categorias/activas", $"/categorias/{ownId}" })
                {
                    using var positive = await CategoryRequestAsync(jwt, tenantId, path);
                    Assert.Equal(HttpStatusCode.OK, positive.StatusCode);
                    var body = await positive.Content.ReadAsStringAsync();
                    Assert.Contains(ownName, body, StringComparison.Ordinal);
                    Assert.DoesNotContain(foreignName, body, StringComparison.Ordinal);
                }
                using var foreignCategory =
                    await CategoryRequestAsync(jwt, tenantId, $"/categorias/{foreignId}");
                Assert.Equal(HttpStatusCode.NotFound, foreignCategory.StatusCode);
                Assert.DoesNotContain(foreignName,
                    await foreignCategory.Content.ReadAsStringAsync(),
                    StringComparison.Ordinal);
            }

            foreach (var (jwt, selector) in new[] { (jwtA, b.Id), (jwtB, a.Id) })
            {
                using var spoof = await CategoryRequestAsync(jwt, selector, "/categorias");
                Assert.Equal(HttpStatusCode.ServiceUnavailable, spoof.StatusCode);
                Assert.Contains("TENANT_DATA_ISOLATION_REQUIRED",
                    await spoof.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            }

            // Revoke only Categories.Ver for A; B remains independently authorized.
            var revokedCategoryGrantA = await db.RolPermisos
                .SingleAsync(x => x.RolId == roleA.Id &&
                    x.PermisoId == categoryReadPermission.Id);
            db.RolPermisos.Remove(revokedCategoryGrantA);
            await db.SaveChangesAsync();
            using (var deniedA = await CategoryRequestAsync(jwtA, a.Id, "/categorias"))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, deniedA.StatusCode);
            using (var stillB = await CategoryRequestAsync(jwtB, b.Id, "/categorias"))
                Assert.Equal(HttpStatusCode.OK, stillB.StatusCode);

            // Pilot OFF remains fail-closed even for B's valid JWT and RBAC.
            pilotConfig["MultiEmpresa:PrivateReadPilotEnabled"] = "false";
            using (var defaultOff = await CategoryRequestAsync(jwtB, b.Id, "/categorias"))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, defaultOff.StatusCode);
            pilotConfig["MultiEmpresa:PrivateReadPilotEnabled"] = "true";

            // V30: real ImpuestosController -> service -> MySQL ownership A/B.
            await using (var taxPhysical = new AppDbContext(options))
            {
                var owners = await taxPhysical.Impuestos.IgnoreQueryFilters()
                    .AsNoTracking().Where(t => t.Id == taxAId || t.Id == taxBId)
                    .Select(t => new { t.Id, EmpresaId = EF.Property<int?>(t, "EmpresaId") })
                    .ToArrayAsync();
                Assert.Equal(a.Id, owners.Single(t => t.Id == taxAId).EmpresaId);
                Assert.Equal(b.Id, owners.Single(t => t.Id == taxBId).EmpresaId);
            }

            foreach (var (jwt, tenantId, ownId, otherId, ownCode, otherCode) in new[]
            {
                (jwtA, a.Id, taxAId, taxBId, "C0701TAXA", "C0701TAXB"),
                (jwtB, b.Id, taxBId, taxAId, "C0701TAXB", "C0701TAXA")
            })
            {
                foreach (var path in new[] { "/impuestos", $"/impuestos/{ownId}" })
                {
                    using var positive = await CategoryRequestAsync(jwt, tenantId, path);
                    Assert.Equal(HttpStatusCode.OK, positive.StatusCode);
                    var body = await positive.Content.ReadAsStringAsync();
                    Assert.Contains(ownCode, body, StringComparison.Ordinal);
                    Assert.DoesNotContain(otherCode, body, StringComparison.Ordinal);
                }
                // Positive active-only list is private to each verified tenant.
                using (var activeTax = await CategoryRequestAsync(jwt, tenantId, "/impuestos/activos"))
                {
                    Assert.Equal(HttpStatusCode.OK, activeTax.StatusCode);
                    var activeJson = await activeTax.Content.ReadAsStringAsync();
                    Assert.Contains(ownCode, activeJson, StringComparison.Ordinal);
                    Assert.DoesNotContain(otherCode, activeJson, StringComparison.Ordinal);
                    Assert.DoesNotContain("C0701TAXAINACTIVE", activeJson, StringComparison.Ordinal);
                }
                using var foreign = await CategoryRequestAsync(
                    jwt, tenantId, $"/impuestos/{otherId}");
                Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
                Assert.DoesNotContain(otherCode,
                    await foreign.Content.ReadAsStringAsync(), StringComparison.Ordinal);
            }

            using (var spoofedTax = await CategoryRequestAsync(jwtA, b.Id, "/impuestos"))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, spoofedTax.StatusCode);
            using (var spoofedActive = await CategoryRequestAsync(jwtA, b.Id, "/impuestos/activos"))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, spoofedActive.StatusCode);
            var revokedTaxGrantA = await db.RolPermisos.SingleAsync(x =>
                x.RolId == roleA.Id && x.PermisoId == taxReadPermission.Id);
            db.RolPermisos.Remove(revokedTaxGrantA);
            await db.SaveChangesAsync();
            using (var revokedTax = await CategoryRequestAsync(jwtA, a.Id, "/impuestos"))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, revokedTax.StatusCode);
            using (var revokedActive = await CategoryRequestAsync(jwtA, a.Id, "/impuestos/activos"))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, revokedActive.StatusCode);
            using (var stillTaxB = await CategoryRequestAsync(jwtB, b.Id, "/impuestos"))
                Assert.Equal(HttpStatusCode.OK, stillTaxB.StatusCode);
            pilotConfig["MultiEmpresa:PrivateReadPilotEnabled"] = "false";
            using (var offTax = await CategoryRequestAsync(jwtB, b.Id, "/impuestos"))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, offTax.StatusCode);
            using (var offActive = await CategoryRequestAsync(jwtB, b.Id, "/impuestos/activos"))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, offActive.StatusCode);
            pilotConfig["MultiEmpresa:PrivateReadPilotEnabled"] = "true";

            // V30: real JWT, independent SQL fiscal action grants, transaction
            // durability and rollback on an isolated Oracle EF/MySQL 8.4 server.
            async Task<HttpResponseMessage> FiscalStateAsync(
                string jwt, int selector, int taxId, bool activate)
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"/impuestos/{taxId}/{(activate ? "activar" : "desactivar")}");
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", jwt);
                request.Headers.TryAddWithoutValidation(
                    "X-Empresa-Id", selector.ToString());
                return await client.SendAsync(request);
            }

            pilotConfig["MultiEmpresa:PrivateFiscalStatePilotEnabled"] = "false";
            using (var offFiscal = await FiscalStateAsync(jwtA, a.Id, taxAId, false))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, offFiscal.StatusCode);
            pilotConfig["MultiEmpresa:PrivateFiscalStatePilotEnabled"] = "true";
            var fiscalAuditBefore = await db.RegistrosAuditoria.IgnoreQueryFilters()
                .AsNoTracking().CountAsync(x => x.Modulo == ModuloSistema.Impuestos);

            foreach (var (jwt, ownerId, taxId) in new[]
            {
                (jwtA, a.Id, taxAId), (jwtB, b.Id, taxBId)
            })
            {
                using (var off = await FiscalStateAsync(jwt, ownerId, taxId, false))
                    Assert.Equal(HttpStatusCode.OK, off.StatusCode);
                await using (var readback = new AppDbContext(options))
                {
                    var state = await readback.Impuestos.IgnoreQueryFilters()
                        .AsNoTracking().Where(x => x.Id == taxId)
                        .Select(x => new
                        {
                            x.Activo,
                            EmpresaId = EF.Property<int?>(x, "EmpresaId")
                        }).SingleAsync();
                    Assert.False(state.Activo);
                    Assert.Equal(ownerId, state.EmpresaId);
                }
                using (var on = await FiscalStateAsync(jwt, ownerId, taxId, true))
                    Assert.Equal(HttpStatusCode.OK, on.StatusCode);
            }
            await using (var persistedFiscal = new AppDbContext(options))
            {
                Assert.True(await persistedFiscal.Impuestos.IgnoreQueryFilters()
                    .AsNoTracking().Where(x => x.Id == taxAId).Select(x => x.Activo)
                    .SingleAsync());
                Assert.True(await persistedFiscal.Impuestos.IgnoreQueryFilters()
                    .AsNoTracking().Where(x => x.Id == taxBId).Select(x => x.Activo)
                    .SingleAsync());
                Assert.Equal(fiscalAuditBefore + 4,
                    await persistedFiscal.RegistrosAuditoria.IgnoreQueryFilters()
                        .AsNoTracking().CountAsync(x => x.Modulo == ModuloSistema.Impuestos));
            }

            // Foreign IDs, selector spoofing and revoked SQL grant cannot mutate.
            using (var foreignTax = await FiscalStateAsync(jwtA, a.Id, taxBId, false))
                Assert.NotEqual(HttpStatusCode.OK, foreignTax.StatusCode);
            using (var spoofFiscal = await FiscalStateAsync(jwtA, b.Id, taxBId, false))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, spoofFiscal.StatusCode);
            var deactivateGrant = await db.Permisos.IgnoreQueryFilters()
                .SingleAsync(x => x.Modulo == ModuloSistema.Impuestos &&
                    x.Accion == AccionPermiso.Desactivar &&
                    x.Ambito == AmbitoAutorizacion.Empresa && x.Activo && !x.Eliminado);
            var revokedFiscalGrant = await db.RolPermisos.SingleAsync(x =>
                x.RolId == roleA.Id && x.PermisoId == deactivateGrant.Id);
            db.RolPermisos.Remove(revokedFiscalGrant);
            await db.SaveChangesAsync();
            using (var revokedFiscal = await FiscalStateAsync(jwtA, a.Id, taxAId, false))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, revokedFiscal.StatusCode);

            // A failing audit sink rolls back a real SQL UPDATE, verified with
            // a fresh DbContext rather than an in-memory tracked entity.
            var failedAudit = new Mock<IAuditoriaService>();
            failedAudit.Setup(x => x.RegistrarEstrictoAsync(
                    It.IsAny<ModuloSistema>(), It.IsAny<AccionPermiso>(),
                    It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(),
                    It.IsAny<object?>(), It.IsAny<object?>(), It.IsAny<string?>(),
                    It.IsAny<string>(), It.IsAny<string?>()))
                .ThrowsAsync(new InvalidOperationException("FISCAL_AUDIT_SINK_UNAVAILABLE"));
            var fiscalContext = new DefaultHttpContext();
            fiscalContext.Items[Solqaryn.Application.Security.VerifiedCommercialTenant
                .AuthorizedEmpresaIdItemKey] = b.Id;
            var fiscalAccessor = new HttpContextAccessor { HttpContext = fiscalContext };
            await using (var rollbackFiscalDb = new AppDbContext(options,
                tenantHttpContextAccessor: fiscalAccessor))
            {
                var service = new ImpuestoService(
                    new ImpuestoRepository(rollbackFiscalDb), failedAudit.Object,
                    Mock.Of<ICurrentUserService>(), new UnitOfWork(rollbackFiscalDb));
                var failure = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => service.DesactivarAsync(taxBId));
                Assert.Equal("FISCAL_AUDIT_SINK_UNAVAILABLE", failure.Message);
            }
            // A failed durable audit also rolls back a real duplicated tax row,
            // not just an in-memory mock or the HTTP quarantine response.
            var duplicateRollbackCode = "C0701DUPR_" + Guid.NewGuid().ToString("N");
            await using (var duplicateFiscalDb = new AppDbContext(options,
                tenantHttpContextAccessor: fiscalAccessor))
            {
                var duplicateService = new ImpuestoService(
                    new ImpuestoRepository(duplicateFiscalDb), failedAudit.Object,
                    Mock.Of<ICurrentUserService>(), new UnitOfWork(duplicateFiscalDb));
                var duplicationFailure = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => duplicateService.DuplicarAsync(
                        taxBId, "C0701 duplicate rollback", duplicateRollbackCode));
                Assert.Equal("FISCAL_AUDIT_SINK_UNAVAILABLE", duplicationFailure.Message);
            }
            // Distinct mutation contracts: each must reach the failing audit sink,
            // and none may survive the actual database transaction rollback.
            async Task AssertFiscalRollbackAsync(Func<ImpuestoService, Task> mutate)
            {
                await using var rollbackDb = new AppDbContext(options,
                    tenantHttpContextAccessor: fiscalAccessor);
                var service = new ImpuestoService(new ImpuestoRepository(rollbackDb),
                    failedAudit.Object, Mock.Of<ICurrentUserService>(), new UnitOfWork(rollbackDb));
                var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => mutate(service));
                Assert.Equal("FISCAL_AUDIT_SINK_UNAVAILABLE", failure.Message);
            }

            var createRollbackCode = "C0701CR_" + Guid.NewGuid().ToString("N");
            await AssertFiscalRollbackAsync(service => service.CreateAsync(new GuardarImpuestoDto
            {
                Codigo = createRollbackCode, Nombre = "C0701 create rollback",
                Tipo = "Porcentaje", Tasa = 12m, Operaciones = new() { "Venta" }
            }));
            await AssertFiscalRollbackAsync(service => service.UpdateAsync(taxBId, new GuardarImpuestoDto
            {
                Codigo = "C0701TAXB", Nombre = "C0701 update rollback",
                Tipo = "Porcentaje", Tasa = 19m, Operaciones = new() { "Venta" }
            }));
            await AssertFiscalRollbackAsync(service => service.EliminarLogicoAsync(taxBId));
            await AssertFiscalRollbackAsync(service => service.EliminarPermanenteAsync(taxBId));

            await using (var fiscalFresh = new AppDbContext(options))
            {
                Assert.False(await fiscalFresh.Impuestos.IgnoreQueryFilters()
                    .AsNoTracking().AnyAsync(x => x.Codigo == duplicateRollbackCode));
                Assert.False(await fiscalFresh.Impuestos.IgnoreQueryFilters()
                    .AsNoTracking().AnyAsync(x => x.Codigo == createRollbackCode));
                var restoredTax = await fiscalFresh.Impuestos.IgnoreQueryFilters()
                    .AsNoTracking().Where(x => x.Id == taxBId)
                    .Select(x => new { x.Codigo, x.Nombre, x.Tasa, x.Eliminado,
                        EmpresaId = EF.Property<int?>(x, "EmpresaId") }).SingleAsync();
                Assert.Equal("C0701TAXB", restoredTax.Codigo);
                Assert.Equal("C0701FiscalC0701TAXB", restoredTax.Nombre);
                Assert.Equal(15m, restoredTax.Tasa);
                Assert.False(restoredTax.Eliminado);
                Assert.Equal(b.Id, restoredTax.EmpresaId);
                Assert.True(await fiscalFresh.Impuestos.IgnoreQueryFilters()
                    .AsNoTracking().Where(x => x.Id == taxBId).Select(x => x.Activo)
                    .SingleAsync());
                Assert.Equal(fiscalAuditBefore + 4,
                    await fiscalFresh.RegistrosAuditoria.IgnoreQueryFilters()
                        .AsNoTracking().CountAsync(x => x.Modulo == ModuloSistema.Impuestos));
            }
            pilotConfig["MultiEmpresa:PrivateFiscalStatePilotEnabled"] = "false";

            // V31: exact HTTP tax creation, real JWT and Oracle EF/MySQL.
            async Task<HttpResponseMessage> CreateFiscalAsync(
                string jwt, int selector, string code, int productId, int categoryId)
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "/impuestos");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                req.Headers.Add("X-Empresa-Id", selector.ToString());
                req.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
                {
                    nombre = "C0701 HTTP fiscal " + code, codigo = code,
                    tipo = "Porcentaje", tasa = 12m, operaciones = new[] { "Venta" },
                    productoIds = new[] { productId }, categoriaIds = new[] { categoryId }
                }), Encoding.UTF8, "application/json");
                return await client.SendAsync(req);
            }

            using (var offCreate = await CreateFiscalAsync(jwtA, a.Id, "C0701HTTPOFF", productAId, pilotCategoryAId))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, offCreate.StatusCode);
            pilotConfig["MultiEmpresa:PrivateFiscalCreatePilotEnabled"] = "true";
            var fiscalCreateAuditBefore = await db.RegistrosAuditoria.IgnoreQueryFilters()
                .AsNoTracking().CountAsync(x => x.Modulo == ModuloSistema.Impuestos);
            foreach (var (jwt, owner, code, productId, categoryId) in new[]
            {
                (jwtA, a.Id, "C0701HTTPA", productAId, pilotCategoryAId),
                (jwtB, b.Id, "C0701HTTPB", productBId, pilotCategoryBId)
            })
            {
                using var created = await CreateFiscalAsync(jwt, owner, code, productId, categoryId);
                Assert.Equal(HttpStatusCode.Created, created.StatusCode);
                await using var fresh = new AppDbContext(options);
                var row = await fresh.Impuestos.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Codigo == code)
                    .Select(x => new { x.Id, EmpresaId = EF.Property<int?>(x, "EmpresaId") })
                    .SingleAsync();
                Assert.Equal(owner, row.EmpresaId);
                Assert.Equal(productId, await fresh.Set<ImpuestoProducto>().IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.ImpuestoId == row.Id && EF.Property<int?>(x, "EmpresaId") == owner)
                    .Select(x => x.ProductoId).SingleAsync());
                Assert.Equal(categoryId, await fresh.Set<ImpuestoCategoria>().IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.ImpuestoId == row.Id && EF.Property<int?>(x, "EmpresaId") == owner)
                    .Select(x => x.CategoriaId).SingleAsync());
            }
            using (var foreignProduct = await CreateFiscalAsync(jwtA, a.Id, "C0701HTTPXPROD", productBId, pilotCategoryAId))
                Assert.Equal(HttpStatusCode.BadRequest, foreignProduct.StatusCode);
            using (var foreignCategory = await CreateFiscalAsync(jwtB, b.Id, "C0701HTTPXCAT", productBId, pilotCategoryAId))
                Assert.Equal(HttpStatusCode.BadRequest, foreignCategory.StatusCode);
            using (var spoofedCreate = await CreateFiscalAsync(jwtA, b.Id, "C0701HTTPSPOOF", productBId, pilotCategoryBId))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, spoofedCreate.StatusCode);
            var fiscalCreatePermission = await db.Permisos.IgnoreQueryFilters().AsNoTracking()
                .SingleAsync(p => p.Modulo == ModuloSistema.Impuestos && p.Accion == AccionPermiso.Crear
                    && p.Ambito == AmbitoAutorizacion.Empresa && p.Activo && !p.Eliminado);
            var fiscalCreateGrantA = await db.RolPermisos.SingleAsync(x =>
                x.RolId == roleA.Id && x.PermisoId == fiscalCreatePermission.Id);
            db.RolPermisos.Remove(fiscalCreateGrantA);
            await db.SaveChangesAsync();
            using (var revokedCreate = await CreateFiscalAsync(jwtA, a.Id, "C0701HTTPREVOKE", productAId, pilotCategoryAId))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, revokedCreate.StatusCode);
            pilotConfig["MultiEmpresa:PrivateFiscalCreatePilotEnabled"] = "false";
            await using (var fiscalCreateFresh = new AppDbContext(options))
            {
                Assert.Equal(2, await fiscalCreateFresh.Impuestos.IgnoreQueryFilters().AsNoTracking()
                    .CountAsync(x => x.Codigo.StartsWith("C0701HTTP")));
                Assert.Equal(fiscalCreateAuditBefore + 2,
                    await fiscalCreateFresh.RegistrosAuditoria.IgnoreQueryFilters().AsNoTracking()
                        .CountAsync(x => x.Modulo == ModuloSistema.Impuestos));
            }

            // V31: isolated PUT pilot; positive edits and hostile references use the real controller.
            var editTaxA = await db.Impuestos.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Codigo == "C0701HTTPA");
            var editTaxB = await db.Impuestos.IgnoreQueryFilters().AsNoTracking().SingleAsync(x => x.Codigo == "C0701HTTPB");
            async Task<HttpResponseMessage> EditFiscalAsync(string jwt, int selector, int id, string code,
                int productId, int categoryId, decimal tasa)
            {
                using var req = new HttpRequestMessage(HttpMethod.Put, $"/impuestos/{id}");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                req.Headers.Add("X-Empresa-Id", selector.ToString());
                req.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new
                {
                    nombre = "C0701 edit fiscal " + code, codigo = code, tipo = "Porcentaje",
                    tasa, operaciones = new[] { "Venta" },
                    productoIds = new[] { productId }, categoriaIds = new[] { categoryId }
                }), Encoding.UTF8, "application/json");
                return await client.SendAsync(req);
            }
            using (var editOff = await EditFiscalAsync(jwtA, a.Id, editTaxA.Id, editTaxA.Codigo, productAId, pilotCategoryAId, 13m))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, editOff.StatusCode);
            pilotConfig["MultiEmpresa:PrivateFiscalEditPilotEnabled"] = "true";
            var editAuditBefore = await db.RegistrosAuditoria.IgnoreQueryFilters().AsNoTracking()
                .CountAsync(x => x.Modulo == ModuloSistema.Impuestos);
            using (var ownEditA = await EditFiscalAsync(jwtA, a.Id, editTaxA.Id, editTaxA.Codigo, productAId, pilotCategoryAId, 13m))
                Assert.Equal(HttpStatusCode.OK, ownEditA.StatusCode);
            using (var ownEditB = await EditFiscalAsync(jwtB, b.Id, editTaxB.Id, editTaxB.Codigo, productBId, pilotCategoryBId, 14m))
                Assert.Equal(HttpStatusCode.OK, ownEditB.StatusCode);
            using (var crossTarget = await EditFiscalAsync(jwtA, a.Id, editTaxB.Id, editTaxB.Codigo, productAId, pilotCategoryAId, 99m))
                Assert.Equal(HttpStatusCode.BadRequest, crossTarget.StatusCode);
            using (var crossProduct = await EditFiscalAsync(jwtA, a.Id, editTaxA.Id, editTaxA.Codigo, productBId, pilotCategoryAId, 99m))
                Assert.Equal(HttpStatusCode.BadRequest, crossProduct.StatusCode);
            using (var crossCategory = await EditFiscalAsync(jwtB, b.Id, editTaxB.Id, editTaxB.Codigo, productBId, pilotCategoryAId, 99m))
                Assert.Equal(HttpStatusCode.BadRequest, crossCategory.StatusCode);
            using (var spoofEdit = await EditFiscalAsync(jwtA, b.Id, editTaxB.Id, editTaxB.Codigo, productBId, pilotCategoryBId, 99m))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, spoofEdit.StatusCode);
            var fiscalEditPermission = await db.Permisos.IgnoreQueryFilters().AsNoTracking()
                .SingleAsync(p => p.Modulo == ModuloSistema.Impuestos && p.Accion == AccionPermiso.Editar
                    && p.Ambito == AmbitoAutorizacion.Empresa && p.Activo && !p.Eliminado);
            var fiscalEditGrantA = await db.RolPermisos.SingleAsync(x =>
                x.RolId == roleA.Id && x.PermisoId == fiscalEditPermission.Id);
            db.RolPermisos.Remove(fiscalEditGrantA);
            await db.SaveChangesAsync();
            using (var editRevoked = await EditFiscalAsync(jwtA, a.Id, editTaxA.Id, editTaxA.Codigo, productAId, pilotCategoryAId, 99m))
                Assert.Equal(HttpStatusCode.ServiceUnavailable, editRevoked.StatusCode);
            // V31: fail the real durable audit INSERT, not a mocked service.
            // Isolated MySQL schema only; the same JWT B, role and route already
            // succeeded above. The generic HTTP persistence error must not expose
            // the trigger diagnostic, and the fresh readback below must prove
            // the attempted tax update and its receipt were rolled back.
            await db.Database.ExecuteSqlRawAsync(
                "CREATE TRIGGER c0701_fiscal_audit_insert_failure BEFORE INSERT ON RegistrosAuditoria " +
                "FOR EACH ROW BEGIN IF NEW.Entidad = 'Impuesto' THEN " +
                "SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'C0701_HTTP_FISCAL_AUDIT_INSERT_FAILURE'; END IF; END");
            try
            {
                using var failedHttpEdit = await EditFiscalAsync(jwtB, b.Id, editTaxB.Id,
                    editTaxB.Codigo, productBId, pilotCategoryBId, 99m);
                Assert.Equal(HttpStatusCode.ServiceUnavailable, failedHttpEdit.StatusCode);
                var failureBody = await failedHttpEdit.Content.ReadAsStringAsync();
                Assert.Contains("No fue posible guardar", failureBody, StringComparison.Ordinal);
                Assert.DoesNotContain("C0701_HTTP_FISCAL_AUDIT_INSERT_FAILURE", failureBody, StringComparison.Ordinal);
            }
            finally
            {
                await db.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS c0701_fiscal_audit_insert_failure");
            }
            pilotConfig["MultiEmpresa:PrivateFiscalEditPilotEnabled"] = "false";
            await using (var editFresh = new AppDbContext(options))
            {
                foreach (var (taxId, owner, productId, categoryId, tasa) in new[]
                {
                    (editTaxA.Id, a.Id, productAId, pilotCategoryAId, 13m),
                    (editTaxB.Id, b.Id, productBId, pilotCategoryBId, 14m)
                })
                {
                    var actual = await editFresh.Impuestos.IgnoreQueryFilters().AsNoTracking()
                        .Where(x => x.Id == taxId)
                        .Select(x => new { x.Tasa, EmpresaId = EF.Property<int?>(x, "EmpresaId") }).SingleAsync();
                    Assert.Equal(tasa, actual.Tasa);
                    Assert.Equal(owner, actual.EmpresaId);
                    Assert.Equal(productId, await editFresh.Set<ImpuestoProducto>().IgnoreQueryFilters().AsNoTracking()
                        .Where(x => x.ImpuestoId == taxId && EF.Property<int?>(x, "EmpresaId") == owner)
                        .Select(x => x.ProductoId).SingleAsync());
                    Assert.Equal(categoryId, await editFresh.Set<ImpuestoCategoria>().IgnoreQueryFilters().AsNoTracking()
                        .Where(x => x.ImpuestoId == taxId && EF.Property<int?>(x, "EmpresaId") == owner)
                        .Select(x => x.CategoriaId).SingleAsync());
                }
                Assert.Equal(editAuditBefore + 2, await editFresh.RegistrosAuditoria.IgnoreQueryFilters().AsNoTracking()
                    .CountAsync(x => x.Modulo == ModuloSistema.Impuestos));
            }

            // V26 R2: nested ProductoVariantes endpoints must not evade the
            // multiempresa guard through a parent product ID, even with a
            // genuine JWT and an attacker-selected X-Empresa-Id header.
            // Real Oracle EF/MySQL 8.4 variant and durable-audit readback:
            // no private variant feature is activated by this negative proof.
            var nestedVariantBefore = await db.ProductoVariantes.IgnoreQueryFilters()
                .AsNoTracking().OrderBy(v => v.Id)
                .Select(v => new {
                    v.Id, v.ProductoId, v.EmpresaId, v.Sku, v.Cantidad,
                    v.Precio, v.Costo, v.Activo, v.Eliminado
                }).ToArrayAsync();
            Assert.Contains(nestedVariantBefore, v => v.Id == variantAuditId
                && v.ProductoId == auditedProductId && v.EmpresaId == a.Id
                && !v.Activo && !v.Eliminado);
            var nestedVariantAuditBefore = await db.RegistrosAuditoria
                .IgnoreQueryFilters().AsNoTracking()
                .CountAsync(x => x.Entidad == "ProductoVariante");
            var nestedVariantRequests = 0;
            foreach (var (signedJwt, selectedTenant) in new[]
            {
                (jwtA, a.Id), (jwtB, b.Id), (jwtA, b.Id), (jwtB, a.Id)
            })
            {
                foreach (var (method, route) in new[]
                {
                    (HttpMethod.Get, $"/productos/{auditedProductId}/variantes"),
                    (HttpMethod.Get, $"/productos/{auditedProductId}/variantes/{variantAuditId}"),
                    (HttpMethod.Get, $"/productos/{auditedProductId}/variantes/{variantAuditId}/imagenes"),
                    (HttpMethod.Post, $"/productos/{auditedProductId}/variantes"),
                    (HttpMethod.Put, $"/productos/{auditedProductId}/variantes/{variantAuditId}"),
                    (HttpMethod.Patch, $"/productos/{auditedProductId}/variantes/{variantAuditId}/estado"),
                    (HttpMethod.Delete, $"/productos/{auditedProductId}/variantes/{variantAuditId}"),
                    (HttpMethod.Delete, $"/productos/{auditedProductId}/variantes/{variantAuditId}/imagenes/1")
                })
                {
                    using var request = new HttpRequestMessage(method, route);
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", signedJwt);
                    request.Headers.TryAddWithoutValidation(
                        "X-Empresa-Id", selectedTenant.ToString());
                    if (method == HttpMethod.Post || method == HttpMethod.Put)
                        request.Content = new StringContent(
                            "{\"sku\":\"C0701NestedDenied\",\"cantidad\":7,\"precio\":42}",
                            Encoding.UTF8, "application/json");
                    else if (method == HttpMethod.Patch)
                        request.Content = new StringContent(
                            "{\"activo\":true}", Encoding.UTF8, "application/json");
                    using var response = await client.SendAsync(request);
                    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                    var body = await response.Content.ReadAsStringAsync();
                    Assert.Contains("TENANT_DATA_ISOLATION_REQUIRED",
                        body, StringComparison.Ordinal);
                    Assert.DoesNotContain(variantAuditSku, body, StringComparison.Ordinal);
                    nestedVariantRequests++;
                }
            }
            Assert.Equal(32, nestedVariantRequests);
            await using (var nestedVariantReadback = new AppDbContext(options))
            {
                var nestedVariantAfter = await nestedVariantReadback.ProductoVariantes
                    .IgnoreQueryFilters().AsNoTracking().OrderBy(v => v.Id)
                    .Select(v => new {
                        v.Id, v.ProductoId, v.EmpresaId, v.Sku, v.Cantidad,
                        v.Precio, v.Costo, v.Activo, v.Eliminado
                    }).ToArrayAsync();
                Assert.Equal(nestedVariantBefore, nestedVariantAfter);
                Assert.Equal(nestedVariantAuditBefore,
                    await nestedVariantReadback.RegistrosAuditoria
                        .IgnoreQueryFilters().AsNoTracking()
                        .CountAsync(x => x.Entidad == "ProductoVariante"));
            }

            var membershipA = await db.UsuarioEmpresas
                .SingleAsync(x => x.UsuarioId == userA.Id && x.EmpresaId == a.Id);
            membershipA.Desactivar();
            await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, await ReadStatusAsync(client, a.Id, jwtA));
            membershipA.Activar();
            await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.OK, await ReadStatusAsync(client, a.Id, jwtA));

            var grantA = await db.RolPermisos
                .SingleAsync(x => x.RolId == roleA.Id && x.PermisoId == permission.Id);
            db.RolPermisos.Remove(grantA);
            await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, await ReadStatusAsync(client, a.Id, jwtA));
            Assert.Equal(HttpStatusCode.OK, await ReadStatusAsync(client, b.Id, jwtB));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static Usuario NewUser(string suffix, int roleId) => new()
    {
        NombreUsuario = "c0701_m0606_" + suffix + "_" + Guid.NewGuid().ToString("N")[..9],
        NombreCompleto = "C0701 isolated actor " + suffix,
        PasswordHash = "not-a-real-password",
        RolId = roleId, Activo = true
    };

    private static async Task<HttpStatusCode> ReadStatusAsync(
        HttpClient client, int empresaId, string jwt)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", jwt);
        client.DefaultRequestHeaders.Remove("X-Empresa-Id");
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "X-Empresa-Id", empresaId.ToString());
        using var response = await client.GetAsync("/productos?page=1&pageSize=10");
        return response.StatusCode;
    }

    private static string Sign(int userId, int roleId,
        string? issuer = null, string? audience = null,
        DateTime? notBefore = null, DateTime? expires = null)
    {
        var claims = new[]
        {
            new Claim("id", userId.ToString()),
            new Claim("rolId", roleId.ToString()),
            new Claim("esAdministrador", "false")
        };
        var token = new JwtSecurityToken(
            issuer: issuer ?? Issuer, audience: audience ?? Audience, claims: claims,
            notBefore: notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            expires: expires ?? DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(
                Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
