using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Solqaryn.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests.Application.Storage;

public class CloudinaryTenantOwnershipTests
{
    [Fact]
    public async Task DeleteAsync_PublicIdDeOtraEmpresa_FallaCerradoAntesDelProveedor()
    {
        var storage = CrearStorage();
        var tenant = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DeleteAsync(
                tenant,
                "solqaryn/productos/empresas/32/producto-otro-tenant",
                CancellationToken.None));

        Assert.Contains("no pertenece", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_CrossTenant_RegistraDenySafeSinLocatorNiSecretos()
    {
        var logger = new CapturingLogger<CloudinaryImageStorageService>();
        var storage = CrearStorage(logger);
        var tenant = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);
        const string locatorAjeno = "solqaryn/productos/empresas/32/producto-otro-tenant";

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DeleteAsync(tenant, locatorAjeno, CancellationToken.None));

        var audit = Assert.Single(logger.Messages);
        Assert.Contains("TENANT_STORAGE_AUDIT", audit, StringComparison.Ordinal);
        Assert.Contains("DENY_SAFE", audit, StringComparison.Ordinal);
        Assert.Contains("TENANT_LOCATOR_MISMATCH", audit, StringComparison.Ordinal);
        Assert.DoesNotContain(locatorAjeno, audit, StringComparison.Ordinal);
        Assert.DoesNotContain("unit-test-secret", audit, StringComparison.Ordinal);
        Assert.DoesNotContain("empresas/32", audit, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DownloadAsync_UrlDeOtraEmpresa_FallaCerradoAntesDeHttp()
    {
        var storage = CrearStorage();
        var tenant = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DownloadAsync(
                tenant,
                "https://res.cloudinary.com/unit-test/image/upload/v1/solqaryn/productos/empresas/32/producto.jpg",
                CancellationToken.None));

        Assert.Contains("no pertenece", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://res.cloudinary.com/unit-test/image/upload/v1/solqaryn/productos/empresas/31/a.jpg")]
    [InlineData("not-a-url")]
    [InlineData("https://example.test/unit-test/image/upload/v1/solqaryn/productos/empresas/31/a.jpg")]
    [InlineData("https://res.cloudinary.com/otra-cuenta/image/upload/v1/solqaryn/productos/empresas/31/a.jpg")]
    public async Task DownloadAsync_LocatorNoAutorizado_FallaCerrado(string locator)
    {
        var storage = CrearStorage();
        var tenant = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DownloadAsync(tenant, locator, CancellationToken.None));
    }

    [Fact]
    public async Task CompraDeleteAsync_PublicIdDeOtraEmpresa_FallaCerradoAntesDelProveedor()
    {
        var storage = CrearCompraStorage();
        var tenant = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DeleteAsync(
                tenant,
                "solqaryn/compras/empresas/32/comprobante-otro-tenant",
                "raw",
                CancellationToken.None));

        Assert.Contains("no pertenece", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompraDeleteAsync_CrossTenant_RegistraDenySafeSinLocatorNiSecretos()
    {
        var logger = new CapturingLogger<CloudinaryCompraDocumentoStorageService>();
        var storage = CrearCompraStorage(logger);
        var tenant = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);
        const string locatorAjeno = "solqaryn/compras/empresas/32/comprobante-otro-tenant";

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DeleteAsync(tenant, locatorAjeno, "raw", CancellationToken.None));

        var audit = Assert.Single(logger.Messages);
        Assert.Contains("TENANT_STORAGE_AUDIT", audit, StringComparison.Ordinal);
        Assert.Contains("DENY_SAFE", audit, StringComparison.Ordinal);
        Assert.Contains("TENANT_LOCATOR_MISMATCH", audit, StringComparison.Ordinal);
        Assert.DoesNotContain(locatorAjeno, audit, StringComparison.Ordinal);
        Assert.DoesNotContain("unit-test-secret", audit, StringComparison.Ordinal);
        Assert.DoesNotContain("empresas/32", audit, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompraDownloadAsync_UrlDeOtraEmpresa_FallaCerradoAntesDeHttp()
    {
        var storage = CrearCompraStorage();
        var tenant = CrearScope(usuarioId: 7, empresaId: 31, rolId: 4);

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() =>
            storage.DownloadAsync(
                tenant,
                "https://res.cloudinary.com/unit-test/raw/upload/v1/solqaryn/compras/empresas/32/factura.pdf",
                CancellationToken.None));

        Assert.Contains("no pertenece", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolverStorageTenant_ContextoSolicitadoSinMembresia_FallaCerrado()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[StorageTenantContextResolver.EmpresaHeader] = "31";
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var scopes = new Mock<IUsuarioScopeService>();
        scopes
            .Setup(x => x.ObtenerActualAsync(31, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);

        var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            StorageTenantContextResolver.ResolverRequeridoAsync(
                accessor,
                scopes.Object,
                CancellationToken.None));

        Assert.Contains("membresía activa", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolverStorageTenant_MembresiaActiva_UsaEmpresaYUsuarioVerificados()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[StorageTenantContextResolver.EmpresaHeader] = "31";
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var scopes = new Mock<IUsuarioScopeService>();
        scopes
            .Setup(x => x.ObtenerActualAsync(31, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(7, 31, 4, "Operador", false));

        var tenant = await StorageTenantContextResolver.ResolverRequeridoAsync(
            accessor,
            scopes.Object,
            CancellationToken.None);

        Assert.Equal(31, tenant.EmpresaId);
        Assert.Equal(7, tenant.UsuarioId);
        Assert.Equal("empresas/31", tenant.TenantPrefix);
    }

    private static CloudinaryImageStorageService CrearStorage(
        ILogger<CloudinaryImageStorageService>? logger = null) =>
        new(CrearConfiguracion(), logger: logger);

    private static CloudinaryCompraDocumentoStorageService CrearCompraStorage(
        ILogger<CloudinaryCompraDocumentoStorageService>? logger = null) =>
        new(CrearConfiguracion(), logger: logger);

    private static IConfiguration CrearConfiguracion() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cloudinary:CloudName"] = "unit-test",
                ["Cloudinary:ApiKey"] = "unit-test-key",
                ["Cloudinary:ApiSecret"] = "unit-test-secret"
            })
            .Build();

    private static StorageTenantContext CrearScope(int usuarioId, int empresaId, int rolId)
    {
        var membresia = new UsuarioEmpresa(usuarioId, empresaId, rolId);
        var tenant = ContextoTenantActual.DesdeMembresia(membresia, usuarioId, empresaId);
        return StorageTenantContext.Desde(tenant);
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
