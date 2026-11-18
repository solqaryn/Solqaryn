using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5SharedDedicatedPlacementDomainRules;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C02-02.M05.02: causal request/response DTO and sanitized-error contract tests.
/// The Application preflight is inert: no HTTP endpoint or live storage is invoked.
/// </summary>
public sealed class C0202M0502PlacementContractErrorSafetyTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    private static Rules.TrustedPlacementSnapshot Snapshot() => new()
    {
        VerifiedTenantId = "tenant-A",
        AuthenticatedActorId = "actor-A",
        JwtValidatedByBackend = true,
        CurrentMembershipVerified = true,
        CurrentRoleCanReviewPlacement = true,
        CurrentRoleCanProposeMigration = true,
        TrustedConfigurationAvailable = true,
        AssignmentOwnerTenantId = "tenant-A",
        CurrentAssignmentRevision = 7,
        CurrentBindingAlias = "shared-primary",
        BindingFromTrustedCatalog = true,
        UniqueActiveAssignmentVerified = true
    };

    private static C0202PlacementApplicationGate.Request Request(
        Rules.Intent intent = Rules.Intent.InspectExistingAssignment) =>
        new("tenant-A", Sha, intent, Rules.PlacementMode.Shared,
            "dedicated-tenant-A", 7, "request-7");

    private static C0202PlacementApplicationGate.Evidence Evidence() => new(
        Sha, "tenant-A", "tenant-A", "dedicated-tenant-A",
        true, true, true, true, true, true, true, true, true, true, Snapshot());

    private sealed class Reader(
        C0202PlacementApplicationGate.Evidence? evidence,
        Exception? error = null) : IC0202PlacementEvidenceReader
    {
        public int Reads { get; private set; }
        public ValueTask<C0202PlacementApplicationGate.Evidence?> ReadAsync(
            string tenantId, CancellationToken cancellationToken)
        {
            Reads++;
            if (error is not null)
                throw error;
            return ValueTask.FromResult(evidence);
        }
    }

    private static void DoesNotAuthorize(C0202PlacementApplicationGate.Result result)
    {
        Assert.False(result.AuthorizesConnection);
        Assert.False(result.AuthorizesMigration);
        Assert.False(result.AuthorizesCutover);
        Assert.False(result.AuthorizesProduction);
    }

    [Fact]
    public async Task TrustedApplicationDtoRequiresSeparateAdapterNeverConnection()
    {
        var reader = new Reader(Evidence());
        var result = await new C0202PlacementApplicationGate(reader)
            .EvaluateAsync(Request());
        Assert.Equal("AUTHENTICATED_STORAGE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresSeparateAuthenticatedStorageAdapter);
        Assert.Equal(1, reader.Reads);
        DoesNotAuthorize(result);
    }

    [Theory]
    [InlineData("tenant-A ")]
    [InlineData("tenant/A")]
    [InlineData("tenant\nB")]
    [InlineData("tenant\u200bA")]
    [InlineData("")]
    public async Task MalformedTenantDtoIsDeniedBeforeBackendReader(string tenant)
    {
        var reader = new Reader(Evidence());
        var result = await new C0202PlacementApplicationGate(reader)
            .EvaluateAsync(Request() with { TenantId = tenant });
        Assert.Equal("INVALID_PLACEMENT_REQUEST_OR_SOURCE", result.Code);
        Assert.Equal(0, reader.Reads);
        DoesNotAuthorize(result);
    }

    [Fact]
    public async Task MissingRequestAndUnknownModeAreRejectedBeforeReader()
    {
        var reader = new Reader(Evidence());
        var gate = new C0202PlacementApplicationGate(reader);
        Assert.Equal("INVALID_PLACEMENT_REQUEST_OR_SOURCE",
            (await gate.EvaluateAsync(null)).Code);
        Assert.Equal("INVALID_PLACEMENT_REQUEST_OR_SOURCE",
            (await gate.EvaluateAsync(Request() with { TargetMode = (Rules.PlacementMode)999 })).Code);
        Assert.Equal("INVALID_PLACEMENT_REQUEST_OR_SOURCE",
            (await gate.EvaluateAsync(Request() with { SourceSha = new string('f', 39) })).Code);
        Assert.Equal(0, reader.Reads);
    }

    [Fact]
    public async Task ClientDsnDestructiveChangesAndFallbackNeverReachReader()
    {
        var reader = new Reader(Evidence());
        var gate = new C0202PlacementApplicationGate(reader);
        foreach (var candidate in new[]
        {
            Request() with { ClientSuppliesDsn = true },
            Request() with { RequestsDestructiveSchema = true },
            Request() with { RequestsLiveCutoverOrDualWrite = true },
            Request() with { CrossTenantFallback = true }
        })
        {
            var result = await gate.EvaluateAsync(candidate);
            Assert.Equal("UNTRUSTED_STORAGE_OR_DESTRUCTIVE_OPERATION", result.Code);
            DoesNotAuthorize(result);
        }
        Assert.Equal(0, reader.Reads);
    }

    [Fact]
    public async Task CrossTenantOwnerAndStaleShaCannotBeSmuggledIntoDto()
    {
        var reader = new Reader(Evidence() with { PersistedOwnerTenantId = "tenant-B" });
        var gate = new C0202PlacementApplicationGate(reader);
        var owner = await gate.EvaluateAsync(Request());
        Assert.Equal("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED", owner.Code);
        DoesNotAuthorize(owner);

        var stale = await new C0202PlacementApplicationGate(
            new Reader(Evidence() with { SourceSha = new string('b', 40) }))
            .EvaluateAsync(Request());
        Assert.Equal("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED", stale.Code);
        DoesNotAuthorize(stale);
    }

    [Fact]
    public async Task BrowserControlledAliasCannotSelectTrustedBinding()
    {
        var reader = new Reader(Evidence());
        var result = await new C0202PlacementApplicationGate(reader).EvaluateAsync(
            Request(Rules.Intent.ProposeMigration) with
            {
                TargetMode = Rules.PlacementMode.Dedicated,
                TargetBindingAlias = "mysql://root@tenant-B"
            });
        Assert.Equal("TARGET_BINDING_NOT_SERVER_APPROVED", result.Code);
        DoesNotAuthorize(result);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BackendExceptionsNeverLeakSecretsAndCannotAuthorize(bool cancellationException)
    {
        var failure = cancellationException
            ? (Exception)new OperationCanceledException("private-dsn;token=secret")
            : new InvalidOperationException("private-dsn;token=secret");
        var reader = new Reader(Evidence(), failure);
        var result = await new C0202PlacementApplicationGate(reader)
            .EvaluateAsync(Request());
        Assert.Equal("TRUSTED_TENANT_PLACEMENT_EVIDENCE_REQUIRED", result.Code);
        Assert.DoesNotContain("private-dsn", result.Code);
        Assert.DoesNotContain("secret", result.Code);
        Assert.Equal(1, reader.Reads);
        DoesNotAuthorize(result);
    }

    [Fact]
    public async Task CallerRequestedCancellationPropagatesWithoutProviderCall()
    {
        var reader = new Reader(Evidence());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await new C0202PlacementApplicationGate(reader)
                .EvaluateAsync(Request(), cancellation.Token));
        Assert.Equal(0, reader.Reads);
    }
}
