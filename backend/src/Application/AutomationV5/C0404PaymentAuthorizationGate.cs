namespace Solqaryn.Application.AutomationV5;
/// <summary>
/// C04-04.M03.03: trusted backend-only reader. A future real adapter MUST derive
/// the authenticated actor from validated JWT and current tenancy/grants from MySQL.
/// Never accept a browser role, token, client membership, or host as authority.
/// No adapter, API endpoint, database write or PSP is registered by this class.
/// </summary>
public interface IC0404PaymentAuthorizationEvidenceReader
{
    ValueTask<C0404PaymentAuthorizationGate.TrustedSnapshot?> ReadCurrentAsync(CancellationToken cancellationToken);
}
/// <summary>R2 fail-closed gate for pure payment planning; NEVER authorizes money movements.</summary>
public sealed class C0404PaymentAuthorizationGate
{
    private readonly IC0404PaymentAuthorizationEvidenceReader _reader;
    public C0404PaymentAuthorizationGate(IC0404PaymentAuthorizationEvidenceReader reader) =>
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    public enum Operation { CreateLink = 1, Refund = 2 }
    public sealed record Request(int TenantId, int InvoiceId, Operation Action,
        bool ClientClaimsRole = false, bool ClientSuppliesJwt = false,
        bool ClientClaimsMembership = false, bool ClientSuppliesProviderSignature = false,
        bool RequestsImmediateCharge = false, bool RequestsBypassOfLedger = false);
    public sealed record TrustedSnapshot
    {
        public int ActorId { get; init; }
        public int TenantId { get; init; }
        public int InvoiceId { get; init; }
        public bool JwtSignatureIssuerAudienceLifetimeVerifiedByBackend { get; init; }
        public bool CurrentSessionVerified { get; init; }
        public bool UserActive { get; init; }
        public bool AuthorityDatabaseAvailable { get; init; }
        public bool MembershipFromPersistence { get; init; }
        public bool MembershipActive { get; init; }
        public bool RoleAndGrantFromPersistence { get; init; }
        public bool PermissionRevoked { get; init; }
        public bool TenantReadFencedByDatabase { get; init; }
        public bool InvoiceOwnershipVerifiedFromPersistence { get; init; }
        public bool AuthorizationRevisionFresh { get; init; }
        public bool FeatureEnabledByTrustedConfig { get; init; }
        public bool CreateGrant { get; init; }
        public bool RefundGrant { get; init; }
        public bool ReceiptAndLedgerVersionVerified { get; init; }
    }
    public readonly record struct Decision(string Code)
    {
        public bool MayInspectPureApplicationPlan => Code == "READONLY_PAYMENT_PLAN_PREFLIGHT_ONLY";
        public bool AuthorizesFinancialMutation => false;
        public bool AuthorizesProviderCall => false;
        public bool AuthorizesLedgerWrite => false;
        public bool InstallsTenantContext => false;
        public bool CertifiesJwtOrDatabaseIntegration => false;
    }
    private static Decision Deny() => new("PAYMENT_AUTHORIZATION_DENIED");
    public async ValueTask<Decision> InspectAsync(Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || request.TenantId <= 0 || request.InvoiceId <= 0 ||
            !Enum.IsDefined(request.Action) || request.ClientClaimsRole ||
            request.ClientSuppliesJwt || request.ClientClaimsMembership ||
            request.ClientSuppliesProviderSignature || request.RequestsImmediateCharge ||
            request.RequestsBypassOfLedger)
            return Deny();
        TrustedSnapshot? trusted;
        try { trusted = await _reader.ReadCurrentAsync(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return Deny(); }
        if (trusted is null || trusted.ActorId <= 0 || trusted.TenantId != request.TenantId ||
            trusted.InvoiceId != request.InvoiceId ||
            !trusted.JwtSignatureIssuerAudienceLifetimeVerifiedByBackend ||
            !trusted.CurrentSessionVerified || !trusted.UserActive ||
            !trusted.AuthorityDatabaseAvailable || !trusted.MembershipFromPersistence ||
            !trusted.MembershipActive || !trusted.RoleAndGrantFromPersistence ||
            trusted.PermissionRevoked || !trusted.TenantReadFencedByDatabase ||
            !trusted.InvoiceOwnershipVerifiedFromPersistence ||
            !trusted.AuthorizationRevisionFresh || !trusted.FeatureEnabledByTrustedConfig)
            return Deny();
        if (request.Action == Operation.CreateLink && !trusted.CreateGrant) return Deny();
        if (request.Action == Operation.Refund &&
            (!trusted.RefundGrant || !trusted.ReceiptAndLedgerVersionVerified)) return Deny();
        return new("READONLY_PAYMENT_PLAN_PREFLIGHT_ONLY");
    }
}
