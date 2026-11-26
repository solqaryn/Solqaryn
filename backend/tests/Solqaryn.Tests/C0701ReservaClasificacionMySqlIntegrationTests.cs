using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

// C07-01.M06.06: real service, repositories, transactions, audit and Oracle MySQL.
// The server-side tenant marker and actor claims are synthetic fixture inputs.
// This is A3 persistence/service integration, not HTTP JWT/RBAC certification.
[Trait("Category", "Integration")]
public sealed class C0701ReservaClasificacionMySqlIntegrationTests
{
    private sealed record Seed(int OrderId, int WarehouseId, int[] Products, int[] Variants, int[] Stocks);

    [Fact]
    public async Task MixedClassification_DeniesWithoutEffects_ThenAuditFailureRollsBack_AndReplayIsIdempotent()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral MySQL 8.4 connection required.");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") || origin.Port != 3306)
            throw new InvalidOperationException("Only isolated MySQL localhost:3306 is allowed.");
        var isolated = new MySqlConnectionStringBuilder(origin.ConnectionString)
        {
            Database = "v44_c0701_reserva_" + Guid.NewGuid().ToString("N")
        };
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated.ConnectionString, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        int actor = 0;
        HttpContextAccessor Access(int tenant)
        {
            var h = new DefaultHttpContext();
            h.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenant;
            h.TraceIdentifier = "qa-" + Guid.NewGuid().ToString("N");
            h.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("id", actor.ToString()),
                new Claim("nombreUsuario", "qa-reserva"),
                new Claim("nombreCompleto", "QA Reserva")
            }, "IntegrationFixture"));
            return new HttpContextAccessor { HttpContext = h };
        }

        PedidoVentaService Compose(AppDbContext db, HttpContextAccessor access)
        {
            var current = new CurrentUserService(access);
            return new PedidoVentaService(new PedidoVentaRepository(db), new CotizacionRepository(db),
                new AuditoriaService(new AuditoriaRepository(db), current, access, NullLogger<AuditoriaService>.Instance),
                current, new UnitOfWork(db), new ReservaInventarioRepository(db),
                new ProductoVarianteRepository(db),
                new ExistenciaVarianteConcurrencyService(db, new ExistenciaVarianteRepository(db)),
                new ProductoRepository(db));
        }

        async Task<Seed> AddOrderAsync(int tenant, string suffix)
        {
            await using var db = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant));
            var type = new TipoCliente { Codigo = "QA_" + suffix, Nombre = "QA " + suffix,
                NombreNormalizado = "QA_" + suffix, Activo = true };
            db.TipoClientes.Add(type);
            var branch = new Sucursal { EmpresaId = tenant, Codigo = "QA_" + suffix, Nombre = "QA " + suffix };
            db.Set<Sucursal>().Add(branch);
            await db.SaveChangesAsync();
            var client = new Cliente { Nombre = "QA " + suffix, TipoClienteId = type.Id };
            var warehouse = new Almacen { SucursalId = branch.Id, Codigo = "QA_WH_" + suffix,
                Nombre = "QA " + suffix, Tipo = TipoAlmacen.Bodega };
            db.Clientes.Add(client);
            db.Set<Almacen>().Add(warehouse);
            var products = Enumerable.Range(1, 2).Select(i => new Producto {
                Nombre = "QA " + suffix + i, Precio = 25m, Costo = 12m, Cantidad = 10,
                TipoInventario = TipoInventario.MercaderiaVenta }).ToArray();
            db.Productos.AddRange(products);
            await db.SaveChangesAsync();
            var variants = products.Select((p, i) => new ProductoVariante {
                ProductoId = p.Id, Sku = "QA_" + suffix + i, Cantidad = 10, Precio = 25m, Costo = 12m }).ToArray();
            db.ProductoVariantes.AddRange(variants);
            await db.SaveChangesAsync();
            var stocks = variants.Select(v => {
                var e = new ExistenciaVariante { ProductoVarianteId = v.Id, AlmacenId = warehouse.Id };
                e.EstablecerStocks(10, 0, 0, 0, null);
                return e;
            }).ToArray();
            db.ExistenciasVariante.AddRange(stocks);
            var quote = new Cotizacion { ClienteId = client.Id, Cliente = client, ClienteNombreSnapshot = client.Nombre };
            for (int i = 0; i < products.Length; i++)
            {
                var line = new CotizacionDetalle { ProductoId = products[i].Id, Producto = products[i],
                    ProductoVarianteId = variants[i].Id, ProductoSkuSnapshot = variants[i].Sku,
                    ProductoNombreSnapshot = products[i].Nombre };
                line.EstablecerValores(i == 0 ? 2m : 1m, 25m);
                quote.Detalles.Add(line);
            }
            db.Cotizaciones.Add(quote);
            await db.SaveChangesAsync();
            quote.Enviar(actor, DateTime.UtcNow);
            quote.Aceptar(actor, DateTime.UtcNow);
            await db.SaveChangesAsync();
            var order = PedidoVenta.CrearDesdeCotizacion(quote);
            order.EstablecerIdempotencia("qa-reserva-" + suffix, new string('a', 64));
            db.PedidosVenta.Add(order);
            await db.SaveChangesAsync();
            return new Seed(order.Id, warehouse.Id, products.Select(p => p.Id).ToArray(),
                variants.Select(v => v.Id).ToArray(), stocks.Select(e => e.Id).ToArray());
        }

        static ConfirmarPedidoVentaDto Request(Seed s)
        {
            var dto = new ConfirmarPedidoVentaDto();
            for (int i = 0; i < s.Variants.Length; i++)
                dto.Asignaciones.Add(new AsignacionReservaPedidoDto {
                    ProductoVarianteId = s.Variants[i], AlmacenId = s.WarehouseId, Cantidad = i == 0 ? 2 : 1 });
            return dto;
        }

        async Task VerifyAsync(int tenant, Seed s, bool confirmed)
        {
            await using var verify = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant));
            var order = await verify.PedidosVenta.AsNoTracking().SingleAsync(x => x.Id == s.OrderId);
            Assert.Equal(tenant, order.EmpresaId);
            Assert.Equal(confirmed ? EstadoPedidoVenta.Confirmado : EstadoPedidoVenta.Borrador, order.Estado);
            for (int i = 0; i < s.Stocks.Length; i++)
            {
                var e = await verify.ExistenciasVariante.AsNoTracking().SingleAsync(x => x.Id == s.Stocks[i]);
                var expected = confirmed ? (i == 0 ? 2 : 1) : 0;
                Assert.Equal(10, e.StockFisico);
                Assert.Equal(expected, e.StockReservado);
                Assert.Equal(10 - expected, e.StockDisponible);
            }
            var reservations = await verify.ReservasInventario.AsNoTracking().Include(x => x.Detalles)
                .Where(x => x.PedidoVentaId == s.OrderId).ToListAsync();
            var audit = await verify.RegistrosAuditoria.AsNoTracking()
                .Where(x => x.EmpresaId == tenant && x.Accion == AccionPermiso.Confirmar &&
                    ((x.Entidad == nameof(PedidoVenta) && x.ReferenciaId == s.OrderId) ||
                     x.Entidad == nameof(ReservaInventario))).ToListAsync();
            if (!confirmed)
            {
                Assert.Empty(reservations);
                Assert.Empty(audit);
            }
            else
            {
                var reservation = Assert.Single(reservations);
                Assert.Equal(EstadoReservaInventario.Activa, reservation.Estado);
                Assert.Equal(2, reservation.Detalles.Count);
                Assert.Equal(2, audit.Count);
                Assert.Contains(audit, x => x.Entidad == nameof(PedidoVenta) && x.ReferenciaId == s.OrderId);
                Assert.Contains(audit, x => x.Entidad == nameof(ReservaInventario) && x.ReferenciaId == reservation.Id);
            }
        }

        try
        {
            int a, b;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var ea = new Empresa("QA Reserva A");
                var eb = new Empresa("QA Reserva B");
                setup.Set<Empresa>().AddRange(ea, eb);
                var role = new Rol { Nombre = "QA Reserva", NombreNormalizado = "QA_RESERVA",
                    EsSistema = true, Activo = true };
                setup.Roles.Add(role);
                await setup.SaveChangesAsync();
                a = ea.Id; b = eb.Id;
                var user = new Usuario { NombreUsuario = "qa-reserva", NombreCompleto = "QA Reserva",
                    PasswordHash = "fixture-no-login", RolId = role.Id, Activo = true };
                setup.Usuarios.Add(user);
                await setup.SaveChangesAsync();
                actor = user.Id;
                setup.UsuarioEmpresas.AddRange(new UsuarioEmpresa(actor, a, role.Id), new UsuarioEmpresa(actor, b, role.Id));
                await setup.SaveChangesAsync();
            }
            Assert.NotEqual(a, b);
            var sa = await AddOrderAsync(a, "A");
            var sb = await AddOrderAsync(b, "B");

            // Cross-tenant order cannot be resolved by the real SQL locking repository.
            var cross = Access(a);
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: cross))
                await Assert.ThrowsAsync<ResourceNotFoundException>(() => Compose(db, cross).ConfirmarAsync(sb.OrderId, Request(sb)));

            // A stale tracked product must not override the SQL row acquired under lock.
            // Prove both tenant directions before the existing fresh-context cases.
            foreach (var (tenant, seed) in new[] { (a, sa), (b, sb) })
            {
                var access = Access(tenant);
                await using var stale = new AppDbContext(options, tenantHttpContextAccessor: access);
                var cached = await stale.Productos.SingleAsync(x => x.Id == seed.Products[1]);
                Assert.Equal(TipoInventario.MercaderiaVenta, cached.TipoInventario);

                await using (var writer = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                {
                    var current = await writer.Productos.SingleAsync(x => x.Id == seed.Products[1]);
                    current.TipoInventario = TipoInventario.InsumoAdministrativo;
                    await writer.SaveChangesAsync();
                }
                Assert.Equal(TipoInventario.MercaderiaVenta, cached.TipoInventario);

                var deniedStale = await Assert.ThrowsAsync<BusinessRuleException>(
                    () => Compose(stale, access).ConfirmarAsync(seed.OrderId, Request(seed)));
                Assert.Contains("no disponibles para reserva comercial", deniedStale.Message);
                await VerifyAsync(a, sa, false);
                await VerifyAsync(b, sb, false);

                await using (var writer = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                {
                    var current = await writer.Productos.SingleAsync(x => x.Id == seed.Products[1]);
                    current.TipoInventario = TipoInventario.MercaderiaVenta;
                    await writer.SaveChangesAsync();
                }
            }

            // A variant tracked before deactivation must not be accepted by a later locking query.
            foreach (var (tenant, seed) in new[] { (a, sa), (b, sb) })
            {
                var access = Access(tenant);
                await using var stale = new AppDbContext(options, tenantHttpContextAccessor: access);
                var cached = await stale.ProductoVariantes.SingleAsync(x => x.Id == seed.Variants[1]);
                Assert.True(cached.Activo);
                await using (var writer = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                {
                    var current = await writer.ProductoVariantes.SingleAsync(x => x.Id == seed.Variants[1]);
                    current.Activo = false;
                    await writer.SaveChangesAsync();
                }
                Assert.True(cached.Activo);
                var deniedVariant = await Assert.ThrowsAsync<BusinessRuleException>(
                    () => Compose(stale, access).ConfirmarAsync(seed.OrderId, Request(seed)));
                Assert.Contains("no está activa para reservar inventario", deniedVariant.Message);
                await VerifyAsync(a, sa, false);
                await VerifyAsync(b, sb, false);
                await using (var writer = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                {
                    var current = await writer.ProductoVariantes.SingleAsync(x => x.Id == seed.Variants[1]);
                    current.Activo = true;
                    await writer.SaveChangesAsync();
                }
            }

            // A draft exists before a product is reclassified; its other line stays commercial.
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: Access(a)))
            {
                var p = await db.Productos.SingleAsync(x => x.Id == sa.Products[1]);
                p.TipoInventario = TipoInventario.InsumoAdministrativo;
                await db.SaveChangesAsync();
            }
            var denied = Access(a);
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: denied))
            {
                var error = await Assert.ThrowsAsync<BusinessRuleException>(() => Compose(db, denied).ConfirmarAsync(sa.OrderId, Request(sa)));
                Assert.Contains("no disponibles para reserva comercial", error.Message);
            }
            await VerifyAsync(a, sa, false);
            await VerifyAsync(b, sb, false);

            // Restore the synthetic product; fail a real audit INSERT after reservation and stock writes.
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: Access(a)))
            {
                var p = await db.Productos.SingleAsync(x => x.Id == sa.Products[1]);
                p.TipoInventario = TipoInventario.MercaderiaVenta;
                await db.SaveChangesAsync();
            }
            await using (var ddl = new AppDbContext(options))
                await ddl.Database.ExecuteSqlRawAsync("CREATE TRIGGER qa_reserva_audit_fail BEFORE INSERT ON RegistrosAuditoria FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'qa-reserva-rollback'");
            try
            {
                var failure = Access(a);
                await using var db = new AppDbContext(options, tenantHttpContextAccessor: failure);
                var error = await Assert.ThrowsAsync<DbUpdateException>(() => Compose(db, failure).ConfirmarAsync(sa.OrderId, Request(sa)));
                Assert.Contains("qa-reserva-rollback", error.ToString());
            }
            finally
            {
                await using var ddl = new AppDbContext(options);
                await ddl.Database.ExecuteSqlRawAsync("DROP TRIGGER qa_reserva_audit_fail");
            }
            await VerifyAsync(a, sa, false);
            await VerifyAsync(b, sb, false);

            // After rollback, same request succeeds; a committed replay cannot reserve again.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var access = Access(a);
                await using var db = new AppDbContext(options, tenantHttpContextAccessor: access);
                await Compose(db, access).ConfirmarAsync(sa.OrderId, Request(sa));
                await VerifyAsync(a, sa, true);
                await VerifyAsync(b, sb, false);
            }

            // Reverse denial and independent success/replay in company B.
            var reverse = Access(b);
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: reverse))
                await Assert.ThrowsAsync<ResourceNotFoundException>(() => Compose(db, reverse).ConfirmarAsync(sa.OrderId, Request(sa)));
            await VerifyAsync(a, sa, true);
            await VerifyAsync(b, sb, false);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var access = Access(b);
                await using var db = new AppDbContext(options, tenantHttpContextAccessor: access);
                await Compose(db, access).ConfirmarAsync(sb.OrderId, Request(sb));
                await VerifyAsync(a, sa, true);
                await VerifyAsync(b, sb, true);
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
