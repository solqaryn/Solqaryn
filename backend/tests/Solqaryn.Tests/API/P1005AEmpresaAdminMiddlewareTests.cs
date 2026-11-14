using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests.API;

/// <summary>Allowlist administrativa exacta: no se extiende a otras rutas ERP.</summary>
public sealed class P1005AEmpresaAdminMiddlewareTests
{
    [Theory]
    [InlineData("GET","/roles")]
    [InlineData("GET","/roles/8")]
    [InlineData("POST","/roles")]
    [InlineData("PUT","/roles/8")]
    [InlineData("GET","/usuarios")]
    [InlineData("POST","/usuarios")]
    [InlineData("PUT","/usuarios/19")]
    [InlineData("DELETE","/usuarios/19")]
    [InlineData("GET","/usuarios/19/empresas")]
    [InlineData("POST","/usuarios/19/empresas")]
    [InlineData("PUT","/usuarios/19/empresas/11/rol")]
    [InlineData("PUT","/permisos/matriz/8")]
    public async Task RutaExactaConAdminA_PasaAlControlador(string method,string path)
    {
        var result=await Ejecutar(method,path,true,true);
        Assert.True(result.Passed);
        Assert.Equal(204,result.Status);
    }

    [Theory]
    [InlineData("GET","/roles/0")]
    [InlineData("POST","/roles/3")]
    [InlineData("PATCH","/roles/3/activar")]
    [InlineData("DELETE","/roles/3")]
    [InlineData("GET","/roles/8/extra")]
    [InlineData("GET","/usuarios/0")]
    [InlineData("POST","/usuarios/19/roles")]
    [InlineData("DELETE","/usuarios/0")]
    [InlineData("DELETE","/usuarios/-1")]
    [InlineData("DELETE","/usuarios/19/")]
    [InlineData("DELETE","/usuarios/19/extra")]
    [InlineData("DELETE","/usuarios/19/empresas")]
    [InlineData("PUT","/usuarios/19/empresas/11/estado")]
    [InlineData("PUT","/usuarios/19/empresas/11/rol/extra")]
    [InlineData("PUT","/permisos/catalogo/1")]
    [InlineData("POST","/permisos/catalogo")]
    [InlineData("POST","/plataforma/delegaciones/otra")]
    public async Task RutasVecinas_MantienenCuarentena(string method,string path)
    {
        var result=await Ejecutar(method,path,true,true);
        Assert.False(result.Passed);
        Assert.Equal(503,result.Status);
    }

    [Fact]
    public async Task SoloJwtSinAdminVivo_Denegado()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(()=>
            Ejecutar("POST","/usuarios",true,false));
    }

    [Fact]
    public async Task SinJwt_NuncaPasaAunqueHeaderSeaValido()
    {
        var result=await Ejecutar("POST","/usuarios",false,true);
        Assert.False(result.Passed);
        Assert.Equal(503,result.Status);
    }

    [Fact]
    public async Task EliminarSinAdministradorOPermiso_EsDenegado()
    {
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Ejecutar("DELETE","/usuarios/19",true,false));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            Ejecutar("DELETE","/usuarios/19",true,true,false));
    }

    [Fact]
    public async Task EliminarSinJwt_NoAtravesaraGate()
    {
        var result=await Ejecutar("DELETE","/usuarios/19",false,true);
        Assert.False(result.Passed);
        Assert.Equal(503,result.Status);
    }

    private static async Task<(bool Passed,int Status)> Ejecutar(
        string method,string path,bool jwt,bool admin,bool permit=true)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p1005a-mw-"+Guid.NewGuid().ToString("N")).Options);
        db.Set<Empresa>().AddRange(new Empresa("Prueba A"),new Empresa("Prueba B"));
        await db.SaveChangesAsync();

        var scope=new Mock<IUsuarioScopeService>();
        scope.Setup(x=>x.ObtenerActualAsync(11,It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioTenantScopeActual(1,11,1,"Admin A",admin));
        var permisos=new Mock<IPermisoService>();
        permisos.Setup(x=>x.VerificarPermisoAsync(11,
            It.IsAny<ModuloSistema>(),It.IsAny<AccionPermiso>()))
            .Returns(Task.CompletedTask);
        if(!permit)
            permisos.Setup(x=>x.VerificarPermisoAsync(11,
                ModuloSistema.Usuarios,AccionPermiso.EliminarLogico))
                .ThrowsAsync(new ForbiddenAccessException("Sin permiso EliminarLogico."));
        using var provider=new ServiceCollection()
            .AddSingleton(scope.Object)
            .AddSingleton(permisos.Object)
            .BuildServiceProvider();

        var http=new DefaultHttpContext
        {
            RequestServices=provider,
            User=jwt
                ? new ClaimsPrincipal(new ClaimsIdentity(
                    new[]{new Claim(ClaimTypes.NameIdentifier,"1")},"testjwt"))
                : new ClaimsPrincipal(new ClaimsIdentity())
        };
        http.Request.Headers["X-Empresa-Id"]="11";
        http.Request.Method=method;
        http.Request.Path=path;
        if(method is "POST" or "PUT")
        {
            http.Request.ContentLength=128;
            http.Request.ContentType="application/json";
        }
        http.Response.Body=new MemoryStream();
        var passed=false;
        var middleware=new UnscopedCommercialTenantGuardMiddleware(ctx=>
        {
            passed=true;
            ctx.Response.StatusCode=204;
            return Task.CompletedTask;
        });
        await middleware.InvokeAsync(http,db);
        return (passed,http.Response.StatusCode);
    }
}
