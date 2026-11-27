using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Exceptions;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Entities.Catalogos;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Enums.Bancos;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;
using MetodoPagoCatalogo = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class QaCommercialOwnershipV3MySqlTests
{
    [Fact]
    public async Task CatalogosImpuestosDescuentosGaleriasConteosYConciliacion_AislamientoEmpresarial()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database=test_qa_v3_{Guid.NewGuid():N};User=root;Password=root;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        try
        {
            int a,b,legacy,warehouseA,warehouseB,bank;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                await QaCommercialOwnershipV3.VerifyPhysicalAsync(setup);
                var ea=new Empresa("V3 A"); var eb=new Empresa("V3 B");
                var old=new Categoria { Nombre="Historica" };
                var banco=new Banco { Codigo="V3", Nombre="Banco QA" };
                setup.AddRange(ea,eb,old,banco);
                await setup.SaveChangesAsync();
                a=ea.Id; b=eb.Id; legacy=old.Id; bank=banco.Id;
                var sa=new Sucursal { EmpresaId=a,Codigo="V3",Nombre="Sucursal A" };
                var sb=new Sucursal { EmpresaId=b,Codigo="V3",Nombre="Sucursal B" };
                var wa=new Almacen { Sucursal=sa,Codigo="V3A",Nombre="Almacén A",Tipo=TipoAlmacen.Tienda };
                var wb=new Almacen { Sucursal=sb,Codigo="V3B",Nombre="Almacén B",Tipo=TipoAlmacen.Tienda };
                setup.AddRange(sa,sb,wa,wb);
                await setup.SaveChangesAsync();
                warehouseA=wa.Id; warehouseB=wb.Id;
                await using var transaction=await setup.Database.BeginTransactionAsync();
                await QaCommercialOwnershipV3.ProvisionDefaultsAsync(setup,a);
                await QaCommercialOwnershipV3.ProvisionDefaultsAsync(setup,b);
                await QaCommercialOwnershipV3.ProvisionDefaultsAsync(setup,a);
                await transaction.CommitAsync();
            }
            async Task<(int Category,int Discount,int Tax,int Count,int BankEntry,int Finance)> Create(int owner,int warehouse)
            {
                accessor.HttpContext!.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"]=owner;
                await using var db=new AppDbContext(options,httpContextAccessor:accessor);
                var c=new Categoria { Nombre="Misma categoría" };
                var brand=new Marca { Nombre="Misma marca" };
                var color=new Color { Nombre="Mismo color" };
                var size=new Talla { Nombre="M" };
                var account=new CuentaBancaria(bank,"Cuenta QA","V3-01","HNL");
                var finance=new MovimientoFinanciero { Tipo=TipoMovimientoFinanciero.Ingreso,Categoria=CategoriaMovimientoFinanciero.Otro,Concepto="QA",Monto=10,ModuloOrigen="Manual" };
                db.AddRange(c,brand,color,size,account,finance);
                await db.SaveChangesAsync();
                var model=new Modelo { Nombre="Mismo modelo", MarcaId=brand.Id };
                var discount=new Descuento { Nombre="Mismo descuento", CodigoPromocional="V3", CodigoPromocionalNormalizado="V3", Tipo=TipoDescuento.Porcentaje, Valor=5 };
                var tax=new Impuesto { Nombre="Mismo impuesto", Codigo="V3", Tipo=TipoImpuesto.Porcentaje, Tasa=15 };
                var count=new ConteoInventario { Numero="V3-01",AlmacenId=warehouse,CategoriaId=c.Id };
                var reconciliation=new ConciliacionBancaria(account.Id,DateTime.UtcNow.Date,DateTime.UtcNow.Date,0,10);
                var statement=new MovimientoEstadoCuenta("V3-01",DateTime.UtcNow,"QA","QA",TipoMovimientoEstadoCuenta.Credito,10);
                reconciliation.AgregarMovimiento(statement);
                db.AddRange(model,discount,tax,count,reconciliation);
                await db.SaveChangesAsync();
                statement.AgregarMatch(finance.Id,5,TipoMatchConciliacion.Manual);
                db.AddRange(new DescuentoCategoria { DescuentoId=discount.Id,CategoriaId=c.Id },new ImpuestoCategoria { ImpuestoId=tax.Id,CategoriaId=c.Id },
                    new CategoriaGaleria { EmpresaId=owner,CategoriaId=c.Id,Version="v3",ImagenesJson="[]" });
                await db.SaveChangesAsync();
                Assert.Single(await db.Categorias.ToListAsync());
                Assert.Single(await db.Marcas.ToListAsync());
                Assert.Single(await db.Modelos.ToListAsync());
                Assert.Single(await db.Colores.ToListAsync());
                Assert.Single(await db.Tallas.ToListAsync());
                Assert.Single(await db.TipoClientes.Where(t=>t.EsPredeterminado).ToListAsync());
                Assert.Equal(4,await db.Set<MetodoPagoCatalogo>().CountAsync());
                Assert.Single(await db.Descuentos.ToListAsync());
                Assert.Single(await db.Impuestos.ToListAsync());
                Assert.Single(await db.CategoriasGalerias.ToListAsync());
                Assert.Single(await db.Set<ConteoInventario>().ToListAsync());
                Assert.Single(await db.CuentasBancarias.ToListAsync());
                Assert.Single(await db.Set<MatchConciliacion>().ToListAsync());
                Assert.False(await db.Categorias.AnyAsync(x=>x.Id==legacy));
                return(c.Id,discount.Id,tax.Id,count.Id,statement.Id,finance.Id);
            }
            var aa=await Create(a,warehouseA);var bb=await Create(b,warehouseB);
            accessor.HttpContext!.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"]=a;
            await using (var read=new AppDbContext(options,httpContextAccessor:accessor))
            {
                Assert.Null(await new CategoriaRepository(read).GetByIdAsync(bb.Category));
                Assert.False(await read.Categorias.IgnoreQueryFilters(new[]{QaCommercialOwnershipV1.SoftDeleteFilter}).AnyAsync(x=>x.Id==bb.Category));
                Assert.False(await read.Descuentos.AnyAsync(x=>x.Id==bb.Discount));
                Assert.False(await read.Impuestos.AnyAsync(x=>x.Id==bb.Tax));
                Assert.False(await read.CategoriasGalerias.AnyAsync(x=>x.EmpresaId==b));
                Assert.False(await read.Set<ConteoInventario>().AnyAsync(x=>x.Id==bb.Count));
                Assert.False(await read.Set<MovimientoEstadoCuenta>().AnyAsync(x=>x.Id==bb.BankEntry));
            }
            await using (var cross=new AppDbContext(options,httpContextAccessor:accessor))
            {
                cross.DescuentoCategorias.Add(new DescuentoCategoria { DescuentoId=aa.Discount,CategoriaId=bb.Category });
                await Assert.ThrowsAsync<ForbiddenAccessException>(()=>cross.SaveChangesAsync());
            }
            await using (var cross=new AppDbContext(options,httpContextAccessor:accessor))
            {
                cross.ImpuestoCategorias.Add(new ImpuestoCategoria { ImpuestoId=aa.Tax,CategoriaId=bb.Category });
                await Assert.ThrowsAsync<ForbiddenAccessException>(()=>cross.SaveChangesAsync());
            }
            await using (var cross=new AppDbContext(options,httpContextAccessor:accessor))
            {
                cross.CategoriasGalerias.Add(new CategoriaGaleria { EmpresaId=a,CategoriaId=bb.Category,Version="no",ImagenesJson="[]" });
                await Assert.ThrowsAsync<ForbiddenAccessException>(()=>cross.SaveChangesAsync());
            }
            await using (var cross=new AppDbContext(options,httpContextAccessor:accessor))
            {
                cross.Set<ConteoInventario>().Add(new ConteoInventario { Numero="NO",AlmacenId=warehouseB,CategoriaId=aa.Category });
                await Assert.ThrowsAsync<ForbiddenAccessException>(()=>cross.SaveChangesAsync());
            }
            await using (var cross=new AppDbContext(options,httpContextAccessor:accessor))
            {
                var statement=await cross.Set<MovimientoEstadoCuenta>().Include(x=>x.Matches).SingleAsync(x=>x.Id==aa.BankEntry);
                statement.AgregarMatch(bb.Finance,1,TipoMatchConciliacion.Manual);
                await Assert.ThrowsAsync<ForbiddenAccessException>(()=>cross.SaveChangesAsync());
            }
            await using (var physical=new AppDbContext(options))
            {
                var link=new ImpuestoCategoria { ImpuestoId=aa.Tax,CategoriaId=bb.Category };
                physical.ImpuestoCategorias.Add(link);
                physical.Entry(link).Property("EmpresaId").CurrentValue=a;
                await Assert.ThrowsAsync<DbUpdateException>(()=>physical.SaveChangesAsync());
            }
            await using (var physical=new AppDbContext(options))
            {
                var count=new ConteoInventario { Numero="NO-FISICO",AlmacenId=warehouseB,CategoriaId=aa.Category };
                physical.Set<ConteoInventario>().Add(count);
                physical.Entry(count).Property("EmpresaId").CurrentValue=a;
                await Assert.ThrowsAsync<DbUpdateException>(()=>physical.SaveChangesAsync());
            }
            await using (var physical=new AppDbContext(options))
            {
                var statement=await physical.Set<MovimientoEstadoCuenta>().Include(x=>x.Matches).SingleAsync(x=>x.Id==bb.BankEntry);
                statement.AgregarMatch(aa.Finance,1,TipoMatchConciliacion.Manual);
                var match=statement.Matches.Single(x=>x.MovimientoFinancieroId==aa.Finance);
                physical.ChangeTracker.DetectChanges();
                physical.Entry(match).Property("EmpresaId").CurrentValue=a;
                await Assert.ThrowsAsync<DbUpdateException>(()=>physical.SaveChangesAsync());
            }
            accessor.HttpContext.Items.Remove("SOLQARYN_AUTHORIZED_EMPRESA_ID");
            await using (var noScope=new AppDbContext(options,httpContextAccessor:accessor))
            {
                Assert.Empty(await noScope.Descuentos.ToListAsync());
                Assert.Empty(await noScope.Impuestos.ToListAsync());
                Assert.Empty(await noScope.CategoriasGalerias.ToListAsync());
                Assert.Empty(await noScope.CuentasBancarias.ToListAsync());
            }
        }
        finally
        {
            accessor.HttpContext=null;
            await using var cleanup=new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
