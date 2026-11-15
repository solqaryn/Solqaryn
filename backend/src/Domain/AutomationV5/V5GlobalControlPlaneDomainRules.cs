namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// C02-04.M03.01: non-authorizing, fail-closed preflight for the global control plane.
/// Callers cannot turn these assertions into authenticated server authority.
/// API/Application must independently verify JWT, membership, MFA, reauthentication,
/// grant revocation and audit persistence before any real access.
/// </summary>
public static class V5GlobalControlPlaneDomainRules
{
    public enum Operation
    {
        OwnTenantMetadataRead = 1,
        PlatformTenantDirectoryRead = 2,
        ExceptionalTenantMetadataRead = 3
    }

    public sealed record TrustedSnapshot(
        string? ActorId,
        string? AuthenticatedTenantId,
        string? VerifiedTargetTenantId,
        bool BackendIdentityVerified,
        bool CurrentRoleGrantVerified,
        bool CurrentMembershipVerified,
        bool PlatformIdentityVerified,
        bool PlatformGrantVerified,
        bool RecentMfaVerified,
        bool SensitiveReauthenticationVerified,
        bool AuditSinkVerified,
        string? AuditCorrelationId,
        bool ExceptionGrantActive,
        bool ExceptionGrantRevoked,
        string? ExceptionTenantId,
        DateTimeOffset? ExceptionIssuedUtc,
        DateTimeOffset? ExceptionExpiresUtc,
        bool IndependentApprovalVerified,
        string? IndependentApproverActorId);

    public sealed record ProposedAction(
        Operation Operation,
        string? Justification,
        bool ClientSuppliedAuthority,
        bool AttemptsWriteOrExport,
        bool IncludesSensitiveData);

    public readonly record struct Verdict(string Code)
    {
        // This is deliberately NOT an authorization flag.
        public bool RequiresBackendEnforcement =>
            Code == "TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED";
    }

    private static bool ValidIdentifier(string? text) =>
        !string.IsNullOrWhiteSpace(text) &&
        text.Length <= 128 &&
        !text.Any(char.IsControl);

    public static Verdict Evaluate(
        TrustedSnapshot? evidence,
        ProposedAction? proposed,
        DateTimeOffset serverNowUtc)
    {
        if (evidence is null || proposed is null ||
            !evidence.BackendIdentityVerified ||
            !evidence.CurrentRoleGrantVerified ||
            !ValidIdentifier(evidence.ActorId))
            return new("SERVER_IDENTITY_AND_GRANT_REQUIRED");

        if (!Enum.IsDefined(proposed.Operation))
            return new("UNKNOWN_CONTROL_PLANE_OPERATION");

        if (proposed.ClientSuppliedAuthority)
            return new("CLIENT_AUTHORITY_OVERRIDE_DENIED");

        if (proposed.AttemptsWriteOrExport || proposed.IncludesSensitiveData)
            return new("UNSAFE_CONTROL_PLANE_OPERATION_DENIED");

        if (!evidence.RecentMfaVerified ||
            !evidence.SensitiveReauthenticationVerified)
            return new("MFA_AND_REAUTHENTICATION_REQUIRED");

        if (!evidence.AuditSinkVerified ||
            !ValidIdentifier(evidence.AuditCorrelationId))
            return new("AUDIT_INTEGRITY_REQUIRED");

        if (!ValidIdentifier(evidence.VerifiedTargetTenantId))
            return new("VERIFIED_TARGET_TENANT_REQUIRED");

        if (proposed.Operation == Operation.OwnTenantMetadataRead)
        {
            if (!evidence.CurrentMembershipVerified ||
                !ValidIdentifier(evidence.AuthenticatedTenantId) ||
                !string.Equals(
                    evidence.AuthenticatedTenantId,
                    evidence.VerifiedTargetTenantId,
                    StringComparison.Ordinal))
                return new("TENANT_OWNERSHIP_REQUIRED");

            return new("TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED");
        }

        // A platform grant is distinct from an ERP tenant role.
        if (!evidence.PlatformIdentityVerified ||
            !evidence.PlatformGrantVerified)
            return new("INDEPENDENT_PLATFORM_GRANT_REQUIRED");

        if (proposed.Operation == Operation.PlatformTenantDirectoryRead)
            return new("TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED");

        if (!evidence.ExceptionGrantActive ||
            evidence.ExceptionGrantRevoked ||
            !string.Equals(
                evidence.ExceptionTenantId,
                evidence.VerifiedTargetTenantId,
                StringComparison.Ordinal) ||
            !ValidIdentifier(evidence.ExceptionTenantId) ||
            string.IsNullOrWhiteSpace(proposed.Justification) ||
            proposed.Justification.Trim().Length is < 12 or > 256 ||
            proposed.Justification.Any(char.IsControl))
            return new("EXCEPTION_GRANT_AND_REASON_REQUIRED");

        if (evidence.ExceptionIssuedUtc is not { } issued ||
            evidence.ExceptionExpiresUtc is not { } expires ||
            issued > serverNowUtc ||
            expires <= serverNowUtc ||
            expires <= issued ||
            expires - issued > TimeSpan.FromMinutes(15))
            return new("EXCEPTION_EXPIRED_OR_OVERBROAD");

        if (!evidence.IndependentApprovalVerified ||
            !ValidIdentifier(evidence.IndependentApproverActorId) ||
            string.Equals(
                evidence.ActorId,
                evidence.IndependentApproverActorId,
                StringComparison.Ordinal))
            return new("INDEPENDENT_APPROVAL_REQUIRED");

        // Even perfect snapshots cannot grant access: this is a pure gate.
        return new("TRUSTED_RUNTIME_ENFORCEMENT_REQUIRED");
    }
}
