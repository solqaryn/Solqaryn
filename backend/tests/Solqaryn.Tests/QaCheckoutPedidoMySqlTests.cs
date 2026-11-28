using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
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
                var p = new Producto { Nombre = "Producto QA checkout", Precio = 25m, Costo = 10m, Cantidad = 8 };
                db.Productos.Add(p);
                await db.SaveChangesAsync();
                producto = p.Id;
            }
            var resolver = new Mock<ITipoClientePredeterminadoResolver>();
            resolver.Setup(x => x.ResolverIdPredeterminadoAsync()).ReturnsAsync(tipo);
            var checkout = new Mock<ICheckoutTiendaService>();
            checkout.Setup(x => x.ValidarAsync(It.IsAny<ValidarCheckoutTiendaDto>()))
                .ReturnsAsync(CheckoutTiendaResultado.Ok(new CheckoutTiendaValidadoDto
                {
                    ExpiraUtc = DateTime.UtcNow.AddHours(1), Subtotal = 50m, CostoEnvio = 7m,
                    Total = 57m, TarifaEnvio = "Tarifa QA",
                    Lineas = new() { new() { ProductoId = producto, Nombre = "Producto QA checkout",
                        Unidades = 2, PrecioUnitario = 25m, Total = 50m, StockDisponible = 8 } }
                }, "OK"));
            var dto = new CrearPedidoCheckoutDto
            {
                Nombre = "Invitado QA", Telefono = "99991111", Correo = "pedido@example.invalid",
                Checkout = new() { Items = new() { new() { ProductoId = producto, Unidades = 2 } } },
                SubtotalEsperado = 50m, EnvioEsperado = 7m, MonedaEsperada = "HNL",
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
                    await controller.CrearPedidoCheckout(dto, checkout.Object, default);
            }
            var primera = Assert.IsType<ObjectResult>(await Call(a, key));
            Assert.Equal(201, primera.StatusCode);
            var pedido = Assert.IsType<ApiResponse<TiendaPedidoCuentaDto>>(primera.Value).Data!;
            Assert.Equal(57m, pedido.Total);
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
            Assert.IsType<OkObjectResult>(await Call(a, key, pedido.Id));
            Assert.IsType<UnauthorizedObjectResult>(await Call(a, null, pedido.Id));
            Assert.IsType<NotFoundObjectResult>(await Call(a, new string('b', 64), pedido.Id));
            Assert.IsType<NotFoundObjectResult>(await Call(b, key, pedido.Id));
            await using var read = new AppDbContext(options);
            var stored = await read.Set<PedidoVenta>().Include(x => x.Detalles).SingleAsync(x => x.Id == pedido.Id);
            Assert.Equal(EstadoPedidoVenta.Borrador, stored.Estado);
            Assert.Equal(57m, stored.Total);
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
