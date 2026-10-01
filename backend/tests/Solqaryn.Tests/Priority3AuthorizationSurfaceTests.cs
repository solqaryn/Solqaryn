using System.Reflection;
using Solqaryn.API.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Solqaryn.Tests;

public sealed class Priority3AuthorizationSurfaceTests
{
    private static readonly HashSet<string> AnonymousMethodAllowlist = new(StringComparer.Ordinal)
    {
        "Solqaryn.API.Controllers.AuthController.Login",
        "Solqaryn.API.Controllers.EmpresaConfiguracionController.GetPublica",
        "Solqaryn.API.Controllers.FacturasController.DescargarPdfPublico",
        "Solqaryn.API.Controllers.TemaVisualController.Get",
        "Solqaryn.Api.Controllers.InboundWebhooksController.ReceiveAsync",
        "Solqaryn.API.Controllers.WhatsAppController.Webhook",
        "Solqaryn.API.Controllers.WhatsAppController.GetPublicoAsync"
    };

    [Fact]
    public void EveryControllerEndpointHasExplicitAuthorizationIntent()
    {
        var apiAssembly = typeof(Solqaryn.API.Controllers.AuthController).Assembly;
        var endpoints = apiAssembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(IsHttpEndpoint)
                .Select(method => new { Controller = type, Method = method }))
            .ToList();

        Assert.NotEmpty(endpoints);

        foreach (var endpoint in endpoints)
        {
            var controller = endpoint.Controller;
            var method = endpoint.Method;
            var endpointName = $"{controller.FullName}.{method.Name}";

            var allowAnonymous = HasAttribute<AllowAnonymousAttribute>(controller, method);
            var authenticated = HasAttribute<AuthorizeAttribute>(controller, method);
            var permissionChecked = HasAttribute<RequierePermisoAttribute>(controller, method);

            Assert.True(
                allowAnonymous || authenticated || permissionChecked,
                $"Endpoint without explicit authorization intent: {endpointName}");

            if (!allowAnonymous)
                continue;

            // Storefront customer accounts intentionally use their own opaque
            // X-Storefront-Session boundary instead of the administrative JWT.
            // Every private action inside this controller resolves the account
            // server-side before reading customer data.
            var explicitlyAllowed = controller.FullName == "Solqaryn.API.Controllers.TiendaController"
                || controller.FullName == "Solqaryn.API.Controllers.TiendaCuentaController"
                || AnonymousMethodAllowlist.Contains(endpointName);

            Assert.True(
                explicitlyAllowed,
                $"Unexpected anonymous endpoint outside the reviewed allowlist: {endpointName}");
        }
    }

    [Fact]
    public void PermissionProtectedEndpointsCannotAlsoBeAnonymous()
    {
        var apiAssembly = typeof(Solqaryn.API.Controllers.AuthController).Assembly;
        var conflicts = apiAssembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(IsHttpEndpoint)
                .Select(method => new { Controller = type, Method = method }))
            .Where(endpoint =>
                HasAttribute<AllowAnonymousAttribute>(endpoint.Controller, endpoint.Method) &&
                HasAttribute<RequierePermisoAttribute>(endpoint.Controller, endpoint.Method))
            .Select(endpoint => $"{endpoint.Controller.FullName}.{endpoint.Method.Name}")
            .ToList();

        Assert.True(conflicts.Count == 0,
            "Endpoints cannot be both anonymous and permission-protected: " + string.Join(", ", conflicts));
    }

    private static bool IsHttpEndpoint(MethodInfo method) =>
        method.GetCustomAttributes(inherit: true).OfType<HttpMethodAttribute>().Any();

    private static bool HasAttribute<T>(Type controller, MethodInfo method) where T : Attribute =>
        controller.GetCustomAttributes(typeof(T), inherit: true).Any() ||
        method.GetCustomAttributes(typeof(T), inherit: true).Any();
}
