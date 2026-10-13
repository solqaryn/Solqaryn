using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CommercialTenantIndependentOwnershipTests
{
    private static AppDbContext Db(HttpContext? http = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("tenant-independent-" + Guid.NewGuid().ToString("N"))
            .Options,
            tenantHttpContextAccessor: http is null ? null :
                new HttpContextAccessor { HttpContext = http });

    public static IEnumerable<object[]> EntityTypes() => new[]
    {
        "Solqaryn.Domain.Entities.AjusteInventario",
        "Solqaryn.Domain.Entities.Almacen",
        "Solqaryn.Domain.Entities.Contabilidad.AsientoDetalle",
        "Solqaryn.Domain.Entities.Contabilidad.AsientoContable",
        "Solqaryn.Domain.Entities.Cajas.CajaMovimiento",
        "Solqaryn.Domain.Entities.Cajas.Caja",
        "Solqaryn.Domain.Entities.Cajas.CajaSesion",
        "Solqaryn.Domain.Entities.CargaMasivaError",
        "Solqaryn.Domain.Entities.CargaMasiva",
        "Solqaryn.Domain.Entities.Categoria",
        "Solqaryn.Domain.Entities.Contabilidad.CentroCosto",
        "Solqaryn.Domain.Entities.Color",
        "Solqaryn.Domain.Entities.Bancos.ConciliacionBancaria",
        "Solqaryn.Domain.Entities.Contabilidad.ConfiguracionContable",
        "Solqaryn.Domain.Entities.ConsumoInsumo",
        "Solqaryn.Domain.Entities.ConteoInventario",
        "Solqaryn.Domain.Entities.CostoEnvio",
        "Solqaryn.Domain.Entities.Bancos.CuentaBancaria",
        "Solqaryn.Domain.Entities.CuentaContable",
        "Solqaryn.Domain.Entities.DashboardKpiConfiguracion",
        "Solqaryn.Domain.Entities.DescuentoCategoria",
        "Solqaryn.Domain.Entities.DescuentoRol",
        "Solqaryn.Domain.Entities.Descuento",
        "Solqaryn.Domain.Entities.EmpresaConfiguracion",
        "Solqaryn.Domain.Entities.EnlacePublicoFactura",
        "Solqaryn.Domain.Entities.HistorialAplicacionImpuesto",
        "Solqaryn.Domain.Entities.HistorialEnvioFactura",
        "Solqaryn.Domain.Entities.ImpuestoCategoria",
        "Solqaryn.Domain.Entities.ImpuestoOperacion",
        "Solqaryn.Domain.Entities.Impuesto",
        "Solqaryn.Domain.Entities.Marca",
        "Solqaryn.Domain.Entities.Modelo",
        "Solqaryn.Domain.Entities.Bancos.MovimientoEstadoCuenta",
        "Solqaryn.Domain.Entities.Contabilidad.PeriodoContable",
        "Solqaryn.Domain.Entities.PoliticaCosteoInventario",
        "Solqaryn.Domain.Entities.RevisionFinanciera",
        "Solqaryn.Domain.Entities.Talla",
        "Solqaryn.Domain.Entities.TemaVisual",
        "Solqaryn.Domain.Entities.TipoCliente",
        "Solqaryn.Domain.Entities.TransferenciaInventario",
        "Solqaryn.Domain.Entities.UbicacionAlmacen",
    }.Select(x => new object[] { x });

    public static IEnumerable<object[]> GlobalUniquesConverted() => new object[][]
    {
        new object[] { "Solqaryn.Domain.Entities.AjusteInventario", new[] { "NumeroAjuste" } },
        new object[] { "Solqaryn.Domain.Entities.Almacen", new[] { "CodigoActivoUnico" } },
        new object[] { "Solqaryn.Domain.Entities.Contabilidad.AsientoContable", new[] { "Numero" } },
        new object[] { "Solqaryn.Domain.Entities.Cajas.Caja", new[] { "SesionActivaId" } },
        new object[] { "Solqaryn.Domain.Entities.CargaMasiva", new[] { "Tipo", "HashArchivo" } },
        new object[] { "Solqaryn.Domain.Entities.Contabilidad.CentroCosto", new[] { "CodigoActivoUnico" } },
        new object[] { "Solqaryn.Domain.Entities.Color", new[] { "NombreActivoUnico" } },
        new object[] { "Solqaryn.Domain.Entities.Contabilidad.ConfiguracionContable", new[] { "Evento" } },
        new object[] { "Solqaryn.Domain.Entities.ConsumoInsumo", new[] { "NumeroConsumo" } },
        new object[] { "Solqaryn.Domain.Entities.ConteoInventario", new[] { "Numero" } },
        new object[] { "Solqaryn.Domain.Entities.CostoEnvio", new[] { "PredeterminadoActivoUnico" } },
        new object[] { "Solqaryn.Domain.Entities.Bancos.CuentaBancaria", new[] { "BancoId", "NumeroCuenta" } },
        new object[] { "Solqaryn.Domain.Entities.CuentaContable", new[] { "Codigo" } },
        new object[] { "Solqaryn.Domain.Entities.DashboardKpiConfiguracion", new[] { "RolId", "MetricKey" } },
        new object[] { "Solqaryn.Domain.Entities.DashboardKpiConfiguracion", new[] { "UsuarioId", "MetricKey" } },
        new object[] { "Solqaryn.Domain.Entities.DescuentoCategoria", new[] { "DescuentoId", "CategoriaId" } },
        new object[] { "Solqaryn.Domain.Entities.DescuentoRol", new[] { "DescuentoId", "RolId" } },
        new object[] { "Solqaryn.Domain.Entities.Descuento", new[] { "CodigoPromocionalNormalizado" } },
        new object[] { "Solqaryn.Domain.Entities.EmpresaConfiguracion", new[] { "ActivaUnica" } },
        new object[] { "Solqaryn.Domain.Entities.ImpuestoCategoria", new[] { "ImpuestoId", "CategoriaId" } },
        new object[] { "Solqaryn.Domain.Entities.ImpuestoOperacion", new[] { "ImpuestoId", "Operacion" } },
        new object[] { "Solqaryn.Domain.Entities.Impuesto", new[] { "Codigo" } },
        new object[] { "Solqaryn.Domain.Entities.Marca", new[] { "NombreActivoUnico" } },
        new object[] { "Solqaryn.Domain.Entities.Modelo", new[] { "NombreMarcaActivoUnico" } },
        new object[] { "Solqaryn.Domain.Entities.Bancos.MovimientoEstadoCuenta", new[] { "ConciliacionBancariaId", "IdempotencyKey" } },
        new object[] { "Solqaryn.Domain.Entities.Contabilidad.PeriodoContable", new[] { "FechaInicio", "FechaFin" } },
        new object[] { "Solqaryn.Domain.Entities.PoliticaCosteoInventario", new[] { "EmpresaConfiguracionVigenteId" } },
        new object[] { "Solqaryn.Domain.Entities.Talla", new[] { "NombreActivoUnico" } },
        new object[] { "Solqaryn.Domain.Entities.TipoCliente", new[] { "Codigo" } },
        new object[] { "Solqaryn.Domain.Entities.TipoCliente", new[] { "EsPredeterminadoUnico" } },
        new object[] { "Solqaryn.Domain.Entities.TipoCliente", new[] { "NombreNormalizado" } },
        new object[] { "Solqaryn.Domain.Entities.TransferenciaInventario", new[] { "Numero" } },
        new object[] { "Solqaryn.Domain.Entities.UbicacionAlmacen", new[] { "AlmacenId", "CodigoActivoUnico" } },
    };

    [Theory]
    [MemberData(nameof(EntityTypes))]
    public void Cada_recurso_comercial_independiente_tiene_empresa_fk_e_identidad_local(string entityName)
    {
        using var db = Db();
        var e = db.Model.FindEntityType(entityName)!;
        Assert.NotNull(e);
        Assert.True(e.FindProperty("EmpresaId")!.IsNullable);
        Assert.Contains(e.GetIndexes(), idx => idx.IsUnique &&
            idx.Properties.Select(p => p.Name).SequenceEqual(new[] { "EmpresaId", "Id" }));
        Assert.Contains(e.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Empresa) &&
            fk.Properties.SingleOrDefault()?.Name == "EmpresaId" &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Theory]
    [MemberData(nameof(GlobalUniquesConverted))]
    public void Unicidad_comercial_deja_de_ser_global(string entityName, string[] oldFields)
    {
        using var db = Db();
        var e=db.Model.FindEntityType(entityName)!;
        Assert.Contains(e.GetIndexes(), idx => idx.IsUnique &&
            idx.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "EmpresaId" }.Concat(oldFields)));
        Assert.DoesNotContain(e.GetIndexes(), idx => idx.IsUnique &&
            idx.Properties.Select(p => p.Name).SequenceEqual(oldFields));
    }

    [Fact]
    public async Task Catalogos_creados_por_empresa_A_reciben_su_owner_verificado()
    {
        var http = new DefaultHttpContext();
        await using var db=Db(http);
        var a = new Empresa("Empresa A");
        db.Set<Empresa>().Add(a);
        await db.SaveChangesAsync();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        var categoria=new Categoria { Nombre="Categoria A" };
        var marca=new Marca { Nombre="Marca A" };
        db.Categorias.Add(categoria);
        db.Marcas.Add(marca);
        await db.SaveChangesAsync();
        Assert.Equal(a.Id, db.Entry(categoria).Property<int?>("EmpresaId").CurrentValue);
        Assert.Equal(a.Id, db.Entry(marca).Property<int?>("EmpresaId").CurrentValue);

        var modelo=new Modelo { MarcaId=marca.Id, Nombre="Modelo A" };
        db.Modelos.Add(modelo);
        await db.SaveChangesAsync();
        Assert.Equal(a.Id, db.Entry(modelo).Property<int?>("EmpresaId").CurrentValue);
    }

    [Fact]
    public async Task Modelo_de_tenant_B_no_puede_referenciar_marca_de_A()
    {
        var http=new DefaultHttpContext();
        await using var db=Db(http);
        var a=new Empresa("Empresa A");
        var b=new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a,b);
        await db.SaveChangesAsync();
        var marcaA = new Marca { Nombre="Marca A" };
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        db.Marcas.Add(marcaA);
        await db.SaveChangesAsync();

        var modeloB=new Modelo { Nombre="Modelo B", MarcaId=marcaA.Id };
        db.Modelos.Add(modeloB);
        db.Entry(modeloB).Property<int?>("EmpresaId").CurrentValue=b.Id;
        await Assert.ThrowsAsync<ForbiddenAccessException>(()=>db.SaveChangesAsync());
    }

    [Fact]
    public void El_modelo_tiene_123_entidades_scoped_y_excepciones_de_plataforma_deliberadas()
    {
        using var db=Db();
        var owned=db.Model.GetEntityTypes().Count(e => e.FindProperty("EmpresaId") != null);
        Assert.Equal(123, owned);
        var globales=new[] {
            typeof(Empresa), typeof(Usuario), typeof(Permiso),
            typeof(RolPermiso), typeof(Plan), typeof(PlanModulo)
        };
        Assert.All(globales, type => Assert.Null(db.Model.FindEntityType(type)?.FindProperty("EmpresaId")));
    }
}
