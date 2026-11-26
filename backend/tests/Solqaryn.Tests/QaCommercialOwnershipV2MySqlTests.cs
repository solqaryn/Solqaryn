using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Repositories;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class QaCommercialOwnershipV2MySqlTests
{
    [Fact]
    public async Task TarifasKpiYFinanzas_SeparadosPorEmpresa_YPredeterminadoUnico()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL($"Server=localhost;Port=3306;Database=test_qa_residual_{Guid.NewGuid():N};User=root;Password=root;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        try
        {
            int a,b,legacy,usuario;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                await QaCommercialOwnershipV2.VerifyPhysicalAsync(setup);
                var ea = new Empresa("Fixture A");
                var eb = new Empresa("Fixture B");
                var role = new Rol { Nombre = "Fixture residual", NombreNormalizado = "FIXTURE RESIDUAL", Activo = true, Ambito = AmbitoAutorizacion.Empresa };
                setup.Set<Empresa>().AddRange(ea,eb);
                setup.Roles.Add(role);
                await setup.SaveChangesAsync();
                var user = new Usuario { NombreUsuario = "fixture_residual", NombreCompleto = "Fixture", PasswordHash = "unused", RolId = role.Id, Activo = true };
                var old = new CostoEnvio { Nombre = "Legacy sin dueño", Monto = 99m, EsPredeterminado = true };
                setup.Usuarios.Add(user);
                setup.CostosEnvio.Add(old);
                await setup.SaveChangesAsync();
                a=ea.Id; b=eb.Id; legacy=old.Id; usuario=user.Id;
            }
            var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            accessor.HttpContext.Request.Headers["X-Empresa-Id"] = b.ToString();
            var audit = new Mock<IAuditoriaService>();
            var current = Mock.Of<ICurrentUserService>();
            async Task<int> Provision(int empresa, decimal monto)
            {
                accessor.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = empresa;
                await using var db = new AppDbContext(options, httpContextAccessor:accessor);
                var service = new CostoEnvioService(new CostoEnvioRepository(db),current,audit.Object);
                Assert.Empty(await service.GetAllAsync());
                Assert.Null(await service.GetByIdAsync(legacy));
                var tarifa = await service.CreateAsync(new GuardarCostoEnvioDto { Nombre = "General", Monto = monto, EsPredeterminado = true });
                var especifica = await service.CreateAsync(new GuardarCostoEnvioDto { Nombre = "Local", Monto = monto+1, Ciudad = "Fixture", Prioridad = 0 });
                Assert.Equal(especifica.Id,(await service.ResolverAsync(new ResolverCostoEnvioDto { Ciudad=" fixture " }))!.Id);
                Assert.Equal(tarifa.Id,(await service.ResolverAsync(new ResolverCostoEnvioDto { Ciudad="Otra" }))!.Id);
                await Assert.ThrowsAsync<ConflictException>(() => service.EliminarAsync(tarifa.Id));
                var replacement = await service.UpdateAsync(especifica.Id,new GuardarCostoEnvioDto { Nombre="Local", Monto=monto+1, EsPredeterminado=true });
                Assert.Equal(especifica.Id,(await service.GetPredeterminadoVigenteAsync())!.Id);
                Assert.True(await service.EliminarAsync(tarifa.Id));
                Assert.True(await service.ExisteIncluidoEliminadoAsync(tarifa.Id));
                db.DashboardKpiConfiguraciones.Add(new DashboardKpiConfiguracion { UsuarioId=usuario, MetricKey="Fixture", EtiquetaVisible=empresa.ToString() });
                db.MovimientosFinancieros.Add(new MovimientoFinanciero { Concepto="Fixture", Monto=monto, ModuloOrigen="Manual" });
                db.RevisionesFinancieras.Add(new RevisionFinanciera { FechaDesde=DateTime.UtcNow.Date, FechaHasta=DateTime.UtcNow.Date, RevisadoPorUsuarioId=usuario, RevisadoPorNombreUsuario="Fixture" });
                await db.SaveChangesAsync();
                return replacement!.Id;
            }
            int ta=await Provision(a,12m),tb=await Provision(b,27m);
            accessor.HttpContext.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"]=a;
            await using(var scoped = new AppDbContext(options,httpContextAccessor:accessor))
            {
                var svc=new CostoEnvioService(new CostoEnvioRepository(scoped),current,audit.Object);
                Assert.Equal(ta,(await svc.GetPredeterminadoVigenteAsync())!.Id);
                Assert.Null(await svc.GetByIdAsync(tb));
                Assert.False(await svc.ExisteIncluidoEliminadoAsync(tb));
                Assert.False(await svc.CambiarEstadoAsync(tb,false));
                Assert.False(await svc.EliminarAsync(tb));
                Assert.Equal(a.ToString(),(await scoped.DashboardKpiConfiguraciones.SingleAsync()).EtiquetaVisible);
                Assert.Equal(12m,(await scoped.MovimientosFinancieros.SingleAsync()).Monto);
                Assert.Single(await scoped.RevisionesFinancieras.ToListAsync());
            }
            await using(var foreign = new AppDbContext(options,httpContextAccessor:accessor))
            {
                foreign.CostosEnvio.Update(new CostoEnvio { Id=tb, Nombre="Ajena" });
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => foreign.SaveChangesAsync());
            }
            await using(var physical = new AppDbContext(options))
            {
                var duplicate = new CostoEnvio { Nombre="Duplicado", EsPredeterminado=true };
                physical.CostosEnvio.Add(duplicate);
                physical.Entry(duplicate).Property("EmpresaId").CurrentValue=a;
                await Assert.ThrowsAsync<DbUpdateException>(() => physical.SaveChangesAsync());
            }
            accessor.HttpContext.Items.Clear();
            await using(var noGate = new AppDbContext(options,httpContextAccessor:accessor))
            {
                Assert.Empty(await noGate.CostosEnvio.ToListAsync());
                Assert.Empty(await noGate.DashboardKpiConfiguraciones.ToListAsync());
                Assert.Empty(await noGate.MovimientosFinancieros.ToListAsync());
            }
        }
        finally
        {
            await using var cleanup=new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
