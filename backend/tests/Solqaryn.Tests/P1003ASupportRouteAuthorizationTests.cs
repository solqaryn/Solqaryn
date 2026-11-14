using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Solqaryn.API.Filters;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public sealed class P1003ASupportRouteAuthorizationTests
{
    [Fact]
    public async Task Explicit_read_authorizes_only_same_company_module_and_Ver()
    {
        var http = new DefaultHttpContext();
        http.Request.Method = HttpMethods.Get;
        http.Request.Headers[TenantHeader] = "22";
        var candidate = new VerifiedPlatformSupportTenant.Scope(
            501, 22, ModuloSistema.Productos);
        VerifiedPlatformSupportTenant.MarkCandidate(http, candidate);

        var filters = new List<IFilterMetadata>();
        var context = new ActionExecutingContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            filters,
            new Dictionary<string, object?>(),
            new object());

        await new RequierePermisoAttribute(
                ModuloSistema.Productos, AccionPermiso.Ver)
            .OnActionExecutionAsync(context, () =>
                Task.FromResult(new ActionExecutedContext(
                    context, filters, new object())));

        Assert.Equal(22, VerifiedPlatformSupportTenant.ResolveEmpresaId(http));
        Assert.Equal(candidate, VerifiedPlatformSupportTenant.Resolve(http));
    }

    [Theory]
    [InlineData("23", "GET", ModuloSistema.Productos, AccionPermiso.Ver)]
    [InlineData("22", "GET", ModuloSistema.Clientes, AccionPermiso.Ver)]
    [InlineData("22", "PUT", ModuloSistema.Productos, AccionPermiso.Ver)]
    [InlineData("22", "GET", ModuloSistema.Productos, AccionPermiso.Editar)]
    public async Task Support_candidate_rejects_cross_company_module_and_write_routes(
        string companyHeader,
        string method,
        ModuloSistema module,
        AccionPermiso action)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        http.Request.Headers[TenantHeader] = companyHeader;
        VerifiedPlatformSupportTenant.MarkCandidate(
            http, new VerifiedPlatformSupportTenant.Scope(
                502, 22, ModuloSistema.Productos));

        var filters = new List<IFilterMetadata>();
        var context = new ActionExecutingContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()),
            filters,
            new Dictionary<string, object?>(),
            new object());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            new RequierePermisoAttribute(module, action)
                .OnActionExecutionAsync(context, () =>
                    Task.FromResult(new ActionExecutedContext(
                        context, filters, new object()))));

        Assert.Null(VerifiedPlatformSupportTenant.Resolve(http));
    }

    [Theory]
    [InlineData("GET", "Productos", "GetPaged", "productos", ModuloSistema.Productos, true)]
    [InlineData("GET", "Productos", "GetById", "productos/{id:int}", ModuloSistema.Productos, true)]
    [InlineData("GET", "Categorias", "GetAll", "categorias", ModuloSistema.Categorias, true)]
    [InlineData("GET", "Categorias", "GetActivas", "categorias/activas", ModuloSistema.Categorias, true)]
    [InlineData("GET", "Categorias", "GetById", "categorias/{id:int}", ModuloSistema.Categorias, true)]
    [InlineData("POST", "Productos", "GetPaged", "productos", ModuloSistema.Productos, false)]
    [InlineData("GET", "Ventas", "GetAll", "ventas", ModuloSistema.Ventas, false)]
    [InlineData("GET", "Productos", "Download", "productos/{id:int}/imagenes/{imagenId:int}/descargar", ModuloSistema.Productos, false)]
    [InlineData("GET", "Productos", "GetById", "otro/{id:int}", ModuloSistema.Productos, false)]
    [InlineData("GET", "Productos", "GetPaged", "productos", ModuloSistema.Categorias, false)]
    [InlineData("GET", "Usuarios", "GetById", "usuarios/{id:int}", ModuloSistema.Categorias, false)]
    public void Allowlist_accepts_only_reviewed_read_routes(
        string method,
        string controller,
        string action,
        string route,
        ModuloSistema module,
        bool expected)
    {
        Assert.Equal(expected, PlatformSupportPolicy.IsAllowedReadRoute(
            method, controller, action, route, module));
    }

    private const string TenantHeader = "X-Empresa-Id";
}
