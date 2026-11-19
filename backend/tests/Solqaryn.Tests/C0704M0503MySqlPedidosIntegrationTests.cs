using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C07-04.M05.03: persistence of commercial orders using real ephemeral MySQL 8.4.
/// Authenticated HTTP/JWT, payments and fiscal emission are NOT exercised here.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0704M0503MySqlPedidosIntegrationTests
{
    private static MySqlConnectionStringBuilder LocalOnly(string connection)
    {
        var parsed = new MySqlConnectionStringBuilder(connection);
        if (parsed.Server is not ("127.0.0.1" or "localhost") || parsed.Port != 3306)
            throw new InvalidOperationException("Only isolated MySQL localhost:3306 is allowed.");
        return parsed;
    }

    [Theory]
    [InlineData("Server=mysql.prod.example;Port=3306;User=root;Password=dummy;")]
    [InlineData("Server=127.0.0.1;Port=3307;User=root;Password=dummy;")]
    public void DestructiveIntegrationRefusesRemoteOrUnexpectedPort(string connection)
    {
        Assert.Throws<InvalidOperationException>(() => LocalOnly(connection));
    }

    [Fact]
    public void FingerprintInvalidIsRejectedBeforePersistence()
    {
        Assert.Throws<ArgumentException>(() =>
            new PedidoVenta().EstablecerIdempotencia("PED-1", "not-a-sha256"));
    }

    [Fact]
    public async Task MySql84_OrdersAreTenantScoped_IdempotentAndRollbackSafe()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral MySQL 8.4 connection is required.");
        var original = LocalOnly(raw);
        var isolated = new MySqlConnectionStringBuilder(original.ConnectionString)
        {
            Database = "v20_c0704m0503_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        static HttpContextAccessor ForTenant(int tenant)
        {
            var http = new DefaultHttpContext();
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenant;
            return new HttpContextAccessor { HttpContext = http };
        }

        async Task<int> AddOrderAsync(AppDbContext db, string suffix, string idempotencyKey)
        {
            var kind = new TipoCliente {
                Codigo = "PED_" + suffix, Nombre = "Client " + suffix,
                NombreNormalizado = "CLIENT_" + suffix, Activo = true
            };
            db.TipoClientes.Add(kind);
            await db.SaveChangesAsync();

            var client = new Cliente {
                Nombre = "Client " + suffix, TipoClienteId = kind.Id
            };
            var product = new Producto {
                Nombre = "Product " + suffix, Precio = 25m, Costo = 12m
            };
            db.Clientes.Add(client);
            db.Productos.Add(product);
            await db.SaveChangesAsync();

            var quote = new Cotizacion {
                ClienteId = client.Id, Cliente = client,
                ClienteNombreSnapshot = client.Nombre
            };
            var line = new CotizacionDetalle { ProductoId = product.Id, Producto = product };
            line.EstablecerValores(2m, 25m);
            quote.Detalles.Add(line);
            db.Cotizaciones.Add(quote);
            await db.SaveChangesAsync();
            quote.Enviar(1, DateTime.UtcNow);
            quote.Aceptar(1, DateTime.UtcNow);
            await db.SaveChangesAsync();

            var order = PedidoVenta.CrearDesdeCotizacion(quote);
            order.EstablecerIdempotencia(idempotencyKey, new string('a', 64));
            db.PedidosVenta.Add(order);
            await db.SaveChangesAsync();
            return order.Id;
        }

        try
        {
            int tenantA, tenantB;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var a = new Empresa("Order test A");
                var b = new Empresa("Order test B");
                setup.Set<Empresa>().AddRange(a, b);
                await setup.SaveChangesAsync();
                tenantA = a.Id;
                tenantB = b.Id;
                Assert.NotEqual(tenantA, tenantB);
            }

            int orderA, orderB;
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: ForTenant(tenantA)))
                orderA = await AddOrderAsync(db, "A1", "shared-request-key");
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: ForTenant(tenantB)))
                orderB = await AddOrderAsync(db, "B1", "shared-request-key");
            Assert.NotEqual(orderA, orderB);

            await using (var a = new AppDbContext(options, tenantHttpContextAccessor: ForTenant(tenantA)))
            {
                Assert.Equal(new[] { orderA }, await a.PedidosVenta.AsNoTracking()
                    .Select(p => p.Id).ToArrayAsync());
                Assert.Null(await a.PedidosVenta.AsNoTracking()
                    .SingleOrDefaultAsync(p => p.Id == orderB));
            }
            await using (var b = new AppDbContext(options, tenantHttpContextAccessor: ForTenant(tenantB)))
            {
                Assert.Equal(new[] { orderB }, await b.PedidosVenta.AsNoTracking()
                    .Select(p => p.Id).ToArrayAsync());
                Assert.Null(await b.PedidosVenta.AsNoTracking()
                    .SingleOrDefaultAsync(p => p.Id == orderA));
            }

            // The physical unique index must reject a replay with the same key
            // inside tenant A even when a different quotation is supplied.
            await using (var duplicate = new AppDbContext(options, tenantHttpContextAccessor: ForTenant(tenantA)))
            {
                await Assert.ThrowsAsync<DbUpdateException>(async () =>
                {
                    await AddOrderAsync(duplicate, "A2", "shared-request-key");
                });
            }
            await using (var verify = new AppDbContext(options))
            {
                Assert.Equal(2, await verify.PedidosVenta.IgnoreQueryFilters()
                    .CountAsync(p => p.IdempotencyKey == "shared-request-key"));
            }

            // A committed quotation and a new order inside a transaction must
            // disappear after rollback; no inventory or payment is mutated.
            await using (var txDb = new AppDbContext(options, tenantHttpContextAccessor: ForTenant(tenantA)))
            {
                await using var tx = await txDb.Database.BeginTransactionAsync();
                await AddOrderAsync(txDb, "A3", "rolled-back-key");
                await tx.RollbackAsync();
            }
            await using (var verify = new AppDbContext(options))
            {
                Assert.Equal(0, await verify.PedidosVenta.IgnoreQueryFilters()
                    .CountAsync(p => p.IdempotencyKey == "rolled-back-key"));
                Assert.Equal(2, await verify.PedidosVenta.IgnoreQueryFilters()
                    .CountAsync(p => p.IdempotencyKey == "shared-request-key"));
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
