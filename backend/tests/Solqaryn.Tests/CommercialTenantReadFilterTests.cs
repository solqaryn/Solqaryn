using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// Defensa de lecturas EF: incluye raíces, descendientes e inventario,
/// preservando la contención HTTP P0 para rutas públicas/no EF.
/// </summary>
public sealed class CommercialTenantReadFilterTests
{
    private static AppDbContext CreateDb(HttpContext? http = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("tenant-read-filters-" + Guid.NewGuid().ToString("N"))
                .Options,
            tenantHttpContextAccessor: http is null
                ? null
                : new HttpContextAccessor { HttpContext = http });

    [Fact]
    public void Todas_las_entidades_con_empresa_tienen_filtro_y_globales_no()
    {
        using var db = CreateDb();
        var owners = db.Model.GetEntityTypes()
            .Where(entity => entity.BaseType is null &&
                             entity.FindProperty("EmpresaId") is not null)
            .ToList();

        Assert.Equal(124, owners.Count);
        Assert.All(owners, entity =>
            Assert.NotNull(entity.GetQueryFilter()));

        Assert.Null(db.Model.FindEntityType(typeof(Empresa))!.GetQueryFilter());
        Assert.Null(db.Model.FindEntityType(typeof(Permiso))!.GetQueryFilter());
    }

    [Fact]
    public void Filtro_tenant_preserva_el_borrado_logico_en_productos_y_categorias()
    {
        using var db = CreateDb();
        var producto = db.Model.FindEntityType(typeof(Producto))!.GetQueryFilter()!;
        var categoria = db.Model.FindEntityType(typeof(Categoria))!.GetQueryFilter()!;

        Assert.Contains("Eliminado", producto.ToString(), StringComparison.Ordinal);
        Assert.Contains("Eliminada", categoria.ToString(), StringComparison.Ordinal);
        Assert.Contains("EmpresaId", producto.ToString(), StringComparison.Ordinal);
        Assert.Contains("EmpresaId", categoria.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Lectura_por_tenant_aisla_A_B_y_excluye_legado_sin_owner()
    {
        var http = new DefaultHttpContext();
        await using var db = CreateDb(http);
        var a = new Empresa("LECTURAS A");
        var b = new Empresa("LECTURAS B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var legacy = new Categoria { Nombre = "sin dueño histórico" };
        db.Categorias.Add(legacy);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var categoriaA = new Categoria { Nombre = "solo A" };
        db.Categorias.Add(categoriaA);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        var categoriaB = new Categoria { Nombre = "solo B" };
        db.Categorias.Add(categoriaB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var vistasA = await db.Categorias.AsNoTracking().ToListAsync();
        Assert.Single(vistasA);
        Assert.Equal(categoriaA.Id, vistasA[0].Id);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        var vistasB = await db.Categorias.AsNoTracking().ToListAsync();
        Assert.Single(vistasB);
        Assert.Equal(categoriaB.Id, vistasB[0].Id);

        // Asegura que el modelo EF cacheado no fija el tenant del primer request.
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        Assert.Equal(categoriaA.Id,
            (await db.Categorias.AsNoTracking().SingleAsync()).Id);
    }

    [Fact]
    public async Task Plantillas_RBAC_siguen_visibles_sin_mostrar_roles_de_B_en_A()
    {
        var http = new DefaultHttpContext();
        await using var db = CreateDb(http);
        var a = new Empresa("ROLES A");
        var b = new Empresa("ROLES B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var plantilla = new Rol
        {
            Nombre = "PLANTILLA",
            NombreNormalizado = "PLANTILLA",
            EsSistema = true
        };
        db.Roles.Add(plantilla);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        var soloB = new Rol
        {
            Nombre = "PRIVADO B",
            NombreNormalizado = "PRIVADO_B",
            EmpresaId = b.Id
        };
        db.Roles.Add(soloB);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var visibles = await db.Roles.AsNoTracking().ToListAsync();
        Assert.Contains(visibles, role => role.Id == plantilla.Id);
        Assert.DoesNotContain(visibles, role => role.Id == soloB.Id);
    }
}
