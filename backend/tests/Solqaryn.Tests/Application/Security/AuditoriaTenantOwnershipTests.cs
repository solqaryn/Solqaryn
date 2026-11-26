using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Application.Security;

public sealed class AuditoriaTenantOwnershipTests
{
    private static (AuditoriaService Service, List<RegistroAuditoria> Auditorias) Create(HttpContext? http)
    {
        var auditorias = new List<RegistroAuditoria>();
        var repo = new Mock<IAuditoriaRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Callback<RegistroAuditoria>(auditorias.Add)
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var user = new Mock<ICurrentUserService>();
        user.SetupGet(u => u.UsuarioId).Returns(10);
        user.SetupGet(u => u.NombreUsuario).Returns("empleado-prueba");

        var accessor = new HttpContextAccessor { HttpContext = http };
        var service = new AuditoriaService(repo.Object, user.Object, accessor,
            Mock.Of<ILogger<AuditoriaService>>());
        return (service, auditorias);
    }

    [Fact]
    public async Task Marca_RBAC_verificada_se_guarda_como_propiedad_de_auditoria()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = "999";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 7;
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(ModuloSistema.Productos, AccionPermiso.Crear,
            "Alta probada", 55, "Producto");

        var record = Assert.Single(records);
        Assert.Equal(7, record.EmpresaId);
        Assert.Equal(55, record.ReferenciaId);
        Assert.Equal("Producto", record.Entidad);
    }

    [Fact]
    public async Task Header_manipulado_sin_RBAC_no_atribuye_una_empresa()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = "999";
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(ModuloSistema.Productos, AccionPermiso.Crear,
            "Alta sin marca", 56, "Producto");

        Assert.Null(Assert.Single(records).EmpresaId);
    }

    [Fact]
    public async Task FacturaPublica_UsaEmpresaDelTokenVerificado_NoHeader()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        http.Request.Path = "/facturas/publico/abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG/pdf";
        http.Request.Headers["X-Empresa-Id"] = "999";
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = 7;
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(
            ModuloSistema.Facturacion, AccionPermiso.Exportar, "PDF público A");

        Assert.Equal(7, Assert.Single(records).EmpresaId);
    }

    [Fact]
    public async Task TiendaPublica_UsaEmpresaVerificadaPorHost_NoHeader()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/tienda/checkout/validar";
        http.Request.Headers["X-Empresa-Id"] = "999";
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = 8;
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(
            ModuloSistema.Ventas, AccionPermiso.Crear, "Checkout tienda B");

        Assert.Equal(8, Assert.Single(records).EmpresaId);
    }

    [Fact]
    public async Task RbacAdministrativoTienePrecedenciaSobreMarcasPublicas()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/tienda/cuenta";
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 9;
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = 8;
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = 7;
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(
            ModuloSistema.Auditoria, AccionPermiso.Ver, "Prioridad RBAC");

        Assert.Equal(9, Assert.Single(records).EmpresaId);
    }

    [Fact]
    public async Task MarcasPublicasFueraDeSuRutaNoAtribuyenEmpresa()
    {
        var http = new DefaultHttpContext();
        http.Request.Path = "/reportes/ventas";
        http.Request.Headers["X-Empresa-Id"] = "999";
        http.Items[VerifiedPublicStoreTenant.AuthorizedEmpresaIdItemKey] = 8;
        http.Items[VerifiedPublicInvoiceTenant.AuthorizedEmpresaIdItemKey] = 7;
        var (service, records) = Create(http);

        await service.RegistrarEstrictoAsync(
            ModuloSistema.Auditoria, AccionPermiso.Ver, "Marca pública fuera de ruta");

        Assert.Null(Assert.Single(records).EmpresaId);
    }

    [Fact]
    public async Task Operacion_no_HTTP_no_infiere_propietario()
    {
        var (service, records) = Create(null);

        await service.RegistrarEstrictoAsync(ModuloSistema.Productos, AccionPermiso.Crear,
            "Proceso sin contexto", 57, "Producto");

        Assert.Null(Assert.Single(records).EmpresaId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Fallo_best_effort_no_expone_descripcion_ni_excepcion_y_estricto_propaga(bool failOnSave)
    {
        const string privateMarker = "SYNTHETIC_PRIVATE_AUDIT_PAYLOAD";
        const string providerMarker = "SYNTHETIC_PRIVATE_PROVIDER_ERROR";
        var failure = new InvalidOperationException(providerMarker);
        var repo = new Mock<IAuditoriaRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Returns(failOnSave ? Task.CompletedTask : Task.FromException(failure));
        if (failOnSave)
            repo.Setup(r => r.SaveChangesAsync()).ThrowsAsync(failure);
        var logger = new CapturingAuditLogger();
        var user = new Mock<ICurrentUserService>();
        var service = new AuditoriaService(repo.Object, user.Object, new HttpContextAccessor(), logger);

        await service.RegistrarAsync(ModuloSistema.Productos, AccionPermiso.Crear, privateMarker,
            error: privateMarker, valoresNuevos: new { PrivateValue = privateMarker });

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Contains("AUDIT_WRITE_FAILED", entry.Message);
        Assert.DoesNotContain(privateMarker, entry.Message);
        Assert.DoesNotContain(providerMarker, entry.Message);
        Assert.Equal(ModuloSistema.Productos, entry.Fields.Single(p => p.Key == "Modulo").Value);
        Assert.Equal(AccionPermiso.Crear, entry.Fields.Single(p => p.Key == "Accion").Value);
        Assert.All(entry.Fields, field =>
        {
            Assert.DoesNotContain(privateMarker, field.Value?.ToString() ?? "");
            Assert.DoesNotContain(providerMarker, field.Value?.ToString() ?? "");
        });
        repo.Verify(r => r.SaveChangesAsync(), failOnSave ? Times.Once() : Times.Never());

        var strictFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegistrarEstrictoAsync(ModuloSistema.Productos, AccionPermiso.Crear, privateMarker));
        Assert.Same(failure, strictFailure);
        Assert.Single(logger.Entries);
    }

    private sealed class CapturingAuditLogger : ILogger<AuditoriaService>
    {
        public List<(LogLevel Level, Exception? Exception, string Message,
            KeyValuePair<string, object?>[] Fields)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var fields = state is IEnumerable<KeyValuePair<string, object?>> structured
                ? structured.ToArray() : Array.Empty<KeyValuePair<string, object?>>();
            Entries.Add((logLevel, exception, formatter(state, exception), fields));
        }
    }
}
