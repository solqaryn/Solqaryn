using System.Security.Cryptography;
using System.Text;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class FacturaCompartirServiceTests
{
    private readonly Mock<IFacturaCompartirRepository> _repository = new();
    private readonly Mock<IFacturaService> _facturaService = new();
    private readonly Mock<IFacturaPdfService> _pdfService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly IConfiguration _configuration;
    private readonly FacturaCompartirService _service;

    public FacturaCompartirServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:BackendPublicUrl"] = "https://api.storefront.test",
                ["AppSettings:EnlacePublicoFacturaHorasValidez"] = "24",
                ["AppSettings:EnlacePublicoFacturaMaximoAccesos"] = "3",
                ["AppSettings:CorreoFacturaIdempotenciaMinutos"] = "15"
            })
            .Build();

        _currentUser.Setup(c => c.UsuarioId).Returns(7);
        _currentUser.Setup(c => c.NombreUsuario).Returns("admin");
        _repository.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _repository.Setup(r => r.AddHistorialAsync(It.IsAny<HistorialEnvioFactura>())).Returns(Task.CompletedTask);

        _service = new FacturaCompartirService(
            _repository.Object,
            _facturaService.Object,
            _pdfService.Object,
            _emailService.Object,
            _auditoria.Object,
            _currentUser.Object,
            _configuration,
            new HttpContextAccessor());
    }

    [Fact]
    public async Task RegistrarIntentoAsync_WhatsApp_Enmascara_El_Numero_Normalizado_En_Persistencia()
    {
        var factura = CrearFactura();
        HistorialEnvioFactura? guardado = null;
        _facturaService.Setup(s => s.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        _repository.Setup(r => r.AddHistorialAsync(It.IsAny<HistorialEnvioFactura>()))
            .Callback<HistorialEnvioFactura>(h => guardado = h)
            .Returns(Task.CompletedTask);

        await _service.RegistrarIntentoAsync(factura.Id, new RegistrarEnvioDto
        {
            Canal = "WhatsApp",
            Destinatario = "50499999999",
            Resultado = WhatsAppSharePolicy.ClientOpenRequested
        });

        Assert.NotNull(guardado);
        Assert.Equal("*******9999", guardado!.Destinatario);
        Assert.DoesNotContain("50499999999", guardado.Destinatario, StringComparison.Ordinal);
        Assert.Equal(WhatsAppSharePolicy.ClientOpenRequested, guardado.Resultado);
        Assert.DoesNotContain("SENT", guardado.Resultado, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELIVERED", guardado.Resultado, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("READ", guardado.Resultado, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarIntentoAsync_WhatsApp_Rechaza_Numero_PreEnmascarado()
    {
        var factura = CrearFactura();
        _facturaService.Setup(s => s.GetByIdAsync(factura.Id)).ReturnsAsync(factura);

        var error = await Assert.ThrowsAsync<BusinessRuleException>(() => _service.RegistrarIntentoAsync(
            factura.Id,
            new RegistrarEnvioDto
            {
                Canal = "WhatsApp",
                Destinatario = "*******9999",
                Resultado = WhatsAppSharePolicy.ClientOpenRequested
            }));

        Assert.Contains("teléfono", error.Message, StringComparison.OrdinalIgnoreCase);
        _repository.Verify(r => r.AddHistorialAsync(It.IsAny<HistorialEnvioFactura>()), Times.Never);
    }

    [Fact]
    public async Task PrepararCompartirAsync_Persiste_Solo_Hash_Del_Token()
    {
        var factura = CrearFactura();
        EnlacePublicoFactura? guardado = null;

        _facturaService.Setup(s => s.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        _repository.Setup(r => r.ExpirarVigentesAsync(factura.Id, It.IsAny<DateTime>())).ReturnsAsync(2);
        _repository.Setup(r => r.AddEnlaceAsync(It.IsAny<EnlacePublicoFactura>()))
            .Callback<EnlacePublicoFactura>(e => guardado = e)
            .Returns(Task.CompletedTask);

        var resultado = await _service.PrepararCompartirAsync(factura.Id);

        Assert.NotNull(guardado);
        Assert.Equal(64, guardado!.Token.Length);
        Assert.Matches("^[0-9A-F]{64}$", guardado.Token);
        Assert.DoesNotContain(guardado.Token, resultado.UrlPdfPublica, StringComparison.Ordinal);

        var tokenPublico = resultado.UrlPdfPublica
            .Split("/facturas/publico/", StringSplitOptions.None)[1]
            .Split("/pdf", StringSplitOptions.None)[0];
        var hashEsperado = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenPublico)));

        Assert.Equal(hashEsperado, guardado.Token);
        Assert.Equal(7, guardado.CreadoPorUsuarioId);
        Assert.True(guardado.FechaExpiracion > guardado.FechaCreacion);
        _repository.Verify(r => r.ExpirarVigentesAsync(factura.Id, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ObtenerPdfPorTokenAsync_Enlace_Expirado_No_Genera_Pdf()
    {
        const string token = "token-publico-seguro-con-mas-de-32-caracteres-123";
        _repository.Setup(r => r.GetPorTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(new EnlacePublicoFactura
            {
                Id = 1,
                FacturaId = 15,
                Token = "HASH",
                FechaExpiracion = DateTime.UtcNow.AddMinutes(-1)
            });

        var resultado = await _service.ObtenerPdfPorTokenAsync(token);

        Assert.Null(resultado);
        _facturaService.Verify(s => s.GetByIdAsync(It.IsAny<int>()), Times.Never);
        _pdfService.Verify(s => s.GenerarPdfAsync(It.IsAny<FacturaDto>()), Times.Never);
    }

    [Fact]
    public async Task ObtenerPdfPorTokenAsync_Respeta_Limite_De_Accesos()
    {
        const string token = "token-publico-seguro-con-mas-de-32-caracteres-456";
        _repository.Setup(r => r.GetPorTokenHashAsync(It.IsAny<string>()))
            .ReturnsAsync(new EnlacePublicoFactura
            {
                Id = 2,
                FacturaId = 16,
                Token = "HASH",
                FechaExpiracion = DateTime.UtcNow.AddHours(1),
                VecesAccedido = 3
            });

        var resultado = await _service.ObtenerPdfPorTokenAsync(token);

        Assert.Null(resultado);
        _pdfService.Verify(s => s.GenerarPdfAsync(It.IsAny<FacturaDto>()), Times.Never);
    }

    [Fact]
    public async Task RevocarEnlacesAsync_Adelanta_Expiracion_Y_Conserva_Historial()
    {
        var factura = CrearFactura();
        _facturaService.Setup(s => s.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        _repository.Setup(r => r.ExpirarVigentesAsync(factura.Id, It.IsAny<DateTime>())).ReturnsAsync(3);

        var revocados = await _service.RevocarEnlacesAsync(factura.Id);

        Assert.Equal(3, revocados);
        _repository.Verify(r => r.ExpirarVigentesAsync(factura.Id, It.IsAny<DateTime>()), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(), Times.Once);
        _repository.Verify(r => r.AddEnlaceAsync(It.IsAny<EnlacePublicoFactura>()), Times.Never);
    }

    [Fact]
    public async Task EnviarPorCorreoAsync_Adjunta_Pdf_Y_Registra_Intentos()
    {
        var factura = CrearFactura();
        var pdf = Encoding.UTF8.GetBytes("%PDF-prueba-fase7");
        _facturaService.Setup(s => s.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        _pdfService.Setup(s => s.GenerarPdfAsync(factura)).ReturnsAsync(pdf);
        _emailService.Setup(s => s.EnviarAsync(
                "cliente@example.com",
                It.Is<string>(x => x.Contains(factura.NumeroFactura)),
                It.Is<string>(x => x.Contains("PDF oficial A4")),
                It.Is<List<AdjuntoCorreo>>(x =>
                    x.Count == 1 &&
                    x[0].NombreArchivo == $"{factura.NumeroFactura}.pdf" &&
                    x[0].ContentType == "application/pdf" &&
                    x[0].Contenido.SequenceEqual(pdf)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoEntregaEmail
            {
                Exito = true,
                Codigo = "ENVIADO",
                Intentos = 2,
                MessageId = "solqaryn-test"
            });

        var resultado = await _service.EnviarPorCorreoAsync(
            factura.Id,
            "cliente@example.com",
            $"test-{Guid.NewGuid():N}");

        Assert.True(resultado.Exito);
        Assert.Equal(2, resultado.Intentos);
        Assert.Equal("solqaryn-test", resultado.MessageId);
        Assert.Contains("2 intentos", resultado.Mensaje);
        _repository.Verify(r => r.AddHistorialAsync(It.Is<HistorialEnvioFactura>(h =>
            h.FacturaId == factura.Id &&
            h.Canal == "Correo" &&
            h.Resultado == "Enviado (2 intentos)" &&
            h.Error == null)), Times.Once);
    }

    [Fact]
    public async Task EnviarPorCorreoAsync_Misma_Clave_No_Duplica_Envio()
    {
        var factura = CrearFactura();
        var clave = $"idem-{Guid.NewGuid():N}";
        _facturaService.Setup(s => s.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        _pdfService.Setup(s => s.GenerarPdfAsync(factura)).ReturnsAsync(Encoding.UTF8.GetBytes("%PDF-idempotente"));
        _emailService.Setup(s => s.EnviarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<List<AdjuntoCorreo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoEntregaEmail
            {
                Exito = true,
                Codigo = "ENVIADO",
                Intentos = 1,
                MessageId = "solqaryn-idem"
            });

        var primero = await _service.EnviarPorCorreoAsync(factura.Id, "cliente@example.com", clave);
        var segundo = await _service.EnviarPorCorreoAsync(factura.Id, "cliente@example.com", clave);

        Assert.True(primero.Exito);
        Assert.True(segundo.Exito);
        Assert.True(segundo.YaProcesado);
        _emailService.Verify(s => s.EnviarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<List<AdjuntoCorreo>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(r => r.AddHistorialAsync(It.IsAny<HistorialEnvioFactura>()), Times.Once);
    }

    [Fact]
    public async Task EnviarPorCorreoAsync_Propaga_Error_Transitorio_Seguro()
    {
        var factura = CrearFactura();
        _facturaService.Setup(s => s.GetByIdAsync(factura.Id)).ReturnsAsync(factura);
        _pdfService.Setup(s => s.GenerarPdfAsync(factura)).ReturnsAsync(Encoding.UTF8.GetBytes("%PDF-error"));
        _emailService.Setup(s => s.EnviarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<List<AdjuntoCorreo>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoEntregaEmail
            {
                Exito = false,
                Codigo = "SMTP_TEMPORAL",
                Error = "El servidor de correo presentó un problema temporal.",
                EsTransitorio = true,
                Intentos = 3,
                MessageId = "solqaryn-error"
            });

        var resultado = await _service.EnviarPorCorreoAsync(
            factura.Id,
            "cliente@example.com",
            $"error-{Guid.NewGuid():N}");

        Assert.False(resultado.Exito);
        Assert.True(resultado.EsTransitorio);
        Assert.Equal("SMTP_TEMPORAL", resultado.Codigo);
        Assert.Equal(3, resultado.Intentos);
        _repository.Verify(r => r.AddHistorialAsync(It.Is<HistorialEnvioFactura>(h =>
            h.Resultado == "Error" &&
            h.Error == "SMTP_TEMPORAL: El servidor de correo presentó un problema temporal.")), Times.Once);
    }

    private static FacturaDto CrearFactura() => new()
    {
        Id = 15,
        NumeroFactura = "FAC-000015",
        Estado = "Emitida",
        ClienteNombre = "Cliente prueba",
        ClienteTelefono = "99999999",
        ClienteCorreo = "cliente@example.com",
        EmpresaNombre = "Storefront",
        Total = 500m,
        FechaEmision = DateTime.UtcNow
    };
}
