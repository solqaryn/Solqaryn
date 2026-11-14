using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public class N69FSuscripcionesSaaSSecurityAuditTests
{
    [Fact]
    public async Task Onboarding_Nuevo_RegistraAuditoriaTenantUnaSolaVez()
    {
        const int empresaId = 71;
        const int planId = 11;
        var inicio = new DateTime(2026, 9, 13, 0, 50, 0, DateTimeKind.Utc);
        var plan = new Plan("TEAM", "Team") { Id = planId };
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = CrearScope(empresaId);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);

        repository.Setup(x => x.ObtenerPorIdempotenciaAsync(empresaId, "req-audit", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Suscripcion?)null);
        repository.Setup(x => x.ObtenerVigenteAsync(empresaId, inicio, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Suscripcion?)null);
        repository.Setup(x => x.ObtenerPlanActivoPorCodigoAsync("TEAM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);
        repository.Setup(x => x.AgregarAsync(It.IsAny<Suscripcion>(), "req-audit", It.IsAny<CancellationToken>()))
            .Callback<Suscripcion, string, CancellationToken>((suscripcion, _, _) => suscripcion.Id = 321)
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        auditoria.Setup(x => x.RegistrarAsync(
                ModuloSistema.Configuracion,
                AccionPermiso.Crear,
                It.Is<string>(descripcion => descripcion.Contains($"empresa {empresaId}") && descripcion.Contains("TEAM")),
                321,
                "Suscripcion",
                null,
                It.IsAny<object?>(),
                null,
                "Exito",
                null))
            .Returns(Task.CompletedTask);

        var service = new SuscripcionesSaaSService(repository.Object, scope.Object, auditoria.Object);
        var result = await service.OnboardingAsync(
            empresaId,
            new OnboardingSuscripcionSaaSRequest("team", inicio),
            "req-audit",
            CancellationToken.None);

        Assert.Equal(321, result.Id);
        auditoria.Verify(x => x.RegistrarAsync(
            ModuloSistema.Configuracion,
            AccionPermiso.Crear,
            It.IsAny<string>(),
            321,
            "Suscripcion",
            null,
            It.IsAny<object?>(),
            null,
            "Exito",
            null), Times.Once);
        repository.VerifyAll();
        scope.VerifyAll();
        auditoria.VerifyAll();
    }

    [Fact]
    public async Task Onboarding_ReplayIdempotente_NoDuplicaAuditoria()
    {
        const int empresaId = 72;
        const int planId = 12;
        var inicio = new DateTime(2026, 9, 13, 0, 55, 0, DateTimeKind.Utc);
        var plan = new Plan("PRO", "Pro") { Id = planId };
        var existente = new Suscripcion(empresaId, planId, inicio) { Id = 444 };
        var repository = new Mock<ISuscripcionesSaaSRepository>(MockBehavior.Strict);
        var scope = CrearScope(empresaId);
        var auditoria = new Mock<IAuditoriaService>(MockBehavior.Strict);

        repository.Setup(x => x.ObtenerPorIdempotenciaAsync(empresaId, "req-replay-audit", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existente);
        repository.Setup(x => x.ObtenerPlanPorIdAsync(planId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var service = new SuscripcionesSaaSService(repository.Object, scope.Object, auditoria.Object);
        var result = await service.OnboardingAsync(
            empresaId,
            new OnboardingSuscripcionSaaSRequest("pro", inicio),
            "req-replay-audit",
            CancellationToken.None);

        Assert.Equal(existente.Id, result.Id);
        auditoria.VerifyNoOtherCalls();
        repository.VerifyAll();
        scope.VerifyAll();
    }

    private static Mock<IUsuarioScopeService> CrearScope(int empresaId)
    {
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        scope.Setup(x => x.ObtenerActualAsync(empresaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(
                UsuarioId: 900,
                EmpresaId: empresaId,
                RolId: 1,
                RolNombre: "ADMIN",
                EsAdministrador: true));
        return scope;
    }
}
