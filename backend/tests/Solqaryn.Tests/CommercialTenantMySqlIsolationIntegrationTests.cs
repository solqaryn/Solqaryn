using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
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
            int empresaAId, empresaBId, productoAId, productoBId, categoriaAId, categoriaBId;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var empresaA = new Empresa("TENANT TEST A");
                var empresaB = new Empresa("TENANT TEST B");
                setup.Set<Empresa>().AddRange(empresaA, empresaB);
                await setup.SaveChangesAsync();
                empresaAId = empresaA.Id;
                empresaBId = empresaB.Id;
                setup.Categorias.Add(new Categoria { Nombre = "LEGACY SIN OWNER" });
                await setup.SaveChangesAsync();
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

                var vistaB = await dbB.Productos.AsNoTracking().Include(p => p.Categoria).ToListAsync();
                Assert.Single(vistaB);
                Assert.Equal(productoBId, vistaB[0].Id);
                Assert.Equal(categoriaBId, vistaB[0].CategoriaId);
            }

            await using (var dbA = new AppDbContext(options,
                tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = httpA }))
            {
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
