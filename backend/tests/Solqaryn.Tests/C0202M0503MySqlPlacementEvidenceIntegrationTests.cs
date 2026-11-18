using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Domain.AutomationV5;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// C02-02.M05.03 — EF Oracle / MySQL 8.4 ephemeral persistence boundary.
/// Never queries Aiven, production, user DSNs, or a live placement router.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0202M0503MySqlPlacementEvidenceIntegrationTests
{
    private static MySqlConnectionStringBuilder LocalOnly(string raw)
    {
        var parsed = new MySqlConnectionStringBuilder(raw);
        if (parsed.Server is not ("127.0.0.1" or "localhost") || parsed.Port != 3306)
            throw new InvalidOperationException("C02-02.M05.03 only allows isolated localhost:3306 MySQL.");
        return parsed;
    }

    [Theory]
    [InlineData("Server=example.invalid;Port=3306;Database=live;User=root;Password=synthetic;")]
    [InlineData("Server=localhost;Port=3307;Database=other;User=root;Password=synthetic;")]
    public void RemoteOrDifferentPortNeverStartsAnIntegrationDatabase(string raw)
    {
        Assert.Throws<InvalidOperationException>(() => LocalOnly(raw));
    }

    [Fact]
    public async Task TenantAAndBArePersistedAndRollbackIsDurableWithoutPlacementAuthorization()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("GitHub Actions MySQL 8.4 local service required.");
        var isolated = new MySqlConnectionStringBuilder(LocalOnly(raw).ConnectionString)
        {
            Database = "v19_c0202m0503_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(isolated, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        try
        {
            int idA, idB;
            await using (var db = new AppDbContext(options))
            {
                await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
                var a = new Empresa("V19 C0202 Placement tenant A");
                var b = new Empresa("V19 C0202 Placement tenant B");
                db.Set<Empresa>().AddRange(a, b);
                await db.SaveChangesAsync();
                (idA, idB) = (a.Id, b.Id);
                Assert.True(idA > 0);
                Assert.True(idB > 0);
                Assert.NotEqual(idA, idB);
            }

            // Persisted EF facts must be observed in fresh contexts, not
            // from in-memory snapshots or browser-provided tenant hints.
            await using (var db = new AppDbContext(options))
            {
                var tenantA = await db.Set<Empresa>().AsNoTracking()
                    .SingleAsync(x => x.Id == idA);
                var tenantB = await db.Set<Empresa>().AsNoTracking()
                    .SingleAsync(x => x.Id == idB);
                Assert.Equal(idA, tenantA.Id);
                Assert.Equal(idB, tenantB.Id);
                // Both rows are intentionally queryable by a privileged EF context.
                // A contradictory ID predicate can never prove tenant isolation.
                // Authorization is tested independently against the actual persisted IDs.
                Assert.Equal(2, await db.Set<Empresa>().AsNoTracking()
                    .CountAsync(x => x.Id == idA || x.Id == idB));

                // Real MySQL ownership facts are NOT JWT, membership, RBAC
                // or approved storage binding. All runtime grants stay false.
                var evidence = new V5SharedDedicatedPlacementDomainRules.TrustedPlacementSnapshot
                {
                    VerifiedTenantId = "tenant_" + idA,
                    AuthenticatedActorId = "unverified-actor",
                    AssignmentOwnerTenantId = "tenant_" + idA,
                    CurrentAssignmentRevision = 1,
                    JwtValidatedByBackend = false,
                    CurrentMembershipVerified = false,
                    TrustedConfigurationAvailable = false,
                    UniqueActiveAssignmentVerified = false
                };
                var targetA = new V5SharedDedicatedPlacementDomainRules.PlacementCandidate
                {
                    Intent = V5SharedDedicatedPlacementDomainRules.Intent.InspectExistingAssignment,
                    RequestedTenantId = "tenant_" + idA,
                    TargetMode = V5SharedDedicatedPlacementDomainRules.PlacementMode.Shared,
                    ExpectedAssignmentRevision = 1
                };
                var denied = V5SharedDedicatedPlacementDomainRules.Evaluate(evidence, targetA);
                Assert.Equal("TRUSTED_AUTHENTICATED_TENANT_REQUIRED", denied.Code);
                Assert.False(denied.AuthorizesConnection);
                Assert.False(denied.AuthorizesMigration);
                var cross = V5SharedDedicatedPlacementDomainRules.Evaluate(
                    evidence, targetA with { RequestedTenantId = "tenant_" + idB });
                Assert.False(cross.AuthorizesConnection);
                Assert.False(cross.AuthorizesMigration);

                // Adversarial regression: satisfy all earlier trusted policy gates
                // with a synthetic, test-only snapshot derived from the two real
                // MySQL IDs. A policy regression that removes the A/B mismatch
                // check MUST fail this assertion. This is not authenticated HTTP.
                var trustedA = evidence with
                {
                    JwtValidatedByBackend = true,
                    CurrentMembershipVerified = true,
                    TrustedConfigurationAvailable = true,
                    UniqueActiveAssignmentVerified = true,
                    BindingFromTrustedCatalog = true,
                    CurrentBindingAlias = "trusted-ci-shared-a",
                    CurrentRoleCanReviewPlacement = true
                };
                var sameTenant = V5SharedDedicatedPlacementDomainRules.Evaluate(trustedA, targetA);
                Assert.Equal("TRUSTED_PLACEMENT_RUNTIME_VERIFICATION_REQUIRED", sameTenant.Code);
                Assert.False(sameTenant.AuthorizesConnection);
                Assert.False(sameTenant.AuthorizesMigration);

                var attemptedCrossTenantRead = V5SharedDedicatedPlacementDomainRules.Evaluate(
                    trustedA, targetA with { RequestedTenantId = "tenant_" + idB });
                Assert.Equal("CROSS_TENANT_PLACEMENT_DENIED", attemptedCrossTenantRead.Code);
                Assert.False(attemptedCrossTenantRead.AuthorizesConnection);
                Assert.False(attemptedCrossTenantRead.AuthorizesMigration);
            }

            int rolledBackId;
            await using (var db = new AppDbContext(options))
            {
                await using var transaction = await db.Database.BeginTransactionAsync();
                var provisional = new Empresa("V19 C0202 Placement rollback-only");
                db.Set<Empresa>().Add(provisional);
                await db.SaveChangesAsync();
                rolledBackId = provisional.Id;
                Assert.True(rolledBackId > 0);
                await transaction.RollbackAsync();
            }
            await using (var verified = new AppDbContext(options))
            {
                Assert.False(await verified.Set<Empresa>().AsNoTracking()
                    .AnyAsync(x => x.Id == rolledBackId));
                Assert.Equal(2, await verified.Set<Empresa>().AsNoTracking()
                    .CountAsync(x => x.Id == idA || x.Id == idB));
            }
        }
        finally
        {
            // Only the random dedicated CI database is dropped.
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
