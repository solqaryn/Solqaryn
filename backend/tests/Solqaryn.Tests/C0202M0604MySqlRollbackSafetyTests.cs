using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C02-02.M06.04: actual EF Oracle / MySQL 8.4 ephemeral transaction rollback.
/// No client data, production DSN, live tenant router, Aiven or cutover is involved.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0202M0604MySqlRollbackSafetyTests
{
    private static MySqlConnectionStringBuilder LocalOnly(string value)
    {
        var parsed = new MySqlConnectionStringBuilder(value);
        if (parsed.Server is not ("127.0.0.1" or "localhost") || parsed.Port != 3306)
            throw new InvalidOperationException("Only ephemeral localhost:3306 MySQL is allowed.");
        return parsed;
    }

    [Theory]
    [InlineData("Server=outside.invalid;Port=3306;Database=customer;User=root;Password=synthetic;")]
    [InlineData("Server=localhost;Port=3307;Database=customer;User=root;Password=synthetic;")]
    public void RemoteOrWrongPortDatabaseIsRejectedBeforeUse(string value)
    {
        Assert.Throws<InvalidOperationException>(() => LocalOnly(value));
    }

    [Fact]
    public async Task ExplicitAndImplicitRollbacksPreserveTwoLegitimateCompaniesAcrossContexts()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("The GitHub-hosted MySQL 8.4 ephemeral service is required.");
        var isolated = new MySqlConnectionStringBuilder(LocalOnly(raw).ConnectionString)
        {
            Database = "v20_c0202_m0604_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            int legitimateA, legitimateB;
            await using (var db = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
                var tenantA = new Empresa("V20 M06.04 legitimate enterprise A");
                var tenantB = new Empresa("V20 M06.04 legitimate enterprise B");
                db.Set<Empresa>().AddRange(tenantA, tenantB);
                await db.SaveChangesAsync();
                legitimateA = tenantA.Id;
                legitimateB = tenantB.Id;
                Assert.True(legitimateA > 0);
                Assert.True(legitimateB > 0);
                Assert.NotEqual(legitimateA, legitimateB);
            }

            for (var attempt = 0; attempt < 2; attempt++)
            {
                int provisionalId;
                await using (var db = new AppDbContext(options))
                {
                    await using var transaction = await db.Database.BeginTransactionAsync();
                    var provisional = new Empresa("V20 M06.04 provisional rollback " + attempt);
                    db.Set<Empresa>().Add(provisional);
                    await db.SaveChangesAsync();
                    provisionalId = provisional.Id;
                    Assert.True(provisionalId > 0);

                    // The first attempt explicitly rolls back; the second exits
                    // without Commit, simulating a worker interruption. Dispose
                    // must roll back this uncommitted transaction.
                    if (attempt == 0)
                        await transaction.RollbackAsync();
                }

                // Durable readback from a NEW context is required: do not
                // accept an in-memory ChangeTracker snapshot as restoration.
                await using (var verified = new AppDbContext(options))
                {
                    Assert.False(await verified.Set<Empresa>().AsNoTracking()
                        .AnyAsync(x => x.Id == provisionalId));
                    Assert.Equal(2, await verified.Set<Empresa>().AsNoTracking()
                        .CountAsync(x => x.Id == legitimateA || x.Id == legitimateB));
                    Assert.True(await verified.Set<Empresa>().AsNoTracking()
                        .AnyAsync(x => x.Id == legitimateA));
                    Assert.True(await verified.Set<Empresa>().AsNoTracking()
                        .AnyAsync(x => x.Id == legitimateB));
                }
            }

            // SQL persistence facts alone NEVER create tenancy, JWT or RBAC
            // authority for a live storage placement operation.
            var proof = new V5SharedDedicatedPlacementDomainRules.TrustedPlacementSnapshot
            {
                VerifiedTenantId = "tenant_" + legitimateA,
                AuthenticatedActorId = "unverified-ci-actor",
                AssignmentOwnerTenantId = "tenant_" + legitimateA,
                CurrentAssignmentRevision = 1,
                JwtValidatedByBackend = false,
                CurrentMembershipVerified = false,
                TrustedConfigurationAvailable = false,
                UniqueActiveAssignmentVerified = false
            };
            var proposal = new V5SharedDedicatedPlacementDomainRules.PlacementCandidate
            {
                Intent = V5SharedDedicatedPlacementDomainRules.Intent.InspectExistingAssignment,
                RequestedTenantId = "tenant_" + legitimateB,
                TargetMode = V5SharedDedicatedPlacementDomainRules.PlacementMode.Shared,
                ExpectedAssignmentRevision = 1
            };
            var denied = V5SharedDedicatedPlacementDomainRules.Evaluate(proof, proposal);
            Assert.False(denied.AuthorizesConnection);
            Assert.False(denied.AuthorizesMigration);
        }
        finally
        {
            // The only destructive operation targets THIS fresh random CI DB.
            // Never delete, migrate or restore customer or shared production DBs.
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
