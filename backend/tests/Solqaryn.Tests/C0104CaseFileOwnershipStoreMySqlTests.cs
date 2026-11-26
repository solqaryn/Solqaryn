using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.AutomationV5;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class C0104CaseFileOwnershipStoreMySqlTests
{
    // Explicit synthetic source for storage integration only: no production grant.
    private sealed class FixtureAuthority(int tenant, int branch, int actor)
        : V5CaseFileOwnershipStore.IWriteAuthority
    {
        public bool RevokeAfterWrite { get; set; }
        private int _calls;
        public ValueTask<V5CaseFileOwnershipStore.WriteAuthority?> ResolveAsync(
            AppDbContext db, V5CaseFileOwnershipStore.Selection selection, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Assert.NotNull(db.Database.CurrentTransaction);
            _calls++;
            return ValueTask.FromResult<V5CaseFileOwnershipStore.WriteAuthority?>(
                selection.ResourceType != "fixture" || (RevokeAfterWrite && _calls > 1)
                    ? null : new(actor, tenant, branch, selection.CaseId,
                        selection.ResourceType, selection.ResourceRef));
        }
    }

    [Fact]
    public async Task Store_IsolatesTenants_AppendsVersions_AndRollsBackRevocation()
    {
        var origin = new MySqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral MySQL required."));
        if (origin.Server is not ("127.0.0.1" or "localhost") || origin.Port != 3306)
            throw new InvalidOperationException("Only isolated localhost MySQL allowed.");
        origin.Database = "v44_c0104_store_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(origin.ConnectionString, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        await using var setup = new AppDbContext(options);
        try
        {
            await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
            // Exercise only the additive migration's empty-schema downgrade/reapply.
            await setup.Database.MigrateAsync("20261117215700_V19WhatsAppE164LiteralPlus");
            Assert.Contains("20261126165100_V44CaseFileOwnershipStore",
                await setup.Database.GetPendingMigrationsAsync());
            await setup.Database.MigrateAsync();
            Assert.Empty(await setup.Database.GetPendingMigrationsAsync());
            var a = new Empresa("QA A");
            var b = new Empresa("QA B");
            setup.Set<Empresa>().AddRange(a,b);
            await setup.SaveChangesAsync();
            var branchA = new Sucursal { EmpresaId = a.Id, Codigo = "QA_A", Nombre = "QA A" };
            var branchB = new Sucursal { EmpresaId = b.Id, Codigo = "QA_B", Nombre = "QA B" };
            setup.Set<Sucursal>().AddRange(branchA,branchB);
            // Phase7 bootstrap does not guarantee a user. Seed only this disposable DB.
            var role = new Rol { Nombre = "QA Casefile", NombreNormalizado = "QA_CASEFILE",
                EsSistema = true, Activo = true };
            setup.Roles.Add(role);
            await setup.SaveChangesAsync();
            var user = new Usuario { NombreUsuario = "qa-casefile", NombreCompleto = "QA Casefile",
                PasswordHash = "fixture-no-login", RolId = role.Id, Activo = true };
            setup.Usuarios.Add(user);
            await setup.SaveChangesAsync();
            var actor = user.Id;
            setup.UsuarioEmpresas.AddRange(new UsuarioEmpresa(actor, a.Id, role.Id),
                new UsuarioEmpresa(actor, b.Id, role.Id));
            await setup.SaveChangesAsync();
            HttpContextAccessor Access(int tenant)
            {
                var h = new DefaultHttpContext();
                h.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenant;
                return new() { HttpContext = h };
            }
            await using var dbA = new AppDbContext(options, tenantHttpContextAccessor: Access(a.Id));
            await using var dbB = new AppDbContext(options, tenantHttpContextAccessor: Access(b.Id));
            var storeA = new V5CaseFileOwnershipStore(dbA, new FixtureAuthority(a.Id, branchA.Id, actor));
            var storeB = new V5CaseFileOwnershipStore(dbB, new FixtureAuthority(b.Id, branchB.Id, actor));
            var one = new V5CaseFileOwnershipStore.Selection(1,"fixture","r1",null,new string('a',64),"create");
            var two = one with { CaseId = 2, ResourceRef = "r2" };
            Assert.Equal(1L, (await storeA.AppendAsync(one)).Version);
            Assert.Equal(1L, (await storeA.AppendAsync(two)).Version);
            Assert.Equal(1L, (await storeB.AppendAsync(one)).Version);
            Assert.Null(await storeB.ReadCandidateAsync(a.Id,1,branchA.Id));
            Assert.Null(await storeA.ReadCandidateAsync(a.Id,1,branchB.Id));
            Assert.Null(await storeB.ReadCandidateAsync(b.Id,2,branchB.Id));
            Assert.Equal(2L, (await storeA.AppendAsync(one with { ExpectedVersion = 1 })).Version);
            Assert.Equal("CASEFILE_VERSION_CONFLICT",
                (await storeA.AppendAsync(one with { ExpectedVersion = 1 })).Code);
            Assert.Equal(2L,(await storeA.ReadCandidateAsync(a.Id,1,branchA.Id))!.Version);
            Assert.Equal(1L,(await storeB.ReadCandidateAsync(b.Id,1,branchB.Id))!.Version);
            var revoke = new FixtureAuthority(a.Id,branchA.Id,actor) { RevokeAfterWrite = true };
            var revokedStore = new V5CaseFileOwnershipStore(dbA,revoke);
            var three = one with { CaseId = 3, ResourceRef = "r3" };
            Assert.Equal("CASEFILE_WRITE_DENIED",(await revokedStore.AppendAsync(three)).Code);
            Assert.Null(await storeA.ReadCandidateAsync(a.Id,3,branchA.Id));
            Assert.Equal(1L,(await storeA.AppendAsync(three)).Version);
            Assert.Equal("CASEFILE_WRITE_DENIED",
                (await storeA.AppendAsync(one with { CaseId = 4, ResourceType = "unmapped" })).Code);
            Assert.Null(await storeA.ReadCandidateAsync(a.Id,4,branchA.Id));
        }
        finally { await setup.Database.EnsureDeletedAsync(); }
    }
}
