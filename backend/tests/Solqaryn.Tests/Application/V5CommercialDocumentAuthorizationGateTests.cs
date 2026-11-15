using Solqaryn.Application.AutomationV5;
using Solqaryn.Domain.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5CommercialDocumentDomainRules;

namespace Solqaryn.Tests.Application;

public sealed class V5CommercialDocumentAuthorizationGateTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private static readonly V5CommercialDocumentAuthorizationGate Gate = new();

    private static Rules.TrustedContext Authority() =>
        new(11, 7, new HashSet<Rules.Operation>(Enum.GetValues<Rules.Operation>()),
            true, true);

    private static Rules.PersistedSnapshot Document() =>
        new(11, 11, 11, "BORRADOR", "HNL", 125.50m, 4);

    private static V5CommercialDocumentAuthorizationGate.ServerAccess Server() =>
        new(7, 11, true, true, true, Authority(), Document());

    private static V5CommercialDocumentAuthorizationGate.ClientCommand Client() =>
        new(11, Sha, Sha, "ci:commercial:tenant-a",
            new(Rules.Operation.ConfirmOrder, 4, 125.50m, "HNL",
                "tenant-a-confirm-4", new string('a', 64)));

    [Fact]
    public void LegitimatePreflightStillRequiresDurableAuthenticatedAdapterAndA5()
    {
        var result = Gate.Prepare(Server(), Client());
        Assert.Equal("DURABLE_COMMERCIAL_ADAPTER_AND_A5_REQUIRED", result.Code);
        Assert.True(result.NeedsAuthenticatedTransactionalAdapter);
        Assert.False(result.AuthorizesMutation);
        Assert.False(result.AuthorizesCrossTenantRead);
        Assert.False(result.AuthorizesPayment);
        Assert.False(result.AuthorizesFiscalIssuance);
        Assert.False(result.AuthorizesRelease);
    }

    [Fact]
    public void NullUnauthenticatedRevokedOrUnprivilegedServerFailsClosed()
    {
        Assert.Equal("VERIFIED_BACKEND_AUTHORITY_REQUIRED", Gate.Prepare(null, Client()).Code);
        Assert.Equal("VERIFIED_BACKEND_AUTHORITY_REQUIRED",
            Gate.Prepare(Server() with { IdentityVerifiedByBackend = false }, Client()).Code);
        Assert.Equal("VERIFIED_BACKEND_AUTHORITY_REQUIRED",
            Gate.Prepare(Server() with { MembershipAndRoleCurrent = false }, Client()).Code);
        Assert.Equal("VERIFIED_BACKEND_AUTHORITY_REQUIRED",
            Gate.Prepare(Server() with { DocumentOwnershipFromPersistence = false }, Client()).Code);
    }

    [Fact]
    public void ClientClaimFromTenantBCannotChooseTenantAOrOverrideAuthority()
    {
        Assert.Equal("CROSS_TENANT_CLIENT_CLAIM_DENIED",
            Gate.Prepare(Server(), Client() with { RequestedTenantId = 22 }).Code);
        Assert.Equal("CROSS_TENANT_CLIENT_CLAIM_DENIED", Gate.Prepare(Server(), null).Code);
    }

    [Fact]
    public void BackendPrincipalOrAuthorityTenantMismatchIsDenied()
    {
        Assert.Equal("SERVER_IDENTITY_SCOPE_MISMATCH",
            Gate.Prepare(Server() with { Authority = Authority() with
                { AuthenticatedUserId = 9 } }, Client()).Code);
        Assert.Equal("SERVER_IDENTITY_SCOPE_MISMATCH",
            Gate.Prepare(Server() with { Authority = Authority() with
                { AuthenticatedTenantId = 22 } }, Client()).Code);
        Assert.Equal("SERVER_IDENTITY_SCOPE_MISMATCH",
            Gate.Prepare(Server() with { Authority = null }, Client()).Code);
    }

    [Fact]
    public void LegacyNullOwnershipOrForeignPersistedDocumentCannotPass()
    {
        Assert.Equal("PERSISTED_TENANT_OWNERSHIP_REQUIRED",
            Gate.Prepare(Server() with { Document = Document() with
                { ParentTenantId = null } }, Client()).Code);
        Assert.Equal("PERSISTED_TENANT_OWNERSHIP_REQUIRED",
            Gate.Prepare(Server() with { Document = Document() with
                { CustomerTenantId = 22 } }, Client()).Code);
    }

    [Fact]
    public void RevokedCommercialGrantOrStaleVersionIsDenied()
    {
        Assert.Equal("OPERATION_PERMISSION_REQUIRED",
            Gate.Prepare(Server() with { Authority = Authority() with
                { ServerGrantedOperations = new HashSet<Rules.Operation>() } }, Client()).Code);
        Assert.Equal("STALE_DOCUMENT_VERSION",
            Gate.Prepare(Server(), Client() with { Command = Client().Command! with
                { ExpectedVersion = 3 } }).Code);
    }

    [Fact]
    public void UnsafeSourceShaOrEvidenceReferenceIsDenied()
    {
        Assert.Equal("COMMERCIAL_SOURCE_SHA_UNVERIFIED",
            Gate.Prepare(Server(), Client() with { ObservedSourceSha = "short" }).Code);
        Assert.Equal("COMMERCIAL_EVIDENCE_REFERENCE_REQUIRED",
            Gate.Prepare(Server(), Client() with
                { SanitizedEvidenceReference = "token@tenant-b" }).Code);
    }

    [Fact]
    public void InvalidCurrencyIdempotencyOrAmountNeverReachesAdapter()
    {
        Assert.Equal("CURRENCY_MISMATCH",
            Gate.Prepare(Server(), Client() with { Command = Client().Command! with
                { Currency = "USD" } }).Code);
        Assert.Equal("INVALID_IDEMPOTENCY_CONTRACT",
            Gate.Prepare(Server(), Client() with { Command = Client().Command! with
                { PayloadSha256 = "invalid" } }).Code);
        Assert.Equal("INVALID_DOCUMENT_AMOUNT",
            Gate.Prepare(Server(), Client() with { Command = Client().Command! with
                { Amount = 125.51m } }).Code);
    }

    [Fact]
    public void PaymentAndFiscalFlagsCannotCreateRuntimePermission()
    {
        var invoice = Client() with { Command = Client().Command! with
            { RequestedOperation = Rules.Operation.IssueInvoice } };
        Assert.Equal("DOCUMENT_STATE_CONFLICT", Gate.Prepare(Server(), invoice).Code);
        var payment = Server() with
        {
            Authority = Authority() with { PaymentProviderApproved = false },
            Document = Document() with { State = "EMITIDO" }
        };
        var pay = Client() with { Command = Client().Command! with
            { RequestedOperation = Rules.Operation.ApplyPayment } };
        Assert.Equal("PAYMENT_PROVIDER_NOT_AUTHORIZED", Gate.Prepare(payment, pay).Code);
    }
}
