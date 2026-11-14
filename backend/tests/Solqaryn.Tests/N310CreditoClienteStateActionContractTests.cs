using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N310CreditoClienteStateActionContractTests
{
    [Fact]
    public void Service_contract_exposes_grounded_state_actions()
    {
        var methods = typeof(ICreditoClienteService).GetMethods().Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(nameof(ICreditoClienteService.AplicarBloqueoAutomaticoAsync), methods);
        Assert.Contains(nameof(ICreditoClienteService.LiberarBloqueoAutomaticoAsync), methods);
        Assert.Contains(nameof(ICreditoClienteService.AutorizarExcepcionAsync), methods);
        Assert.Contains(nameof(ICreditoClienteService.RevocarExcepcionAsync), methods);
    }

    [Fact]
    public void Controller_keeps_global_authorization_and_permission_guards_for_state_actions()
    {
        var controller = typeof(CreditosClienteController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());

        foreach (var methodName in new[]
                 {
                     nameof(CreditosClienteController.AplicarBloqueoAutomatico),
                     nameof(CreditosClienteController.LiberarBloqueoAutomatico),
                     nameof(CreditosClienteController.AutorizarExcepcion),
                     nameof(CreditosClienteController.RevocarExcepcion)
                 })
        {
            var method = controller.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.NotNull(method);
            Assert.NotNull(method!.GetCustomAttribute<RequierePermisoAttribute>());
        }
    }

    [Fact]
    public void State_action_dtos_do_not_encode_unapproved_credit_formula()
    {
        Assert.Equal(new[] { "Motivo" }, typeof(AplicarBloqueoCreditoClienteDto).GetProperties().Select(x => x.Name).OrderBy(x => x).ToArray());
        Assert.Equal(new[] { "Monto", "VigenteHastaUtc" }, typeof(AutorizarExcepcionCreditoClienteDto).GetProperties().Select(x => x.Name).OrderBy(x => x).ToArray());
    }
}
