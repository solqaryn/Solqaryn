using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class ProductoImagenPrincipalTests
{
    [Fact]
    public void ImagenPrincipal_Devuelve_La_Marcada_Como_Principal()
    {
        var producto = new Producto();
        producto.Imagenes.Add(new ProductoImagen { Id = 1, Orden = 0, EsPrincipal = false, Url = "a.jpg" });
        producto.Imagenes.Add(new ProductoImagen { Id = 2, Orden = 1, EsPrincipal = true, Url = "b.jpg" });

        Assert.Equal("b.jpg", producto.ImagenPrincipal?.Url);
    }

    [Fact]
    public void ImagenPrincipal_Cae_A_La_De_Menor_Orden_Si_Ninguna_Es_Principal()
    {
        var producto = new Producto();
        producto.Imagenes.Add(new ProductoImagen { Id = 1, Orden = 1, EsPrincipal = false, Url = "segunda.jpg" });
        producto.Imagenes.Add(new ProductoImagen { Id = 2, Orden = 0, EsPrincipal = false, Url = "primera.jpg" });

        Assert.Equal("primera.jpg", producto.ImagenPrincipal?.Url);
    }

    [Fact]
    public void ImagenPrincipal_Ignora_La_Principal_De_Una_Variante()
    {
        var producto = new Producto();
        producto.Imagenes.Add(new ProductoImagen
        {
            Id = 1,
            ProductoVarianteId = 20,
            Orden = 0,
            EsPrincipal = true,
            Url = "variante.jpg"
        });
        producto.Imagenes.Add(new ProductoImagen
        {
            Id = 2,
            ProductoVarianteId = null,
            Orden = 1,
            EsPrincipal = true,
            Url = "general.jpg"
        });

        Assert.Equal("general.jpg", producto.ImagenPrincipal?.Url);
    }

    [Fact]
    public void ImagenPrincipal_Es_Null_Si_Solo_Hay_Imagenes_De_Variantes()
    {
        var producto = new Producto();
        producto.Imagenes.Add(new ProductoImagen
        {
            Id = 1,
            ProductoVarianteId = 20,
            Orden = 0,
            EsPrincipal = true,
            Url = "variante.jpg"
        });

        Assert.Null(producto.ImagenPrincipal);
    }

    [Fact]
    public void ImagenPrincipal_Es_Null_Sin_Imagenes()
    {
        var producto = new Producto();
        Assert.Null(producto.ImagenPrincipal);
    }

    [Fact]
    public void Producto_Puede_Tener_Categoria_Asignada()
    {
        var categoria = new Categoria { Id = 3, Nombre = "Audio" };
        var producto = new Producto { CategoriaId = categoria.Id, Categoria = categoria };

        Assert.Equal("Audio", producto.Categoria?.Nombre);
    }
}
