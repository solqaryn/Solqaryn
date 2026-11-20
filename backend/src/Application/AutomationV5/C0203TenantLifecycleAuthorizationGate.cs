using System;
using System.Linq;
using Rules = Solqaryn.Domain.AutomationV5.V5TenantLifecycleDomainRules;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Trusted read-only authorization evidence, supplied exclusively by a future
/// backend adapter. Never accept actor, grant, JWT or tenant ownership from a client.
/// No live implementation is registered by C02-03.M03.03.
/// </summary>
public interface IC0203VerifiedOperatorEvidenceReader
{
    ValueTask<C0203TenantLifecycleAuthorizationGate.TrustedOperatorSnapshot?> ReadAsync(
        string requestedTenantId, Rules.Command requestedCommand,
        CancellationToken cancellationToken);
}

/// <summary>
/// R2 deny-by-default guard for lifecycle preflight. A positive decision is
/// NOT authorization to persist, grant, restore, deploy or mutate a tenant.
/// </summary>
public sealed class C0203TenantLifecycleAuthorizationGate
{
    private readonly IC0203VerifiedOperatorEvidenceReader _reader;

    public C0203TenantLifecycleAuthorizationGate(IC0203VerifiedOperatorEvidenceReader reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    public sealed record Intent(
        string? TargetTenantId, Rules.Command Operation,
        bool ClientClaimsPlatformGrant = false,
        bool ClientSuppliesActorOrJwt = false,
        bool ClientSuppliesStorageRouting = false,
        bool RequestsPhysicalDeletion = false,
        bool RequestsAutomaticGrantOrRevival = false,
        bool RequestsLiveCutover = false);

    public sealed record TrustedOperatorSnapshot
    {
        public string? VerifiedActorId { get; init; }
        public string? VerifiedTenantId { get; init; }
        // Positive policy evidence must be bound to the EXACT requested operation.
        public Rules.Command VerifiedCommand { get; init; }
        public bool ServerControlledEvidence { get; init; }
        public bool BackendJwtSignatureVerified { get; init; }
        public bool CurrentPlatformGrantVerified { get; init; }
        public bool CurrentOperatorRoleVerified { get; init; }
        public bool MembershipAndSessionFresh { get; init; }
        public bool ActorOrGrantRevoked { get; init; }
        public bool TargetOwnershipVerifiedServerSide { get; init; }
        public bool PolicyRevisionFresh { get; init; }
        public bool LifecycleFeatureEnabled { get; init; }
        public bool CommandEnabledByTrustedConfig { get; init; }
        public bool TenantFencedEvidenceRead { get; init; }
    }

    public readonly record struct Decision(string Code)
    {
        public bool MayProceedToReadOnlyApplicationPreflight =>
            Code == "VERIFIED_READONLY_PREFLIGHT_ONLY";
        public bool AuthorizesTenantMutation => false;
        public bool GrantsPlatformPermission => false;
        public bool WritesToDatabase => false;
        public bool AuthorizesProductionCutover => false;
    }

    private static bool SafeId(string? value) =>
        // Business tenants and users may have valid positive one-digit SQL IDs.
        // A lone letter or zero remains invalid; longer opaque trusted IDs
        // retain the existing strict ASCII whitelist.
        value is { Length: >= 1 and <= 128 } &&
        (value.Length > 1 || value[0] is >= '1' and <= '9') &&
        value.All(c => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or
                               >= '0' and <= '9' or '-' or '_');

    public async ValueTask<Decision> InspectAsync(
        Intent? intent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (intent is null || !SafeId(intent.TargetTenantId) ||
            !Enum.IsDefined(intent.Operation) ||
            intent.ClientClaimsPlatformGrant || intent.ClientSuppliesActorOrJwt ||
            intent.ClientSuppliesStorageRouting || intent.RequestsPhysicalDeletion ||
            intent.RequestsAutomaticGrantOrRevival || intent.RequestsLiveCutover)
            return new("LIFECYCLE_ACCESS_DENIED");

        TrustedOperatorSnapshot? trusted;
        try
        {
            trusted = await _reader.ReadAsync(intent.TargetTenantId!, intent.Operation, cancellationToken);
            // A provider may ignore cancellation after I/O; never grant a stale preflight.
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Deliberately indistinguishable from other denials, no provider detail.
            return new("LIFECYCLE_ACCESS_DENIED");
        }

        if (trusted is null || !SafeId(trusted.VerifiedActorId) ||
            !SafeId(trusted.VerifiedTenantId) ||
            !string.Equals(trusted.VerifiedTenantId, intent.TargetTenantId, StringComparison.Ordinal) ||
            !Enum.IsDefined(trusted.VerifiedCommand) ||
            trusted.VerifiedCommand != intent.Operation ||
            !trusted.ServerControlledEvidence ||
            !trusted.BackendJwtSignatureVerified ||
            !trusted.CurrentPlatformGrantVerified ||
            !trusted.CurrentOperatorRoleVerified ||
            !trusted.MembershipAndSessionFresh ||
            trusted.ActorOrGrantRevoked ||
            !trusted.TargetOwnershipVerifiedServerSide ||
            !trusted.PolicyRevisionFresh ||
            !trusted.LifecycleFeatureEnabled ||
            !trusted.CommandEnabledByTrustedConfig ||
            !trusted.TenantFencedEvidenceRead)
            return new("LIFECYCLE_ACCESS_DENIED");

        // Even a fully trusted snapshot can only proceed to additional
        // Application/Domain checks. No mutation, CAS or live adapter here.
        return new("VERIFIED_READONLY_PREFLIGHT_ONLY");
    }
}
