using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Solqaryn.API.Controllers;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-005.C: delimita la superficie HTTP actual de autenticacion.
/// Comprueba el rate limit declarado en login y que renovar no sea anonimo.
/// No certifica revocacion de JWT, recuperacion de clave, ejecucion de 429
/// ni resistencia a ataques distribuidos: requieren pruebas propias posteriores.
/// </summary>
public sealed class I8005CAuthTokenAntifuerzaBrutaDelimitacionTests
{
    [Fact]
    public void Login_anonimo_exige_politica_antifuerza_bruta_y_no_abre_otro_post_anonimo()
    {
        var tipo = typeof(AuthController);
        Assert.Equal("auth", tipo.GetCustomAttribute<RouteAttribute>()?.Template);

        var login = tipo.GetMethod(nameof(AuthController.Login));
        Assert.NotNull(login);
        Assert.Equal("login", login!.GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.NotNull(login.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal("AuthLogin",
            login.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);

        var otrosPostAnonimos = tipo.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.Name != nameof(AuthController.Login))
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() is not null)
            .Where(m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null)
            .Select(m => m.Name)
            .ToArray();
        Assert.Empty(otrosPostAnonimos);
    }

    [Fact]
    public void Renovacion_de_token_no_expone_una_ruta_anonima()
    {
        var renovar = typeof(AuthController).GetMethod(nameof(AuthController.Renovar));
        Assert.NotNull(renovar);
        Assert.Equal("renovar", renovar!.GetCustomAttribute<HttpPostAttribute>()?.Template);
        Assert.NotNull(renovar.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(renovar.GetCustomAttribute<AllowAnonymousAttribute>());
    }
}
