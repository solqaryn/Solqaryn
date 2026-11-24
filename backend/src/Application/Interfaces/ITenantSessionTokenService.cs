namespace Solqaryn.Application.Interfaces;

/// <summary>Credential limited to the explicitly selected context.</summary>
public sealed record TenantSessionAuthority(int ActorId, int BaseRoleId, int TenantId,
    int BranchId, int TenantRoleId, DateTime BaseExpiresUtc);

public interface ITenantSessionTokenService
{
    (string Token, DateTime ExpiraEn) GenerarTokenContexto(TenantSessionAuthority authority);
}

public static class TenantSessionToken
{
    public const string Scheme = "TenantContextBearer";
    public static string Audience(string baseAudience) => baseAudience + ":tenant-context";
}
