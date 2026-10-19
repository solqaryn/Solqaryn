using Microsoft.Extensions.DependencyInjection;
using Solqaryn.Domain.Enums;
using Solqaryn.Application.Interfaces;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using Solqaryn.API.Middleware;
using Solqaryn.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
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

    [Theory]
    [InlineData("GET", "/facturas/publico/abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG/pdf")]
    [InlineData("GET", "/FACTURAS/PUBLICO/abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG/PDF")]
    public async Task DosEmpresas_PermitenSoloRutaPdfPublicaConTokenDeFormatoValido(
        string metodo, string ruta)
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().AddRange(new Empresa("Empresa A"), new Empresa("Empresa B"));
        await db.SaveChangesAsync();

        // Atravesar el middleware NO valida el token ni habilita los datos:
        // FacturaCompartirRepository exige hash, vigencia y EmpresaId real.
        var next = false;
        var middleware = new UnscopedCommercialTenantGuardMiddleware(ctx =>
        {
            next = true;
            Assert.Null(VerifiedPublicInvoiceTenant.Resolve(ctx));
            ctx.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var http = new DefaultHttpContext();
        http.Request.Method = metodo;
        http.Request.Path = ruta;
        http.Request.Headers["X-Empresa-Id"] = "999";
        http.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(http, db);
        Assert.True(next);
        Assert.Equal(StatusCodes.Status204NoContent, http.Response.StatusCode);
        Assert.Null(VerifiedPublicInvoiceTenant.Resolve(http));
    }

    [Theory]
    [InlineData("POST", "/facturas/publico/abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG/pdf")]
    [InlineData("GET", "/facturas/publico/corto/pdf")]
    [InlineData("GET", "/facturas/publico/abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG/pdf/extra")]
    [InlineData("GET", "/facturas/publico/abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG/editar")]
    [InlineData("GET", "/facturas/publico/abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG%2F/pdf")]
    [InlineData("GET", "/facturas/15/pdf")]
    [InlineData("GET", "/facturas/publico/pdf")]
    public async Task DosEmpresas_NoPermitenRutasParecidasNiOperacionesNoPublicas(
        string metodo, string ruta)
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().AddRange(new Empresa("Empresa A"), new Empresa("Empresa B"));
        await db.SaveChangesAsync();
        var (status, payload, next) = await ProcesarAsync(db, metodo, ruta);
        Assert.False(next);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED",
            payload.GetProperty("code").GetString());
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

    [Fact]
    public async Task DosEmpresas_Permiten_Solo_GetPerfilPropio_Autenticado()
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().AddRange(new Empresa("A"), new Empresa("B"));
        await db.SaveChangesAsync();

        var (status, _, ejecutada) = await ProcesarAsync(
            db, "GET", "/perfil", autenticado: true);
        Assert.True(ejecutada);
        Assert.Equal(StatusCodes.Status204NoContent, status);

        foreach (var (metodo, path) in new[]
        {
            ("GET", "/perfil/25"),
            ("GET", "/perfil/"),
            ("POST", "/perfil"),
            ("PUT", "/perfil"),
            ("PUT", "/perfil/foto"),
            ("DELETE", "/perfil/foto"),
            ("PUT", "/perfil/password"),
            ("GET", "/permisos/mis-permisos/empresa/2")
        })
        {
            var (bloqueado, payload, next) = await ProcesarAsync(
                db, metodo, path, autenticado: true);
            Assert.False(next);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, bloqueado);
            Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED",
                payload.GetProperty("code").GetString());
        }
    }

    [Fact]
    public async Task DosEmpresas_Bloquean_GetPerfil_SinJwt()
    {
        await using var db = NuevoContexto();
        db.Set<Empresa>().AddRange(new Empresa("A"), new Empresa("B"));
        await db.SaveChangesAsync();

        var (status, payload, ejecutada) = await ProcesarAsync(db, "GET", "/perfil");

        Assert.False(ejecutada);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal("TENANT_DATA_ISOLATION_REQUIRED",
            payload.GetProperty("code").GetString());
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

    [Fact]
    public async Task HostsAprobadosA_B_AislanCategoriasPublicas_SinHabilitarRutasPrivadas()
    {
        var accessor = new HttpContextAccessor();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("host-multi-tenant-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);
        var a = new Empresa("Tienda A");
        var b = new Empresa("Tienda B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        var categoriaA = new Categoria { Nombre = "Privada A" };
        var categoriaB = new Categoria { Nombre = "Privada B" };
        db.Categorias.AddRange(categoriaA, categoriaB);
        db.Entry(categoriaA).Property<int?>("EmpresaId").CurrentValue = a.Id;
        db.Entry(categoriaB).Property<int?>("EmpresaId").CurrentValue = b.Id;
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PublicStore:EnableMultiEmpresaBinding"] = "true",
                ["PublicStore:HostBindings:0:Host"] = "tienda-a.example.test",
                ["PublicStore:HostBindings:0:EmpresaId"] = a.Id.ToString(),
                ["PublicStore:HostBindings:1:Host"] = "tienda-b.example.test",
                ["PublicStore:HostBindings:1:EmpresaId"] = b.Id.ToString()
            }).Build();
        var resolver = new ConfiguredPublicStoreHostTenantResolver(config);
        var visibilidad = new List<(int EmpresaId, int CategoriaId)>();
        var middleware = new UnscopedCommercialTenantGuardMiddleware(async http =>
        {
            var empresa = VerifiedPublicStoreTenant.Resolve(http);
            Assert.True(empresa is > 0);
            var categorias = await db.Categorias.AsNoTracking().ToListAsync();
            Assert.Single(categorias);
            visibilidad.Add((empresa!.Value, categorias[0].Id));
            http.Response.StatusCode = StatusCodes.Status204NoContent;
        });

        async Task<int> SolicitarAsync(string host, string ruta)
        {
            var http = new DefaultHttpContext();
            http.Request.Path = ruta;
            http.Request.Method = "GET";
            http.Request.Host = new HostString(host);
            http.Request.Headers["X-Empresa-Id"] = "99999";
            http.Request.Headers["X-Forwarded-Host"] = "tienda-b.example.test";
            http.Response.Body = new MemoryStream();
            accessor.HttpContext = http;
            await middleware.InvokeAsync(http, db, resolver);
            return http.Response.StatusCode;
        }

        Assert.Equal(204, await SolicitarAsync("tienda-a.example.test", "/tienda/categorias"));
        Assert.Equal(204, await SolicitarAsync("tienda-b.example.test", "/tienda/categorias"));
        Assert.Equal(503, await SolicitarAsync("no-validada.example.test", "/tienda/categorias"));
        Assert.Equal(503, await SolicitarAsync("tienda-a.example.test", "/productos"));
        Assert.Equal(new[]
        {
            (a.Id, categoriaA.Id), (b.Id, categoriaB.Id)
        }, visibilidad.ToArray());
    }

    [Fact]
    public async Task DominioDeTiendaSuspendida_SiempreSeRechaza()
    {
        await using var db = NuevoContexto();
        var a = new Empresa("Activa");
        var b = new Empresa("Suspendida");
        b.Desactivar();
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["PublicStore:EnableMultiEmpresaBinding"] = "true",
                ["PublicStore:HostBindings:0:Host"] = "suspendida.example.test",
                ["PublicStore:HostBindings:0:EmpresaId"] = b.Id.ToString()
            }).Build();
        var resolver = new ConfiguredPublicStoreHostTenantResolver(config);
        var llamada = false;
        var middleware = new UnscopedCommercialTenantGuardMiddleware(_ =>
        {
            llamada = true;
            return Task.CompletedTask;
        });
        var http = new DefaultHttpContext();
        http.Request.Path = "/tienda/cuenta/login";
        http.Request.Method = "POST";
        http.Request.Host = new HostString("suspendida.example.test");
        http.Response.Body = new MemoryStream();
        await middleware.InvokeAsync(http, db, resolver);

        Assert.Equal(503, http.Response.StatusCode);
        Assert.False(llamada);
        Assert.Null(VerifiedPublicStoreTenant.Resolve(http));
    }

    [Fact]
    public async Task PilotoLecturaPrivada_AB_Exige_RBAC_Y_Oculta_Datos_Ajenos_Sin_Abrir_Mutaciones()
    {
        var accessor = new HttpContextAccessor();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p4-read-pilot-" + Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);
        var empresaA = new Empresa("Empresa A");
        var empresaB = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(empresaA, empresaB);
        await db.SaveChangesAsync();

        var categoriaA = new Categoria { Nombre = "A reservado" };
        var categoriaB = new Categoria { Nombre = "B reservado" };
        db.Categorias.AddRange(categoriaA, categoriaB);
        db.Entry(categoriaA).Property<int?>("EmpresaId").CurrentValue = empresaA.Id;
        db.Entry(categoriaB).Property<int?>("EmpresaId").CurrentValue = empresaB.Id;
        await db.SaveChangesAsync();

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MultiEmpresa:PrivateReadPilotEnabled"] = "true"
            }).Build();

        var permisos = new Mock<IPermisoService>(MockBehavior.Strict);
        permisos.Setup(x => x.TienePermisoAsync(
                It.IsAny<int>(), It.IsAny<ModuloSistema>(), AccionPermiso.Ver))
            .Returns((int empresaId, ModuloSistema _, AccionPermiso __) =>
                Task.FromResult(empresaId == empresaA.Id));
        using var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuracion)
            .AddSingleton<IPermisoService>(permisos.Object)
            .BuildServiceProvider();

        var llamadas = 0;
        var gate = new UnscopedCommercialTenantGuardMiddleware(async http =>
        {
            llamadas++;
            Assert.Equal(empresaA.Id, VerifiedCommercialTenant.Resolve(http));
            var categorias = await db.Categorias.AsNoTracking().ToListAsync();
            Assert.Single(categorias);
            Assert.Equal(categoriaA.Id, categorias[0].Id);
            http.Response.StatusCode = StatusCodes.Status204NoContent;
        });

        async Task<(int Status, int? Tenant)> SolicitudAsync(
            string metodo, string path, int idEmpresa, bool authenticated = true)
        {
            var http = new DefaultHttpContext { RequestServices = services };
            http.Request.Method = metodo;
            http.Request.Path = path;
            http.Request.Headers["X-Empresa-Id"] = idEmpresa.ToString();
            http.Response.Body = new MemoryStream();
            http.User = authenticated
                ? new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "user-A") }, "Test"))
                : new ClaimsPrincipal(new ClaimsIdentity());
            accessor.HttpContext = http;
            await gate.InvokeAsync(http, db);
            return (http.Response.StatusCode, VerifiedCommercialTenant.Resolve(http));
        }

        var a = await SolicitudAsync("GET", "/productos", empresaA.Id);
        Assert.Equal(204, a.Status);
        Assert.Equal(empresaA.Id, a.Tenant);

        var b = await SolicitudAsync("GET", "/productos", empresaB.Id);
        Assert.Equal(503, b.Status);
        Assert.Null(b.Tenant);

        var anonimo = await SolicitudAsync("GET", "/productos", empresaA.Id, false);
        Assert.Equal(503, anonimo.Status);
        Assert.Null(anonimo.Tenant);

        var escritura = await SolicitudAsync("POST", "/productos", empresaA.Id);
        Assert.Equal(503, escritura.Status);
        Assert.Null(escritura.Tenant);

        foreach (var ruta in new[]
        {
            "/productos/1", "/clientes/1", "/clientes/activos", "/clientes/buscar",
            "/proveedores/1", "/proveedores/activos", "/proveedores/buscar"
        })
        {
            var detalle = await SolicitudAsync("GET", ruta, empresaA.Id);
            Assert.Equal(204, detalle.Status);
            Assert.Equal(empresaA.Id, detalle.Tenant);
            var ajena = await SolicitudAsync("GET", ruta, empresaB.Id);
            Assert.Equal(503, ajena.Status);
            Assert.Null(ajena.Tenant);
        }

        foreach (var ruta in new[]
        {
            "/productos/1/imagenes", "/clientes/1/ventas", "/proveedores/1/compras",
            "/clientes/-1", "/productos/0", "/proveedores/2147483648",
            "/clientes/buscar/todos"
        })
        {
            var cerrada = await SolicitudAsync("GET", ruta, empresaA.Id);
            Assert.Equal(503, cerrada.Status);
            Assert.Null(cerrada.Tenant);
        }

        Assert.Equal(8, llamadas);
        permisos.Verify(x => x.TienePermisoAsync(
            empresaA.Id, ModuloSistema.Productos, AccionPermiso.Ver), Times.Exactly(2));
        permisos.Verify(x => x.TienePermisoAsync(
            empresaB.Id, ModuloSistema.Productos, AccionPermiso.Ver), Times.Exactly(2));
    }

    private static AppDbContext NuevoContexto()
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("tenant-quarantine-" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(opciones);
    }

    private static async Task<(int Status, JsonElement Payload, bool Next)> ProcesarAsync(
        AppDbContext db, string metodo, string path, bool autenticado = false)
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
        if (autenticado)
        {
            contexto.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "25") },
                authenticationType: "Bearer"));
        }

        await middleware.InvokeAsync(contexto, db);
        contexto.Response.Body.Position = 0;
        using var json = contexto.Response.Body.Length == 0
            ? JsonDocument.Parse("{}")
            : await JsonDocument.ParseAsync(contexto.Response.Body);
        return (contexto.Response.StatusCode, json.RootElement.Clone(), ejecutada);
    }
}
