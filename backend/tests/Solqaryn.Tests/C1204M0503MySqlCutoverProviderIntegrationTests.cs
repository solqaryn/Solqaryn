using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C12-04.M05.03: Oracle EF Core sobre MySQL 8.4 LOCAL efimero.
/// Verifica lectura A/B y huellas deterministas de datos de prueba, NO un cutover real.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C1204M0503MySqlCutoverProviderIntegrationTests
{
    private static MySqlConnectionStringBuilder ExigirServidorEfimero(string raw)
    {
        var parsed = new MySqlConnectionStringBuilder(raw);
        if (!string.Equals(parsed.Server, "127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(parsed.Server, "localhost", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo MySQL local efimero; prohibido servidor remoto/Aiven.");
        if (parsed.Port != 3306)
            throw new InvalidOperationException("Solo puerto efimero 3306.");
        return parsed;
    }

    [Theory]
    [InlineData("Server=example.com;Port=3306;Database=production;User=root;Password=dummy;")]
    [InlineData("Server=127.0.0.1;Port=3307;Database=production;User=root;Password=dummy;")]
    public void HostRemotoOPuertoNoAislado_SeRechazaAntesDeConectar(string config)
    {
        Assert.Throws<InvalidOperationException>(() => ExigirServidorEfimero(config));
    }

    [Fact]
    public async Task SnapshotMySql8_4_RelecturaEstable_AislamientoA_B_YHeaderNoAutoritativo()
    {
        var root = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("MySQL 8.4 efimero de GitHub Actions requerido.");
        var origen = ExigirServidorEfimero(root);
        var conn = new MySqlConnectionStringBuilder(root)
        {
            Database = "v19_cutover_ab_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(conn, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        try
        {
            int a, b;
            await using (var setup = new AppDbContext(opts))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var ea = new Empresa("V19 Cutover solo ensayo A");
                var eb = new Empresa("V19 Cutover solo ensayo B");
                setup.Set<Empresa>().AddRange(ea, eb);
                await setup.SaveChangesAsync();
                (a, b) = (ea.Id, eb.Id);
                Assert.NotEqual(a, b);
            }

            AppDbContext ContextoConTenant(int empresa, int? headerHostil = null)
            {
                var http = new DefaultHttpContext();
                http.Request.Method = HttpMethods.Get;
                http.Request.Path = "/productos";
                http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresa;
                if (headerHostil.HasValue)
                    http.Request.Headers["X-Empresa-Id"] =
                        headerHostil.Value.ToString(CultureInfo.InvariantCulture);
                return new AppDbContext(opts,
                    tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
            }

            async Task<int> CrearProductoAsync(int empresa, string nombre)
            {
                await using var db = ContextoConTenant(empresa);
                var categoria = new Categoria { Nombre = "V19 categoria " + nombre };
                db.Categorias.Add(categoria);
                await db.SaveChangesAsync();
                var producto = new Producto
                {
                    Nombre = nombre, CategoriaId = categoria.Id,
                    Precio = 90m, Costo = 40m
                };
                db.Productos.Add(producto);
                await db.SaveChangesAsync();
                Assert.Equal(empresa, producto.EmpresaId);
                return producto.Id;
            }

            var productoA = await CrearProductoAsync(a, "Ficha A confidencial");
            var productoB = await CrearProductoAsync(b, "Ficha B confidencial");
            Assert.NotEqual(productoA, productoB);

            async Task<string> HuellaLecturaAsync(int empresa, int productoEsperado, int? header = null)
            {
                await using var db = ContextoConTenant(empresa, header);
                var filas = await db.Productos.AsNoTracking()
                    .OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.EmpresaId, x.Nombre, x.Precio })
                    .ToArrayAsync();
                var unica = Assert.Single(filas);
                Assert.Equal(empresa, unica.EmpresaId);
                Assert.Equal(productoEsperado, unica.Id);
                var serializado = string.Join("|",
                    unica.Id.ToString(CultureInfo.InvariantCulture),
                    unica.EmpresaId.ToString(CultureInfo.InvariantCulture),
                    unica.Nombre,
                    unica.Precio.ToString(CultureInfo.InvariantCulture));
                return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serializado)));
            }

            var huellaA1 = await HuellaLecturaAsync(a, productoA);
            var huellaA2 = await HuellaLecturaAsync(a, productoA, b);
            var huellaB1 = await HuellaLecturaAsync(b, productoB);
            var huellaB2 = await HuellaLecturaAsync(b, productoB, a);
            Assert.Equal(huellaA1, huellaA2);
            Assert.Equal(huellaB1, huellaB2);
            Assert.NotEqual(huellaA1, huellaB1);
            Assert.Equal(64, huellaA1.Length);
            Assert.Equal(64, huellaB1.Length);

            await using var contextoA = ContextoConTenant(a, b);
            Assert.Empty(await contextoA.Productos.AsNoTracking()
                .Where(x => x.Id == productoB).ToListAsync());
            await using var contextoB = ContextoConTenant(b, a);
            Assert.Empty(await contextoB.Productos.AsNoTracking()
                .Where(x => x.Id == productoA).ToListAsync());
            Assert.Equal("127.0.0.1", origen.Server);
        }
        finally
        {
            // Solo la base aleatoria del MySQL local aislado, nunca Aiven ni productivo.
            await using var cleanup = new AppDbContext(opts);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
