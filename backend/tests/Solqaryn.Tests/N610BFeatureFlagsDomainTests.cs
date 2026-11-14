using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class N610BFeatureFlagsDomainTests
{
    private static readonly DateTime Ahora = new(2026, 9, 13, 1, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Free_ModuloConReglaExplicita_QuedaHabilitado()
    {
        var plan = CrearPlan(10, "FREE", "Free");
        var suscripcion = new Suscripcion(7, 10, Ahora.AddDays(-1), Ahora.AddDays(29));
        var reglas = new[] { new PlanModulo(10, " free ", " inventario ") };

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, plan, reglas, "INVENTARIO", Ahora);

        Assert.True(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.HabilitadoPorReglaExplicita, decision.Motivo);
        Assert.Equal("FREE", reglas[0].PlanCodigo);
        Assert.Equal("INVENTARIO", reglas[0].ModuloClave);
    }

    [Fact]
    public void Free_ModuloSinReglaExplicita_FallaCerrado()
    {
        var plan = CrearPlan(10, "FREE", "Free");
        var suscripcion = new Suscripcion(7, 10, Ahora.AddDays(-1), Ahora.AddDays(29));
        var reglas = new[] { new PlanModulo(10, "FREE", "INVENTARIO") };

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, plan, reglas, "REPORTES", Ahora);

        Assert.False(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.SinReglaExplicita, decision.Motivo);
    }

    [Fact]
    public void Pro_NoTieneWildcardImplicito()
    {
        var plan = CrearPlan(20, "PRO", "Pro");
        var suscripcion = new Suscripcion(9, 20, Ahora.AddDays(-1));
        var reglas = new[] { new PlanModulo(20, "PRO", "REPORTES") };

        var permitida = PoliticaModulosSaaS.Evaluar(9, 9, suscripcion, plan, reglas, "REPORTES", Ahora);
        var noDeclarada = PoliticaModulosSaaS.Evaluar(9, 9, suscripcion, plan, reglas, "ADMIN_TOTAL", Ahora);

        Assert.True(permitida.Habilitado);
        Assert.False(noDeclarada.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.SinReglaExplicita, noDeclarada.Motivo);
    }

    [Fact]
    public void TenantCruzado_NoPuedeReutilizarDecisionDeOtraEmpresa()
    {
        var plan = CrearPlan(20, "PRO", "Pro");
        var suscripcionEmpresaB = new Suscripcion(22, 20, Ahora.AddDays(-1));
        var reglas = new[] { new PlanModulo(20, "PRO", "REPORTES") };

        var decision = PoliticaModulosSaaS.Evaluar(11, 22, suscripcionEmpresaB, plan, reglas, "REPORTES", Ahora);

        Assert.False(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.TenantNoCoincide, decision.Motivo);
    }

    [Fact]
    public void SinSuscripcionActiva_FallaCerrado()
    {
        var plan = CrearPlan(10, "FREE", "Free");
        var reglas = new[] { new PlanModulo(10, "FREE", "INVENTARIO") };

        var sinSuscripcion = PoliticaModulosSaaS.Evaluar(7, 7, null, plan, reglas, "INVENTARIO", Ahora);

        var expirada = new Suscripcion(7, 10, Ahora.AddDays(-10), Ahora.AddDays(-1));
        var conSuscripcionExpirada = PoliticaModulosSaaS.Evaluar(7, 7, expirada, plan, reglas, "INVENTARIO", Ahora);

        Assert.False(sinSuscripcion.Habilitado);
        Assert.False(conSuscripcionExpirada.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.SuscripcionNoVigente, sinSuscripcion.Motivo);
        Assert.Equal(MotivoDecisionModuloSaaS.SuscripcionNoVigente, conSuscripcionExpirada.Motivo);
    }

    [Fact]
    public void PlanInactivoONoCoincidente_FallaCerrado()
    {
        var plan = CrearPlan(20, "PRO", "Pro");
        var suscripcion = new Suscripcion(7, 20, Ahora.AddDays(-1));
        var reglas = new[] { new PlanModulo(20, "PRO", "REPORTES") };

        plan.Desactivar();
        var inactivo = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, plan, reglas, "REPORTES", Ahora);

        var planDistinto = CrearPlan(21, "PRO_OTRO", "Pro Otro");
        var distinto = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, planDistinto, reglas, "REPORTES", Ahora);

        Assert.False(inactivo.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.PlanNoDisponible, inactivo.Motivo);
        Assert.False(distinto.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.PlanNoCoincide, distinto.Motivo);
    }

    [Fact]
    public void CambioDeCodigoDePlan_NoReutilizaReglaAnterior()
    {
        var plan = CrearPlan(20, "PRO", "Pro");
        var suscripcion = new Suscripcion(7, 20, Ahora.AddDays(-1));
        var reglas = new[] { new PlanModulo(20, "PRO", "REPORTES") };

        plan.CambiarCodigo("ENTERPRISE");
        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, plan, reglas, "REPORTES", Ahora);

        Assert.False(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.SinReglaExplicita, decision.Motivo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ModuloVacioODesconocido_NoHabilita(string? moduloClave)
    {
        var plan = CrearPlan(10, "FREE", "Free");
        var suscripcion = new Suscripcion(7, 10, Ahora.AddDays(-1));
        var reglas = new[] { new PlanModulo(10, "FREE", "INVENTARIO") };

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, plan, reglas, moduloClave, Ahora);

        Assert.False(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.ModuloInvalido, decision.Motivo);
    }

    [Fact]
    public void LimiteCuantitativo_NoSeInterpretaComoFeatureFlag()
    {
        var plan = CrearPlan(10, "FREE", "Free");
        plan.DefinirLimite("INVENTARIO", 100);
        var suscripcion = new Suscripcion(7, 10, Ahora.AddDays(-1));

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, plan, Array.Empty<PlanModulo>(), "INVENTARIO", Ahora);

        Assert.False(decision.Habilitado);
        Assert.Single(plan.Limites);
        Assert.Equal(MotivoDecisionModuloSaaS.SinReglaExplicita, decision.Motivo);
    }

    [Fact]
    public void ReglaDesactivada_NoHabilitaModulo()
    {
        var plan = CrearPlan(20, "PRO", "Pro");
        var suscripcion = new Suscripcion(7, 20, Ahora.AddDays(-1));
        var regla = new PlanModulo(20, "PRO", "REPORTES");
        regla.Desactivar();

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, plan, new[] { regla }, "REPORTES", Ahora);

        Assert.False(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.SinReglaExplicita, decision.Motivo);
    }

    [Fact]
    public void PlanSinIdentidadPersistida_FallaCerrado()
    {
        var plan = new Plan("FREE", "Free");
        var suscripcion = new Suscripcion(7, 10, Ahora.AddDays(-1));
        var reglas = new[] { new PlanModulo(10, "FREE", "INVENTARIO") };

        var decision = PoliticaModulosSaaS.Evaluar(7, 7, suscripcion, plan, reglas, "INVENTARIO", Ahora);

        Assert.False(decision.Habilitado);
        Assert.Equal(MotivoDecisionModuloSaaS.PlanNoDisponible, decision.Motivo);
    }

    private static Plan CrearPlan(int id, string codigo, string nombre) =>
        new(codigo, nombre) { Id = id };
}
