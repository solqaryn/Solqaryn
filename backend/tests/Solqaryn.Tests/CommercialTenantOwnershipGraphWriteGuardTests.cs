using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class CommercialTenantOwnershipGraphWriteGuardTests
{
    private static AppDbContext NewDb(HttpContext? http = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("commercial-graph-" + Guid.NewGuid().ToString("N"))
            .Options, tenantHttpContextAccessor: http is null ? null :
                new HttpContextAccessor { HttpContext = http });

    [Fact]
    public async Task Un_nuevo_descuento_cliente_hereda_la_empresa_del_cliente()
    {
        await using var db = NewDb();
        var a = new Empresa("Empresa A");
        db.Set<Empresa>().Add(a);
        await db.SaveChangesAsync();
        var client = new Cliente { Nombre = "Cliente A", EmpresaId = a.Id };
        db.Clientes.Add(client);
        await db.SaveChangesAsync();

        var row = new DescuentoCliente { ClienteId = client.Id, DescuentoId = 1 };
        db.Set<DescuentoCliente>().Add(row);
        await db.SaveChangesAsync();
        Assert.Equal(a.Id, db.Entry(row).Property<int?>("EmpresaId").CurrentValue);
    }

    [Fact]
    public async Task Descuento_no_puede_puntar_a_cliente_de_otra_empresa()
    {
        await using var db = NewDb();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a,b);
        await db.SaveChangesAsync();
        var client = new Cliente { Nombre = "Cliente B", EmpresaId = b.Id };
        db.Clientes.Add(client);
        await db.SaveChangesAsync();

        var row = new DescuentoCliente { ClienteId = client.Id, DescuentoId = 1 };
        db.Set<DescuentoCliente>().Add(row);
        db.Entry(row).Property<int?>("EmpresaId").CurrentValue = a.Id;
        await Assert.ThrowsAsync<BusinessRuleException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Marca_A_rechaza_dependiente_del_cliente_B()
    {
        var http = new DefaultHttpContext();
        await using var db = NewDb(http);
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a,b);
        await db.SaveChangesAsync();
        var client = new Cliente { Nombre = "Cliente B", EmpresaId = b.Id };
        db.Clientes.Add(client);
        await db.SaveChangesAsync();

        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = a.Id;
        db.Set<DescuentoCliente>().Add(new DescuentoCliente { ClienteId=client.Id, DescuentoId=1 });
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Empresa_persistida_no_puede_reasignarse_ni_con_el_mismo_cliente()
    {
        await using var db = NewDb();
        var a = new Empresa("Empresa A");
        var b = new Empresa("Empresa B");
        db.Set<Empresa>().AddRange(a,b);
        await db.SaveChangesAsync();
        var client = new Cliente { Nombre="Cliente A", EmpresaId=a.Id };
        db.Clientes.Add(client);
        await db.SaveChangesAsync();
        var row = new DescuentoCliente { ClienteId=client.Id, DescuentoId=1 };
        db.Set<DescuentoCliente>().Add(row);
        await db.SaveChangesAsync();
        db.Entry(row).Property<int?>("EmpresaId").CurrentValue = b.Id;
        await Assert.ThrowsAsync<BusinessRuleException>(()=>db.SaveChangesAsync());
    }

    [Fact]
    public void Descendientes_nuevos_no_pueden_saltar_validacion_con_SaveChanges_sincrono()
    {
        using var db = NewDb();
        db.Set<ImpuestoCliente>().Add(new ImpuestoCliente { ImpuestoId=1, ClienteId=1 });
        Assert.Throws<NotSupportedException>(()=>db.SaveChanges());
    }
}
