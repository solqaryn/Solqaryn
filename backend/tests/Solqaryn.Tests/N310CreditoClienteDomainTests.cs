using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class N310CreditoClienteDomainTests
{
    private static readonly DateTime AhoraUtc = new(2026, 8, 27, 3, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Crear_ConfiguraLimiteDiasYUmbralSinMutarCliente()
    {
        var cliente = CrearCliente();
        var credito = CreditoCliente.Crear(cliente, "hnl", 1000m, 30, 80m);

        Assert.Equal(cliente.Id, credito.ClienteId);
        Assert.Equal("HNL", credito.Moneda);
        Assert.Equal(1000m, credito.LimiteCredito);
        Assert.Equal(30, credito.DiasCredito);
        Assert.Equal(80m, credito.UmbralAlertaPorcentaje);
        Assert.False(credito.BloqueadoAutomaticamente);
        Assert.True(cliente.Activo);
    }

    [Theory]
    [InlineData(-1, 30, 80)]
    [InlineData(1000, -1, 80)]
    [InlineData(1000, 30, 0)]
    [InlineData(1000, 30, 101)]
    public void Crear_ConfiguracionInvalida_FallaCerrado(decimal limite, int dias, decimal alerta)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            CreditoCliente.Crear(CrearCliente(), "HNL", limite, dias, alerta));
    }

    [Fact]
    public void BloqueoAutomatico_RegistraEstadoMotivoYUtc_SinInventarFormula()
    {
        var credito = CrearCredito();

        credito.AplicarBloqueoAutomatico("POLITICA_EXTERNA", AhoraUtc);

        Assert.True(credito.BloqueadoAutomaticamente);
        Assert.Equal("POLITICA_EXTERNA", credito.MotivoBloqueo);
        Assert.Equal(AhoraUtc, credito.BloqueadoUtc);

        credito.LiberarBloqueoAutomatico(AhoraUtc.AddMinutes(1));
        Assert.False(credito.BloqueadoAutomaticamente);
        Assert.Null(credito.MotivoBloqueo);
    }

    [Fact]
    public void AutorizarExcepcion_ExigeMontoActorYVigenciaUtcFutura()
    {
        var credito = CrearCredito();

        credito.AutorizarExcepcion(250m, AhoraUtc.AddHours(2), "supervisor", AhoraUtc);

        Assert.Equal(250m, credito.MontoExcepcion);
        Assert.Equal("supervisor", credito.ExcepcionAutorizadaPor);
        Assert.True(credito.TieneExcepcionVigente(AhoraUtc.AddMinutes(1)));
        Assert.False(credito.TieneExcepcionVigente(AhoraUtc.AddHours(3)));

        Assert.Throws<ArgumentException>(() =>
            credito.AutorizarExcepcion(10m, AhoraUtc.AddHours(1), " ", AhoraUtc));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            credito.AutorizarExcepcion(10m, AhoraUtc, "actor", AhoraUtc));
    }

    [Fact]
    public void Politica_NoMaterializaFormulaDeDisponibleNiThresholdHardcodeado()
    {
        var credito = CreditoCliente.Crear(CrearCliente(), "USD", 5000m, 45, null);

        Assert.Equal("USD", credito.Moneda);
        Assert.Equal(5000m, credito.LimiteCredito);
        Assert.Equal(45, credito.DiasCredito);
        Assert.Null(credito.UmbralAlertaPorcentaje);
        Assert.False(credito.BloqueadoAutomaticamente);
    }

    private static CreditoCliente CrearCredito() =>
        CreditoCliente.Crear(CrearCliente(), "HNL", 1000m, 30, 80m);

    private static Cliente CrearCliente() => new()
    {
        Id = 44,
        Nombre = "Cliente Crédito",
        Activo = true,
        TipoClienteId = 1
    };
}
