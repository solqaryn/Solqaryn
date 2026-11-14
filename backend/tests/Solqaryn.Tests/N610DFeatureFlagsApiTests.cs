using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class N610DFeatureFlagsApiTests
{
    private static readonly DateTime Ahora = new(2026, 9, 13, 2, 20, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ModuloPermitido_UsaTenantVerificadoYReglaExplicita()
    {
        var plan = new Plan("FREE", "Free") { Id = 10 };
        var suscripcion = new Suscripcion(7, 10, Ahora.AddDays(-1));
        var repository = new FakeRepository
        {
            SuscripcionVigente = suscripcion,
            Plan = plan,
            Reglas = new[] { new PlanModulo(10, "FREE", "INVENTARIO") }
        };
        var service = new SuscripcionesSaaSService(
            repository,
            new FakeScopeService(new UsuarioTenantScopeActual(3, 7, 2, "Admin", true)));

        var result = await service.EvaluarModuloAsync(7, " inventario ", Ahora);

        Assert.True(result.Habilitado);
        Assert.Equal("INVENTARIO", result.ModuloClave);
        Assert.Equal(MotivoDecisionModuloSaaS.HabilitadoPorReglaExplicita, result.Motivo);
        Assert.Equal(10, result.PlanId);
        Assert.Equal("FREE", result.PlanCodigo);
        Assert.Equal(7, repository.UltimaEmpresaConsultada);
        Assert.Equal(10, repository.UltimoPlanModulosConsultado);
        Assert.Equal("FREE", repository.UltimoCodigoPlanModulosConsultado);
    }

    [Fact]
    public async Task ModuloSinReglaExplicita_FallaCerrado()
    {
        var plan = new Plan("PRO", "Pro") { Id = 20 };
        var repository = new FakeRepository
        {
            SuscripcionVigente = new Suscripcion(9, 20, Ahora.AddDays(-1)),
            Plan = plan,
            Reglas = Array.Empty<PlanModulo>()
        };
        var service = new SuscripcionesSaaSService(
            repository,
            new FakeScopeService(new UsuarioTenantScopeActual(4, 9, 2, "Admin", true)));

        var result = await service.EvaluarModuloAsync(9, "REPORTES", Ahora);

        Assert.False(result.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.SinReglaExplicita, result.Motivo);
    }

    [Fact]
    public async Task TenantNoAutorizado_FallaAntesDeConsultarSaaS()
    {
        var repository = new FakeRepository();
        var service = new SuscripcionesSaaSService(
            repository,
            new FakeScopeService(null));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            service.EvaluarModuloAsync(22, "INVENTARIO", Ahora));

        Assert.Equal(0, repository.LecturasTenant);
    }

    private sealed class FakeScopeService : IUsuarioScopeService
    {
        private readonly UsuarioTenantScopeActual? _scope;

        public FakeScopeService(UsuarioTenantScopeActual? scope)
        {
            _scope = scope;
        }

        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);

        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_scope);
    }

    private sealed class FakeRepository : ISuscripcionesSaaSRepository
    {
        public Suscripcion? SuscripcionVigente { get; init; }
        public Plan? Plan { get; init; }
        public IReadOnlyList<PlanModulo> Reglas { get; init; } = Array.Empty<PlanModulo>();
        public int LecturasTenant { get; private set; }
        public int? UltimaEmpresaConsultada { get; private set; }
        public int? UltimoPlanModulosConsultado { get; private set; }
        public string? UltimoCodigoPlanModulosConsultado { get; private set; }

        public Task<Plan?> ObtenerPlanActivoPorCodigoAsync(
            string codigo,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Plan);

        public Task<Plan?> ObtenerPlanPorIdAsync(
            int planId,
            CancellationToken cancellationToken = default)
        {
            LecturasTenant++;
            return Task.FromResult(Plan?.Id == planId ? Plan : null);
        }

        public Task<IReadOnlyList<PlanModulo>> ObtenerModulosPlanAsync(
            int planId,
            string planCodigo,
            CancellationToken cancellationToken = default)
        {
            LecturasTenant++;
            UltimoPlanModulosConsultado = planId;
            UltimoCodigoPlanModulosConsultado = planCodigo;
            return Task.FromResult(Reglas);
        }

        public Task<Suscripcion?> ObtenerVigenteAsync(
            int empresaId,
            DateTime instanteUtc,
            CancellationToken cancellationToken = default)
        {
            LecturasTenant++;
            UltimaEmpresaConsultada = empresaId;
            return Task.FromResult(
                SuscripcionVigente?.EmpresaId == empresaId && SuscripcionVigente.EsVigenteEn(instanteUtc)
                    ? SuscripcionVigente
                    : null);
        }

        public Task<Suscripcion?> ObtenerPorIdempotenciaAsync(
            int empresaId,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Suscripcion?>(null);

        public Task AgregarAsync(
            Suscripcion suscripcion,
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
