using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class M8BusquedaRepositoriosTests
{
    [Fact]
    public async Task VentaPaged_BuscaTelefonoYNotas_YNoDejaTracking()
    {
        await using var context = CrearContexto();
        context.Ventas.AddRange(
            new Venta
            {
                NumeroVenta = "V-001",
                ClienteNombre = "Ana López",
                ClienteTelefono = "9999-1111",
                ClienteCorreo = "ana@example.com",
                Notas = "Entrega en Plaza Central",
                Fecha = DateTime.UtcNow,
                CreadoPorUsuarioId = 9,
                CreadoPorNombreUsuario = "Admin"
            },
            new Venta
            {
                NumeroVenta = "V-002",
                ClienteNombre = "Carlos Pérez",
                ClienteTelefono = "8888-2222",
                ClienteCorreo = "carlos@example.com",
                Fecha = DateTime.UtcNow,
                CreadoPorUsuarioId = 9,
                CreadoPorNombreUsuario = "Admin"
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new VentaRepository(context, new Mock<ICurrentUserService>().Object, CrearScope());

        var porTelefono = await repo.GetPagedAsync(new PagedRequest { Search = "9999", Page = 1, PageSize = 10 });
        Assert.Single(porTelefono.Items);
        Assert.Equal("V-001", porTelefono.Items[0].NumeroVenta);
        Assert.Empty(context.ChangeTracker.Entries<Venta>());

        var porNotas = await repo.GetPagedAsync(new PagedRequest { Search = "plaza central", Page = 1, PageSize = 10 });
        Assert.Single(porNotas.Items);
        Assert.Equal("V-001", porNotas.Items[0].NumeroVenta);
    }

    [Fact]
    public async Task CompraPaged_BuscaDocumentoReferenciaYProveedor_YNoDejaTracking()
    {
        await using var context = CrearContexto();
        context.Compras.AddRange(
            new Compra
            {
                NumeroCompra = "C-001",
                ProveedorNombre = "Proveedor Uno",
                ProveedorDocumento = "0801-9999",
                ProveedorTelefono = "2233-4455",
                DocumentoReferencia = "FAC-7788",
                Notas = "Compra urgente",
                Fecha = DateTime.UtcNow,
                CreadoPorUsuarioId = 9,
                CreadoPorNombreUsuario = "Admin"
            },
            new Compra
            {
                NumeroCompra = "C-002",
                ProveedorNombre = "Proveedor Dos",
                ProveedorDocumento = "0801-1111",
                Fecha = DateTime.UtcNow,
                CreadoPorUsuarioId = 9,
                CreadoPorNombreUsuario = "Admin"
            });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new CompraRepository(context, new Mock<ICurrentUserService>().Object, CrearScope());

        var porReferencia = await repo.GetPagedAsync(new PagedRequest { Search = "fac-7788", Page = 1, PageSize = 10 });
        Assert.Single(porReferencia.Items);
        Assert.Equal("C-001", porReferencia.Items[0].NumeroCompra);
        Assert.Empty(context.ChangeTracker.Entries<Compra>());

        var porDocumento = await repo.GetPagedAsync(new PagedRequest { Search = "0801-9999", Page = 1, PageSize = 10 });
        Assert.Single(porDocumento.Items);
        Assert.Equal("C-001", porDocumento.Items[0].NumeroCompra);
    }

    [Fact]
    public async Task ClienteAutocomplete_AplicaLimiteMaximo30_YNoDejaTracking()
    {
        await using var context = CrearContexto();
        var tipo = new TipoCliente
        {
            Codigo = "M8",
            Nombre = "M8",
            NombreNormalizado = "M8",
            Activo = true,
            CreadoPorUsuarioId = 9,
            CreadoPorNombreUsuario = "Admin"
        };
        context.TipoClientes.Add(tipo);
        await context.SaveChangesAsync();

        for (var i = 1; i <= 35; i++)
        {
            context.Clientes.Add(new Cliente
            {
                Nombre = $"Cliente M8 {i:00}",
                Telefono = $"9999-{i:0000}",
                Activo = true,
                TipoClienteId = tipo.Id,
                CreadoPorUsuarioId = 9,
                CreadoPorNombreUsuario = "Admin"
            });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ClienteRepository(context);
        var encontrados = await repo.BuscarActivosAsync("Cliente M8", 200);

        Assert.Equal(30, encontrados.Count);
        Assert.Empty(context.ChangeTracker.Entries<Cliente>());
    }

    [Fact]
    public async Task ProveedorAutocomplete_AplicaLimiteMaximo30_YNoDejaTracking()
    {
        await using var context = CrearContexto();
        for (var i = 1; i <= 35; i++)
        {
            context.Proveedores.Add(new Proveedor
            {
                Nombre = $"Proveedor M8 {i:00}",
                Telefono = $"2233-{i:0000}",
                Activo = true,
                CreadoPorUsuarioId = 9,
                CreadoPorNombreUsuario = "Admin"
            });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var repo = new ProveedorRepository(context);
        var encontrados = await repo.BuscarActivosAsync("Proveedor M8", 200);

        Assert.Equal(30, encontrados.Count);
        Assert.Empty(context.ChangeTracker.Entries<Proveedor>());
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"m8-busqueda-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static IUsuarioScopeService CrearScope()
    {
        var scope = new Mock<IUsuarioScopeService>();
        scope.Setup(s => s.ObtenerActualAsync())
            .ReturnsAsync(new UsuarioScopeActual(9, 1, "Admin", true));
        return scope.Object;
    }
}
