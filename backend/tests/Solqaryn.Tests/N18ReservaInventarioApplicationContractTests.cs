using Solqaryn.API.Controllers;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N18ReservaInventarioApplicationContractTests
{
    [Fact]
    public void Servicio_ImplementaContratoYDeclaraAutoridadFisica()
    {
        Assert.Contains(typeof(IReservaInventarioService), typeof(ReservaInventarioService).GetInterfaces());

        var constructor = Assert.Single(typeof(ReservaInventarioService).GetConstructors());
        var dependencias = constructor.GetParameters().Select(x => x.ParameterType).ToHashSet();
        Assert.Contains(typeof(IReservaInventarioRepository), dependencias);
        Assert.Contains(typeof(IProductoVarianteRepository), dependencias);
        Assert.Contains(typeof(IExistenciaVarianteConcurrencyService), dependencias);
        Assert.Contains(typeof(ICurrentUserService), dependencias);
        Assert.Contains(typeof(IAuditoriaService), dependencias);
        Assert.Contains(typeof(IUnitOfWork), dependencias);
    }

    [Fact]
    public void Concurrencia_ExponeMutacionPesimistaDeStockReservado()
    {
        var metodo = typeof(IExistenciaVarianteConcurrencyService)
            .GetMethod(nameof(IExistenciaVarianteConcurrencyService.AjustarStockReservadoPesimistaAsync));

        Assert.NotNull(metodo);
        Assert.Equal(typeof(Task), metodo!.ReturnType);
        var parametros = metodo.GetParameters();
        Assert.Equal(3, parametros.Length);
        Assert.Equal(typeof(InventarioExistenciaClave), parametros[0].ParameterType);
        Assert.Equal(typeof(int), parametros[1].ParameterType);
        Assert.Equal(typeof(int), parametros[2].ParameterType);
    }

    [Fact]
    public void Controller_ExigeAutenticacionYExponeLifecycleCompleto()
    {
        var tipo = typeof(ReservasInventarioController);
        Assert.NotNull(tipo.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());

        var rutas = tipo.GetMethods()
            .Where(m => m.DeclaringType == tipo)
            .SelectMany(m => m.GetCustomAttributes(typeof(HttpMethodAttribute), inherit: true)
                .Cast<HttpMethodAttribute>()
                .Select(a => (Metodo: m.Name, Plantilla: a.Template ?? string.Empty)))
            .ToList();

        Assert.Contains(rutas, x => x.Metodo == "Create" && x.Plantilla == string.Empty);
        Assert.Contains(rutas, x => x.Metodo == "Update" && x.Plantilla == "{id:int}");
        Assert.Contains(rutas, x => x.Metodo == "Activar" && x.Plantilla == "{id:int}/activar");
        Assert.Contains(rutas, x => x.Metodo == "Consumir" && x.Plantilla == "{id:int}/consumir");
        Assert.Contains(rutas, x => x.Metodo == "Liberar" && x.Plantilla == "{id:int}/liberar");
        Assert.Contains(rutas, x => x.Metodo == "Expirar" && x.Plantilla == "{id:int}/expirar");
        Assert.Contains(rutas, x => x.Metodo == "Cancelar" && x.Plantilla == "{id:int}/cancelar");
    }
}
