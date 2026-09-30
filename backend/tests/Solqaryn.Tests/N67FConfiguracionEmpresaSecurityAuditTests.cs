using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.API.Middleware;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N67FConfiguracionEmpresaSecurityAuditTests
{
    [Theory]
    [InlineData(nameof(EmpresaConfiguracionController.GetTenant), AccionPermiso.Ver)]
    [InlineData(nameof(EmpresaConfiguracionController.UpdateTenant), AccionPermiso.Editar)]
    [InlineData(nameof(EmpresaConfiguracionController.UpdateTenantLogo), AccionPermiso.Editar)]
    [InlineData(nameof(EmpresaConfiguracionController.RestaurarTenantLogo), AccionPermiso.Editar)]
    [InlineData(nameof(EmpresaConfiguracionController.UpsertPlantillaTenant), AccionPermiso.Editar)]
    [InlineData(nameof(EmpresaConfiguracionController.DesactivarPlantillaTenant), AccionPermiso.Editar)]
    public void TenantActions_RequireExactConfiguracionPermission(string methodName, AccionPermiso expectedAction)
    {
        var method = typeof(EmpresaConfiguracionController).GetMethod(methodName);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>());

        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);

        Assert.Equal(ModuloSistema.Configuracion, (ModuloSistema)moduloField!.GetValue(permiso)!);
        Assert.Equal(expectedAction, (AccionPermiso)accionField!.GetValue(permiso)!);
    }

    [Fact]
    public async Task UpdateTenantAsync_SaveFailure_DoesNotEmitFalseAuditSuccess()
    {
        var repository = new Mock<IEmpresaConfiguracionRepository>();
        var imageStorage = new Mock<IImageStorageService>();
        var auditoria = new Mock<IAuditoriaService>();
        var empresaRepository = new Mock<IEmpresaRepository>();
        var scope = new Mock<IUsuarioScopeService>();

        var empresa = new Empresa("Acme Honduras") { Id = 23 };
        var config = new ConfigEmpresa(23) { Version = 5 };

        scope.Setup(service => service.ObtenerActualAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(9, 23, 4, "Administrador", true));
        empresaRepository.Setup(repository => repository.GetByIdAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);
        repository.Setup(repository => repository.GetTenantAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        repository.Setup(repository => repository.ListPlantillasAsync(23, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlantillaCorreoEmpresa>());
        repository.Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(false);

        var service = new EmpresaConfiguracionService(
            repository.Object,
            imageStorage.Object,
            auditoria.Object,
            empresaRepository.Object,
            scope.Object);

        var dto = new UpdateConfigEmpresaTenantDto
        {
            Nombre = "Acme Honduras",
            Moneda = "HNL",
            ZonaHoraria = "America/Tegucigalpa",
            ImpuestosJson = "{\"isv\":15}",
            EmisionJson = "{\"factura\":true}",
            Version = 5
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpdateTenantAsync(23, dto));

        auditoria.Verify(x => x.RegistrarAsync(
            It.IsAny<ModuloSistema>(),
            It.IsAny<AccionPermiso>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string?>(),
            It.IsAny<object?>(),
            It.IsAny<object?>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task CorrelationMiddleware_PropagatesTenantConfigurationRequestCorrelation()
    {
        const string correlationId = "n67f-tenant-config-001";
        string? observed = null;
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;

        var middleware = new CorrelationIdMiddleware(
            ctx =>
            {
                observed = ctx.TraceIdentifier;
                return Task.CompletedTask;
            },
            NullLogger<CorrelationIdMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal(correlationId, observed);
        Assert.Equal(correlationId, context.Items[CorrelationIdMiddleware.ItemKey]);
        Assert.Equal(correlationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
    }
}
