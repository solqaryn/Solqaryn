using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class TiendaBootstrapServiceTests
{
    [Fact]
    public async Task ObtenerAsync_ComponeContratoPublicoMinimoEnUnSoloCasoDeUso()
    {
        var empresa = new Mock<IEmpresaConfiguracionService>(MockBehavior.Strict);
        empresa.Setup(x => x.GetActivaAsync()).ReturnsAsync(new EmpresaConfiguracionDto
        {
            NombreComercial = "Storefront",
            Eslogan = "Compra fácil",
            Correo = "ventas@example.com",
            Telefono = "+50422000000",
            WhatsApp = null,
            LogoUrl = "https://cdn.example/logo.png",
            Moneda = "HNL",
            EncabezadoActivo = true,
            EncabezadoTexto = "Bienvenido",
            PiePaginaActivo = true,
            PiePaginaTexto = "Tienda",
            Copyright = "© 2026 Storefront",
            MostrarCopyright = true,
            UsarAnioAutomaticoCopyright = true
        });

        var tema = new Mock<ITemaVisualService>(MockBehavior.Strict);
        tema.Setup(x => x.GetAsync()).ReturnsAsync(new TemaVisualDto
        {
            ColorPrimario = "#111111",
            ColorSecundario = "#222222",
            ColorAcento = "#333333",
            FondoPrincipal = "#ffffff",
            FondoTarjetas = "#ffffff",
            MenuLateral = "#111111",
            BarraSuperior = "#ffffff",
            Encabezados = "#111111",
            BotonesPrincipales = "#111111",
            TextoPrincipal = "#111111",
            TextoSecundario = "#444444",
            ColorExito = "#008000",
            ColorAdvertencia = "#ffaa00",
            ColorError = "#cc0000",
            ColorInformacion = "#0066cc"
        });

        var categorias = new Mock<ICategoriaService>(MockBehavior.Strict);
        categorias.Setup(x => x.GetActivasAsync()).ReturnsAsync(new List<CategoriaDto>
        {
            new() { Id = 2, Nombre = "Audio", Descripcion = "Sonido", Activa = true },
            new() { Id = 1, Nombre = "Accesorios", Descripcion = "Complementos", Activa = true },
            new() { Id = 3, Nombre = "Cámaras", Activa = true },
            new() { Id = 4, Nombre = "Drones", Activa = true },
            new() { Id = 5, Nombre = "Gaming", Activa = true },
            new() { Id = 6, Nombre = "Hogar", Activa = true },
            new() { Id = 7, Nombre = "Zulu", Activa = true },
            new() { Id = 9, Nombre = "Oculta", Activa = false }
        });

        var catalogo = new Mock<ICatalogoPublicoService>(MockBehavior.Strict);
        catalogo.Setup(x => x.ObtenerDestacadosAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TiendaProductoResumenDto>
            {
                new() { Id = 7, Slug = "laptop-7", Nombre = "Laptop", EsDestacado = true, Precio = 100m }
            });

        var whatsApp = new Mock<IWhatsAppPublicoService>(MockBehavior.Strict);
        whatsApp.Setup(x => x.ObtenerAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WhatsAppPublicoDto("+50499999999", true));

        var sut = new TiendaBootstrapService(
            empresa.Object,
            tema.Object,
            categorias.Object,
            catalogo.Object,
            whatsApp.Object);

        var resultado = await sut.ObtenerAsync();

        Assert.Equal("Storefront", resultado.Identidad.NombreComercial);
        Assert.Equal("+50499999999", resultado.Identidad.WhatsApp);
        Assert.Equal("#111111", resultado.Tema.ColorPrimario);
        Assert.Equal(6, resultado.Categorias.Count);
        Assert.Equal("Accesorios", resultado.Categorias[0].Nombre);
        Assert.DoesNotContain(resultado.Categorias, categoria => categoria.Nombre == "Zulu");
        Assert.Null(resultado.Categorias[0].TotalProductos);
        Assert.Equal("laptop-7", Assert.Single(resultado.Destacados).Slug);
        empresa.VerifyAll();
        tema.VerifyAll();
        categorias.VerifyAll();
        catalogo.VerifyAll();
        whatsApp.VerifyAll();
    }
}
