using InventoryApp.Application.Interfaces;
using InventoryApp.Domain.Entities;
using InventoryApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InventoryApp.Tests;

public sealed class PublicStoreCacheInvalidationTests
{
    [Fact]
    public async Task SaveChanges_InvalidaSegmentosPublicosAfectados()
    {
        var cache = new Mock<IPublicStoreCache>(MockBehavior.Strict);
        cache.Setup(x => x.InvalidateAll(It.Is<string[]>(segments =>
            segments.Contains(PublicStoreCacheSegments.Identity)
            && segments.Contains(PublicStoreCacheSegments.Theme)
            && segments.Contains(PublicStoreCacheSegments.Categories)
            && segments.Contains(PublicStoreCacheSegments.Products)
            && segments.Contains(PublicStoreCacheSegments.Featured))));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"public-cache-{Guid.NewGuid():N}")
            .Options;

        await using var db = new AppDbContext(options, cache.Object);
        db.EmpresaConfiguraciones.Add(new EmpresaConfiguracion
        {
            NombreComercial = "Tienda",
            NombreVisibleSistema = "Tienda",
            Activa = true
        });
        db.TemasVisuales.Add(new TemaVisual());
        db.Categorias.Add(new Categoria { Nombre = "Audio", Activa = true });
        db.Productos.Add(new Producto { Nombre = "Producto", Activo = true });

        await db.SaveChangesAsync();

        cache.VerifyAll();
    }
}
