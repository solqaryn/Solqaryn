using Solqaryn.Domain.AutomationV5;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class C1104ObservabilitySignalDomainRulesTests
{
    private static C1104ObservabilitySignalDomainRules.PrivateAggregate Valid() =>
        new(
            "http_requests", "correlation-01", "tenant-A", "tenant-A",
            true, true,
            new DateTimeOffset(2026, 11, 15, 16, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 15, 17, 0, 0, TimeSpan.Zero),
            4, 5,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["method_class"] = "GET",
                ["status_class"] = "2xx"
            });

    private static string Code(C1104ObservabilitySignalDomainRules.PrivateAggregate value) =>
        C1104ObservabilitySignalDomainRules.Evaluate(value).Code;

    [Fact]
    public void ValidatedAggregateIsOnlyPureDomainPreflight()
    {
        var result = C1104ObservabilitySignalDomainRules.Evaluate(Valid());
        Assert.True(result.Allowed);
        Assert.Equal("VALIDATED_PRIVATE_AGGREGATE", result.Code);
    }

    [Fact]
    public void NullInputFailsClosed() =>
        Assert.Equal("MISSING_INPUT", C1104ObservabilitySignalDomainRules.Evaluate(null).Code);

    [Theory]
    [InlineData("unknown_signal")]
    [InlineData("http_requests\nAuthorization: Bearer token")]
    [InlineData("")]
    public void RejectsUnapprovedSignalNames(string value) =>
        Assert.Equal("SIGNAL_NOT_ALLOWLISTED", Code(Valid() with { SignalName = value }));

    [Fact]
    public void RejectsCorrelationWithNewlineOrOverlongValue()
    {
        Assert.Equal("UNSAFE_CORRELATION", Code(Valid() with { CorrelationId = "ok\r\nsecret" }));
        Assert.Equal("UNSAFE_CORRELATION", Code(Valid() with { CorrelationId = new string('A', 65) }));
        Assert.Equal("UNSAFE_CORRELATION", Code(Valid() with { CorrelationId = "a/b" }));
    }

    [Fact]
    public void DeniesMissingOrCrossTenantIdentity()
    {
        Assert.Equal("MISSING_TRUSTED_TENANT", Code(Valid() with { IdentityTenant = "" }));
        Assert.Equal("MISSING_TRUSTED_TENANT", Code(Valid() with { ResourceTenant = "" }));
        Assert.Equal("CROSS_TENANT_DENIED", Code(Valid() with { ResourceTenant = "tenant-B" }));
    }

    [Fact]
    public void DeniesRevokedMembershipIndependentlyOfReadPermission()
    {
        Assert.Equal("MEMBERSHIP_REVOKED", Code(Valid() with { MembershipActive = false }));
        Assert.Equal("READ_PERMISSION_DENIED", Code(Valid() with { HasReadPermission = false }));
    }

    [Fact]
    public void RejectsUnboundedOrAmbiguousTimeWindows()
    {
        Assert.Equal("INVALID_MEASUREMENT_WINDOW",
            Code(Valid() with { WindowEndUtc = Valid().WindowStartUtc }));
        Assert.Equal("INVALID_MEASUREMENT_WINDOW",
            Code(Valid() with { WindowEndUtc = Valid().WindowEndUtc.AddDays(2) }));
        Assert.Equal("INVALID_MEASUREMENT_WINDOW",
            Code(Valid() with { WindowStartUtc = Valid().WindowStartUtc.ToOffset(TimeSpan.FromHours(-6)) }));
    }

    [Fact]
    public void RejectsInvalidCountsAndKeepsZeroDenominatorUnmeasured()
    {
        Assert.Equal("INVALID_AGGREGATE_COUNTS", Code(Valid() with { Numerator = -1 }));
        Assert.Equal("INVALID_AGGREGATE_COUNTS", Code(Valid() with { Numerator = 6 }));
        Assert.Equal("NO_MEDIDO", Code(Valid() with { Numerator = 0, Denominator = 0 }));
    }

    [Fact]
    public void RejectsUnboundedLabelsAndSensitiveFreeValues()
    {
        Assert.Equal("UNVERIFIED_LABEL_CATALOG", Code(Valid() with { Labels = null }));
        Assert.Equal("LABEL_NOT_ALLOWLISTED", Code(Valid() with
        {
            Labels = new Dictionary<string, string> { ["tenant_id"] = "tenant-A" }
        }));
        Assert.Equal("LABEL_NOT_ALLOWLISTED", Code(Valid() with
        {
            Labels = new Dictionary<string, string> { ["method_class"] = "user@example.com" }
        }));
        Assert.Equal("LABEL_NOT_ALLOWLISTED", Code(Valid() with
        {
            Labels = new Dictionary<string, string> { ["status_class"] = "200" }
        }));
    }
}
