using System.Text.RegularExpressions;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C11-04.M03.01: pure, fail-closed preflight for private aggregate signals.
/// A trusted API caller must still authenticate JWT, load current membership,
/// authorize the resource, and establish tenant ownership server-side.
/// This class does not read tenants, create metrics, export data or certify an SLO.
/// </summary>
public static class C1104ObservabilitySignalDomainRules
{
    private static readonly Regex CorrelationPattern = new(
        @"^[a-zA-Z0-9_.-]{1,64}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> Signals = new(StringComparer.Ordinal)
    {
        "http_requests", "request_latency", "error_rate", "db_duration"
    };

    private static readonly Dictionary<string, HashSet<string>> AllowedLabelValues =
        new(StringComparer.Ordinal)
        {
            ["method_class"] = new(StringComparer.Ordinal)
                { "GET", "POST", "PUT", "PATCH", "DELETE", "OTHER" },
            ["status_class"] = new(StringComparer.Ordinal)
                { "2xx", "3xx", "4xx", "5xx" }
        };

    public sealed record PrivateAggregate(
        string SignalName,
        string CorrelationId,
        string IdentityTenant,
        string ResourceTenant,
        bool MembershipActive,
        bool HasReadPermission,
        DateTimeOffset WindowStartUtc,
        DateTimeOffset WindowEndUtc,
        long Numerator,
        long Denominator,
        IReadOnlyDictionary<string, string>? Labels);

    public readonly record struct Assessment(string Code)
    {
        public bool Allowed => Code == "VALIDATED_PRIVATE_AGGREGATE";
    }

    public static Assessment Evaluate(PrivateAggregate? value)
    {
        if (value is null)
            return new Assessment("MISSING_INPUT");

        if (string.IsNullOrWhiteSpace(value.SignalName) ||
            !Signals.Contains(value.SignalName))
            return new Assessment("SIGNAL_NOT_ALLOWLISTED");

        if (string.IsNullOrWhiteSpace(value.CorrelationId) ||
            !CorrelationPattern.IsMatch(value.CorrelationId))
            return new Assessment("UNSAFE_CORRELATION");

        if (string.IsNullOrWhiteSpace(value.IdentityTenant) ||
            string.IsNullOrWhiteSpace(value.ResourceTenant))
            return new Assessment("MISSING_TRUSTED_TENANT");

        if (!string.Equals(value.IdentityTenant, value.ResourceTenant,
                StringComparison.Ordinal))
            return new Assessment("CROSS_TENANT_DENIED");

        if (!value.MembershipActive)
            return new Assessment("MEMBERSHIP_REVOKED");

        if (!value.HasReadPermission)
            return new Assessment("READ_PERMISSION_DENIED");

        if (value.WindowStartUtc.Offset != TimeSpan.Zero ||
            value.WindowEndUtc.Offset != TimeSpan.Zero ||
            value.WindowStartUtc >= value.WindowEndUtc ||
            value.WindowEndUtc - value.WindowStartUtc > TimeSpan.FromDays(1))
            return new Assessment("INVALID_MEASUREMENT_WINDOW");

        if (value.Numerator < 0 || value.Denominator < 0 ||
            value.Numerator > value.Denominator)
            return new Assessment("INVALID_AGGREGATE_COUNTS");

        // A denominator of zero can never produce an SLO/SLI PASS.
        if (value.Denominator == 0)
            return new Assessment("NO_MEDIDO");

        if (value.Labels is null)
            return new Assessment("UNVERIFIED_LABEL_CATALOG");

        foreach (var (key, labelValue) in value.Labels)
        {
            if (!AllowedLabelValues.TryGetValue(key, out var allowed) ||
                string.IsNullOrWhiteSpace(labelValue) ||
                !allowed.Contains(labelValue))
                return new Assessment("LABEL_NOT_ALLOWLISTED");
        }

        // This is only a domain preflight, never an authenticated API verdict.
        return new Assessment("VALIDATED_PRIVATE_AGGREGATE");
    }
}
