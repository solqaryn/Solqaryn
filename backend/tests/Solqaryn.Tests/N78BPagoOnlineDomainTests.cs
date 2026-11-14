using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N78BPagoOnlineDomainTests
{
    [Fact]
    public void EstadoPagoOnline_MantieneContratoCanonicoDeTresEstados()
    {
        Assert.Equal(1, (int)EstadoPagoOnline.Pendiente);
        Assert.Equal(2, (int)EstadoPagoOnline.Pagado);
        Assert.Equal(3, (int)EstadoPagoOnline.Fallido);
        Assert.Equal(3, Enum.GetValues<EstadoPagoOnline>().Length);
    }

    [Fact]
    public void PagoOnline_Nuevo_QuedaPendienteYTenantBound()
    {
        var pago = new PagoOnline
        {
            EmpresaId = 7,
            FacturaId = 41,
            Proveedor = "provider-test",
            Monto = 125.50m,
            Moneda = "HNL"
        };

        Assert.Equal(7, pago.EmpresaId);
        Assert.Equal(41, pago.FacturaId);
        Assert.Equal(EstadoPagoOnline.Pendiente, pago.Estado);
        Assert.Null(pago.ProviderEventId);
        Assert.Null(pago.ConfirmadoUtc);
    }

    [Fact]
    public void PagoOnline_NoExponeCredencialesDeProveedorEnDominio()
    {
        var nombres = typeof(PagoOnline)
            .GetProperties()
            .Select(p => p.Name)
            .ToArray();

        Assert.DoesNotContain(nombres, n => n.Contains("Secret", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, n => n.Contains("Token", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, n => n.Contains("ApiKey", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, n => n.Contains("Credential", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProviderContract_EsTransaccionalYNoRecibeSecretos()
    {
        var metodo = typeof(IPagoOnlineProvider).GetMethod(nameof(IPagoOnlineProvider.IniciarAsync));
        Assert.NotNull(metodo);

        var parametros = metodo!.GetParameters();
        Assert.Equal(typeof(SolicitudInicioPagoOnline), parametros[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parametros[1].ParameterType);

        var solicitud = new SolicitudInicioPagoOnline(7, 41, 125.50m, "HNL", "FAC-00041");
        Assert.Equal(7, solicitud.EmpresaId);
        Assert.Equal(41, solicitud.FacturaId);
        Assert.Equal(125.50m, solicitud.Monto);
    }
}
