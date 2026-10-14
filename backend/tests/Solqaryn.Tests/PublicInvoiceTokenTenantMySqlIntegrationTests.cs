using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Token público es capability de una sola factura y Empresa; nunca un
/// selector arbitrario por cabecera o por ID. MySQL descartable por prueba.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PublicInvoiceTokenTenantMySqlIntegrationTests
{
    [Fact]
    public async Task TokenA_HabilitaSoloFacturaA_y_RbacA_NiegaTokenDeB()
    {
        var root = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL efímero de CI no configurado.");
        var connection = new MySqlConnectionStringBuilder(root)
        {
            Database = "p4_public_pdf_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, mysql =>
                mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        const string tokenA = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFG";
        const string tokenB = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefg";
        static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        try
        {
            int empresaAId, empresaBId;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var a = new Empresa("PDF público A");
                var b = new Empresa("PDF público B");
                setup.Set<Empresa>().AddRange(a, b);
                await setup.SaveChangesAsync();
                empresaAId = a.Id;
                empresaBId = b.Id;
            }

            async Task<int> CrearFacturaPublicaAsync(int owner, string token)
            {
                var http = new DefaultHttpContext();
                http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = owner;
                await using var db = new AppDbContext(options,
                    tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });

                var venta = new Venta
                {
                    EmpresaId = owner,
                    NumeroVenta = "VENTA-" + owner
                };
                db.Ventas.Add(venta);
                await db.SaveChangesAsync();

                var factura = new Factura
                {
                    EmpresaId = owner,
                    VentaId = venta.Id,
                    NumeroFactura = "FACTURA-" + owner,
                    EmpresaNombre = "EMPRESA " + owner
                };
                db.Facturas.Add(factura);
                await db.SaveChangesAsync();

                var enlace = new EnlacePublicoFactura
                {
                    FacturaId = factura.Id,
                    Token = Hash(token),
                    FechaExpiracion = DateTime.UtcNow.AddHours(2)
                };
                db.EnlacesPublicosFactura.Add(enlace);
                db.Entry(enlace).Property<int?>("EmpresaId").CurrentValue = owner;
                await db.SaveChangesAsync();
                return factura.Id;
            }

            var facturaAId = await CrearFacturaPublicaAsync(empresaAId, tokenA);
            var facturaBId = await CrearFacturaPublicaAsync(empresaBId, tokenB);

            async Task<(int? Scope, int? FacturaVisible, bool FacturaAjenaVisible)>
                ConsultarConTokenAsync(string token, int? rbacEmpresa = null)
            {
                var http = new DefaultHttpContext();
                http.Request.Path = "/facturas/publico/" + token + "/pdf";
                http.Request.Method = HttpMethods.Get;
                if (rbacEmpresa is > 0)
                    http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = rbacEmpresa.Value;
                await using var db = new AppDbContext(options,
                    tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
                var repository = new FacturaCompartirRepository(
                    db, new HttpContextAccessor { HttpContext = http });
                var enlace = await repository.GetPorTokenHashAsync(Hash(token));
                if (enlace is null)
                    return (VerifiedPublicInvoiceTenant.Resolve(http), null, false);

                var owner = VerifiedPublicInvoiceTenant.Resolve(http);
                Assert.True(owner is > 0);
                var visible = await db.Facturas.AsNoTracking().SingleAsync();
                var otra = await db.Facturas.AsNoTracking()
                    .AnyAsync(f => f.Id != enlace.FacturaId);
                Assert.Equal(enlace.FacturaId, visible.Id);
                Assert.Equal(enlace.FacturaId,
                    http.Items[Solqaryn.Application.Common.PublicInvoiceAccessContext.FacturaIdKey]);
                return (owner, visible.Id, otra);
            }

            var a = await ConsultarConTokenAsync(tokenA);
            Assert.Equal(empresaAId, a.Scope);
            Assert.Equal(facturaAId, a.FacturaVisible);
            Assert.False(a.FacturaAjenaVisible);

            var b = await ConsultarConTokenAsync(tokenB);
            Assert.Equal(empresaBId, b.Scope);
            Assert.Equal(facturaBId, b.FacturaVisible);
            Assert.False(b.FacturaAjenaVisible);

            var bloqueado = await ConsultarConTokenAsync(tokenB, empresaAId);
            Assert.Null(bloqueado.Scope);
            Assert.Null(bloqueado.FacturaVisible);

            var incorrecto = await ConsultarConTokenAsync(new string('Z', 43));
            Assert.Null(incorrecto.Scope);
            Assert.Null(incorrecto.FacturaVisible);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
