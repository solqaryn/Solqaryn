using Microsoft.AspNetCore.Http;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Pure route-and-request contract for P1-003.B. The original full MySQL
/// integration remains in SupportSessionMySqlIntegrationTests and the separate
/// LIVE DEV workflow verifies real JWT, session persistence and revocation.
/// </summary>
public sealed class P1003BSupportRouteCertificationTests
{
    [Theory]
    [InlineData("GET", "Productos", "GetPaged", "productos", ModuloSistema.Productos)]
    [InlineData("GET", "Productos", "GetById", "productos/{id:int}", ModuloSistema.Productos)]
    [InlineData("GET", "Categorias", "GetAll", "categorias", ModuloSistema.Categorias)]
    [InlineData("GET", "Categorias", "GetActivas", "categorias/activas", ModuloSistema.Categorias)]
    [InlineData("GET", "Categorias", "GetById", "categorias/{id:int}", ModuloSistema.Categorias)]
    public void Only_reviewed_read_routes_are_allowed(string verb, string controller,
        string action, string route, ModuloSistema module) =>
        Assert.True(PlatformSupportPolicy.IsAllowedReadRoute(
            verb, controller, action, route, module));

    [Theory]
    [InlineData("POST", "Productos", "GetPaged", "productos", ModuloSistema.Productos)]
    [InlineData("DELETE", "Productos", "GetById", "productos/{id:int}", ModuloSistema.Productos)]
    [InlineData("GET", "Usuarios", "GetById", "usuarios/{id:int}", ModuloSistema.Usuarios)]
    [InlineData("GET", "Productos", "GetPaged", "productos", ModuloSistema.Categorias)]
    [InlineData("GET", "Ventas", "GetAll", "ventas", ModuloSistema.Ventas)]
    [InlineData("GET", "Productos", "Download", "productos/{id:int}/imagenes/{imagenId:int}/descargar", ModuloSistema.Productos)]
    public void Unreviewed_verb_module_or_route_fails_closed(string verb,
        string controller, string action, string route, ModuloSistema module) =>
        Assert.False(PlatformSupportPolicy.IsAllowedReadRoute(
            verb, controller, action, route, module));

    [Fact]
    public void Candidate_does_not_authorize_commercial_data_until_permission_filter_confirms()
    {
        var http = new DefaultHttpContext();
        var candidate = new VerifiedPlatformSupportTenant.Scope(19, 2, ModuloSistema.Productos);
        Assert.Null(VerifiedPlatformSupportTenant.ResolveEmpresaId(http));
        VerifiedPlatformSupportTenant.MarkCandidate(http, candidate);
        Assert.Null(VerifiedPlatformSupportTenant.Resolve(http));
        Assert.Null(VerifiedPlatformSupportTenant.ResolveEmpresaId(http));
        VerifiedPlatformSupportTenant.MarkAuthorized(http, candidate);
        Assert.Equal(2, VerifiedPlatformSupportTenant.ResolveEmpresaId(http));
    }

    [Fact]
    public void Forged_different_candidate_cannot_be_promoted_to_verified_scope()
    {
        var http = new DefaultHttpContext();
        VerifiedPlatformSupportTenant.MarkCandidate(http,
            new VerifiedPlatformSupportTenant.Scope(20, 2, ModuloSistema.Productos));
        Assert.Throws<InvalidOperationException>(() =>
            VerifiedPlatformSupportTenant.MarkAuthorized(http,
                new VerifiedPlatformSupportTenant.Scope(20, 3, ModuloSistema.Productos)));
        Assert.Null(VerifiedPlatformSupportTenant.ResolveEmpresaId(http));
    }

    [Fact]
    public void Invalid_session_or_tenant_cannot_be_marked_as_verified()
    {
        var http = new DefaultHttpContext();
        Assert.Throws<ArgumentException>(() =>
            VerifiedPlatformSupportTenant.MarkCandidate(http,
                new VerifiedPlatformSupportTenant.Scope(0, 2, ModuloSistema.Productos)));
        Assert.Throws<ArgumentException>(() =>
            VerifiedPlatformSupportTenant.MarkCandidate(http,
                new VerifiedPlatformSupportTenant.Scope(20, 0, ModuloSistema.Productos)));
    }
}
