using System.Security.Cryptography;
using System.Text;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class TiendaCuentaClienteTests
{
    [Fact]
    public async Task Registrar_HasheaPasswordYToken_SinReusarCredencialAdministrativa()
    {
        await using var db = CrearDb();
        var resolver = new Mock<ITipoClientePredeterminadoResolver>();
        resolver.Setup(x => x.ResolverIdPredeterminadoAsync()).ReturnsAsync(1);
        var controller = CrearController(db, resolver.Object);

        var result = await controller.Registrar(new TiendaCuentaRegistroDto
        {
            Nombre = "Cliente Web",
            Correo = "Cliente.Web@Example.com",
            Clave = "ClaveSegura123!"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<TiendaCuentaSesionDto>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Matches("^[A-Za-z0-9_-]{32,128}$", response.Data!.Token);

        var cuenta = await db.TiendaCuentasCliente.SingleAsync();
        Assert.Equal("cliente.web@example.com", cuenta.CorreoNormalizado);
        Assert.NotEqual("ClaveSegura123!", cuenta.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("ClaveSegura123!", cuenta.PasswordHash));

        var sesion = await db.TiendaSesionesCliente.SingleAsync();
        Assert.Equal(64, sesion.TokenHash.Length);
        Assert.NotEqual(response.Data.Token, sesion.TokenHash);
        Assert.Equal(HashToken(response.Data.Token), sesion.TokenHash);
    }

    [Fact]
    public async Task Registrar_CorreoMayorAlLimiteErp_RetornaBadRequestSinCrearCliente()
    {
        await using var db = CrearDb();
        var resolver = new Mock<ITipoClientePredeterminadoResolver>();
        resolver.Setup(x => x.ResolverIdPredeterminadoAsync()).ReturnsAsync(1);
        var controller = CrearController(db, resolver.Object);
        var correoLargo = $"{new string('a', 64)}@{new string('b', 63)}.{new string('c', 30)}";
        Assert.True(correoLargo.Length > 150);

        var result = await controller.Registrar(new TiendaCuentaRegistroDto
        {
            Nombre = "Cliente Correo Largo",
            Correo = correoLargo,
            Clave = "ClaveSegura123!"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await db.Clientes.ToListAsync());
        Assert.Empty(await db.TiendaCuentasCliente.ToListAsync());
    }

    [Fact]
    public async Task Pedido_DeOtroCliente_NoSePuedeConsultarPorId()
    {
        await using var db = CrearDb();

        var clienteA = new Cliente { Nombre = "Cliente A", Correo = "a@example.com", Activo = true, TipoClienteId = 1 };
        var clienteB = new Cliente { Nombre = "Cliente B", Correo = "b@example.com", Activo = true, TipoClienteId = 1 };
        db.Clientes.AddRange(clienteA, clienteB);
        await db.SaveChangesAsync();

        var cuentaA = new TiendaCuentaCliente
        {
            ClienteId = clienteA.Id,
            Cliente = clienteA,
            Nombre = "Cliente A",
            Correo = "a@example.com",
            CorreoNormalizado = "a@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ClaveSegura123!"),
            Activa = true
        };
        db.TiendaCuentasCliente.Add(cuentaA);
        await db.SaveChangesAsync();

        const string token = "token_cliente_a_0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        db.TiendaSesionesCliente.Add(new TiendaSesionCliente
        {
            CuentaClienteId = cuentaA.Id,
            CuentaCliente = cuentaA,
            TokenHash = HashToken(token),
            ExpiraUtc = DateTime.UtcNow.AddHours(1)
        });

        var pedidoAjeno = new PedidoVenta();
        AsignarPrivado(pedidoAjeno, nameof(PedidoVenta.ClienteId), clienteB.Id);
        AsignarPrivado(pedidoAjeno, nameof(PedidoVenta.Cliente), clienteB);
        AsignarPrivado(pedidoAjeno, nameof(PedidoVenta.ClienteNombreSnapshot), clienteB.Nombre);
        db.Set<PedidoVenta>().Add(pedidoAjeno);
        await db.SaveChangesAsync();

        var resolver = new Mock<ITipoClientePredeterminadoResolver>();
        var controller = CrearController(db, resolver.Object, token);

        var detalle = await controller.Pedido(pedidoAjeno.Id);
        Assert.IsType<NotFoundObjectResult>(detalle);

        var listadoResult = await controller.Pedidos();
        var ok = Assert.IsType<OkObjectResult>(listadoResult);
        var response = Assert.IsType<ApiResponse<List<TiendaPedidoCuentaDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Data!);
    }

    [Fact]
    public async Task AgregarFavorito_ProductoEliminado_RetornaNotFound()
    {
        await using var db = CrearDb();

        var cliente = new Cliente { Nombre = "Cliente A", Correo = "a@example.com", Activo = true, TipoClienteId = 1 };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var cuenta = new TiendaCuentaCliente
        {
            ClienteId = cliente.Id,
            Cliente = cliente,
            Nombre = "Cliente A",
            Correo = "a@example.com",
            CorreoNormalizado = "a@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ClaveSegura123!"),
            Activa = true
        };
        db.TiendaCuentasCliente.Add(cuenta);
        await db.SaveChangesAsync();

        const string token = "token_favorito_0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        db.TiendaSesionesCliente.Add(new TiendaSesionCliente
        {
            CuentaClienteId = cuenta.Id,
            CuentaCliente = cuenta,
            TokenHash = HashToken(token),
            ExpiraUtc = DateTime.UtcNow.AddHours(1)
        });

        var producto = new Producto
        {
            Nombre = "Producto eliminado",
            Marca = "Marca",
            Modelo = "Modelo",
            Activo = true,
            Eliminado = true
        };
        db.Productos.Add(producto);
        await db.SaveChangesAsync();

        var resolver = new Mock<ITipoClientePredeterminadoResolver>();
        var controller = CrearController(db, resolver.Object, token);

        var result = await controller.AgregarFavorito(producto.Id);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(await db.TiendaFavoritosCliente.ToListAsync());
    }

    [Fact]
    public async Task EndpointsPrivados_SinSesionCliente_Retornan401()
    {
        await using var db = CrearDb();
        var resolver = new Mock<ITipoClientePredeterminadoResolver>();
        var controller = CrearController(db, resolver.Object);

        Assert.IsType<UnauthorizedObjectResult>(await controller.Perfil());
        Assert.IsType<UnauthorizedObjectResult>(await controller.Direcciones());
        Assert.IsType<UnauthorizedObjectResult>(await controller.Favoritos());
        Assert.IsType<UnauthorizedObjectResult>(await controller.Pedidos());
        Assert.IsType<UnauthorizedObjectResult>(await controller.Notificaciones());
    }

    private static AppDbContext CrearDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"fase12-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static TiendaCuentaController CrearController(
        AppDbContext db,
        ITipoClientePredeterminadoResolver resolver,
        string? token = null)
    {
        var controller = new TiendaCuentaController(db, resolver)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        if (!string.IsNullOrWhiteSpace(token))
            controller.Request.Headers["X-Storefront-Session"] = token;
        return controller;
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    private static void AsignarPrivado<T>(T entity, string property, object? value)
    {
        var info = typeof(T).GetProperty(property)
            ?? throw new InvalidOperationException($"Propiedad {property} no encontrada.");
        info.SetValue(entity, value);
    }
}
