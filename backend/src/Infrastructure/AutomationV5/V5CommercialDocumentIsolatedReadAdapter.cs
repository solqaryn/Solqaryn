using Solqaryn.Application.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5CommercialDocumentDomainRules;

namespace Solqaryn.Infrastructure.AutomationV5;

/// <summary>Backend-only current identity, membership and grants. No client-supplied claims.</summary>
public interface IV5CommercialTrustedPrincipalSource
{
    ValueTask<V5CommercialDocumentIsolatedReadAdapter.PrincipalSnapshot?> ReadCurrentAsync(
        CancellationToken cancellationToken);
}

/// <summary>Persistent source MUST filter by (tenantId, documentId), not documentId alone.</summary>
public interface IV5CommercialTenantDocumentSource
{
    ValueTask<V5CommercialDocumentIsolatedReadAdapter.DocumentSnapshot?> ReadAsync(
        int tenantId, int documentId, CancellationToken cancellationToken);
}

/// <summary>
/// C07-04.M03.04: read-only, isolated Infrastructure adapter which obtains both
/// current backend authority and persisted document ownership before calling M03.03.
/// It is deliberately not registered in DI: no EF mapping, migration, payment, invoice,
/// export, transaction or PSP provider is installed by this changeset.
/// </summary>
public sealed class V5CommercialDocumentIsolatedReadAdapter
{
    private readonly IV5CommercialTrustedPrincipalSource _principals;
    private readonly IV5CommercialTenantDocumentSource _documents;
    private readonly V5CommercialDocumentAuthorizationGate _gate = new();

    public V5CommercialDocumentIsolatedReadAdapter(
        IV5CommercialTrustedPrincipalSource principals,
        IV5CommercialTenantDocumentSource documents)
    {
        _principals = principals ?? throw new ArgumentNullException(nameof(principals));
        _documents = documents ?? throw new ArgumentNullException(nameof(documents));
    }

    public sealed record PrincipalSnapshot(
        int ActorId, int TenantId, string? SourceSha, string? EvidenceReference,
        bool AuthenticatedByBackend, bool MembershipReadFromBackend,
        bool MembershipActive, bool GrantsReadFromBackend, bool GrantsRevoked,
        IReadOnlySet<Rules.Operation>? CurrentGrantedOperations,
        bool FiscalIssuerVerified, bool PaymentProviderApproved);

    public sealed record DocumentSnapshot(
        int DocumentId, int? DocumentTenantId, int? CustomerTenantId,
        int? ParentTenantId, string? State, string? Currency, decimal Amount,
        long Version, string? SourceSha, bool OwnershipReadFromPersistence,
        bool UnattributedLegacy);

    public readonly record struct Result(string Code)
    {
        public bool NeedsDurableAuthenticatedTransaction =>
            Code == "DURABLE_COMMERCIAL_ADAPTER_AND_A5_REQUIRED";
        public bool GrantsMutation => false;
        public bool GrantsPayment => false;
        public bool GrantsFiscalIssuance => false;
        public bool GrantsExport => false;
        public bool GrantsCrossTenantRead => false;
    }

    public async ValueTask<Result> InspectAsync(
        int documentId, Rules.Command? command, string? expectedSourceSha,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (documentId <= 0 || command is null || !FullSha(expectedSourceSha))
            return new("VALID_RESOURCE_COMMAND_AND_SHA_REQUIRED");

        PrincipalSnapshot? principal;
        try
        {
            principal = await _principals.ReadCurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("TRUSTED_PRINCIPAL_SOURCE_UNAVAILABLE");
        }

        if (principal is null || principal.ActorId <= 0 || principal.TenantId <= 0 ||
            !principal.AuthenticatedByBackend || !principal.MembershipReadFromBackend ||
            !principal.MembershipActive || !principal.GrantsReadFromBackend ||
            principal.GrantsRevoked || principal.CurrentGrantedOperations is null ||
            !principal.CurrentGrantedOperations.Contains(command.RequestedOperation) ||
            !string.Equals(principal.SourceSha, expectedSourceSha, StringComparison.Ordinal))
            return new("CURRENT_SERVER_IDENTITY_AND_GRANT_REQUIRED");

        cancellationToken.ThrowIfCancellationRequested();
        DocumentSnapshot? stored;
        try
        {
            stored = await _documents.ReadAsync(principal.TenantId, documentId, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new("TENANT_DOCUMENT_SOURCE_UNAVAILABLE");
        }

        if (stored is null || stored.DocumentId != documentId ||
            stored.DocumentTenantId != principal.TenantId ||
            stored.CustomerTenantId != principal.TenantId ||
            stored.ParentTenantId != principal.TenantId ||
            stored.Version <= 0 || stored.UnattributedLegacy ||
            !stored.OwnershipReadFromPersistence ||
            !string.Equals(stored.SourceSha, expectedSourceSha, StringComparison.Ordinal))
            return new("PERSISTED_TENANT_DOCUMENT_SNAPSHOT_REQUIRED");

        // Permissions and ownership are evaluated again by the already-tested
        // Application gate. A positive preflight is *not* permission to transact.
        var authority = new Rules.TrustedContext(
            principal.TenantId, principal.ActorId,
            new HashSet<Rules.Operation>(principal.CurrentGrantedOperations),
            principal.FiscalIssuerVerified, principal.PaymentProviderApproved);
        var document = new Rules.PersistedSnapshot(
            stored.DocumentTenantId, stored.CustomerTenantId, stored.ParentTenantId,
            stored.State ?? string.Empty, stored.Currency ?? string.Empty,
            stored.Amount, stored.Version);
        var server = new V5CommercialDocumentAuthorizationGate.ServerAccess(
            principal.ActorId, principal.TenantId, true, true, true, authority, document);
        var client = new V5CommercialDocumentAuthorizationGate.ClientCommand(
            principal.TenantId, expectedSourceSha, stored.SourceSha,
            principal.EvidenceReference, command);
        var decision = _gate.Prepare(server, client);
        return new(decision.Code);
    }

    private static bool FullSha(string? sha) =>
        sha is { Length: 40 } && sha.All(Uri.IsHexDigit);
}
