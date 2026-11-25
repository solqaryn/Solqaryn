using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

// C15-03.M05.05: actual Oracle EF/MySQL and service authorization.
// The authenticated actor and verified request marker are supplied at the
// service boundary, not through JWT/HTTP. The P0 HTTP quarantine is untouched.
[Trait("Category", "Integration")]
public sealed class C1503M0505CargaMasivaOwnershipIntegrationTests
{
    private static AppDbContext Context(DbContextOptions<AppDbContext> options, int empresaId)
    {
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = empresaId;
        return new AppDbContext(options,
            tenantHttpContextAccessor: new HttpContextAccessor { HttpContext = http });
    }

    private static CargaMasivaService Service(AppDbContext db, int actorId)
    {
        var actor = new Mock<ICurrentUserService>();
        actor.SetupGet(x => x.UsuarioId).Returns((int?)actorId);
        actor.SetupGet(x => x.EstaAutenticado).Returns(true);
        // Deliberately contradictory legacy role: live tenant role must win.
        actor.SetupGet(x => x.EsAdministrador).Returns(true);
        return new CargaMasivaService(db, new UnitOfWork(db), actor.Object,
            Mock.Of<IAuditoriaService>(), NullLogger<CargaMasivaService>.Instance,
            Mock.Of<ITipoClientePredeterminadoResolver>(),
            new UsuarioScopeService(db, actor.Object));
    }

    private static async Task<int> JobAsync(DbContextOptions<AppDbContext> options,
        int empresaId, int actorId, string name, byte[]? file = null,
        EstadoCargaMasiva state = EstadoCargaMasiva.Validada)
    {
        await using var db = Context(options, empresaId);
        var job = new CargaMasiva
        {
            Tipo = TipoCargaMasiva.VariantesInventario,
            Estado = state,
            NombreArchivo = name + ".csv",
            Extension = ".csv",
            ContentType = "text/csv",
            TamanoBytes = 64,
            HashArchivo = file is null ? Guid.NewGuid().ToString("N").PadRight(64, '0')
                : Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant(),
            DatosNormalizadosJson = "[]",
            CreadoPorUsuarioId = actorId,
            CreadoPorNombreUsuario = name
        };
        db.CargasMasivas.Add(job);
        await db.SaveChangesAsync();
        Assert.Equal(empresaId, db.Entry(job).Property<int?>("EmpresaId").CurrentValue);
        return job.Id;
    }

    [Fact]
    public async Task RealMySqlJobsRequireLiveTenantMembershipAndCreatorEvenWithLegacyAdmin()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral MySQL is mandatory");
        var cs = new MySqlConnectionStringBuilder(raw);
        if (cs.Server is not ("127.0.0.1" or "localhost") || cs.Port != 3306)
            throw new InvalidOperationException("Only local CI MySQL is allowed");
        cs.Database = "c1503_jobs_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(cs.ConnectionString, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        try
        {
            int aId, bId, actorA, peerA, actorB;
            await using (var setup = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
                var a = new Empresa("C1503 jobs A");
                var b = new Empresa("C1503 jobs B");
                setup.Set<Empresa>().AddRange(a, b);
                await setup.SaveChangesAsync();
                aId = a.Id;
                bId = b.Id;
                var roleA = new Rol
                {
                    Nombre = "C1503 jobs A member", NombreNormalizado = "C1503_JOBS_A_MEMBER",
                    Ambito = AmbitoAutorizacion.Empresa, EmpresaId = aId,
                    EsSistema = false, EsAdministrador = false, Activo = true
                };
                var roleB = new Rol
                {
                    Nombre = "C1503 jobs B member", NombreNormalizado = "C1503_JOBS_B_MEMBER",
                    Ambito = AmbitoAutorizacion.Empresa, EmpresaId = bId,
                    EsSistema = false, EsAdministrador = false, Activo = true
                };
                setup.Roles.AddRange(roleA, roleB);
                await setup.SaveChangesAsync();
                var uA = new Usuario { NombreUsuario = "c1503-job-a", NombreCompleto = "Actor A",
                    PasswordHash = "not-authenticatable", RolId = roleA.Id, Activo = true };
                var pA = new Usuario { NombreUsuario = "c1503-job-peer-a", NombreCompleto = "Peer A",
                    PasswordHash = "not-authenticatable", RolId = roleA.Id, Activo = true };
                var uB = new Usuario { NombreUsuario = "c1503-job-b", NombreCompleto = "Actor B",
                    PasswordHash = "not-authenticatable", RolId = roleB.Id, Activo = true };
                setup.Usuarios.AddRange(uA, pA, uB);
                await setup.SaveChangesAsync();
                actorA = uA.Id;
                peerA = pA.Id;
                actorB = uB.Id;
                setup.UsuarioEmpresas.AddRange(
                    new UsuarioEmpresa(actorA, aId, roleA.Id),
                    new UsuarioEmpresa(peerA, aId, roleA.Id),
                    new UsuarioEmpresa(actorB, bId, roleB.Id));
                await setup.SaveChangesAsync();
            }
            var ownA = await JobAsync(options, aId, actorA, "own-a");
            var otherA = await JobAsync(options, aId, peerA, "peer-a");
            var ownB = await JobAsync(options, bId, actorB, "own-b");
            Assert.NotEqual(ownA, otherA);
            Assert.NotEqual(ownA, ownB);

            await using (var db = Context(options, aId))
            {
                var service = Service(db, actorA);
                Assert.Equal(ownA, (await service.GetByIdAsync(ownA))?.Id);
                await service.AutorizarConfirmacionAsync(ownA);
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.AutorizarConfirmacionAsync(otherA));
                var foreign = await Assert.ThrowsAsync<BusinessRuleException>(() => service.AutorizarConfirmacionAsync(ownB));
                var missing = await Assert.ThrowsAsync<BusinessRuleException>(() => service.AutorizarConfirmacionAsync(int.MaxValue));
                Assert.Equal(missing.Message, foreign.Message);
                using (var cancelled = new CancellationTokenSource())
                {
                    cancelled.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                        service.AutorizarConfirmacionAsync(ownA, cancelled.Token));
                }
                await using (var lockDb = Context(options, aId))
                {
                    await using var held = await CargaMasivaConfirmationLock.TryAcquireAsync(lockDb, otherA, CancellationToken.None);
                    Assert.NotNull(held);
                    var controller = new Solqaryn.API.Controllers.CargasMasivasController(service, db);
                    await Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
                    {
                        await controller.Confirmar(otherA, CancellationToken.None);
                    });
                }
                await using (var lockDb = Context(options, aId))
                {
                    await using var held = await CargaMasivaConfirmationLock.TryAcquireAsync(lockDb, ownA, CancellationToken.None);
                    Assert.NotNull(held);
                    var controller = new Solqaryn.API.Controllers.CargasMasivasController(service, db);
                    var busy = await Assert.ThrowsAsync<BusinessRuleException>(async () =>
                    {
                        await controller.Confirmar(ownA, CancellationToken.None);
                    });
                    Assert.Equal("Esta carga ya está siendo confirmada por otra solicitud. Espera a que finalice.", busy.Message);
                }
                Assert.Null(await service.GetByIdAsync(ownB));
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetByIdAsync(otherA));
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ConfirmarAsync(otherA));
                var page = await service.GetPagedAsync(new PagedRequest { PageSize = 100 });
                Assert.Equal(1, page.TotalCount);
                Assert.Equal(ownA, Assert.Single(page.Items).Id);
            }
            await using (var db = Context(options, bId))
            {
                var service = Service(db, actorB);
                Assert.Equal(ownB, (await service.GetByIdAsync(ownB))?.Id);
                Assert.Null(await service.GetByIdAsync(ownA));
                Assert.Null(await service.GetByIdAsync(otherA));
                var page = await service.GetPagedAsync(new PagedRequest { PageSize = 100 });
                Assert.Equal(1, page.TotalCount);
                Assert.Equal(ownB, Assert.Single(page.Items).Id);
                var wrongActor = Service(db, actorA);
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => wrongActor.GetByIdAsync(ownB));
            }
            var confirmedBytes = Encoding.UTF8.GetBytes("peer-confirmed-file");
            var pendingBytes = Encoding.UTF8.GetBytes("peer-pending-file");
            var confirmedId = await JobAsync(options, aId, peerA, "confirmed-peer",
                confirmedBytes, EstadoCargaMasiva.Confirmada);
            var pendingId = await JobAsync(options, aId, peerA, "pending-peer", pendingBytes);

            async Task<CargaMasivaDetalleDto> ValidateAsync(CargaMasivaService service, byte[] bytes)
            {
                using var stream = new MemoryStream(bytes);
                return await service.ValidarAsync(TipoCargaMasiva.VariantesInventario,
                    "duplicate.csv", "text/csv", bytes.Length, stream);
            }

            await using (var db = Context(options, aId))
            {
                var unauthorized = Service(db, actorA);
                var deniedConfirmed = await Assert.ThrowsAsync<ForbiddenAccessException>(
                    () => ValidateAsync(unauthorized, confirmedBytes));
                var deniedPending = await Assert.ThrowsAsync<ForbiddenAccessException>(
                    () => ValidateAsync(unauthorized, pendingBytes));
                Assert.Equal("No tienes acceso a esta carga masiva.", deniedConfirmed.Message);
                Assert.Equal(deniedConfirmed.Message, deniedPending.Message);
                var owner = Service(db, peerA);
                var ownConfirmed = await Assert.ThrowsAsync<BusinessRuleException>(
                    () => ValidateAsync(owner, confirmedBytes));
                Assert.Contains($"#{confirmedId}", ownConfirmed.Message);
                var ownPending = await ValidateAsync(owner, pendingBytes);
                Assert.Equal(pendingId, ownPending.Id);
                Assert.True(ownPending.ArchivoReutilizado);
                Assert.Equal(4, await db.CargasMasivas.CountAsync());
            }

            await using (var db = new AppDbContext(options))
            {
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => Service(db, actorA)
                    .GetPagedAsync(new PagedRequest()));
                var membership = await db.UsuarioEmpresas.IgnoreQueryFilters()
                    .SingleAsync(x => x.UsuarioId == actorA && x.EmpresaId == aId);
                membership.Desactivar();
                await db.SaveChangesAsync();
            }
            await using (var db = Context(options, aId))
            {
                var service = Service(db, actorA);
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetByIdAsync(ownA));
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.GetPagedAsync(new PagedRequest()));
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => ValidateAsync(service, confirmedBytes));
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.AutorizarConfirmacionAsync(ownA));
                await Assert.ThrowsAsync<ForbiddenAccessException>(() => service.ConfirmarAsync(ownA));
            }
            await using (var db = Context(options, aId))
            {
                // A denied confirmation must leave both the peer's job and the
                // revoked actor's job unchanged in the physical database.
                var jobs = await db.CargasMasivas.AsNoTracking()
                    .Where(x => x.Id == otherA || x.Id == ownA).ToListAsync();
                Assert.Equal(2, jobs.Count);
                Assert.All(jobs, job =>
                {
                    Assert.Equal(EstadoCargaMasiva.Validada, job.Estado);
                    Assert.Null(job.ErrorGeneral);
                    Assert.Null(job.FechaConfirmacion);
                });
            }
            await using (var db = Context(options, bId))
                Assert.Equal(ownB, (await Service(db, actorB).GetByIdAsync(ownB))?.Id);
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
