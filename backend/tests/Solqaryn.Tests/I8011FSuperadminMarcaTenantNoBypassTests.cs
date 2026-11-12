using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Solqaryn.Application.Security;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-011.F: caso dirigido para certificacion superadmin sin bypass de tenant.
/// Prueba el contrato de la marca server-side; NO ejecuta JWT, middleware,
/// autorizacion de endpoints ni persistencia MySQL.
/// </summary>
public sealed class I8011FSuperadminMarcaTenantNoBypassTests
{
    private static DefaultHttpContext ContextoGlobalConTenantDeclarado()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "911"),
            new Claim(ClaimTypes.Role, "Superadmin"),
            new Claim("ambito", "Plataforma"),
            new Claim("es_administrador_plataforma", "true")
        }, "fixture-sintetico"));
        // Un consumidor no confiable intenta declarar empresa B.
        context.Request.Headers["X-Empresa-Id"] = "22";
        context.Request.QueryString = new QueryString("?empresaId=22");
        return context;
    }

    [Fact]
    public void SuperadminGlobalConHeaderYQueryNoObtieneTenantSinMarcaServerSide()
    {
        var context = ContextoGlobalConTenantDeclarado();

        Assert.Null(VerifiedCommercialTenant.Resolve(context));
        Assert.Null(VerifiedCommercialTenant.Resolve(null));
        Assert.Equal("22", context.Request.Headers["X-Empresa-Id"].ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-11)]
    public void MarcaServerSideNoPositivaEsRechazada(int empresaId)
    {
        var context = ContextoGlobalConTenantDeclarado();
        context.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaId;

        Assert.Null(VerifiedCommercialTenant.Resolve(context));
    }

    [Fact]
    public void IdentificadorTextoNoSustituyeMarcaAutorizadaEntera()
    {
        var context = ContextoGlobalConTenantDeclarado();
        context.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = "11";

        Assert.Null(VerifiedCommercialTenant.Resolve(context));
    }

    [Fact]
    public void MarcaEnteraServerSidePrevaleceSobreHeaderYQueryNoConfiables()
    {
        var context = ContextoGlobalConTenantDeclarado();
        context.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 11;

        Assert.Equal(11, VerifiedCommercialTenant.Resolve(context));
        Assert.NotEqual(22, VerifiedCommercialTenant.Resolve(context));
    }
}
