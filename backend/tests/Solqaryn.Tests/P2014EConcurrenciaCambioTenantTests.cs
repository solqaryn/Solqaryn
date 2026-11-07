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
/// P2-014.E: prueba negativa de contaminacion de alcance al alternar el tenant
/// verificado A/B/A dentro del MISMO DbContext de cada flujo concurrente.
/// EF InMemory no acredita HTTP/JWT ni transacciones MySQL reales.
/// </summary>
public sealed class P2014EConcurrenciaCambioTenantTests
{
    [Fact]
    public async Task Ocho_flujos_ABA_no_reutilizan_el_tenant_anterior_ni_obedecen_header_cruzado()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("p2014e-" + Guid.NewGuid().ToString("N"))
            .Options;
        const string folio = "P2014E-FOLIO-REPETIDO";
        int empresaAId;
        int empresaBId;

        var seedRequest = new DefaultHttpContext();
        await using (var seed = new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = seedRequest }))
        {
            var empresaA = new Empresa("P2014E Empresa A");
            var empresaB = new Empresa("P2014E Empresa B");
            seed.Set<Empresa>().AddRange(empresaA, empresaB);
            await seed.SaveChangesAsync();

            empresaAId = empresaA.Id;
            empresaBId = empresaB.Id;
            seedRequest.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaAId;
            seed.Ventas.Add(new Venta { EmpresaId = empresaAId, NumeroVenta = folio });
            await seed.SaveChangesAsync();

            seedRequest.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaBId;
            seed.Ventas.Add(new Venta { EmpresaId = empresaBId, NumeroVenta = folio });
            await seed.SaveChangesAsync();

            Assert.Equal(2, await seed.Ventas.IgnoreQueryFilters()
                .CountAsync(v => v.NumeroVenta == folio));
        }

        async Task<int?[][]> ConsultarABAAisladoAsync(int primero, int contrario)
        {
            var request = new DefaultHttpContext();
            var accessor = new HttpContextAccessor { HttpContext = request };
            await Task.Yield();
            await using var db = new AppDbContext(options, tenantHttpContextAccessor: accessor);

            async Task<int?[]> LeerAsync(int verificado, int cabeceraFalsa)
            {
                request.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = verificado;
                request.Request.Headers["X-Empresa-Id"] = cabeceraFalsa.ToString();
                return await db.Ventas.AsNoTracking()
                    .Where(v => v.NumeroVenta == folio)
                    .Select(v => v.EmpresaId)
                    .ToArrayAsync();
            }

            return new[]
            {
                await LeerAsync(primero, contrario),
                await LeerAsync(contrario, primero),
                await LeerAsync(primero, contrario)
            };
        }

        // No compartir DbContext entre Tasks; cada contexto debe resolver el
        // tenant actual en cada consulta, incluso despues de cambiarlo dos veces.
        var tareas = Enumerable.Range(0, 8)
            .Select(i => Task.Run(() => ConsultarABAAisladoAsync(
                i % 2 == 0 ? empresaAId : empresaBId,
                i % 2 == 0 ? empresaBId : empresaAId)))
            .ToArray();
        var resultados = await Task.WhenAll(tareas);

        for (var i = 0; i < resultados.Length; i++)
        {
            var primero = i % 2 == 0 ? empresaAId : empresaBId;
            var contrario = i % 2 == 0 ? empresaBId : empresaAId;
            Assert.Equal(new int?[] { primero }, resultados[i][0]);
            Assert.Equal(new int?[] { contrario }, resultados[i][1]);
            Assert.Equal(new int?[] { primero }, resultados[i][2]);
        }
    }
}
