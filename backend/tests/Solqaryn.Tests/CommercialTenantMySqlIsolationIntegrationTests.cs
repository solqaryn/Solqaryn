using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Application.Interfaces;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Dos empresas simultáneas en MySQL efímero, sin modificar instancias DEV.
/// Se ejercita el mismo modelo EF y el bloqueo FOR UPDATE de producción.
/// La cuarentena HTTP de segunda empresa permanece activa por separado.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CommercialTenantMySqlIsolationIntegrationTests
{
    [Fact]
    public async Task A_y_B_aislan_lecturas_escrituras_catalogo_y_consultas_relacionales()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL efímero de CI no está configurado.");
        var dbName = "p4_ab_" + Guid.NewGuid().ToString("N");
        var cs = new MySqlConnectionStringBuilder(raw) { Database = dbName }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(cs, mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int empresaAId, empresaBId, productoAId, productoBId, categoriaAId, categoriaBId, miembroId;
            int compraAId, compraBId, ventaAId, ventaBId;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var empresaA = new Empresa("TENANT TEST A");
                var empresaB = new Empresa("TENANT TEST B");
                setup.Set<Empresa>().AddRange(empresaA, empresaB, new Empresa("Tercer tenant existente"));
                await setup.SaveChangesAsync();
                empresaAId = empresaA.Id;
                empresaBId = empresaB.Id;
                setup.Categorias.Add(new Categoria { Nombre = "LEGACY SIN OWNER" });
                await setup.SaveChangesAsync();
            }

            // La identidad puede tener una membresía inactiva en A y otra
            // activa en B. Un administrador de A debe poder reactivar solo A.
            await using (var global = new AppDbContext(options))
            {
                var rol = new Rol
                {
                    Nombre = "P4 Vendedor A B",
                    NombreNormalizado = "P4_VENDEDOR_AB",
                    // Compartido A/B solamente como plantilla empresarial de sistema.
                    EsSistema = true,
                    Activo = true
                };
                global.Roles.Add(rol);
                await global.SaveChangesAsync();
                var usuario = new Usuario
                {
                    NombreUsuario = "p4-miembro-ab",
                    NombreCompleto = "P4 Miembro A B",
                    PasswordHash = "hash-efimero",
                    RolId = rol.Id,
                    Activo = true
                };
                global.Usuarios.Add(usuario);
                await global.SaveChangesAsync();
                miembroId = usuario.Id;
                var membresiaInactiva = new UsuarioEmpresa(miembroId, empresaAId, rol.Id);
                membresiaInactiva.Desactivar();
                global.UsuarioEmpresas.AddRange(
                    membresiaInactiva, new UsuarioEmpresa(miembroId, empresaBId, rol.Id));
                await global.SaveChangesAsync();
            }

            var httpA = new DefaultHttpContext();
            httpA.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaAId;
            var httpB = new DefaultHttpContext();
            httpB.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaBId;

            await using (var dbA = new AppDbContext(options,
                tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = httpA }))
            {
                var categoria = new Categoria { Nombre = "Catálogo A" };
                dbA.Categorias.Add(categoria);
                await dbA.SaveChangesAsync();
                categoriaAId = categoria.Id;

                var producto = new Producto
                {
                    Nombre = "Producto confidencial A",
                    Precio = 100m, Costo = 50m, CategoriaId = categoriaAId
                };
                dbA.Productos.Add(producto);
                await dbA.SaveChangesAsync();
                productoAId = producto.Id;
                Assert.Equal(empresaAId, producto.EmpresaId);

                var ventaA = new Venta { NumeroVenta = "LOCK-VENTA", CreadoPorUsuarioId = miembroId };
                var compraA = new Compra { NumeroCompra = "LOCK-COMPRA", CreadoPorUsuarioId = miembroId };
                dbA.Ventas.Add(ventaA);
                dbA.Compras.Add(compraA);
                await dbA.SaveChangesAsync();
                ventaAId = ventaA.Id;
                compraAId = compraA.Id;
                Assert.Equal(empresaAId, ventaA.EmpresaId);
                Assert.Equal(empresaAId, compraA.EmpresaId);
            }

            await using (var dbB = new AppDbContext(options,
                tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = httpB }))
            {
                var categoria = new Categoria { Nombre = "Catálogo B" };
                dbB.Categorias.Add(categoria);
                await dbB.SaveChangesAsync();
                categoriaBId = categoria.Id;

                var producto = new Producto
                {
                    Nombre = "Producto confidencial B",
                    Precio = 200m, Costo = 75m, CategoriaId = categoriaBId
                };
                dbB.Productos.Add(producto);
                await dbB.SaveChangesAsync();
                productoBId = producto.Id;
                Assert.Equal(empresaBId, producto.EmpresaId);

                // Mismo número es legal en empresas distintas; el dueño no se infiere del número.
                var ventaB = new Venta { NumeroVenta = "LOCK-VENTA", CreadoPorUsuarioId = miembroId };
                var compraB = new Compra { NumeroCompra = "LOCK-COMPRA", CreadoPorUsuarioId = miembroId };
                dbB.Ventas.Add(ventaB);
                dbB.Compras.Add(compraB);
                await dbB.SaveChangesAsync();
                ventaBId = ventaB.Id;
                compraBId = compraB.Id;

                var usuarioB = new UsuarioRepository(dbB);
                Assert.Equal(miembroId, (await usuarioB.GetByIdAsync(miembroId))?.Id);
                var activaB = Assert.Single(await usuarioB.GetEmpresasAsync(miembroId));
                Assert.Equal(empresaBId, activaB.EmpresaId);
                Assert.True(activaB.Activa);
                Assert.Null(await usuarioB.GetEmpresaAsync(miembroId, empresaAId));

                var vistaB = await dbB.Productos.AsNoTracking().Include(p => p.Categoria).ToListAsync();
                Assert.Single(vistaB);
                Assert.Equal(productoBId, vistaB[0].Id);
                Assert.Equal(categoriaBId, vistaB[0].CategoriaId);
            }

            await using (var dbA = new AppDbContext(options,
                tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = httpA }))
            {
                var usuarioA = new UsuarioRepository(dbA);
                Assert.Equal(miembroId, (await usuarioA.GetByIdAsync(miembroId))?.Id);
                var inactivaA = Assert.Single(await usuarioA.GetEmpresasAsync(miembroId));
                Assert.Equal(empresaAId, inactivaA.EmpresaId);
                Assert.False(inactivaA.Activa);
                Assert.Null(await usuarioA.GetEmpresaAsync(miembroId, empresaBId));

                var vistaA = await dbA.Productos.AsNoTracking().Include(p => p.Categoria).ToListAsync();
                Assert.Single(vistaA);
                Assert.Equal(productoAId, vistaA[0].Id);
                Assert.Equal(categoriaAId, vistaA[0].CategoriaId);
                Assert.Null(await dbA.Productos.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == productoBId));
                Assert.Equal(categoriaAId, (await dbA.Categorias.AsNoTracking().SingleAsync()).Id);

                var inclusoConBorrados = await TenantQueryBypassGuard.RespectCurrentTenant(
                    dbA.Categorias.IgnoreQueryFilters(), dbA).AsNoTracking().ToListAsync();
                Assert.Single(inclusoConBorrados);
                Assert.Equal(categoriaAId, inclusoConBorrados[0].Id);

                await using (var transaction = await dbA.Database.BeginTransactionAsync())
                {
                    var bloqueado = await new ProductoRepository(dbA)
                        .GetByIdForUpdateAsync(productoBId);
                    Assert.Null(bloqueado);
                    await transaction.RollbackAsync();
                }

                dbA.Productos.Update(new Producto
                {
                    Id = productoBId,
                    EmpresaId = empresaBId,
                    Nombre = "Intento de modificación B desde A"
                });
                await Assert.ThrowsAsync<ForbiddenAccessException>(
                    () => dbA.SaveChangesAsync());

                foreach (var sql in new[]
                {
                    dbA.Ventas.AsNoTracking().ToQueryString(),
                    dbA.Compras.AsNoTracking().ToQueryString(),
                    dbA.Facturas.AsNoTracking().ToQueryString(),
                    dbA.Set<ExistenciaVariante>().AsNoTracking().ToQueryString(),
                    dbA.MovimientosInventario.AsNoTracking().ToQueryString(),
                    dbA.EnlacesPublicosFactura.AsNoTracking().ToQueryString()
                })
                {
                    Assert.Contains("EmpresaId", sql, StringComparison.OrdinalIgnoreCase);
                    Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
                }
            }


            // Lectura SQL FOR UPDATE no compone autorizaciones de roles legacy:
            // el predicado EmpresaId debe estar dentro del lock de base real.
            var scope = new Mock<IUsuarioScopeService>();
            scope.Setup(x => x.ObtenerActualAsync(
                    empresaAId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UsuarioTenantScopeActual(
                    miembroId, empresaAId, 1, "Administrador A", true));
            scope.Setup(x => x.ObtenerActualAsync(
                    empresaBId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UsuarioTenantScopeActual(
                    miembroId, empresaBId, 1, "Administrador B", true));
            var currentUser = new Mock<ICurrentUserService>().Object;

            await using (var dbA = new AppDbContext(options,
                tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = httpA }))
            {
                var compras = new CompraRepository(dbA, currentUser, scope.Object);
                var ventas = new VentaRepository(dbA, currentUser, scope.Object);
                Assert.Equal(1, await compras.ContarTodasAsync());

                await using var transaction = await dbA.Database.BeginTransactionAsync();
                Assert.Null(await compras.GetByIdForUpdateAsync(compraBId));
                Assert.Null(await ventas.GetByIdForUpdateAsync(ventaBId));
                Assert.Equal(compraAId, (await compras.GetByIdForUpdateAsync(compraAId))?.Id);
                Assert.Equal(ventaAId, (await ventas.GetByIdForUpdateAsync(ventaAId))?.Id);
                await transaction.RollbackAsync();
            }
            await using (var dbB = new AppDbContext(options,
                tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = httpB }))
            {
                var compras = new CompraRepository(dbB, currentUser, scope.Object);
                var ventas = new VentaRepository(dbB, currentUser, scope.Object);
                Assert.Equal(1, await compras.ContarTodasAsync());

                await using var transaction = await dbB.Database.BeginTransactionAsync();
                Assert.Null(await compras.GetByIdForUpdateAsync(compraAId));
                Assert.Null(await ventas.GetByIdForUpdateAsync(ventaAId));
                Assert.Equal(compraBId, (await compras.GetByIdForUpdateAsync(compraBId))?.Id);
                Assert.Equal(ventaBId, (await ventas.GetByIdForUpdateAsync(ventaBId))?.Id);
                await transaction.RollbackAsync();
            }
            // Un admin GLOBAL legacy sin marca RBAC, o una tienda pública, no
            // puede forzar locks ni obtener conteos, incluso con membresía mockeada.
            var publicHttp = new DefaultHttpContext();
            publicHttp.Request.Path = "/tienda/productos";
            publicHttp.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = empresaBId;
            await using (var dbPublic = new AppDbContext(options,
                tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = publicHttp }))
            {
                var compras = new CompraRepository(dbPublic, currentUser, scope.Object);
                var ventas = new VentaRepository(dbPublic, currentUser, scope.Object);
                Assert.Equal(0, await compras.ContarTodasAsync());
                await using var transaction = await dbPublic.Database.BeginTransactionAsync();
                Assert.Null(await compras.GetByIdForUpdateAsync(compraBId));
                Assert.Null(await ventas.GetByIdForUpdateAsync(ventaBId));
                await transaction.RollbackAsync();
            }

            await using (var comprobacion = new AppDbContext(options))
            {
                var persistedB = await comprobacion.Productos.IgnoreQueryFilters()
                    .AsNoTracking().SingleAsync(p => p.Id == productoBId);
                Assert.Equal("Producto confidencial B", persistedB.Nombre);
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
