using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solqaryn.API.Controllers;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-002.C: delimita la superficie actual de AuthController para recuperar
/// usuarios empresariales. No implementa recuperacion ni certifica HTTP, JWT,
/// correo, tokens de reset o aislamiento multiempresa en runtime.
/// </summary>
public sealed class I8002CRecuperacionUsuariosEmpresaDelimitacionTests
{
    [Fact]
    public void AuthController_no_expone_reset_sin_contrato_empresarial_autorizado()
    {
        var posts = typeof(AuthController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpPostAttribute>(inherit: false)
                .Select(attribute => new { Method = method, Route = attribute.Template ?? string.Empty }))
            .ToArray();

        Assert.Contains(posts, p => p.Route.Equals("login", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(posts, p => p.Route.Equals("renovar", StringComparison.OrdinalIgnoreCase)
            && p.Method.IsDefined(typeof(AuthorizeAttribute), inherit: true));

        Assert.DoesNotContain(posts, p =>
            p.Route.Contains("recuper", StringComparison.OrdinalIgnoreCase) ||
            p.Route.Contains("reset", StringComparison.OrdinalIgnoreCase) ||
            p.Route.Contains("forgot", StringComparison.OrdinalIgnoreCase) ||
            p.Route.Contains("password", StringComparison.OrdinalIgnoreCase));
    }
}
