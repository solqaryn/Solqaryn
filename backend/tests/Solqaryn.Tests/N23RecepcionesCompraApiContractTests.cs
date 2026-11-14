using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public class N23RecepcionesCompraApiContractTests
{
    [Fact]
    public void Controller_exige_autenticacion_y_ruta_canonica()
    {
        var tipo = typeof(RecepcionesCompraController);

        Assert.NotNull(tipo.GetCustomAttribute<AuthorizeAttribute>());
        var route = Assert.Single(tipo.GetCustomAttributes<RouteAttribute>());
        Assert.Equal("recepciones-compra", route.Template);
        Assert.DoesNotContain(tipo.GetCustomAttributes(), a => a.GetType().Name == "AllowAnonymousAttribute");
    }

    [Theory]
    [InlineData(nameof(RecepcionesCompraController.Buscar), "Ver")]
    [InlineData(nameof(RecepcionesCompraController.GetById), "Ver")]
    [InlineData(nameof(RecepcionesCompraController.GetSaldoOrden), "Ver")]
    [InlineData(nameof(RecepcionesCompraController.Create), "Crear")]
    [InlineData(nameof(RecepcionesCompraController.Update), "Editar")]
    [InlineData(nameof(RecepcionesCompraController.Confirmar), "Confirmar")]
    [InlineData(nameof(RecepcionesCompraController.Anular), "Anular")]
    public void Cada_endpoint_exige_permiso_compras_especifico(string metodo, string accionEsperada)
    {
        var method = typeof(RecepcionesCompraController).GetMethod(metodo)
            ?? throw new InvalidOperationException($"No existe el método {metodo}.");
        var permiso = Assert.Single(method.CustomAttributes.Where(x => x.AttributeType == typeof(RequierePermisoAttribute)));

        Assert.Equal((int)ModuloSistema.Compras, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)Enum.Parse<AccionPermiso>(accionEsperada), Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Fact]
    public void Servicio_declara_materializador_fisico_en_constructor()
    {
        var ctor = Assert.Single(typeof(RecepcionCompraService).GetConstructors());
        var parametros = ctor.GetParameters().Select(x => x.ParameterType).ToArray();

        Assert.Contains(typeof(IRecepcionCompraRepository), parametros);
        Assert.Contains(typeof(RecepcionCompraExistenciaMaterializador), parametros);
        Assert.Contains(typeof(IUnitOfWork), parametros);
        Assert.Contains(typeof(IAuditoriaService), parametros);
    }

    [Fact]
    public void Lifecycle_http_conserva_verbos_y_rutas_estables()
    {
        AssertPost(nameof(RecepcionesCompraController.Confirmar), "{id:int}/confirmar");
        AssertPost(nameof(RecepcionesCompraController.Anular), "{id:int}/anular");
    }

    [Fact]
    public void Saldo_orden_conserva_ruta_get_estable()
    {
        var method = typeof(RecepcionesCompraController).GetMethod(nameof(RecepcionesCompraController.GetSaldoOrden))
            ?? throw new InvalidOperationException("No existe GetSaldoOrden.");
        var attr = Assert.Single(method.GetCustomAttributes<HttpGetAttribute>());
        Assert.Equal("ordenes/{ordenCompraId:int}/saldo", attr.Template);
    }

    private static void AssertPost(string methodName, string template)
    {
        var method = typeof(RecepcionesCompraController).GetMethod(methodName)
            ?? throw new InvalidOperationException($"No existe el método {methodName}.");
        var attr = Assert.Single(method.GetCustomAttributes<HttpPostAttribute>());
        Assert.Equal(template, attr.Template);
    }
}
