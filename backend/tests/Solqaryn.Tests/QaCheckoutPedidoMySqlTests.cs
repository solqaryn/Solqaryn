using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class QaCheckoutPedidoMySqlTests
{
    [Fact]
    public async Task Checkout_ConservaPedidoReal_ClaveA_B_Y_Importes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database=test_qa_checkout_{Guid.NewGuid():N};User=root;Password=root;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        try
        {
            int a, b, tipo, producto;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var ea = new Empresa("Checkout A");
                var eb = new Empresa("Checkout B");
                setup.AddRange(ea, eb);
                if (!await setup.EmpresaConfiguraciones.AnyAsync(x => x.Activa))
                    setup.Add(new EmpresaConfiguracion { Moneda = "HNL" });
                await setup.SaveChangesAsync();
                a = ea.Id; b = eb.Id;
                await using var tx = await setup.Database.BeginTransactionAsync();
                await QaCommercialOwnershipV3.ProvisionDefaultsAsync(setup, a);
                await QaCommercialOwnershipV3.ProvisionDefaultsAsync(setup, b);
                await tx.CommitAsync();
            }
            var admin = new DefaultHttpContext();
            admin.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = a;
            var access = new Mock<IHttpContextAccessor>();
            access.SetupGet(x => x.HttpContext).Returns(admin);
            await using (var db = new AppDbContext(options, httpContextAccessor: access.Object))
            {
                tipo = (await db.TipoClientes.SingleAsync(x => x.EsPredeterminado)).Id;
                var p = new Producto { Nombre = "Producto QA checkout", Precio = 50m, Costo = 10m, Cantidad = 8 };
                db.Productos.Add(p);
                await db.SaveChangesAsync();
                producto = p.Id;
            }
            // Real tenant-owned rules: A and B have deliberately different rates.
            foreach (var tenant in new[] { a, b })
            {
                admin.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = tenant;
                await using var rules = new AppDbContext(options, httpContextAccessor: access.Object);
                rules.Descuentos.Add(new Descuento { Nombre="Promoción fiscal QA", Tipo=TipoDescuento.Porcentaje,
                    Valor=tenant==a ? 50m : 10m, Activo=true });
                rules.Impuestos.Add(new Impuesto { Nombre="Impuesto fiscal QA", Codigo="QA-ISV",
                    Tipo=TipoImpuesto.Porcentaje, Tasa=tenant==a ? 15m : 99m, Activo=true,
                    Operaciones={ new ImpuestoOperacion { Operacion=OperacionImpuesto.Venta } } });
                await rules.SaveChangesAsync();
            }
            admin.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = a;
            var resolver = new Mock<ITipoClientePredeterminadoResolver>();
            resolver.Setup(x => x.ResolverIdPredeterminadoAsync()).ReturnsAsync(tipo);
            CheckoutTiendaService ServicioCheckout(AppDbContext db, int tenant)
            {
                var productos = new Mock<IProductoService>();
                if (tenant == a)
                    productos.Setup(x => x.GetByIdAsync(producto)).ReturnsAsync(new ProductoDto
                        { Id=producto, Nombre="Producto QA checkout", Activo=true, Precio=50m, Cantidad=8 });
                var envio = new Mock<ICostoEnvioService>();
                envio.Setup(x => x.ResolverAsync(It.IsAny<ResolverCostoEnvioDto>()))
                    .ReturnsAsync(new CostoEnvioDto { Id=1, Nombre="Tarifa QA", Monto=7m, Activo=true });
                var descuentos = new DescuentoRepository(db);
                var calculo = new CalculoService(descuentos, new ImpuestoRepository(db), Mock.Of<ICostoEnvioRepository>());
                return new CheckoutTiendaService(productos.Object, new PromocionPublicaService(descuentos),
                    Mock.Of<IInventarioPublicoService>(), envio.Object, calculo);
            }
            var request = new ValidarCheckoutTiendaDto
                { Items = new() { new() { ProductoId=producto, Unidades=2 } } };
            async Task<CheckoutTiendaValidadoDto> CotizarA()
            {
                var ctx = new DefaultHttpContext();
                ctx.Items[AppDbContext.PublicStoreTenantItemKey] = a;
                ctx.Request.Path = "/tienda/checkout/validar";
                ctx.Request.Method = "POST";
                var accessor = new Mock<IHttpContextAccessor>();
                accessor.SetupGet(x => x.HttpContext).Returns(ctx);
                await using var db = new AppDbContext(options, httpContextAccessor: accessor.Object);
                return (await ServicioCheckout(db,a).ValidarAsync(request)).Datos!;
            }
            var quote = await CotizarA();
            Assert.NotNull(quote);
            Assert.Equal(50m,quote.Subtotal);
            Assert.Equal(50m,quote.Fiscal!.TotalDescuento);
            Assert.Equal(7.50m,quote.Fiscal.ImpuestoAdicional);
            Assert.Equal(64.50m,quote.Total);
            Assert.Matches("^[a-f0-9]{64}$",quote.HuellaFiscal!);
            // In B, the exact same fiscal calculator must see only B's 99% rule.
            admin.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = b;
            await using (var bd = new AppDbContext(options, httpContextAccessor: access.Object))
            {
                var cb = new CalculoService(new DescuentoRepository(bd),new ImpuestoRepository(bd),Mock.Of<ICostoEnvioRepository>());
                var lineasB = new List<DetalleCalculoInput> { new() { ProductoId=producto, Cantidad=2, PrecioUnitario=25m } };
                Assert.Equal(49.50m, Assert.Single(await cb.CalcularImpuestosCheckoutPublicoAsync(lineasB,lineasB,DateTime.UtcNow)).Monto);
            }
            admin.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = a;
            var dto = new CrearPedidoCheckoutDto
            {
                Nombre = "Invitado QA", Telefono = "99991111", Correo = "pedido@example.invalid",
                Checkout = new() { Items = new() { new() { ProductoId = producto, Unidades = 2 } } },
                SubtotalEsperado = 50m, EnvioEsperado = 7m, MonedaEsperada = "HNL", HuellaFiscalEsperada = quote.HuellaFiscal, TotalEsperado = quote.Total,
                LineasEsperadas = new() { new() { ProductoId = producto, Unidades = 2, PrecioUnitario = 25m } }
            };
            var key = new string('a', 64);
            async Task<IActionResult> Call(int tenant, string? token, int? id = null)
            {
                var ctx = new DefaultHttpContext();
                ctx.Items[AppDbContext.PublicStoreTenantItemKey] = tenant;
                ctx.Request.Path = id.HasValue ? "/tienda/cuenta/pedidos/checkout/" + id.Value : "/tienda/cuenta/pedidos/checkout";
                ctx.Request.Method = id.HasValue ? "GET" : "POST";
                if (token is not null) ctx.Request.Headers["X-Storefront-Order-Key"] = token;
                var accessor = new Mock<IHttpContextAccessor>();
                accessor.SetupGet(x => x.HttpContext).Returns(ctx);
                await using var db = new AppDbContext(options, httpContextAccessor: accessor.Object);
                var controller = new TiendaCuentaController(db, resolver.Object)
                    { ControllerContext = new ControllerContext { HttpContext = ctx } };
                return id.HasValue ? await controller.PedidoCheckout(id.Value, default) :
                    await controller.CrearPedidoCheckout(dto, ServicioCheckout(db,tenant), default);
            }
            var primera = Assert.IsType<ObjectResult>(await Call(a, key));
            Assert.Equal(201, primera.StatusCode);
            var pedido = Assert.IsType<ApiResponse<TiendaPedidoCuentaDto>>(primera.Value).Data!;
            Assert.Equal(64.50m, pedido.Total);
            Assert.Equal(50m, pedido.Fiscal!.TotalDescuento);
            Assert.Equal(7.50m, pedido.Fiscal.ImpuestoAdicional);
            Assert.Equal(50m, pedido.Subtotal);
            Assert.Equal(7m, pedido.CostoEnvio);
            Assert.Equal("Borrador", pedido.Estado);
            Assert.Single(pedido.Lineas);
            var replay = Assert.IsType<OkObjectResult>(await Call(a, key));
            Assert.Equal(pedido.Id, Assert.IsType<ApiResponse<TiendaPedidoCuentaDto>>(replay.Value).Data!.Id);
            dto.EnvioEsperado = 8m;
            Assert.IsType<ConflictObjectResult>(await Call(a, key));
            Assert.IsType<ConflictObjectResult>(await Call(a, new string('c', 64)));
            dto.EnvioEsperado = 7m;
            dto.HuellaFiscalEsperada = new string('e',64);
            Assert.IsType<ConflictObjectResult>(await Call(a, new string('d',64)));
            dto.HuellaFiscalEsperada = quote.HuellaFiscal;
            dto.TotalEsperado = 57m;
            Assert.IsType<ConflictObjectResult>(await Call(a, new string('e',64)));
            dto.TotalEsperado = quote.Total;
            await using (var cambios = new AppDbContext(options,httpContextAccessor:access.Object))
            {
                var impuesto = await cambios.Impuestos.SingleAsync(i => i.Codigo=="QA-ISV");
                impuesto.Tasa=20m;
                await cambios.SaveChangesAsync();
            }
            var actualizada = await CotizarA();
            Assert.Equal(67m,actualizada.Total);
            Assert.NotEqual(quote.HuellaFiscal,actualizada.HuellaFiscal);
            Assert.IsType<ConflictObjectResult>(await Call(a,new string('f',64)));
            var mismo = Assert.IsType<ApiResponse<TiendaPedidoCuentaDto>>(Assert.IsType<OkObjectResult>(await Call(a,key)).Value).Data!;
            Assert.Equal(64.50m,mismo.Total);
            var recuperado = Assert.IsType<ApiResponse<TiendaPedidoCuentaDto>>(Assert.IsType<OkObjectResult>(await Call(a, key, pedido.Id)).Value).Data!;
            Assert.Equal(64.50m, recuperado.Total);
            Assert.Equal(7.50m, recuperado.Fiscal!.ImpuestoAdicional);
            Assert.IsType<UnauthorizedObjectResult>(await Call(a, null, pedido.Id));
            Assert.IsType<NotFoundObjectResult>(await Call(a, new string('b', 64), pedido.Id));
            Assert.IsType<NotFoundObjectResult>(await Call(b, key, pedido.Id));
            await using var read = new AppDbContext(options);
            var stored = await read.Set<PedidoVenta>().Include(x => x.Detalles).SingleAsync(x => x.Id == pedido.Id);
            Assert.Equal(EstadoPedidoVenta.Borrador, stored.Estado);
            Assert.Equal(64.50m, stored.Total);
            Assert.Equal(50m, stored.FiscalCheckout!.TotalDescuento);
            Assert.Equal(7.50m, stored.FiscalCheckout.ImpuestoAdicional);
            Assert.Equal(7m, stored.CostoEnvio);
            Assert.Null(stored.CotizacionId);
            Assert.Single(stored.Detalles);
            Assert.NotEqual(key, stored.IdempotencyKey);
            Assert.Equal(1, await read.Set<PedidoVenta>().CountAsync(x => x.CostoEnvio != null));
            Assert.Equal(1, await read.Clientes.CountAsync(x => x.Correo == "pedido@example.invalid"));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
