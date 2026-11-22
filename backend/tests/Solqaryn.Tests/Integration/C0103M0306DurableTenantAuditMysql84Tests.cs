using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.AutomationV5;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class C0103M0306DurableTenantAuditMysql84Tests
{
    private const string Issuer = "c0103-v24-ci-issuer";
    private const string Audience = "c0103-v24-ci-audience";
    private const string FixtureSha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c0103-v24-test-only-signed-jwt-secret-2026-nonprod"));

    private sealed class HttpActor(IHttpContextAccessor access) : ICurrentUserService
    {
        public bool EstaAutenticado => access.HttpContext?.User.Identity?.IsAuthenticated == true;
        public int? UsuarioId => int.TryParse(
            access.HttpContext?.User.FindFirst("id")?.Value, out var id) ? id : null;
        public string? NombreUsuario => null;
        public string? NombreCompleto => null;
        public int? RolId => null;
        public bool EsAdministrador => false;
    }
    private sealed class EmittingLogger : ILogger<C0103DynamicConfigurationAuditLogger>
    {
        public int Emitted;
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => new Scope();
        public bool IsEnabled(LogLevel level) => level is LogLevel.Information or LogLevel.Warning;
        public void Log<TState>(LogLevel level, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            Assert.DoesNotContain("password", message, StringComparison.OrdinalIgnoreCase);
            Interlocked.Increment(ref Emitted);
        }
        private sealed class Scope : IDisposable { public void Dispose() {} }
    }
    private static string SignedToken(int id) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer, audience: Audience,
            claims: [new Claim("id", id.ToString())],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.HmacSha256)));

    [Fact]
    public async Task SignedJwtTenantABMembershipRevocationAndDurableMysqlReadback()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var root = new MySqlConnectionStringBuilder(raw);
        if (root.Server is not ("localhost" or "127.0.0.1") ||
            root.Port != 3306 || root.UserID != "root")
            throw new InvalidOperationException("MYSQL_LOOPBACK_ONLY");
        var isolated = new MySqlConnectionStringBuilder(raw) {
            Database="c0103_v24_"+Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated,x=>x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        int tenantA,tenantB,actorA,actorB,roleB;
        try
        {
            await using (var db = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
                var a=new Empresa("C0103 Tenant A");
                var b=new Empresa("C0103 Tenant B");
                db.Set<Empresa>().AddRange(a,b);
                await db.SaveChangesAsync();
                tenantA=a.Id;tenantB=b.Id;
                var ra=new Rol { Nombre="C0103A",NombreNormalizado="C0103A",
                    EmpresaId=tenantA,Ambito=AmbitoAutorizacion.Empresa,Activo=true };
                var rb=new Rol { Nombre="C0103B",NombreNormalizado="C0103B",
                    EmpresaId=tenantB,Ambito=AmbitoAutorizacion.Empresa,Activo=true };
                db.Roles.AddRange(ra,rb);
                await db.SaveChangesAsync();roleB=rb.Id;
                var perm=new Permiso { Codigo="C0103.CONFIG.VER",Nombre="V24 config read",
                    Modulo=ModuloSistema.Configuracion,Accion=AccionPermiso.Ver,
                    Ambito=AmbitoAutorizacion.Empresa,Activo=true };
                db.Set<Permiso>().Add(perm);
                await db.SaveChangesAsync();
                db.RolPermisos.AddRange(
                    new RolPermiso { RolId=ra.Id,PermisoId=perm.Id },
                    new RolPermiso { RolId=rb.Id,PermisoId=perm.Id });
                var ua=new Usuario { NombreUsuario="c0103a"+Guid.NewGuid().ToString("N")[..8],
                    NombreCompleto="A",PasswordHash="test-only",RolId=ra.Id,Activo=true };
                var ub=new Usuario { NombreUsuario="c0103b"+Guid.NewGuid().ToString("N")[..8],
                    NombreCompleto="B",PasswordHash="test-only",RolId=rb.Id,Activo=true };
                db.Usuarios.AddRange(ua,ub);
                await db.SaveChangesAsync();
                actorA=ua.Id;actorB=ub.Id;
                db.UsuarioEmpresas.AddRange(new UsuarioEmpresa(actorA,tenantA,ra.Id),
                    new UsuarioEmpresa(actorB,tenantB,rb.Id));
                db.Set<ConfigEmpresa>().AddRange(
                    new ConfigEmpresa(tenantA),new ConfigEmpresa(tenantB));
                await db.SaveChangesAsync();
            }

            var logger=new EmittingLogger();
            using var host=await new HostBuilder()
                .ConfigureWebHost(web=>web.UseEnvironment("Development").UseTestServer()
                    .ConfigureServices(services=>{
                        services.AddDbContext<AppDbContext>(o=>o.UseMySQL(isolated,
                            m=>m.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")));
                        services.AddRouting();
                        services.AddHttpContextAccessor();
                        services.AddScoped<ICurrentUserService,HttpActor>();
                        services.AddScoped<IUsuarioScopeService,UsuarioScopeService>();
                        services.AddSingleton<ILogger<C0103DynamicConfigurationAuditLogger>>(logger);
                        services.AddScoped<C0103DynamicConfigurationAuditLogger>();
                        services.AddScoped<C0103DurableTenantConfigurationAudit>();
                        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(o=>o.TokenValidationParameters=new TokenValidationParameters {
                                ValidateIssuer=true,ValidateAudience=true,
                                ValidateLifetime=true,ValidateIssuerSigningKey=true,
                                ValidIssuer=Issuer,ValidAudience=Audience,
                                IssuerSigningKey=Key,ClockSkew=TimeSpan.Zero
                            });
                        services.AddAuthorization();
                    })
                    .Configure(app=>{
                        app.UseRouting();app.UseAuthentication();app.UseAuthorization();
                        app.UseEndpoints(routes=>routes.MapGet("/test-c0103/{tenant:int}",
                            async context=>{
                                var id=int.Parse(Convert.ToString(context.Request.RouteValues["tenant"])!,
                                    System.Globalization.CultureInfo.InvariantCulture);
                                var ok=await context.RequestServices
                                    .GetRequiredService<C0103DurableTenantConfigurationAudit>()
                                    .TryRecordReadAsync(id,1,FixtureSha,context.RequestAborted);
                                context.Response.StatusCode=ok?200:503;
                            }).RequireAuthorization());
                    })).StartAsync();
            using var http=host.GetTestClient();
            async Task<HttpStatusCode> Probe(int tenant,string? token)
            {
                using var req=new HttpRequestMessage(HttpMethod.Get,"/test-c0103/"+tenant);
                if(token is not null)
                    req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
                using var res=await http.SendAsync(req);
                return res.StatusCode;
            }

            Assert.Equal(HttpStatusCode.Unauthorized,await Probe(tenantA,null));
            Assert.Equal(HttpStatusCode.OK,await Probe(tenantA,SignedToken(actorA)));
            Assert.Equal(HttpStatusCode.OK,await Probe(tenantB,SignedToken(actorB)));
            Assert.Equal(HttpStatusCode.ServiceUnavailable,await Probe(tenantB,SignedToken(actorA)));
            Assert.Equal(2,logger.Emitted);
            await using (var db=new AppDbContext(options))
            {
                var receipts=await db.Set<RegistroAuditoria>().IgnoreQueryFilters()
                    .AsNoTracking().Where(x=>x.Entidad=="ConfigEmpresa" &&
                        x.Descripcion.StartsWith("C0103 authenticated")).ToArrayAsync();
                Assert.Equal(2,receipts.Length);
                Assert.Contains(receipts,x=>x.EmpresaId==tenantA && x.UsuarioId==actorA);
                Assert.Contains(receipts,x=>x.EmpresaId==tenantB && x.UsuarioId==actorB);
                Assert.DoesNotContain(receipts,x=>x.EmpresaId==tenantB && x.UsuarioId==actorA);
                Assert.All(receipts,x=>{
                    Assert.Null(x.ValoresAnteriores);Assert.Null(x.ValoresNuevos);
                    Assert.DoesNotContain("password",x.Descripcion,StringComparison.OrdinalIgnoreCase);
                });
                var role=await db.Roles.SingleAsync(x=>x.Id==roleB);
                role.Activo=false;await db.SaveChangesAsync();
            }
            Assert.Equal(HttpStatusCode.ServiceUnavailable,await Probe(tenantB,SignedToken(actorB)));
            await using (var db=new AppDbContext(options))
            {
                var config=await db.Set<ConfigEmpresa>().SingleAsync(x=>x.EmpresaId==tenantA);
                config.Version=2;await db.SaveChangesAsync();
            }
            Assert.Equal(HttpStatusCode.ServiceUnavailable,await Probe(tenantA,SignedToken(actorA)));
            await using (var db=new AppDbContext(options))
            {
                var config=await db.Set<ConfigEmpresa>().SingleAsync(x=>x.EmpresaId==tenantA);
                config.Version=1;await db.SaveChangesAsync();
                await db.Database.ExecuteSqlRawAsync("DROP TABLE RegistrosAuditoria");
            }
            Assert.Equal(HttpStatusCode.ServiceUnavailable,await Probe(tenantA,SignedToken(actorA)));
            Assert.Equal(2,logger.Emitted);
        }
        finally
        {
            await using var admin=new MySqlConnection(raw);
            await admin.OpenAsync();
            await using var drop=admin.CreateCommand();
            drop.CommandText="DROP DATABASE IF EXISTS "+(char)96+
                new MySqlConnectionStringBuilder(isolated).Database+(char)96;
            await drop.ExecuteNonQueryAsync();
        }
    }
}
