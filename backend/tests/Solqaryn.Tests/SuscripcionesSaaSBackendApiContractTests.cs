using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class SuscripcionesSaaSBackendApiContractTests
{
    [Fact]
    public void OnboardingBody_NoAceptaEmpresaIdComoAutoridad()
    {
        var properties = typeof(OnboardingSuscripcionSaaSRequest)
            .GetProperties()
            .Select(x => x.Name)
            .ToArray();

        Assert.DoesNotContain("EmpresaId", properties);
        Assert.Contains("PlanCodigo", properties);
        Assert.Contains("InicioUtc", properties);
    }

    [Fact]
    public void LimitesQuery_NormalizaFiltroYAcotaPaginacion()
    {
        var query = new LimitesSuscripcionSaaSQuery(2, 25, " usuarios ").Normalizada();

        Assert.Equal(2, query.Pagina);
        Assert.Equal(25, query.TamanoPagina);
        Assert.Equal("USUARIOS", query.Clave);
        Assert.Throws<ArgumentOutOfRangeException>(() => new LimitesSuscripcionSaaSQuery(0, 25).Normalizada());
        Assert.Throws<ArgumentOutOfRangeException>(() => new LimitesSuscripcionSaaSQuery(1, 101).Normalizada());
    }

    [Fact]
    public void ServiceContract_ExigeEmpresaSeleccionadaEIdempotenciaParaOnboarding()
    {
        var onboarding = typeof(ISuscripcionesSaaSService).GetMethod(nameof(ISuscripcionesSaaSService.OnboardingAsync));
        Assert.NotNull(onboarding);

        var parameters = onboarding!.GetParameters();
        Assert.Equal("empresaId", parameters[0].Name);
        Assert.Equal(typeof(int), parameters[0].ParameterType);
        Assert.Equal("request", parameters[1].Name);
        Assert.Equal("idempotencyKey", parameters[2].Name);
        Assert.Equal(typeof(string), parameters[2].ParameterType);
    }

    [Fact]
    public void RepositoryContract_TodasLasLecturasTenantOwnedExigenEmpresaId()
    {
        var vigente = typeof(ISuscripcionesSaaSRepository).GetMethod(nameof(ISuscripcionesSaaSRepository.ObtenerVigenteAsync));
        var idempotencia = typeof(ISuscripcionesSaaSRepository).GetMethod(nameof(ISuscripcionesSaaSRepository.ObtenerPorIdempotenciaAsync));

        Assert.NotNull(vigente);
        Assert.NotNull(idempotencia);
        Assert.Equal("empresaId", vigente!.GetParameters()[0].Name);
        Assert.Equal("empresaId", idempotencia!.GetParameters()[0].Name);
    }

    [Theory]
    [InlineData(nameof(SuscripcionesSaaSController.Onboarding), ModuloSistema.Configuracion, AccionPermiso.Crear)]
    [InlineData(nameof(SuscripcionesSaaSController.ObtenerActual), ModuloSistema.Configuracion, AccionPermiso.Ver)]
    [InlineData(nameof(SuscripcionesSaaSController.ObtenerLimites), ModuloSistema.Configuracion, AccionPermiso.Ver)]
    public void Endpoints_ExigenPermisoRelacional(
        string methodName,
        ModuloSistema modulo,
        AccionPermiso accion)
    {
        var method = typeof(SuscripcionesSaaSController).GetMethod(methodName);
        Assert.NotNull(method);

        var permiso = Assert.Single(method!.GetCustomAttributesData()
            .Where(x => x.AttributeType == typeof(RequierePermisoAttribute)));

        Assert.Equal((int)modulo, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)accion, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Fact]
    public async Task Onboarding_SinIdempotencyKey_DevuelveProblemDetailsEstable()
    {
        var service = new Mock<ISuscripcionesSaaSService>(MockBehavior.Strict);
        var controller = new SuscripcionesSaaSController(service.Object);
        var request = new OnboardingSuscripcionSaaSRequest("BASIC", DateTime.UtcNow);

        var result = await controller.Onboarding(1, null, request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal(400, problem.Status);
        Assert.Equal("Idempotency-Key requerida", problem.Title);
        Assert.Equal("Idempotency-Key es obligatorio para onboarding.", problem.Detail);
        Assert.Equal(SuscripcionSaaSErrorCodes.IdempotencyKeyRequerida, problem.Extensions["code"]);
        service.VerifyNoOtherCalls();
    }
}
