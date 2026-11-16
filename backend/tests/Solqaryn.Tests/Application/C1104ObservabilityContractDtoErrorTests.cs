using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C11-04.M05.02: actual .NET contract/DTO/error tests for the in-process
/// preflight only. No HTTP controller, JWT validator or MySQL service is invoked.
/// </summary>
public sealed class C1104ObservabilityContractDtoErrorTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly DateTimeOffset Window =
        new(2026, 11, 15, 16, 0, 0, TimeSpan.Zero);

    private static C1104ObservabilitySignalDomainRules.PrivateAggregate Aggregate() =>
        new("http_requests", "corr-01", "tenant-A", "tenant-A", true, true,
            Window, Window.AddHours(1), 4, 5,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["method_class"] = "GET", ["status_class"] = "2xx"
            });

    private static C1104ObservabilityApplicationService.VerifiedEvidence Evidence() =>
        new(Sha, "tenant-A", "tenant-A", true, true, true, true, true, Aggregate());

    private static C1104ObservabilityApplicationService.Request Request() =>
        new("http_requests", "corr-01", Sha);

    private sealed class Source(
        C1104ObservabilityApplicationService.VerifiedEvidence? evidence)
        : IC1104ObservabilityEvidenceReader
    {
        public int Calls { get; private set; }

        public ValueTask<C1104ObservabilityApplicationService.VerifiedEvidence?> ReadAsync(
            string signalName, string correlationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(evidence);
        }
    }

    [Fact]
    public async Task TypedRequestReturnsOnlyNonAuthorizingAdapterRequiredCode()
    {
        var source = new Source(Evidence());
        var service = new C1104ObservabilityApplicationService(source);
        var result = await service.PrepareAsync(Request());

        Assert.Equal(1, source.Calls);
        Assert.Equal("TRUSTED_PRIVATE_OBSERVABILITY_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresAuthenticatedReadAdapter);
        Assert.False(result.GrantsTenantAccess);
        Assert.False(result.ExportsTelemetry);
        Assert.False(result.CertifiesSloOrCost);
        Assert.False(result.WritesMetricsOrDatabase);
    }

    [Theory]
    [InlineData("", "corr-01", "INVALID_REQUEST_OR_SOURCE_SHA")]
    [InlineData("http_requests", "", "INVALID_REQUEST_OR_SOURCE_SHA")]
    [InlineData("http_requests", "unsafe\r\nsecret", "UNSAFE_CORRELATION")]
    [InlineData("unknown_signal", "corr-01", "SIGNAL_NOT_ALLOWLISTED")]
    public async Task InvalidInputIsTypedAndDoesNotReadEvidence(
        string signal, string correlation, string expected)
    {
        var source = new Source(Evidence());
        var result = await new C1104ObservabilityApplicationService(source)
            .PrepareAsync(new(signal, correlation, Sha));
        Assert.Equal(expected, result.Code);
        Assert.Equal(0, source.Calls);
        Assert.False(result.GrantsTenantAccess);
    }

    [Fact]
    public async Task OversizedCorrelationAndMalformedShaFailBeforeBackendRead()
    {
        var source = new Source(Evidence());
        var service = new C1104ObservabilityApplicationService(source);
        Assert.Equal("INVALID_REQUEST_OR_SOURCE_SHA",
            (await service.PrepareAsync(Request() with { CorrelationId = new string('X', 65) })).Code);
        Assert.Equal("INVALID_REQUEST_OR_SOURCE_SHA",
            (await service.PrepareAsync(Request() with { ExpectedSourceSha = "short" })).Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task CrossTenantAndRevocationReturnOpaqueDenialCodes()
    {
        foreach (var evidence in new[]
        {
            Evidence() with { PersistedResourceTenantId = "tenant-B" },
            Evidence() with { TrustedActorTenantId = "tenant-B" },
            Evidence() with { CurrentMembershipVerified = false },
            Evidence() with { CurrentExplicitReadRbacVerified = false }
        })
        {
            var result = await new C1104ObservabilityApplicationService(new Source(evidence))
                .PrepareAsync(Request());
            Assert.Equal("FRESH_BACKEND_AUTHORIZATION_AND_TENANT_REQUIRED", result.Code);
            Assert.False(result.GrantsTenantAccess);
            Assert.DoesNotContain("tenant-", result.Code);
        }
    }

    [Fact]
    public async Task SourceShaMismatchNeverReturnsPrivateAggregate()
    {
        var wrong = Evidence() with { ObservedSourceSha = new string('b', 40) };
        var result = await new C1104ObservabilityApplicationService(new Source(wrong))
            .PrepareAsync(Request());
        Assert.Equal("TRUSTED_AGGREGATE_SOURCE_REQUIRED", result.Code);
        Assert.False(result.ExportsTelemetry);
    }

    [Fact]
    public async Task SensitiveLabelIsRejectedWithoutPayloadDisclosure()
    {
        var secret = Evidence() with
        {
            Aggregate = Aggregate() with
            {
                Labels = new Dictionary<string, string>
                {
                    ["user_email"] = "PRIVATE_EMAIL@example.invalid"
                }
            }
        };
        var result = await new C1104ObservabilityApplicationService(new Source(secret))
            .PrepareAsync(Request());
        Assert.Equal("LABEL_NOT_ALLOWLISTED", result.Code);
        Assert.DoesNotContain("PRIVATE_EMAIL", result.Code);
        Assert.False(result.ExportsTelemetry);
    }

    [Fact]
    public async Task EmptyMeasurementCannotCertifyServiceLevelObjective()
    {
        var empty = Evidence() with
        {
            Aggregate = Aggregate() with { Numerator = 0, Denominator = 0 }
        };
        var result = await new C1104ObservabilityApplicationService(new Source(empty))
            .PrepareAsync(Request());
        Assert.Equal("NO_MEDIDO", result.Code);
        Assert.False(result.CertifiesSloOrCost);
    }

    [Fact]
    public async Task CancelledRequestDoesNotReachEvidenceReader()
    {
        var source = new Source(Evidence());
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new C1104ObservabilityApplicationService(source)
                .PrepareAsync(Request(), cts.Token));
        Assert.Equal(0, source.Calls);
    }
}
