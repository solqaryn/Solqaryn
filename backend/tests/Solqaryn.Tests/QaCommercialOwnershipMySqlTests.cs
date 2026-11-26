using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class QaCommercialOwnershipMySqlTests
{
    [Fact]
    public async Task MySql_Aislamiento_AB_Claves_Referencias_Y_Cuarentena()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database=test_qa_ownership_{Guid.NewGuid():N};User=root;Password=root;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        try
        {
            int a, b, tipo, legacyId;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                await QaCommercialOwnershipV1.VerifyPhysicalAsync(setup);
                var ea = new Empresa("Empresa comercial A");
                var eb = new Empresa("Empresa comercial B");
                setup.Set<Empresa>().AddRange(ea, eb);
                var legacy = new Producto { Nombre = "Fixture sin dueño", Costo = 2, Precio = 3 };
                setup.Productos.Add(legacy);
                await setup.SaveChangesAsync();
                a = ea.Id; b = eb.Id; legacyId = legacy.Id;
                tipo = await setup.TipoClientes.Select(t => t.Id).FirstAsync();
            }

            var pa = await CrearCatalogoAsync(options, a, tipo, "A");
            var pb = await CrearCatalogoAsync(options, b, tipo, "B");
            var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            try
            {
                accessor.HttpContext.Request.Headers["X-Empresa-Id"] = b.ToString();
                await using (var noGate = new AppDbContext(options, httpContextAccessor: accessor))
                {
                    Assert.Empty(await noGate.Productos.ToListAsync());
                    noGate.Productos.Add(new Producto { Nombre = "Sin autorización" });
                    await Assert.ThrowsAsync<ForbiddenAccessException>(() => noGate.SaveChangesAsync());
                }

                accessor.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = a;
                await using (var scoped = new AppDbContext(options, httpContextAccessor: accessor))
                {
                    Assert.Equal(pa.Producto, (await scoped.Productos.SingleAsync()).Id);
                    Assert.Equal(pa.Variante, (await scoped.ProductoVariantes.SingleAsync()).Id);
                    Assert.Equal(pa.Cliente, (await scoped.Clientes.SingleAsync()).Id);
                    Assert.Equal(pa.Proveedor, (await scoped.Proveedores.SingleAsync()).Id);
                    Assert.False(await scoped.Productos.AnyAsync(p => p.Id == legacyId));
                    Assert.False(await scoped.Productos.IgnoreQueryFilters(new[] { QaCommercialOwnershipV1.SoftDeleteFilter })
                        .AnyAsync(p => p.Id == pb.Producto));
                    var repo = new ProductoRepository(scoped);
                    Assert.Null(await repo.GetByIdAsync(pb.Producto));
                    await using var tx = await scoped.Database.BeginTransactionAsync();
                    Assert.Null(await repo.GetByIdForUpdateAsync(pb.Producto));
                    Assert.NotNull(await repo.GetByIdForUpdateAsync(pa.Producto));
                    await tx.RollbackAsync();
                }

                await using (var cross = new AppDbContext(options, httpContextAccessor: accessor))
                {
                    cross.Productos.Update(new Producto { Id = pb.Producto, Nombre = "Alteración ajena" });
                    await Assert.ThrowsAsync<ForbiddenAccessException>(() => cross.SaveChangesAsync());
                }
                await using (var cross = new AppDbContext(options, httpContextAccessor: accessor))
                {
                    cross.ProductoVariantes.Add(new ProductoVariante { ProductoId = pb.Producto, Sku = "NO-AJENO" });
                    await Assert.ThrowsAsync<ForbiddenAccessException>(() => cross.SaveChangesAsync());
                }
                await using (var cross = new AppDbContext(options, httpContextAccessor: accessor))
                {
                    cross.Compras.Add(new Compra { NumeroCompra = "NO-AJENA", ProveedorId = pb.Proveedor });
                    await Assert.ThrowsAsync<ForbiddenAccessException>(() => cross.SaveChangesAsync());
                }
                await using (var cross = new AppDbContext(options, httpContextAccessor: accessor))
                {
                    cross.Ventas.Add(new Venta { NumeroVenta = "NO-AJENA", ClienteId = pb.Cliente });
                    await Assert.ThrowsAsync<ForbiddenAccessException>(() => cross.SaveChangesAsync());
                }

                // The physical constraint remains effective without the API guard.
                await using (var physical = new AppDbContext(options))
                {
                    var variant = new ProductoVariante { ProductoId = pb.Producto, Sku = "NO-FISICO" };
                    physical.ProductoVariantes.Add(variant);
                    physical.Entry(variant).Property("EmpresaId").CurrentValue = a;
                    await Assert.ThrowsAsync<DbUpdateException>(() => physical.SaveChangesAsync());
                }
                await using (var physical = new AppDbContext(options))
                {
                    var purchase = new Compra { NumeroCompra = "NO-FISICA", ProveedorId = pb.Proveedor };
                    physical.Compras.Add(purchase);
                    physical.Entry(purchase).Property("EmpresaId").CurrentValue = a;
                    await Assert.ThrowsAsync<DbUpdateException>(() => physical.SaveChangesAsync());
                }
                await using (var physical = new AppDbContext(options))
                {
                    var sale = new Venta { NumeroVenta = "NO-FISICA", ClienteId = pb.Cliente };
                    physical.Ventas.Add(sale);
                    physical.Entry(sale).Property("EmpresaId").CurrentValue = a;
                    await Assert.ThrowsAsync<DbUpdateException>(() => physical.SaveChangesAsync());
                }
                await using (var duplicate = new AppDbContext(options, httpContextAccessor: accessor))
                {
                    var p = new Producto { Nombre = "Otro A" };
                    duplicate.Productos.Add(p);
                    await duplicate.SaveChangesAsync();
                    duplicate.ProductoVariantes.Add(new ProductoVariante { ProductoId = p.Id, Sku = "MISMO-SKU" });
                    await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
                }

                accessor.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = b;
                await using (var scoped = new AppDbContext(options, httpContextAccessor: accessor))
                {
                    Assert.Equal("Producto B", (await scoped.Productos.SingleAsync()).Nombre);
                    Assert.Equal(pb.Variante, (await scoped.ProductoVariantes.SingleAsync()).Id);
                    Assert.Equal(pb.Cliente, (await scoped.Clientes.SingleAsync()).Id);
                    Assert.Equal(pb.Proveedor, (await scoped.Proveedores.SingleAsync()).Id);
                    Assert.False(await scoped.Productos.AnyAsync(p => p.Id == pa.Producto));
                }
            }
            finally { accessor.HttpContext = null; }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    private static async Task<(int Producto,int Variante,int Cliente,int Proveedor)> CrearCatalogoAsync(
        DbContextOptions<AppDbContext> options, int empresa, int tipo, string suffix)
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        accessor.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = empresa;
        try
        {
            await using var db = new AppDbContext(options, httpContextAccessor: accessor);
            var p = new Producto { Nombre = "Producto " + suffix, Costo = 5, Precio = 10 };
            var c = new Cliente { Nombre = "Cliente " + suffix, IdentidadORTN = "0801199900001", TipoClienteId = tipo };
            var s = new Proveedor { Nombre = "Proveedor " + suffix, Documento = "0801199900002" };
            db.Productos.Add(p);
            db.Clientes.Add(c);
            db.Proveedores.Add(s);
            await db.SaveChangesAsync();
            var v = new ProductoVariante { ProductoId = p.Id, Sku = "MISMO-SKU", CodigoBarras = "1234567890123" };
            db.ProductoVariantes.Add(v);
            await db.SaveChangesAsync();
            Assert.Equal(empresa, db.Entry(p).Property("EmpresaId").CurrentValue);
            Assert.Equal(empresa, db.Entry(v).Property("EmpresaId").CurrentValue);
            return (p.Id, v.Id, c.Id, s.Id);
        }
        finally { accessor.HttpContext = null; }
    }
}
