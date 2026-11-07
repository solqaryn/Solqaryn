using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-014.D: solicitud concurrente A/B con NumeroVenta identico y cabeceras cruzadas.
/// EF InMemory no acredita JWT/HTTP real ni transacciones MySQL.
/// </summary>
public sealed class P2014DConcurrenciaFolioIdenticoTenantTests
{
    [Fact]
    public async Task Folio_identico_AB_con_ocho_solicitudes_paralelas_no_cruza_tenants()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2014d-" + Guid.NewGuid().ToString("N"))
            .Options;

        var seedHttp = new DefaultHttpContext();
        await using (var seed = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = seedHttp }))
        {
            var empresaA = new Empresa("P2014D Concurrente A");
            var empresaB = new Empresa("P2014D Concurrente B");
            seed.Set<Empresa>().AddRange(empresaA, empresaB);
            await seed.SaveChangesAsync();

            const string folio = "P2014D-FOLIO-HOMONIMO";
            seedHttp.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaA.Id;
            seed.Ventas.Add(new Venta { EmpresaId = empresaA.Id, NumeroVenta = folio });
            await seed.SaveChangesAsync();

            seedHttp.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaB.Id;
            seed.Ventas.Add(new Venta { EmpresaId = empresaB.Id, NumeroVenta = folio });
            await seed.SaveChangesAsync();

            Assert.Equal(2, await seed.Ventas.IgnoreQueryFilters()
                .CountAsync(v => v.NumeroVenta == folio));

            async Task<int[]> LeerConSolicitudAisladaAsync(int verificada, int headerFalsificado)
            {
                // HttpContextAccessor es AsyncLocal: establecer dentro del flujo de cada
                // tarea, y nunca compartir un DbContext entre consultas paralelas.
                var request = new DefaultHttpContext();
                request.Request.Headers["X-Empresa-Id"] = headerFalsificado.ToString();
                request.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = verificada;
                var accessor = new HttpContextAccessor { HttpContext = request };
                await Task.Yield();
                await using var db = new AppDbContext(options,
                    tenantHttpContextAccessor: accessor);
                return await db.Ventas.AsNoTracking()
                    .Where(v => v.NumeroVenta == folio)
                    .Select(v => v.EmpresaId)
                    .ToArrayAsync();
            }

            var tareas = Enumerable.Range(0, 8)
                .Select(i => Task.Run(() => LeerConSolicitudAisladaAsync(
                    i % 2 == 0 ? empresaA.Id : empresaB.Id,
                    i % 2 == 0 ? empresaB.Id : empresaA.Id)))
                .ToArray();
            var resultados = await Task.WhenAll(tareas);
            for (var i = 0; i < resultados.Length; i++)
            {
                var empresaEsperada = i % 2 == 0 ? empresaA.Id : empresaB.Id;
                Assert.Equal(new[] { empresaEsperada }, resultados[i]);
            }
        }
    }
}
