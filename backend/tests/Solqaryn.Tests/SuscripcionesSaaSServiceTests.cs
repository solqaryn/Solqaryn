using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class SuscripcionesSaaSServiceTests
{
    [Fact]
    public async Task Onboarding_TenantNoVerificado_FallaCerradoSinTocarRepositorio()
    {
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync(27, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioTenantScopeActual?)null);
        var service = new SuscripcionesSaaSService(repository.Object, scope.Object);

        var ex = await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.OnboardingAsync(
            27,
            new OnboardingSuscripcionSaaSRequest("BASIC", DateTime.UtcNow),
            "req-tenant-denied",
            CancellationToken.None));

        Assert.Contains(SuscripcionSaaSErrorCodes.TenantNoAutorizado, ex.Message);
        repository.VerifyNoOtherCalls();
        scope.VerifyAll();
    }

    [Fact]
    public async Task Onboarding_ReplayIdempotente_DevuelveMismaSuscripcionSinDuplicar()
    {
        const int empresaId = 9;
        const int planId = 4;
        var inicio = new DateTime(2026, 9, 12, 23, 0, 0, DateTimeKind.Utc);
        var plan = new Plan("BASIC", "Basic") { Id = planId };
        var existente = new Suscripcion(empresaId, planId, inicio) { Id = 88 };
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = CrearScope(empresaId);

        repository.Setup(x => x.ObtenerPorIdempotenciaAsync(empresaId, "req-replay-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);
        repository.Setup(x => x.ObtenerPlanPorIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var service = new SuscripcionesSaaSService(repository.Object, scope.Object);
        var dto = await service.OnboardingAsync(
            empresaId,
            new OnboardingSuscripcionSaaSRequest(" basic ", inicio),
            " req-replay-1 ",
            CancellationToken.None);

        Assert.Equal(existente.Id, dto.Id);
        Assert.Equal("BASIC", dto.PlanCodigo);
        repository.Verify(x => x.AgregarAsync(It.IsAny<Suscripcion>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
        scope.VerifyAll();
    }

    [Fact]
    public async Task Onboarding_MismaKeyConPayloadDistinto_RechazaConflicto()
    {
        const int empresaId = 12;
        var existente = new Suscripcion(empresaId, 7, new DateTime(2026, 9, 12, 20, 0, 0, DateTimeKind.Utc)) { Id = 91 };
        var plan = new Plan("PRO", "Pro") { Id = 7 };
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = CrearScope(empresaId);

        repository.Setup(x => x.ObtenerPorIdempotenciaAsync(empresaId, "req-conflict", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);
        repository.Setup(x => x.ObtenerPlanPorIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var service = new SuscripcionesSaaSService(repository.Object, scope.Object);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.OnboardingAsync(
            empresaId,
            new OnboardingSuscripcionSaaSRequest("BASIC", existente.InicioUtc),
            "req-conflict",
            CancellationToken.None));

        Assert.Contains(SuscripcionSaaSErrorCodes.IdempotencyKeyConflictiva, ex.Message);
        repository.Verify(x => x.AgregarAsync(It.IsAny<Suscripcion>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.VerifyAll();
        scope.VerifyAll();
    }

    [Fact]
    public async Task Onboarding_Nuevo_PersisteConTenantServerVerified()
    {
        const int empresaId = 31;
        const int planId = 6;
        var inicio = new DateTime(2026, 9, 13, 0, 0, 0, DateTimeKind.Utc);
        var plan = new Plan("TEAM", "Team") { Id = planId };
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = CrearScope(empresaId);
        Suscripcion? capturada = null;

        repository.Setup(x => x.ObtenerPorIdempotenciaAsync(empresaId, "req-new", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Suscripcion?)null);
        repository.Setup(x => x.ObtenerVigenteAsync(empresaId, inicio, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Suscripcion?)null);
        repository.Setup(x => x.ObtenerPlanActivoPorCodigoAsync("TEAM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        repository.Setup(x => x.AgregarAsync(It.IsAny<Suscripcion>(), "req-new", It.IsAny<CancellationToken>()))
            .Callback<Suscripcion, string, CancellationToken>((suscripcion, _, _) => capturada = suscripcion)
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new SuscripcionesSaaSService(repository.Object, scope.Object);
        var dto = await service.OnboardingAsync(
            empresaId,
            new OnboardingSuscripcionSaaSRequest("team", inicio),
            "req-new",
            CancellationToken.None);

        Assert.NotNull(capturada);
        Assert.Equal(empresaId, capturada!.EmpresaId);
        Assert.Equal(planId, capturada.PlanId);
        Assert.Equal("TEAM", dto.PlanCodigo);
        repository.VerifyAll();
        scope.VerifyAll();
    }

    [Fact]
    public async Task Onboarding_ColisionConcurrenteMismoPayload_ReleeGanadorYHaceReplay()
    {
        const int empresaId = 41;
        const int planId = 8;
        const string key = "req-race-same";
        var inicio = new DateTime(2026, 9, 13, 0, 15, 0, DateTimeKind.Utc);
        var plan = new Plan("PRO", "Pro") { Id = planId };
        var ganador = new Suscripcion(empresaId, planId, inicio) { Id = 501 };
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = CrearScope(empresaId);
        var lecturasLedger = 0;

        repository.Setup(x => x.ObtenerPorIdempotenciaAsync(empresaId, key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++lecturasLedger == 1 ? null : ganador);
        repository.Setup(x => x.ObtenerVigenteAsync(empresaId, inicio, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Suscripcion?)null);
        repository.Setup(x => x.ObtenerPlanActivoPorCodigoAsync("PRO", It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        repository.Setup(x => x.AgregarAsync(It.IsAny<Suscripcion>(), key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IdempotencyConcurrencyException("duplicate key race"));
        repository.Setup(x => x.ObtenerPlanPorIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var service = new SuscripcionesSaaSService(repository.Object, scope.Object);
        var dto = await service.OnboardingAsync(
            empresaId,
            new OnboardingSuscripcionSaaSRequest("pro", inicio),
            key,
            CancellationToken.None);

        Assert.Equal(ganador.Id, dto.Id);
        Assert.Equal("PRO", dto.PlanCodigo);
        Assert.Equal(2, lecturasLedger);
        repository.VerifyAll();
        scope.VerifyAll();
    }

    [Fact]
    public async Task Onboarding_ColisionConcurrentePayloadDistinto_ReleeGanadorYDevuelveConflicto()
    {
        const int empresaId = 42;
        const string key = "req-race-different";
        var inicio = new DateTime(2026, 9, 13, 0, 20, 0, DateTimeKind.Utc);
        var planSolicitado = new Plan("BASIC", "Basic") { Id = 3 };
        var planGanador = new Plan("PRO", "Pro") { Id = 9 };
        var ganador = new Suscripcion(empresaId, planGanador.Id, inicio) { Id = 502 };
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = CrearScope(empresaId);
        var lecturasLedger = 0;

        repository.Setup(x => x.ObtenerPorIdempotenciaAsync(empresaId, key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++lecturasLedger == 1 ? null : ganador);
        repository.Setup(x => x.ObtenerVigenteAsync(empresaId, inicio, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Suscripcion?)null);
        repository.Setup(x => x.ObtenerPlanActivoPorCodigoAsync("BASIC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(planSolicitado);
        repository.Setup(x => x.AgregarAsync(It.IsAny<Suscripcion>(), key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IdempotencyConcurrencyException("duplicate key race"));
        repository.Setup(x => x.ObtenerPlanPorIdAsync(planGanador.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(planGanador);

        var service = new SuscripcionesSaaSService(repository.Object, scope.Object);
        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.OnboardingAsync(
            empresaId,
            new OnboardingSuscripcionSaaSRequest("BASIC", inicio),
            key,
            CancellationToken.None));

        Assert.Contains(SuscripcionSaaSErrorCodes.IdempotencyKeyConflictiva, ex.Message);
        Assert.Equal(2, lecturasLedger);
        repository.VerifyAll();
        scope.VerifyAll();
    }

    private static Mock<IUsuarioScopeService> CrearScope(int empresaId)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync(empresaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(
                UsuarioId: 100,
                EmpresaId: empresaId,
                RolId: 1,
                RolNombre: "ADMIN",
                EsAdministrador: true));
        return scope;
    }
}
