using System.Text.Json;
using Solqaryn.API.Middleware;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests.API;

public sealed class UnscopedCommercialTenantGuardTests
{
    [Theory]
    [InlineData("/productos")]
    [InlineData("/clientes/17")]
    [InlineData("/proveedores/buscar")]
    [InlineData("/compras")]
    [InlineData("/ventas/12")]
    [InlineData("/tienda/bootstrap")]
    [InlineData("/tienda/checkout/validar")]
    [InlineData("/facturas/8")]
    [InlineData("/reportes/ventas")]
    [InlineData("/inventario/productos")]
    public async Task DosEmpresas_NoPuedenRecibirDatosComerciales(string path)
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().Add(new Empresa("Empresa A"));
        db.Set<Empresa>().Add(new Empresa("Empresa B"));
        await db.SaveChangesAsync();
        var (status, payload, ejecutada) = await ProcesarAsync(db, "GET", path);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.False(ejecutada);
        Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED", payload.GetProperty("code").GetString());
        Assert.DoesNotContain("Empresa A", payload.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Empresa B", payload.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("GET", "/tienda/cuenta")]
    [InlineData("POST", "/tienda/cuenta/registrar")]
    [InlineData("POST", "/tienda/cuenta/login")]
    [InlineData("GET", "/tienda/cuenta/pedidos")]
    [InlineData("GET", "/tienda/productos/contexto")]
    [InlineData("POST", "/tienda/checkout/validar")]
    [InlineData("GET", "/facturas/publico/token-seguro-simulado/pdf")]
    [InlineData("GET", "/productos/12/imagenes/4/descargar")]
    [InlineData("GET", "/facturas/4/pdf")]
    [InlineData("GET", "/reportes/compras")]
    [InlineData("GET", "/reportes/inventario")]
    [InlineData("GET", "/auditoria")]
    [InlineData("POST", "/inventario/ajustes")]
    public async Task DosEmpresas_Bloquean_RecursosPublicosYPrivados_SinJWT(string metodo, string path)
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().AddRange(new Empresa("Empresa A"), new Empresa("Empresa B"));
        await db.SaveChangesAsync();
        var (status, payload, ejecutada) = await ProcesarAsync(db, metodo, path);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.False(ejecutada);
        Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED", payload.GetProperty("code").GetString());
    }

    [Fact]
    public async Task EmpresaInactiva_ConservaBloqueo_PorSusDatosHistoricos()
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().Add(new Empresa("A"));
        var inactiva = new Empresa("B");
        inactiva.Desactivar();
        db.Set<Empresa>().Add(inactiva);
        await db.SaveChangesAsync();

        var (status, _, ejecutada) = await ProcesarAsync(db, "POST", "/ventas");
        Assert.Equal(503, status);
        Assert.False(ejecutada);
    }

    [Fact]
    public async Task NingunaEmpresa_NoConcedeAutoridadComercial()
    {
        await using var db = NuevoContexto();
        var (status, payload, ejecutada) = await ProcesarAsync(db, "GET", "/productos");
        Assert.Equal(503, status);
        Assert.False(ejecutada);
        Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED", payload.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("/productos")]
    [InlineData("/clientes")]
    [InlineData("/proveedores")]
    [InlineData("/ventas")]
    [InlineData("/tienda/productos")]
    public async Task UnaEmpresa_NoCambiaLaAutorizacionExistente(string path)
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().Add(new Empresa("Unica"));
        await db.SaveChangesAsync();

        var (status, _, ejecutada) = await ProcesarAsync(db, "GET", path);
        Assert.True(ejecutada);
        Assert.Equal(StatusCodes.Status204NoContent, status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task NoSePermiteCrearEmpresaNueva_ConDatosSinOwner(int empresas)
    {
        await using var db = NuevoContexto();
        for (var i = 0; i < empresas; i++)
            db.Set<Empresa>().Add(new Empresa("Empresa " + i));
        await db.SaveChangesAsync();

        var (status, _, ejecutada) = await ProcesarAsync(db, "POST", "/empresas");
        Assert.Equal(503, status);
        Assert.False(ejecutada);
    }

    [Theory]
    [InlineData("POST", "/auth/login")]
    [InlineData("POST", "/auth/renovar")]
    [InlineData("GET", "/tenant-context/7")]
    [InlineData("GET", "/health")]
    [InlineData("GET", "/health/ready")]
    [InlineData("OPTIONS", "/productos")]
    public async Task SeguridadYHealth_ContinuanAccesibles_SinDatosComerciales(string metodo, string path)
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().Add(new Empresa("A"));
        db.Set<Empresa>().Add(new Empresa("B"));
        await db.SaveChangesAsync();

        var (status, _, ejecutada) = await ProcesarAsync(db, metodo, path);
        Assert.True(ejecutada);
        Assert.Equal(StatusCodes.Status204NoContent, status);
    }

    [Fact]
    public async Task NoSePermiteUsarPrefijosDeAutenticacionParaSaltarElBloqueo()
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().Add(new Empresa("A"));
        db.Set<Empresa>().Add(new Empresa("B"));
        await db.SaveChangesAsync();

        foreach (var (metodo, path) in new[]
        {
            ("GET", "/auth/login"),
            ("POST", "/auth/login/recursos"),
            ("GET", "/tenant-context-malicioso"),
            ("GET", "/health/tokens"),
            ("GET", "/empresas")
        })
        {
            var (status, _, ejecutada) = await ProcesarAsync(db, metodo, path);
            Assert.Equal(503, status);
            Assert.False(ejecutada);
        }
    }

    [Fact]
    public async Task TiendaPublica_UsaTenantUnicoReal_NoCabeceraNiPrefijoFalso()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/tienda/categorias";
        context.Request.Headers["X-Empresa-Id"] = "99999";
        var accessor = new HttpContextAccessor { HttpContext = context };
        await using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("tenant-public-read-" + Guid.NewGuid().ToString("N"))
                .Options,
            tenantHttpContextAccessor: accessor);
        var empresa = new Empresa("Empresa única");
        db.Set<Empresa>().Add(empresa);
        await db.SaveChangesAsync();
        var cat = new Categoria { Nombre = "Catálogo propietario" };
        db.Categorias.Add(cat);
        db.Entry(cat).Property<int?>("EmpresaId").CurrentValue = empresa.Id;
        await db.SaveChangesAsync();

        var llamado = false;
        var middleware = new UnscopedCommercialTenantGuardMiddleware(async ctx =>
        {
            llamado = true;
            Assert.Equal(empresa.Id, VerifiedPublicStoreTenant.Resolve(ctx));
            Assert.Equal(empresa.Id, db.AuthorizedReadEmpresaId);
            Assert.Equal(cat.Id, (await db.Categorias.AsNoTracking().SingleAsync()).Id);
        });
        await middleware.InvokeAsync(context, db);
        Assert.True(llamado);

        // Ni una cabecera ni una ruta con prefijo engañoso conceden autoridad.
        context.Request.Path = "/tienda-maliciosa/categorias";
        Assert.Null(VerifiedPublicStoreTenant.Resolve(context));
        context.Request.Path = "/productos";
        Assert.Null(VerifiedPublicStoreTenant.Resolve(context));
    }

    [Fact]
    public async Task EmpresaInactiva_NoPuedePublicarTiendaNiCrearCuentas()
    {
        await using var db = NuevoContexto();
        var empresa = new Empresa("Suspendida");
        empresa.Desactivar();
        db.Set<Empresa>().Add(empresa);
        await db.SaveChangesAsync();
        var (status, _, ejecutada) = await ProcesarAsync(db, "POST", "/tienda/cuenta/registrar");
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.False(ejecutada);
    }

    private static AppDbContext NuevoContexto()
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("tenant-quarantine-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(opciones);
    }

    private static async Task<(int Status, JsonElement Payload, bool Next)> ProcesarAsync(
        AppDbContext db, string metodo, string path)
    {
        var ejecutada = false;
        var middleware = new UnscopedCommercialTenantGuardMiddleware(context =>
        {
            ejecutada = true;
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        var contexto = new DefaultHttpContext();
        contexto.Request.Path = path;
        contexto.Request.Method = metodo;
        contexto.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(contexto, db);
        contexto.Response.Body.Position = 0;
        using var json = contexto.Response.Body.Length == 0
            ? JsonDocument.Parse("{}")
            : await JsonDocument.ParseAsync(contexto.Response.Body);
        return (contexto.Response.StatusCode, json.RootElement.Clone(), ejecutada);
    }
}
