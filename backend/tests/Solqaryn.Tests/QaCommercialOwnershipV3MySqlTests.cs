using System.Data;
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
            async Task<(int Category,int Discount,int Tax,int Count,int BankEntry,int Finance,int Detail,int Variant,int Reconciliation,int Account)> Create(int owner,int warehouse)
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
                var product=new Producto { Nombre="Producto para conteo",CategoriaId=c.Id,Costo=3,Precio=5 };
                db.Productos.Add(product);
                await db.SaveChangesAsync();
                var variant=new ProductoVariante { ProductoId=product.Id,Sku="V3-CONTEO" };
                db.ProductoVariantes.Add(variant);
                await db.SaveChangesAsync();
                var detail=new ConteoInventarioDetalle { ConteoInventarioId=count.Id,AlmacenId=warehouse,ProductoVarianteId=variant.Id };
                detail.MaterializarSnapshot(10);
                db.Set<ConteoInventarioDetalle>().Add(detail);
                await db.SaveChangesAsync();
                Assert.Single(await db.Set<ConteoInventario>().ToListAsync());
                Assert.Single(await db.Set<ConteoInventarioDetalle>().ToListAsync());
                Assert.Single(await db.CuentasBancarias.ToListAsync());
                Assert.Single(await db.Set<MatchConciliacion>().ToListAsync());
                Assert.False(await db.Categorias.AnyAsync(x=>x.Id==legacy));
                return(c.Id,discount.Id,tax.Id,count.Id,statement.Id,finance.Id,detail.Id,variant.Id,reconciliation.Id,account.Id);
            }
            var aa=await Create(a,warehouseA);var bb=await Create(b,warehouseB);

            // A fresh context must load persisted statement lines and matches,
            // not depend on relationship fix-up from the fixture writer.
            foreach (var scope in new[] {
                (Owner:a, Own:aa, Foreign:bb),
                (Owner:b, Own:bb, Foreign:aa) })
            {
                accessor.HttpContext!.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"]=scope.Owner;
                await using (var db=new AppDbContext(options,httpContextAccessor:accessor))
                {
                    var repo=new ConciliacionBancariaRepository(db);
                    Assert.Null(await repo.GetByIdAsync(scope.Foreign.Reconciliation));
                    Assert.Null(await repo.GetActivaByCuentaAsync(scope.Foreign.Account));
                    var own=await repo.GetByIdAsync(scope.Own.Reconciliation);
                    Assert.NotNull(own);
                    var line=Assert.Single(own.Movimientos);
                    Assert.Equal(scope.Own.BankEntry,line.Id);
                    Assert.Equal(scope.Own.Finance,Assert.Single(line.Matches).MovimientoFinancieroId);
                    Assert.Equal(5m,line.MontoConciliado);
                    Assert.Equal(5m,line.MontoPendiente);
                    Assert.Equal(5m,own.SaldoConciliado);
                    Assert.Equal(5m,own.Diferencia);
                }
                await using (var db=new AppDbContext(options,httpContextAccessor:accessor))
                {
                    var repo=new ConciliacionBancariaRepository(db);
                    var draft=await repo.GetActivaByCuentaAsync(scope.Own.Account);
                    Assert.NotNull(draft);
                    Assert.Equal(EstadoConciliacionBancaria.Borrador,draft.Estado);
                    Assert.Equal(scope.Own.Reconciliation,draft.Id);
                    Assert.Equal(5m,Assert.Single(draft.Movimientos).MontoConciliado);
                }
                await using (var db=new AppDbContext(options,httpContextAccessor:accessor))
                {
                    var repo=new ConciliacionBancariaRepository(db);
                    var page=await repo.GetPagedAsync(null,null,null,null,1,20);
                    Assert.Equal(1,page.TotalCount);
                    var own=Assert.Single(page.Items);
                    Assert.Equal(scope.Own.Reconciliation,own.Id);
                    Assert.Equal(5m,own.SaldoConciliado);
                    Assert.Equal(5m,own.Diferencia);
                }
                await using (var db=new AppDbContext(options,httpContextAccessor:accessor))
                {
                    var repo=new ConciliacionBancariaRepository(db);
                    var own=await repo.GetByIdAsync(scope.Own.Reconciliation);
                    Assert.NotNull(own);
                    var month=own.FechaInicio.Month;
                    var year=own.FechaInicio.Year;
                    db.ChangeTracker.Clear();
                    Assert.Null(await repo.GetByPeriodoAsync(scope.Foreign.Account,month,year));
                    var period=await repo.GetByPeriodoAsync(scope.Own.Account,month,year);
                    Assert.NotNull(period);
                    Assert.Equal(5m,period.SaldoConciliado);
                }
                await using (var db=new AppDbContext(options,httpContextAccessor:accessor))
                {
                    await using var tx=await db.Database.BeginTransactionAsync();
                    var repo=new ConciliacionBancariaRepository(db);
                    var own=await repo.GetActivaByCuentaAsync(scope.Own.Account);
                    Assert.NotNull(own);
                    var otherDate=own.FechaInicio.AddMonths(1);
                    await repo.AddAsync(new ConciliacionBancaria(scope.Own.Account,otherDate,otherDate,0,0));
                    await repo.SaveChangesAsync();
                    db.ChangeTracker.Clear();
                    await Assert.ThrowsAsync<BusinessRuleException>(()=>repo.GetActivaByCuentaAsync(scope.Own.Account));
                    await tx.RollbackAsync();
                }
            }

            // The operational catalogue must work with verified membership
            // read scope alone, without granting an administrative write scope.
            accessor.HttpContext!.Items.Remove("SOLQARYN_AUTHORIZED_EMPRESA_ID");
            var readKey = AppDbContext.AuthorizedCommercialReadEmpresaItemKey;
            accessor.HttpContext.Items[readKey] = a;
            List<int> methodIdsA;
            await using (var readA = new AppDbContext(options, httpContextAccessor: accessor))
            {
                var methods = await new MetodoPagoRepository(readA).GetActivosAsync();
                Assert.Equal(4, methods.Count);
                Assert.All(methods, method => Assert.True(method.Activo && !method.Eliminado));
                methodIdsA = methods.Select(method => method.Id).ToList();
                Assert.Null(readA.AuthorizedEmpresaId);
                Assert.Null(readA.CommercialWriteEmpresaId);
                Assert.False(readA.CommercialPrivateWriteAuthorized);
                await using (var tx=await readA.Database.BeginTransactionAsync())
                {
                    Assert.NotNull(await new ConteoInventarioRepository(readA).GetByIdAsync(aa.Count));
                    Assert.Null(await new ConteoInventarioRepository(readA).GetByIdForUpdateAsync(aa.Count));
                    await tx.RollbackAsync();
                }
                readA.Add(new MetodoPagoCatalogo { Codigo = "READ_ONLY_DENIED", Nombre = "No autorizado", Tipo = "Efectivo" });
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => readA.SaveChangesAsync());
            }
            accessor.HttpContext.Items[readKey] = b;
            await using (var readB = new AppDbContext(options, httpContextAccessor: accessor))
            {
                var methods = await new MetodoPagoRepository(readB).GetActivosAsync();
                Assert.Equal(4, methods.Count);
                Assert.All(methods, method => Assert.DoesNotContain(method.Id, methodIdsA));
                Assert.Null(await new MetodoPagoRepository(readB).GetByIdAsync(methodIdsA[0]));
            }
            accessor.HttpContext.Items.Remove(readKey);
            await using (var noRead = new AppDbContext(options, httpContextAccessor: accessor))
            {
                Assert.Empty(await new MetodoPagoRepository(noRead).GetActivosAsync());
                await using var tx=await noRead.Database.BeginTransactionAsync();
                Assert.Null(await new ConteoInventarioRepository(noRead).GetByIdForUpdateAsync(aa.Count));
                await tx.RollbackAsync();
            }
            accessor.HttpContext.Items[AppDbContext.PublicStoreTenantItemKey]=a;
            await using (var publicScope = new AppDbContext(options, httpContextAccessor: accessor))
            {
                await using var tx=await publicScope.Database.BeginTransactionAsync();
                Assert.Null(await new ConteoInventarioRepository(publicScope).GetByIdForUpdateAsync(aa.Count));
                await tx.RollbackAsync();
            }
            accessor.HttpContext.Items.Remove(AppDbContext.PublicStoreTenantItemKey);

            // A foreign primary key must not acquire or wait on the other
            // company's row lock. Use the deployed V3 owner index and bound
            // lock waits in this disposable MySQL database.
            foreach (var scope in new[] {
                (Owner:a, Own:aa, Foreign:bb),
                (Owner:b, Own:bb, Foreign:aa) })
            {
                accessor.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"]=scope.Owner;
                await using var blocker=new AppDbContext(options);
                await using var held=await blocker.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                Assert.NotNull(await new ConteoInventarioRepository(blocker).GetByIdForUpdateAsync(scope.Foreign.Count));
                await using var scoped=new AppDbContext(options,httpContextAccessor:accessor);
                await scoped.Database.OpenConnectionAsync();
                await scoped.Database.ExecuteSqlRawAsync("SET SESSION innodb_lock_wait_timeout = 3");
                var repo=new ConteoInventarioRepository(scoped);
                Assert.Null(await repo.GetByIdAsync(scope.Foreign.Count));
                Assert.False(await scoped.Set<ConteoInventarioDetalle>().AnyAsync(x=>x.Id==scope.Foreign.Detail));
                await Assert.ThrowsAsync<InvalidOperationException>(()=>repo.GetByIdForUpdateAsync(scope.Own.Count));
                await using var tx=await scoped.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
                Assert.Null(await repo.GetByIdForUpdateAsync(scope.Foreign.Count));
                var own=await repo.GetByIdForUpdateAsync(scope.Own.Count);
                Assert.NotNull(own);
                var line=Assert.Single(own.Detalles);
                Assert.Equal(scope.Own.Detail,line.Id);
                Assert.Equal(scope.Own.Variant,line.ProductoVarianteId);
                Assert.Equal(10,line.StockEsperadoSnapshot);
                Assert.Equal(scope.Owner,scoped.Entry(line).Property<int?>("EmpresaId").CurrentValue);
                await tx.RollbackAsync();
                await held.RollbackAsync();
            }

            accessor.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"]=a;
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
                cross.Set<ConteoInventarioDetalle>().Add(new ConteoInventarioDetalle {
                    ConteoInventarioId=aa.Count,AlmacenId=warehouseA,ProductoVarianteId=bb.Variant });
                await Assert.ThrowsAsync<ForbiddenAccessException>(()=>cross.SaveChangesAsync());
            }
            await using (var physical=new AppDbContext(options))
            {
                var detail=new ConteoInventarioDetalle {
                    ConteoInventarioId=aa.Count,AlmacenId=warehouseA,ProductoVarianteId=bb.Variant };
                physical.Set<ConteoInventarioDetalle>().Add(detail);
                physical.Entry(detail).Property("EmpresaId").CurrentValue=a;
                await Assert.ThrowsAsync<DbUpdateException>(()=>physical.SaveChangesAsync());
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
