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

// C07-01.M06.06: A3 causal de duplicacion comercial; Oracle EF/MySQL real.
// Identidad/tenant sinteticos del fixture; no constituye certificacion JWT/HTTP.
[Trait("Category", "Integration")]
public sealed class C0701CotizacionDuplicacionMySqlIntegrationTests
{
    private sealed record Seed(int QuoteId, int ReclassProductId);

    [Fact]
    public async Task DuplicateChecksCurrentCatalogAndTenant_RollsBackAuditFailure_PreservesSnapshot()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral MySQL 8.4 connection required.");
        var origin = new MySqlConnectionStringBuilder(raw);
        if (origin.Server is not ("127.0.0.1" or "localhost") || origin.Port != 3306)
            throw new InvalidOperationException("Only isolated MySQL localhost:3306 is allowed.");
        var isolated = new MySqlConnectionStringBuilder(origin.ConnectionString)
        {
            Database = "v44_c0701_quote_" + Guid.NewGuid().ToString("N")
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
                new Claim("nombreUsuario", "qa-quote"),
                new Claim("nombreCompleto", "QA Quote")
            }, "IntegrationFixture"));
            return new HttpContextAccessor { HttpContext = h };
        }

        CotizacionService Compose(AppDbContext db, HttpContextAccessor access)
        {
            var current = new CurrentUserService(access);
            return new CotizacionService(new CotizacionRepository(db), new ClienteRepository(db),
                new ProductoRepository(db), new ProductoVarianteRepository(db),
                new AuditoriaService(new AuditoriaRepository(db), current, access, NullLogger<AuditoriaService>.Instance),
                current, new UnitOfWork(db));
        }

        async Task<Seed> SeedAsync(int tenant, string suffix)
        {
            await using var db = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant));
            var type = new TipoCliente { Codigo = "QA_" + suffix, Nombre = "QA " + suffix,
                NombreNormalizado = "QA_" + suffix, Activo = true };
            db.TipoClientes.Add(type);
            await db.SaveChangesAsync();
            var client = new Cliente { Nombre = "QA " + suffix, TipoClienteId = type.Id };
            db.Clientes.Add(client);
            var products = Enumerable.Range(1, 2).Select(i => new Producto
            {
                Nombre = "QA " + suffix + i, Precio = 25m, Costo = 12m, Cantidad = 10,
                TipoInventario = TipoInventario.MercaderiaVenta
            }).ToArray();
            db.Productos.AddRange(products);
            await db.SaveChangesAsync();
            var marca = new Marca { Nombre = "QA Marca " + suffix };
            var modelo = new Modelo { Nombre = "QA Modelo " + suffix, Marca = marca };
            var color = new Color { Nombre = "QA Color " + suffix };
            var talla = new Talla { Nombre = "QA Talla " + suffix };
            db.AddRange(marca, modelo, color, talla);
            await db.SaveChangesAsync();
            var variants = products.Select((p, i) => new ProductoVariante
            {
                ProductoId = p.Id, Sku = "QA_" + suffix + i, Cantidad = 10, Precio = 25m, Costo = 12m,
                MarcaId = marca.Id, ModeloId = modelo.Id, ColorId = color.Id, TallaId = talla.Id
            }).ToArray();
            db.ProductoVariantes.AddRange(variants);
            await db.SaveChangesAsync();
            var quote = new Cotizacion { ClienteId = client.Id, Cliente = client, ClienteNombreSnapshot = client.Nombre };
            for (int i = 0; i < products.Length; i++)
            {
                var line = new CotizacionDetalle
                {
                    ProductoId = products[i].Id, Producto = products[i], ProductoVarianteId = variants[i].Id,
                    ProductoSkuSnapshot = variants[i].Sku, ProductoNombreSnapshot = products[i].Nombre,
                    ProductoMarcaSnapshot = marca.Nombre, ProductoModeloSnapshot = modelo.Nombre,
                    ProductoColorSnapshot = color.Nombre, ProductoTallaSnapshot = talla.Nombre
                };
                line.EstablecerValores(i == 0 ? 2m : 1m, 25m);
                quote.Detalles.Add(line);
            }
            db.Cotizaciones.Add(quote);
            await db.SaveChangesAsync();
            quote.Enviar(actor, DateTime.UtcNow);
            quote.Aceptar(actor, DateTime.UtcNow);
            await db.SaveChangesAsync();
            return new Seed(quote.Id, products[1].Id);
        }

        async Task VerifyAsync(int tenant, Seed seed, int? copyId = null)
        {
            await using var verify = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant));
            var quotes = await verify.Cotizaciones.AsNoTracking().Include(x => x.Detalles).ToListAsync();
            Assert.Equal(copyId.HasValue ? 2 : 1, quotes.Count);
            var original = Assert.Single(quotes.Where(x => x.Id == seed.QuoteId));
            Assert.Equal(tenant, original.EmpresaId);
            Assert.Equal(EstadoCotizacion.Aceptada, original.Estado);
            Assert.Equal(2, original.Detalles.Count);
            Assert.All(original.Detalles, x => Assert.Equal(25m, x.PrecioUnitario));
            var receipts = await verify.RegistrosAuditoria.AsNoTracking()
                .Where(x => x.EmpresaId == tenant && x.Entidad == nameof(Cotizacion) &&
                    x.Accion == AccionPermiso.Duplicar).ToListAsync();
            if (!copyId.HasValue)
            {
                Assert.Empty(receipts);
                return;
            }
            var copy = Assert.Single(quotes.Where(x => x.Id == copyId.Value));
            Assert.NotEqual(original.Id, copy.Id);
            Assert.Equal(tenant, copy.EmpresaId);
            Assert.Equal(EstadoCotizacion.Borrador, copy.Estado);
            Assert.Equal(original.ClienteId, copy.ClienteId);
            Assert.Equal(original.ClienteNombreSnapshot, copy.ClienteNombreSnapshot);
            Assert.Equal(original.Total, copy.Total);
            var before = original.Detalles.OrderBy(x => x.ProductoId).Select(x =>
                (x.ProductoId, x.ProductoVarianteId, x.Cantidad, x.PrecioUnitario, x.ProductoSkuSnapshot, x.ProductoNombreSnapshot,
                 x.ProductoMarcaSnapshot, x.ProductoModeloSnapshot, x.ProductoColorSnapshot, x.ProductoTallaSnapshot)).ToArray();
            var after = copy.Detalles.OrderBy(x => x.ProductoId).Select(x =>
                (x.ProductoId, x.ProductoVarianteId, x.Cantidad, x.PrecioUnitario, x.ProductoSkuSnapshot, x.ProductoNombreSnapshot,
                 x.ProductoMarcaSnapshot, x.ProductoModeloSnapshot, x.ProductoColorSnapshot, x.ProductoTallaSnapshot)).ToArray();
            Assert.Equal(before, after);
            var receipt = Assert.Single(receipts);
            Assert.Equal(copy.Id, receipt.ReferenciaId);
            Assert.Equal(actor, receipt.UsuarioId);
            Assert.Equal("Exito", receipt.Resultado);
        }

        try
        {
            int a, b;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var ea = new Empresa("QA Quote A");
                var eb = new Empresa("QA Quote B");
                setup.Set<Empresa>().AddRange(ea, eb);
                var role = new Rol { Nombre = "QA Quote", NombreNormalizado = "QA_QUOTE", EsSistema = true, Activo = true };
                setup.Roles.Add(role);
                await setup.SaveChangesAsync();
                a = ea.Id; b = eb.Id;
                var user = new Usuario { NombreUsuario = "qa-quote", NombreCompleto = "QA Quote",
                    PasswordHash = "fixture-no-login", RolId = role.Id, Activo = true };
                setup.Usuarios.Add(user);
                await setup.SaveChangesAsync();
                actor = user.Id;
                setup.UsuarioEmpresas.AddRange(new UsuarioEmpresa(actor, a, role.Id), new UsuarioEmpresa(actor, b, role.Id));
                await setup.SaveChangesAsync();
            }
            Assert.NotEqual(a, b);
            var sa = await SeedAsync(a, "A");
            var sb = await SeedAsync(b, "B");
            foreach (var (tenant, other) in new[] { (a, sb), (b, sa) })
            {
                var access = Access(tenant);
                await using var db = new AppDbContext(options, tenantHttpContextAccessor: access);
                await Assert.ThrowsAsync<ResourceNotFoundException>(() => Compose(db, access).DuplicarComoBorradorAsync(other.QuoteId));
            }
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: Access(a)))
            {
                var p = await db.Productos.SingleAsync(x => x.Id == sa.ReclassProductId);
                p.TipoInventario = TipoInventario.InsumoAdministrativo;
                await db.SaveChangesAsync();
            }
            var denied = Access(a);
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: denied))
            {
                var error = await Assert.ThrowsAsync<BusinessRuleException>(() => Compose(db, denied).DuplicarComoBorradorAsync(sa.QuoteId));
                Assert.Contains("no está habilitado para cotización comercial", error.Message);
            }
            await VerifyAsync(a, sa);
            await VerifyAsync(b, sb);
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: Access(a)))
            {
                var p = await db.Productos.SingleAsync(x => x.Id == sa.ReclassProductId);
                p.TipoInventario = TipoInventario.MercaderiaVenta;
                p.Precio = 999m;
                await db.SaveChangesAsync();
            }
            await using (var ddl = new AppDbContext(options))
                await ddl.Database.ExecuteSqlRawAsync("CREATE TRIGGER qa_quote_audit_fail BEFORE INSERT ON RegistrosAuditoria FOR EACH ROW SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'qa-quote-rollback'");
            try
            {
                var access = Access(a);
                await using var db = new AppDbContext(options, tenantHttpContextAccessor: access);
                var error = await Assert.ThrowsAsync<DbUpdateException>(() => Compose(db, access).DuplicarComoBorradorAsync(sa.QuoteId));
                Assert.Contains("qa-quote-rollback", error.ToString());
            }
            finally
            {
                await using var ddl = new AppDbContext(options);
                await ddl.Database.ExecuteSqlRawAsync("DROP TRIGGER qa_quote_audit_fail");
            }
            await VerifyAsync(a, sa);
            await VerifyAsync(b, sb);
            int copyA, copyB;
            var accessA = Access(a);
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: accessA))
                copyA = (await Compose(db, accessA).DuplicarComoBorradorAsync(sa.QuoteId)).Id;
            await VerifyAsync(a, sa, copyA);
            await VerifyAsync(b, sb);
            var accessB = Access(b);
            await using (var db = new AppDbContext(options, tenantHttpContextAccessor: accessB))
                copyB = (await Compose(db, accessB).DuplicarComoBorradorAsync(sb.QuoteId)).Id;
            await VerifyAsync(a, sa, copyA);
            await VerifyAsync(b, sb, copyB);

            // Alta y edición reales usan el mismo contrato de locks sin habilitar HTTP.
            foreach (var (tenant, own, other) in new[] { (a, sa, sb), (b, sb, sa) })
            {
                int clientId, productId, variantId, draftId, lineId;
                var access = Access(tenant);
                await using (var db = new AppDbContext(options, tenantHttpContextAccessor: access))
                {
                    var original = await db.Cotizaciones.AsNoTracking().Include(x => x.Detalles).SingleAsync(x => x.Id == own.QuoteId);
                    var line = original.Detalles.Single(x => x.ProductoId == own.ReclassProductId);
                    clientId = original.ClienteId; productId = line.ProductoId; variantId = line.ProductoVarianteId!.Value;
                    var service = Compose(db, access);
                    draftId = (await service.CrearAsync(new CreateCotizacionDto { ClienteId = clientId, Detalles = [new CreateCotizacionDetalleDto { ProductoId = productId, ProductoVarianteId = variantId, Cantidad = 1m, PrecioUnitario = 25m }] })).Id;
                }
                await using (var db = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                {
                    var draft = await db.Cotizaciones.AsNoTracking().Include(x => x.Detalles).SingleAsync(x => x.Id == draftId);
                    Assert.Equal(tenant, draft.EmpresaId);
                    Assert.Equal(EstadoCotizacion.Borrador, draft.Estado);
                    lineId = draft.Detalles.Single().Id;
                    Assert.Equal(1m, draft.Detalles.Single().Cantidad);
                    Assert.Equal(1, await db.RegistrosAuditoria.CountAsync(x => x.Entidad == nameof(Cotizacion) && x.ReferenciaId == draftId && x.Accion == AccionPermiso.Crear));
                }
                var deniedAccess = Access(tenant);
                await using (var db = new AppDbContext(options, tenantHttpContextAccessor: deniedAccess))
                {
                    await Assert.ThrowsAsync<BusinessRuleException>(() => Compose(db, deniedAccess).CrearAsync(new CreateCotizacionDto { ClienteId = clientId, Detalles = [new CreateCotizacionDetalleDto { ProductoId = other.ReclassProductId, Cantidad = 1m, PrecioUnitario = 25m }] }));
                }
                await using (var db = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                {
                    var product = await db.Productos.SingleAsync(x => x.Id == productId);
                    product.TipoInventario = TipoInventario.InsumoAdministrativo;
                    await db.SaveChangesAsync();
                }
                var editAccess = Access(tenant);
                await using (var db = new AppDbContext(options, tenantHttpContextAccessor: editAccess))
                {
                    await Assert.ThrowsAsync<BusinessRuleException>(() => Compose(db, editAccess).ActualizarAsync(new UpdateCotizacionDto { Id = draftId, ClienteId = clientId, Detalles = [new UpdateCotizacionDetalleDto { Id = lineId, ProductoId = productId, ProductoVarianteId = variantId, Cantidad = 2m, PrecioUnitario = 25m }] }));
                }
                await using (var verify = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                {
                    Assert.Equal(3, await verify.Cotizaciones.CountAsync());
                    var draft = await verify.Cotizaciones.AsNoTracking().Include(x => x.Detalles).SingleAsync(x => x.Id == draftId);
                    Assert.Equal(1m, draft.Detalles.Single().Cantidad);
                    Assert.Equal(EstadoCotizacion.Borrador, draft.Estado);
                    Assert.Equal(0, await verify.RegistrosAuditoria.CountAsync(x => x.Entidad == nameof(Cotizacion) && x.ReferenciaId == draftId && x.Accion == AccionPermiso.Editar));
                }

                // Dos contextos independientes: el tracker del consumidor conserva la versión previa.
                // El writer confirma reclasificación antes de que Crear/Actualizar tomen el lock.
                foreach (var operationKind in new[] { 0, 1, 2 })
                {
                    var staleAccess = Access(tenant);
                    await using var stale = new AppDbContext(options, tenantHttpContextAccessor: staleAccess);
                    await using (var restore = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                    {
                        var p = await restore.Productos.SingleAsync(x => x.Id == productId);
                        p.TipoInventario = TipoInventario.MercaderiaVenta;
                        await restore.SaveChangesAsync();
                    }
                    var cached = await stale.Productos.SingleAsync(x => x.Id == productId);
                    Assert.Equal(TipoInventario.MercaderiaVenta, cached.TipoInventario);
                    await using (var writer = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                    {
                        var p = await writer.Productos.SingleAsync(x => x.Id == productId);
                        p.TipoInventario = TipoInventario.InsumoAdministrativo;
                        await writer.SaveChangesAsync();
                    }
                    var service = Compose(stale, staleAccess);
                    Func<Task> operation = operationKind switch
                    {
                        0 => () => service.CrearAsync(new CreateCotizacionDto { ClienteId = clientId, Detalles = [new CreateCotizacionDetalleDto { ProductoId = productId, ProductoVarianteId = variantId, Cantidad = 1m, PrecioUnitario = 25m }] }),
                        1 => () => service.ActualizarAsync(new UpdateCotizacionDto { Id = draftId, ClienteId = clientId, Detalles = [new UpdateCotizacionDetalleDto { Id = lineId, ProductoId = productId, ProductoVarianteId = variantId, Cantidad = 2m, PrecioUnitario = 25m }] }),
                        _ => () => service.DuplicarComoBorradorAsync(own.QuoteId)
                    };
                    var error = await Assert.ThrowsAsync<BusinessRuleException>(operation);
                    Assert.Contains("no está habilitado para cotización comercial", error.Message);
                    await using var verify = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant));
                    Assert.Equal(3, await verify.Cotizaciones.CountAsync());
                    var draft = await verify.Cotizaciones.AsNoTracking().Include(x => x.Detalles).SingleAsync(x => x.Id == draftId);
                    Assert.Equal(1m, draft.Detalles.Single().Cantidad);
                    Assert.Equal(0, await verify.RegistrosAuditoria.CountAsync(x => x.Entidad == nameof(Cotizacion) && x.ReferenciaId == draftId && x.Accion == AccionPermiso.Editar));
                    Assert.Equal(1, await verify.RegistrosAuditoria.CountAsync(x => x.Entidad == nameof(Cotizacion) && x.Accion == AccionPermiso.Duplicar));
                }

                // El precio comercial tampoco puede provenir de una entidad rastreada antigua.
                await using (var restore = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                {
                    var product = await restore.Productos.SingleAsync(x => x.Id == productId);
                    product.TipoInventario = TipoInventario.MercaderiaVenta;
                    var variant = await restore.ProductoVariantes.SingleAsync(x => x.Id == variantId);
                    variant.Precio = 25m;
                    await restore.SaveChangesAsync();
                }
                var stalePriceAccess = Access(tenant);
                await using var stalePrice = new AppDbContext(options, tenantHttpContextAccessor: stalePriceAccess);
                var cachedVariant = await stalePrice.ProductoVariantes.Include(x => x.Marca).Include(x => x.Modelo)
                    .Include(x => x.Color).Include(x => x.Talla).SingleAsync(x => x.Id == variantId);
                Assert.Equal<decimal?>(25m, cachedVariant.Precio);
                var oldLabels = (cachedVariant.Marca!.Nombre, cachedVariant.Modelo!.Nombre,
                    cachedVariant.Color!.Nombre, cachedVariant.Talla!.Nombre);
                var currentLabels = (oldLabels.Item1 + " vigente", oldLabels.Item2 + " vigente",
                    oldLabels.Item3 + " vigente", oldLabels.Item4 + " vigente");
                await using (var writer = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                {
                    var variant = await writer.ProductoVariantes.Include(x => x.Marca).Include(x => x.Modelo)
                        .Include(x => x.Color).Include(x => x.Talla).SingleAsync(x => x.Id == variantId);
                    variant.Precio = 80m;
                    variant.Marca!.Nombre = currentLabels.Item1;
                    variant.Modelo!.Nombre = currentLabels.Item2;
                    variant.Color!.Nombre = currentLabels.Item3;
                    variant.Talla!.Nombre = currentLabels.Item4;
                    await writer.SaveChangesAsync();
                }
                var freshQuote = await Compose(stalePrice, stalePriceAccess).CrearAsync(new CreateCotizacionDto
                {
                    ClienteId = clientId,
                    Detalles = [new CreateCotizacionDetalleDto { ProductoId = productId, ProductoVarianteId = variantId, Cantidad = 1m, PrecioUnitario = 25m }]
                });
                Assert.Equal(80m, freshQuote.Detalles.Single().PrecioUnitario);
                var freshLine = freshQuote.Detalles.Single();
                Assert.Equal(currentLabels, (freshLine.ProductoMarcaSnapshot, freshLine.ProductoModeloSnapshot,
                    freshLine.ProductoColorSnapshot, freshLine.ProductoTallaSnapshot));
                Assert.Equal(oldLabels, (cachedVariant.Marca!.Nombre, cachedVariant.Modelo!.Nombre,
                    cachedVariant.Color!.Nombre, cachedVariant.Talla!.Nombre));
                var edited = await Compose(stalePrice, stalePriceAccess).ActualizarAsync(new UpdateCotizacionDto
                {
                    Id = draftId, ClienteId = clientId, Detalles = [new UpdateCotizacionDetalleDto
                    { Id = lineId, ProductoId = productId, ProductoVarianteId = variantId, Cantidad = 1m, PrecioUnitario = 25m }]
                });
                var editedLine = edited.Detalles.Single();
                Assert.Equal(80m, editedLine.PrecioUnitario);
                Assert.Equal(currentLabels, (editedLine.ProductoMarcaSnapshot, editedLine.ProductoModeloSnapshot,
                    editedLine.ProductoColorSnapshot, editedLine.ProductoTallaSnapshot));
                await using (var verify = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                {
                    Assert.Equal(4, await verify.Cotizaciones.CountAsync());
                    var persisted = await verify.Cotizaciones.AsNoTracking().Include(x => x.Detalles).SingleAsync(x => x.Id == freshQuote.Id);
                    Assert.Equal(80m, persisted.Detalles.Single().PrecioUnitario);
                    var persistedEdit = await verify.Cotizaciones.AsNoTracking().Include(x => x.Detalles).SingleAsync(x => x.Id == draftId);
                    foreach (var line in new[] { persisted.Detalles.Single(), persistedEdit.Detalles.Single() })
                    {
                        Assert.Equal(currentLabels, (line.ProductoMarcaSnapshot, line.ProductoModeloSnapshot,
                            line.ProductoColorSnapshot, line.ProductoTallaSnapshot));
                    }
                    var historical = await verify.Cotizaciones.AsNoTracking().Include(x => x.Detalles)
                        .Where(x => x.Id == own.QuoteId || x.Id == (tenant == a ? copyA : copyB)).ToListAsync();
                    Assert.Equal(2, historical.Count);
                    Assert.All(historical.SelectMany(x => x.Detalles), line => Assert.Equal(oldLabels,
                        (line.ProductoMarcaSnapshot, line.ProductoModeloSnapshot, line.ProductoColorSnapshot, line.ProductoTallaSnapshot)));
                    Assert.Equal(1, await verify.RegistrosAuditoria.CountAsync(x => x.Entidad == nameof(Cotizacion) && x.ReferenciaId == freshQuote.Id && x.Accion == AccionPermiso.Crear));
                    Assert.Equal(1, await verify.RegistrosAuditoria.CountAsync(x => x.Entidad == nameof(Cotizacion) && x.ReferenciaId == draftId && x.Accion == AccionPermiso.Editar));
                }

                // Carrera real: dos conexiones abiertas a la vez. El escritor conserva
                // el lock hasta agotar los reintentos de UnitOfWork; sin sleeps de prueba.
                foreach (var operationKind in new[] { 0, 1, 2 })
                {
                    int quotesBefore, receiptsBefore;
                    await using (var restore = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                    {
                        var product = await restore.Productos.SingleAsync(x => x.Id == productId);
                        product.TipoInventario = TipoInventario.MercaderiaVenta;
                        await restore.SaveChangesAsync();
                        quotesBefore = await restore.Cotizaciones.CountAsync();
                        receiptsBefore = await restore.RegistrosAuditoria.CountAsync(x =>
                            x.EmpresaId == tenant && x.Entidad == nameof(Cotizacion));
                    }
                    Func<CotizacionService, Task> attempt = operationKind switch
                    {
                        0 => service => service.CrearAsync(new CreateCotizacionDto
                        {
                            ClienteId = clientId, Detalles = [new CreateCotizacionDetalleDto
                            {
                                ProductoId = productId, ProductoVarianteId = variantId,
                                Cantidad = 1m, PrecioUnitario = 25m
                            }]
                        }),
                        1 => service => service.ActualizarAsync(new UpdateCotizacionDto
                        {
                            Id = draftId, ClienteId = clientId, Detalles = [new UpdateCotizacionDetalleDto
                            {
                                Id = lineId, ProductoId = productId, ProductoVarianteId = variantId,
                                Cantidad = 2m, PrecioUnitario = 25m
                            }]
                        }),
                        _ => service => service.DuplicarComoBorradorAsync(own.QuoteId)
                    };
                    await using (var writer = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                    {
                        await using var writerTx = await writer.Database.BeginTransactionAsync();
                        var product = await writer.Productos.SingleAsync(x => x.Id == productId);
                        product.TipoInventario = TipoInventario.InsumoAdministrativo;
                        await writer.SaveChangesAsync(); // UPDATE real, sin COMMIT todavía.

                        var blockedAccess = Access(tenant);
                        await using (var blocked = new AppDbContext(options, tenantHttpContextAccessor: blockedAccess))
                        {
                            await blocked.Database.OpenConnectionAsync();
                            try
                            {
                                await blocked.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 1");
                                var error = await Record.ExceptionAsync(() => attempt(Compose(blocked, blockedAccess)));
                                Assert.NotNull(error);
                                var lockTimeout = false;
                                for (Exception? current = error; current is not null; current = current.InnerException)
                                    if (current is MySqlException mysql && mysql.Number == 1205)
                                        lockTimeout = true;
                                Assert.True(lockTimeout, "Se requiere timeout real MySQL 1205 mientras el escritor retiene el lock.");
                            }
                            finally
                            {
                                await blocked.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = DEFAULT");
                                await blocked.Database.CloseConnectionAsync();
                            }
                        }
                        await writerTx.CommitAsync();
                    }
                    // Tras el COMMIT rival, una transacción fresca debe rechazar el nuevo estado.
                    var freshAccess = Access(tenant);
                    await using (var fresh = new AppDbContext(options, tenantHttpContextAccessor: freshAccess))
                    {
                        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => attempt(Compose(fresh, freshAccess)));
                        Assert.Contains("no está habilitado para cotización comercial", error.Message);
                    }
                    await using (var verify = new AppDbContext(options, tenantHttpContextAccessor: Access(tenant)))
                    {
                        Assert.Equal(quotesBefore, await verify.Cotizaciones.CountAsync());
                        Assert.Equal(receiptsBefore, await verify.RegistrosAuditoria.CountAsync(x =>
                            x.EmpresaId == tenant && x.Entidad == nameof(Cotizacion)));
                        var draft = await verify.Cotizaciones.AsNoTracking().Include(x => x.Detalles).SingleAsync(x => x.Id == draftId);
                        Assert.Equal(1m, draft.Detalles.Single().Cantidad);
                        Assert.Equal(EstadoCotizacion.Borrador, draft.Estado);
                    }
                }
            }
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
