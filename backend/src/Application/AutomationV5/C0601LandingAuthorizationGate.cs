using System;
namespace Solqaryn.Application.AutomationV5;

public interface IC0601LandingVerifiedActorReader
{
    ValueTask<C0601LandingAuthorizationGate.VerifiedActor?> ReadCurrentAsync(CancellationToken cancellationToken);
}

/// <summary>C06-01.M03.03: tenant-scoped signed JWT + current RBAC read-only preview. No public/runtime grant.</summary>
public sealed class C0601LandingAuthorizationGate(IC0601LandingVerifiedActorReader reader)
{
    private readonly IC0601LandingVerifiedActorReader _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    public sealed record VerifiedScope(int TenantEmpresaId, int ActorId, int RolId);
    public sealed record VerifiedActor(VerifiedScope Scope, bool SignedJwtEstablished, bool MembershipCurrent, bool RoleAndGrantCurrent);
    public sealed record Request(int TenantEmpresaId, bool ClientClaimsRole = false, bool ClientSuppliesHost = false,
        bool RequestsPublication = false, bool RequestsPrivateAccess = false, bool RequestsPayment = false);
    public readonly record struct Decision(string Code)
    {
        public bool MayInspectInertCandidate => Code == "READONLY_LANDING_AUTHORITY_PREFLIGHT_ONLY";
        public bool AuthorizesPublication => false;
        public bool GrantsPrivateOrCrossTenantRead => false;
        public bool AuthorizesPayment => false;
        public bool IssuesJwt => false;
        public bool CertifiesPublicRuntime => false;
    }
    public async ValueTask<Decision> InspectAsync(Request? request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (request is null || request.TenantEmpresaId <= 0 || request.ClientClaimsRole || request.ClientSuppliesHost ||
            request.RequestsPublication || request.RequestsPrivateAccess || request.RequestsPayment) return Deny();
        VerifiedActor? verified;
        try
        {
            verified = await _reader.ReadCurrentAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return Deny(); }
        if (verified?.Scope is not { } scope || scope.TenantEmpresaId != request.TenantEmpresaId ||
            scope.ActorId <= 0 || scope.RolId <= 0 || !verified.SignedJwtEstablished ||
            !verified.MembershipCurrent || !verified.RoleAndGrantCurrent) return Deny();
        return new Decision("READONLY_LANDING_AUTHORITY_PREFLIGHT_ONLY");
    }
    private static Decision Deny() => new("LANDING_AUTHORIZATION_DENIED");
}
