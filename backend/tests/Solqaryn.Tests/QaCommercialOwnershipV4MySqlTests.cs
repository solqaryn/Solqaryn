using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class QaCommercialOwnershipV4MySqlTests
{
    [Fact]
    public async Task CotizacionesPedidosIdentidadFiscal_AislamientoA_B()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database=test_qa_v4_{Guid.NewGuid():N};User=root;Password=root;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        try
        {
            int a, b;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                await QaCommercialOwnershipV4.VerifyPhysicalAsync(setup);
                var ea = new Empresa("Empresa fiscal A");
                ea.ActualizarIdentidadLegal("QA111", "Dirección A", "https://example.invalid/a.png", null);
                var eb = new Empresa("Empresa fiscal B");
                eb.ActualizarIdentidadLegal("QA222", "Dirección B", "https://example.invalid/b.png", null);
                setup.AddRange(ea, eb);
                await setup.SaveChangesAsync();
                a = ea.Id; b = eb.Id;
                setup.AddRange(new ConfigEmpresa(a) { Moneda = "HNL" }, new ConfigEmpresa(b) { Moneda = "USD" });
                await setup.SaveChangesAsync();
                await using var transaction = await setup.Database.BeginTransactionAsync();
                await QaCommercialOwnershipV3.ProvisionDefaultsAsync(setup, a);
                await QaCommercialOwnershipV3.ProvisionDefaultsAsync(setup, b);
                await transaction.CommitAsync();
            }
            async Task<(int Cotizacion, int Pedido, int Cliente, int Producto)> Create(int owner, string suffix)
            {
                accessor.HttpContext!.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = owner;
                await using var db = new AppDbContext(options, httpContextAccessor: accessor);
                var tipo = await db.TipoClientes.SingleAsync(t => t.EsPredeterminado);
                var cliente = new Cliente { Nombre = "Cliente " + suffix, TipoClienteId = tipo.Id };
                var categoria = new Categoria { Nombre = "Categoría " + suffix };
                db.AddRange(cliente, categoria);
                await db.SaveChangesAsync();
                var producto = new Producto { Nombre = "Producto " + suffix, CategoriaId = categoria.Id, Precio = 10, Costo = 5 };
                db.Productos.Add(producto);
                await db.SaveChangesAsync();
                var detalle = new CotizacionDetalle { ProductoId = producto.Id, ProductoNombreSnapshot = producto.Nombre };
                detalle.EstablecerValores(2, 10);
                var cot = new Cotizacion { ClienteId = cliente.Id, ClienteNombreSnapshot = cliente.Nombre, Detalles = new List<CotizacionDetalle> { detalle } };
                db.Cotizaciones.Add(cot);
                await db.SaveChangesAsync();
                cot.Enviar(1, DateTime.UtcNow);
                cot.Aceptar(1, DateTime.UtcNow);
                var pedido = PedidoVenta.CrearDesdeCotizacion(cot);
                pedido.EstablecerIdempotencia("misma-clave-A-B", new string('a', 64));
                cot.Convertir(1, DateTime.UtcNow);
                db.Set<PedidoVenta>().Add(pedido);
                await db.SaveChangesAsync();
                var identidad = await new EmpresaDocumentoIdentity(db).ObtenerAsync(escritura: true);
                Assert.Equal("Empresa fiscal " + suffix, identidad.NombreComercial);
                Assert.Equal(suffix == "A" ? "QA111" : "QA222", identidad.RTN);
                Assert.Equal(suffix == "A" ? "HNL" : "USD", identidad.Moneda);
                Assert.Equal("https://example.invalid/" + suffix.ToLowerInvariant() + ".png", identidad.LogoUrl);
                Assert.Null(identidad.Correo);
                return (cot.Id, pedido.Id, cliente.Id, producto.Id);
            }
            var aa = await Create(a, "A");
            var bb = await Create(b, "B");
            Assert.NotEqual(aa.Pedido, bb.Pedido);
            foreach (var side in new[] { (Owner:a, Own:aa, Foreign:bb), (Owner:b, Own:bb, Foreign:aa) })
            {
                accessor.HttpContext!.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = side.Owner;
                await using var db = new AppDbContext(options, httpContextAccessor: accessor);
                Assert.Equal(side.Own.Cotizacion, (await db.Cotizaciones.SingleAsync()).Id);
                Assert.Equal(side.Own.Pedido, (await db.Set<PedidoVenta>().SingleAsync()).Id);
                Assert.Single(await db.Set<CotizacionDetalle>().ToListAsync());
                Assert.Single(await db.Set<PedidoVentaDetalle>().ToListAsync());
                Assert.Null(await new CotizacionRepository(db).GetByIdAsync(side.Foreign.Cotizacion));
                Assert.Null(await new PedidoVentaRepository(db).GetByIdAsync(side.Foreign.Pedido));
                await using (var tx = await db.Database.BeginTransactionAsync())
                {
                    var own = await new PedidoVentaRepository(db).GetByIdempotencyKeyForUpdateAsync("misma-clave-A-B");
                    Assert.Equal(side.Own.Pedido, own!.Id);
                    await tx.RollbackAsync();
                }
                var cross = new Cotizacion { ClienteId = side.Foreign.Cliente, ClienteNombreSnapshot = "Cruce" };
                db.Cotizaciones.Add(cross);
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => db.SaveChangesAsync());
            }
            accessor.HttpContext!.Items.Clear();
            await using var denied = new AppDbContext(options, httpContextAccessor: accessor);
            Assert.Empty(await denied.Cotizaciones.ToListAsync());
            Assert.Empty(await denied.Set<PedidoVenta>().ToListAsync());
            await Assert.ThrowsAsync<ForbiddenAccessException>(() => new EmpresaDocumentoIdentity(denied).ObtenerAsync(escritura: true));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
