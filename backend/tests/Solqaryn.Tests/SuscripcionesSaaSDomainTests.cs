using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class SuscripcionesSaaSDomainTests
{
    [Fact]
    public void Plan_NormalizaIdentidadYConservaUnaSolaFuentePorLimite()
    {
        var plan = new Plan(" pro ", " Plan Pro ");

        plan.DefinirLimite("usuarios", 10);
        plan.DefinirLimite(" USUARIOS ", 25);

        Assert.Equal("PRO", plan.Codigo);
        Assert.Equal("Plan Pro", plan.Nombre);
        var limite = Assert.Single(plan.Limites);
        Assert.Equal("USUARIOS", limite.Clave);
        Assert.Equal(25, limite.ValorMaximo);
    }

    [Fact]
    public void Plan_RechazaLimitesNoPositivos()
    {
        var plan = new Plan("BASICO", "Basico");

        Assert.Throws<ArgumentOutOfRangeException>(() => plan.DefinirLimite("usuarios", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.DefinirLimite("usuarios", -1));
    }

    [Fact]
    public void Suscripcion_RequiereEmpresaYPlanValidos()
    {
        var inicio = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Suscripcion(0, 1, inicio));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Suscripcion(1, 0, inicio));
    }

    [Fact]
    public void Suscripcion_RechazaPeriodoInvalido()
    {
        var inicio = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Suscripcion(1, 1, inicio, inicio));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Suscripcion(1, 1, inicio, inicio.AddMinutes(-1)));
    }

    [Fact]
    public void Suscripcion_VigenciaRespetaTenantPlanPeriodoYEstado()
    {
        var inicio = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);
        var suscripcion = new Suscripcion(7, 3, inicio, inicio.AddDays(30));

        Assert.Equal(7, suscripcion.EmpresaId);
        Assert.Equal(3, suscripcion.PlanId);
        Assert.True(suscripcion.EsVigenteEn(inicio));
        Assert.True(suscripcion.EsVigenteEn(inicio.AddDays(29)));
        Assert.False(suscripcion.EsVigenteEn(inicio.AddDays(30)));

        suscripcion.Suspender();
        Assert.False(suscripcion.EsVigenteEn(inicio.AddDays(1)));
        suscripcion.Reactivar();
        Assert.True(suscripcion.EsVigenteEn(inicio.AddDays(1)));
    }

    [Fact]
    public void Suscripcion_CanceladaNoPermiteCambiarPlan()
    {
        var inicio = new DateTime(2026, 9, 12, 0, 0, 0, DateTimeKind.Utc);
        var suscripcion = new Suscripcion(1, 2, inicio);

        suscripcion.Cancelar(inicio.AddDays(1));

        Assert.Equal(EstadoSuscripcion.Cancelada, suscripcion.Estado);
        Assert.False(suscripcion.EsVigenteEn(inicio.AddDays(1)));
        Assert.Throws<InvalidOperationException>(() => suscripcion.CambiarPlan(3));
    }
}
