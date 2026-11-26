using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;

namespace Solqaryn.API.Security;

/// <summary>DEV-only authenticated enterprise integration; never a general quarantine exemption.</summary>
public static class V5CaseFileEnterpriseEndpoints
{
    public sealed record Operation(AccionPermiso Permission);
    public sealed record VersionRequest(int AlmacenId, long? ExpectedVersion);
    public sealed record CorrelationRequest(string? Correlation);

    public static bool ShouldMap(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() && configuration.GetValue<bool>("CaseFile:EnableEnterpriseIntegration");

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/__dev/expedientes/almacenes")
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = TenantSessionToken.Scheme });
        group.MapPost("/{caseId:int}/versiones", WriteAsync)
            .WithMetadata(new Operation(AccionPermiso.WriteCaseFileVersion)).ExcludeFromDescription();
        group.MapPost("/{caseId:int}/auditoria/registrar", RecordAsync)
            .WithMetadata(new Operation(AccionPermiso.ReadAuditCorrelation)).ExcludeFromDescription();
        group.MapPost("/{caseId:int}/auditoria/correlacion", ReadAsync)
            .WithMetadata(new Operation(AccionPermiso.ReadAuditCorrelation)).ExcludeFromDescription();
    }

    private static async Task<IResult> WriteAsync(HttpContext http, int caseId, VersionRequest input,
        V5CaseFileEnterpriseService service, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        var result = await service.AppendVersionAsync(http, caseId, input.AlmacenId, input.ExpectedVersion, ct);
        return result.Code switch
        {
            "CASEFILE_VERSION_WRITTEN" => Results.Ok(new { result.Code, result.Version }),
            "CASEFILE_VERSION_CONFLICT" => Results.StatusCode(StatusCodes.Status409Conflict),
            "CASEFILE_WRITE_DENIED" => Results.StatusCode(StatusCodes.Status403Forbidden),
            _ => Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
        };
    }

    private static async Task<IResult> RecordAsync(HttpContext http, int caseId,
        V5CaseFileEnterpriseService service, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        return AuditResult(await service.RecordOrReadAsync(http, caseId, null, ct));
    }

    private static async Task<IResult> ReadAsync(HttpContext http, int caseId, CorrelationRequest input,
        V5CaseFileEnterpriseService service, CancellationToken ct)
    {
        http.Response.Headers.CacheControl = "no-store";
        if (!Guid.TryParseExact(input.Correlation, "N", out var target) || target == Guid.Empty)
            return Results.BadRequest();
        return AuditResult(await service.RecordOrReadAsync(http, caseId, target, ct));
    }

    private static IResult AuditResult(V5CaseFileEnterpriseService.CorrelationResult result) =>
        result.Code switch
        {
            "CASEFILE_AUDIT_RECEIPT_WRITTEN" or "CASEFILE_AUDIT_CORRELATION_READ" =>
                Results.Ok(new { result.Code, Correlation = result.Correlation?.ToString("N"), result.TargetVersion }),
            "CASEFILE_AUDIT_DENIED" => Results.StatusCode(StatusCodes.Status403Forbidden),
            _ => Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
        };
}
