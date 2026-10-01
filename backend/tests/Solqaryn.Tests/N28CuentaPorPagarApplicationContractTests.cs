using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N28CuentaPorPagarApplicationContractTests
{
    [Fact]
    public void Servicio_expone_vertical_completa_N28D()
    {
        var methods = typeof(ICuentaPorPagarService).GetMethods().Select(x => x.Name).ToHashSet();

        Assert.Contains("GetPagedAsync", methods);
        Assert.Contains("GetByIdAsync", methods);
        Assert.Contains("GenerarAsync", methods);
        Assert.Contains("AplicarAsync", methods);
        Assert.Contains("RevertirAplicacionAsync", methods);
        Assert.Contains("AnularAsync", methods);
    }

    [Fact]
    public void Controller_es_autenticado_y_usa_ruta_canonica()
    {
        var type = typeof(CuentasPorPagarController);
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());

        var route = type.GetCustomAttribute<RouteAttribute>();
        Assert.Equal("cuentas-por-pagar", route?.Template);
    }

    [Fact]
    public void Contratos_modelan_contado_credito_y_aplicaciones_tipadas()
    {
        Assert.True(Enum.IsDefined(typeof(CondicionPagoProveedor), CondicionPagoProveedor.Contado));
        Assert.True(Enum.IsDefined(typeof(CondicionPagoProveedor), CondicionPagoProveedor.Credito));
        Assert.True(Enum.IsDefined(typeof(TipoAplicacionCuentaPorPagar), TipoAplicacionCuentaPorPagar.Pago));
        Assert.True(Enum.IsDefined(typeof(TipoAplicacionCuentaPorPagar), TipoAplicacionCuentaPorPagar.Anticipo));
        Assert.True(Enum.IsDefined(typeof(TipoAplicacionCuentaPorPagar), TipoAplicacionCuentaPorPagar.Retencion));
        Assert.True(Enum.IsDefined(typeof(TipoAplicacionCuentaPorPagar), TipoAplicacionCuentaPorPagar.NotaCredito));

        var aplicar = new AplicarCuentaPorPagarDto
        {
            Tipo = TipoAplicacionCuentaPorPagar.Pago,
            Monto = 10m,
            IdempotencyKey = "pago-1",
            FechaAplicacionUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };
        Assert.Equal("pago-1", aplicar.IdempotencyKey);
    }
}
