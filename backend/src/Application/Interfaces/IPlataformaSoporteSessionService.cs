using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Interfaces;

public sealed record PlatformSupportAuditContext(
    string? Ip,
    string? UserAgent,
    string CorrelationId);

public sealed record PlatformSupportOperationResult(
    bool Succeeded,
    int StatusCode,
    string Code,
    int? SessionId = null,
    string? Token = null,
    DateTime? ExpiresUtc = null);

public sealed record PlatformSupportReadResult(
    bool Authorized,
    int StatusCode,
    string Code,
    int? SessionId = null);

public interface IPlataformaSoporteSessionService
{
    Task<PlatformSupportOperationResult> CreateAsync(
        int empresaId,
        ModuloSistema modulo,
        string? motivo,
        int duracionMinutos,
        PlatformSupportAuditContext audit,
        CancellationToken cancellationToken = default);

    Task<PlatformSupportOperationResult> RevokeAsync(
        int sessionId,
        PlatformSupportAuditContext audit,
        CancellationToken cancellationToken = default);

    Task<PlatformSupportReadResult> AuthorizeReadAsync(
        string? token,
        int? empresaId,
        ModuloSistema? modulo,
        bool routeIsExplicitRead,
        PlatformSupportAuditContext audit,
        CancellationToken cancellationToken = default);

    Task CompleteAuthorizedReadAsync(
        bool requestCompleted,
        CancellationToken cancellationToken = default);
}