using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CommercialTenantRemainingModelTests
{
    private static AppDbContext NewDb(HttpContext? http = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("tenant-last-graph-" + Guid.NewGuid().ToString("N")).Options,
            tenantHttpContextAccessor: http is null ? null :
                new HttpContextAccessor { HttpContext = http });

    public static IEnumerable<object[]> NuevosOwners() => new string[]
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
        "Solqaryn.Domain.Entities.RolPermiso",
    }.Select(x => new object[] { x });

    public static IEnumerable<object[]> PadresCompuestos() => new string[]
    {
        "Solqaryn.Domain.Entities.ConfigEmpresa",
        "Solqaryn.Domain.Entities.ConfiguracionWhatsAppEmpresa",
        "Solqaryn.Domain.Entities.CotizacionDetalle",
        "Solqaryn.Domain.Entities.EmailEmpresarial",
        "Solqaryn.Domain.Entities.MensajeOutbox",
        "Solqaryn.Domain.Entities.PedidoVentaDetalle",
        "Solqaryn.Domain.Entities.PlantillaCorreoEmpresa",
        "Solqaryn.Domain.Entities.PlantillaEmailEmpresarial",
        "Solqaryn.Domain.Entities.RegistroAuditoria",
        "Solqaryn.Domain.Entities.Rol",
        "Solqaryn.Domain.Entities.SecuenciaDocumento",
        "Solqaryn.Domain.Entities.Sucursal",
        "Solqaryn.Domain.Entities.Suscripcion",
        "Solqaryn.Domain.Entities.UsuarioEmpresa",
        "Solqaryn.Domain.Entities.WebhookEntrante",
    }.Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(NuevosOwners))]
    public void Tablas_comerciales_tienen_empresa_nullable_fk_y_unicidad_empresa_id(string name)
    {
        using var db = NewDb();
        var entity = db.Model.FindEntityType(name);
        Assert.NotNull(entity);
        Assert.True(entity!.FindProperty("EmpresaId")!.IsNullable);
        Assert.Contains(entity.GetIndexes(), ix => ix.IsUnique &&
            ix.Properties.Select(p => p.Name).SequenceEqual(new[] { "EmpresaId", "Id" }));
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Empresa) &&
            fk.Properties.Count == 1 && fk.Properties[0].Name == "EmpresaId" &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Theory]
    [MemberData(nameof(PadresCompuestos))]
    public void Padres_existentes_admiten_fks_tenant_compuestas(string name)
    {
        using var db = NewDb();
        var entity = db.Model.FindEntityType(name);
        Assert.NotNull(entity);
        Assert.Contains(entity!.GetIndexes(), ix => ix.IsUnique &&
            ix.Properties.Select(p => p.Name).SequenceEqual(new[] { "EmpresaId", "Id" }));
    }

    [Theory]
    [InlineData(typeof(AjusteInventario), "NumeroAjuste")]
    [InlineData(typeof(Almacen), "CodigoActivoUnico")]
    [InlineData(typeof(Solqaryn.Domain.Entities.Bancos.CuentaBancaria), "BancoId", "NumeroCuenta")]
    [InlineData(typeof(EmpresaConfiguracion), "ActivaUnica")]
    [InlineData(typeof(Impuesto), "Codigo")]
    [InlineData(typeof(Marca), "NombreActivoUnico")]
    [InlineData(typeof(TipoCliente), "Codigo")]
    public void Unicidades_de_negocio_separadas_por_empresa(
        Type type, string field1, string? field2 = null)
    {
        using var db = NewDb();
        var entity = db.Model.FindEntityType(type)!;
        var keys = field2 is null
            ? new[] { "EmpresaId", field1 }
            : new[] { "EmpresaId", field1, field2 };
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(p => p.Name).SequenceEqual(keys));
        // Una clave natural no debe seguir siendo global si pertenece al negocio.
        Assert.DoesNotContain(entity.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(p => p.Name).SequenceEqual(keys.Skip(1)));
    }

    [Fact]
    public void Definiciones_globales_de_plataforma_no_reciben_owner_de_cliente()
    {
        using var db = NewDb();
        Assert.Null(db.Model.FindEntityType(typeof(Permiso))!.FindProperty("EmpresaId"));
        Assert.Null(db.Model.FindEntityType(typeof(Empresa))!.FindProperty("EmpresaId"));
    }

    [Fact]
    public async Task Categoria_nueva_hereda_empresa_verificada_y_rechaza_cambio_a_otro_tenant()
    {
        var http = new DefaultHttpContext();
        await using var db = NewDb(http);
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;

        var categoria = new Categoria { Nombre = "Almacén A" };
        db.Set<Categoria>().Add(categoria);
        await db.SaveChangesAsync();
        Assert.Equal(a.Id, db.Entry(categoria).Property<int?>("EmpresaId").CurrentValue);

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = b.Id;
        categoria.Nombre = "No pertenece a B";
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Plantilla_de_rol_empresarial_con_owner_null_mantiene_matriz_compartida()
    {
        var http = new DefaultHttpContext();
        await using var db = NewDb(http);
        var empresa = new Empresa("Empresa A");
        db.Set<Empresa>().Add(empresa);
        var plantilla = new Rol
        {
            Nombre = "Plantilla E2E",
            NombreNormalizado = "PLANTILLA_E2E",
            Ambito = Solqaryn.Domain.Enums.AmbitoAutorizacion.Empresa
        };
        var permiso = new Permiso { Codigo = "PRODUCTOS.LEER", Nombre = "Leer productos" };
        db.Roles.Add(plantilla);
        db.Permisos.Add(permiso);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresa.Id;
        var grant = new RolPermiso { RolId = plantilla.Id, PermisoId = permiso.Id };
        db.RolPermisos.Add(grant);
        await db.SaveChangesAsync();
        Assert.Null(db.Entry(grant).Property<int?>("EmpresaId").CurrentValue);
    }

    [Fact]
    public async Task Rol_con_owner_B_no_puede_crear_grant_desde_empresa_A()
    {
        var http = new DefaultHttpContext();
        await using var db = NewDb(http);
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a, b);
        await db.SaveChangesAsync();

        var roleB = new Rol
        {
            Nombre = "Rol B",
            NombreNormalizado = "ROL_B",
            EmpresaId = b.Id,
            Ambito = Solqaryn.Domain.Enums.AmbitoAutorizacion.Empresa
        };
        var permiso = new Permiso { Codigo = "CLIENTES.LEER", Nombre = "Leer clientes" };
        db.Roles.Add(roleB);
        db.Permisos.Add(permiso);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        db.RolPermisos.Add(new RolPermiso { RolId = roleB.Id, PermisoId = permiso.Id });
        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Rol_plataforma_no_se_modifica_con_marca_empresarial()
    {
        var http = new DefaultHttpContext();
        await using var db = NewDb(http);
        var a = new Empresa("Empresa A");
        db.Set<Empresa>().Add(a);
        var plataforma = new Rol
        {
            Nombre = "Plataforma",
            NombreNormalizado = "PLATAFORMA",
            Ambito = Solqaryn.Domain.Enums.AmbitoAutorizacion.Plataforma
        };
        var permiso = new Permiso { Codigo = "PLATAFORMA.LEER", Nombre = "Plataforma" };
        db.Roles.Add(plataforma);
        db.Permisos.Add(permiso);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        db.RolPermisos.Add(new RolPermiso { RolId = plataforma.Id, PermisoId = permiso.Id });
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public void RolPermiso_tiene_propiedad_empresarial_y_rol_combinado()
    {
        using var db = NewDb();
        var entity = db.Model.FindEntityType(typeof(RolPermiso))!;
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "EmpresaId", "RolId", "PermisoId" }));
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Empresa) &&
            fk.Properties.Single().Name == "EmpresaId");
    }
}
