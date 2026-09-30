using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class ProductoVarianteTecnicaLifecycleIntegrationTests
{
    private static string GetConnectionString(string dbName) =>
        $"Server=localhost;Port=3306;Database={dbName};User=root;Password=root;";

    private static DbContextOptions<AppDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                GetConnectionString(dbName),
                new MySqlServerVersion(new Version(8, 4, 3)))
            .Options;

    [Fact]
    public async Task AsegurarTecnica_Concurrente_CreaExactamenteUnaVariante()
    {
        var dbName = $"test_tecnica_lifecycle_{Guid.NewGuid():N}";
        var options = CreateOptions(dbName);
        int productoId;

        await using (var setup = new AppDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var producto = new Producto
            {
                Nombre = "Producto simple concurrente",
                Marca = "Solqaryn",
                Modelo = "2C2",
                Cantidad = 7,
                Costo = 45m,
                Precio = 80m,
                UmbralStockBajo = 2,
                Activo = true,
                Eliminado = false
            };
            setup.Productos.Add(producto);
            await setup.SaveChangesAsync();
            productoId = producto.Id;
        }

        await using var contextA = new AppDbContext(options);
        await using var contextB = new AppDbContext(options);

        var usuario = new Mock<ICurrentUserService>();
        usuario.SetupGet(x => x.UsuarioId).Returns(1);
        usuario.SetupGet(x => x.NombreUsuario).Returns("integration");
        var catalogos = new Mock<ICatalogoProductoService>();
        var auditoria = new Mock<IAuditoriaService>();

        static ProductoVarianteService CrearServicio(
            AppDbContext context,
            Mock<ICatalogoProductoService> catalogos,
            Mock<ICurrentUserService> usuario,
            Mock<IAuditoriaService> auditoria) =>
            new(
                new ProductoVarianteRepository(context),
                new ProductoRepository(context),
                catalogos.Object,
                usuario.Object,
                new UnitOfWork(context),
                auditoria.Object);

        var serviceA = CrearServicio(contextA, catalogos, usuario, auditoria);
        var serviceB = CrearServicio(contextB, catalogos, usuario, auditoria);
        var inicio = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var tareaA = Task.Run(async () =>
        {
            await inicio.Task;
            return await serviceA.AsegurarTecnicaAsync(productoId);
        });
        var tareaB = Task.Run(async () =>
        {
            await inicio.Task;
            return await serviceB.AsegurarTecnicaAsync(productoId);
        });

        inicio.SetResult(true);
        await Task.WhenAll(tareaA, tareaB).WaitAsync(TimeSpan.FromSeconds(30));

        await using var verify = new AppDbContext(options);
        try
        {
            var tecnicas = await verify.ProductoVariantes
                .IgnoreQueryFilters()
                .Where(v => v.ProductoId == productoId && v.EsTecnica && !v.Eliminado)
                .AsNoTracking()
                .ToListAsync();

            var tecnica = Assert.Single(tecnicas);
            Assert.Null(tecnica.ColorId);
            Assert.Equal($"TEC-{productoId:D10}", tecnica.Sku);
            Assert.Equal(7, tecnica.Cantidad);
            Assert.Equal(45m, tecnica.Costo);
            Assert.Equal(80m, tecnica.Precio);
            Assert.Equal(2, tecnica.UmbralStockBajo);
            Assert.True(tecnica.Activo);
        }
        finally
        {
            await verify.Database.EnsureDeletedAsync();
        }
    }
}
