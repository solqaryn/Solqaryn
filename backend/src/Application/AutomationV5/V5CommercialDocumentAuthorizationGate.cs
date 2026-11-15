using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// C07-04.M03.03. Fail-closed authorization boundary for preparing commercial
/// document commands. The server MUST resolve ServerAccess from authenticated
/// identity, current RBAC/membership and persisted document ownership.
/// ClientCommand cannot supply a TrustedContext or PersistedSnapshot.
/// No mutation, PSP/fiscal execution, JWT adapter, HTTP endpoint or DB writes.
/// </summary>
public sealed class V5CommercialDocumentAuthorizationGate
{
    public sealed record ServerAccess(
        int AuthenticatedActorId,
        int AuthenticatedTenantId,
        bool IdentityVerifiedByBackend,
        bool MembershipAndRoleCurrent,
        bool DocumentOwnershipFromPersistence,
        V5CommercialDocumentDomainRules.TrustedContext? Authority,
        V5CommercialDocumentDomainRules.PersistedSnapshot? Document);

    public sealed record ClientCommand(
        int RequestedTenantId,
        string? ExpectedSourceSha,
        string? ObservedSourceSha,
        string? SanitizedEvidenceReference,
        V5CommercialDocumentDomainRules.Command? Command);

    public readonly record struct Decision(string Code)
    {
        public bool NeedsAuthenticatedTransactionalAdapter =>
            Code == "DURABLE_COMMERCIAL_ADAPTER_AND_A5_REQUIRED";
        public bool AuthorizesMutation => false;
        public bool AuthorizesCrossTenantRead => false;
        public bool AuthorizesPayment => false;
        public bool AuthorizesFiscalIssuance => false;
        public bool AuthorizesRelease => false;
    }

    public Decision Prepare(ServerAccess? trustedBackend, ClientCommand? client)
    {
        if (trustedBackend is null || trustedBackend.AuthenticatedActorId <= 0 ||
            trustedBackend.AuthenticatedTenantId <= 0 ||
            !trustedBackend.IdentityVerifiedByBackend ||
            !trustedBackend.MembershipAndRoleCurrent ||
            !trustedBackend.DocumentOwnershipFromPersistence)
            return new Decision("VERIFIED_BACKEND_AUTHORITY_REQUIRED");

        if (client is null || client.RequestedTenantId <= 0 ||
            client.RequestedTenantId != trustedBackend.AuthenticatedTenantId)
            return new Decision("CROSS_TENANT_CLIENT_CLAIM_DENIED");

        var authority = trustedBackend.Authority;
        var document = trustedBackend.Document;
        if (authority is null || document is null ||
            authority.AuthenticatedUserId != trustedBackend.AuthenticatedActorId ||
            authority.AuthenticatedTenantId != trustedBackend.AuthenticatedTenantId ||
            authority.ServerGrantedOperations is null)
            return new Decision("SERVER_IDENTITY_SCOPE_MISMATCH");

        // Enforce persisted ownership before passing the command to Domain;
        // do not permit nullable legacy ownership to become a tenant grant.
        if (document.DocumentTenantId != trustedBackend.AuthenticatedTenantId ||
            document.CustomerTenantId != trustedBackend.AuthenticatedTenantId ||
            document.ParentTenantId != trustedBackend.AuthenticatedTenantId)
            return new Decision("PERSISTED_TENANT_OWNERSHIP_REQUIRED");

        var preflight = new V5CommercialDocumentApplicationService().Prepare(
            new V5CommercialDocumentApplicationService.Request(
                client.ExpectedSourceSha,
                client.ObservedSourceSha,
                client.SanitizedEvidenceReference,
                authority, document, client.Command));

        if (!preflight.NeedsAuthenticatedAdapter)
            return new Decision(preflight.Code);

        // A positive preflight is not a permission: a future authenticated
        // durable adapter must revalidate roles, CAS, audit, PSP/fiscal and
        // idempotency inside its actual operation before any effect.
        return new Decision("DURABLE_COMMERCIAL_ADAPTER_AND_A5_REQUIRED");
    }
}
