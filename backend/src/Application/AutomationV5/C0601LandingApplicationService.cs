using Solqaryn.Domain.AutomationV5;

namespace Solqaryn.Application.AutomationV5;

/// <summary>
/// Read the canonical path and host from trusted server routing state.
/// Never derive authority from unvalidated Host, X-Forwarded-* or client DTOs.
/// A future adapter must establish this trust: this contract does not do so.
/// </summary>
public interface IC0601VerifiedPublicReadContextSource
{
    ValueTask<C0601PublicReadContext?> ReadCurrentAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Future persisted platform snapshot, selected by the server-derived route.
/// Implementations must enforce platform ownership, revision and publication
/// checks; this interface is NOT a proof of MySQL or JWT integration.
/// </summary>
public interface IC0601VerifiedPublicContentSource
{
    ValueTask<C0601PublicContentSnapshot?> ReadForRouteAsync(
        string serverCanonicalRoute, CancellationToken cancellationToken);
}

/// <summary>
/// C06-01.M03.02: application preflight, bounded to a prospective public
/// candidate. Never exposes content, authenticates a user, issues permission,
/// publishes a page, reserves a price or writes a database.
/// No DI or HTTP endpoint is registered by this microtask.
/// </summary>
public sealed class C0601LandingApplicationService
{
    private readonly IC0601VerifiedPublicReadContextSource _routing;
    private readonly IC0601VerifiedPublicContentSource _content;

    public C0601LandingApplicationService(
        IC0601VerifiedPublicReadContextSource routing,
        IC0601VerifiedPublicContentSource content)
    {
        _routing = routing ?? throw new ArgumentNullException(nameof(routing));
        _content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public readonly record struct Preview(string Code, bool IsCandidateForFurtherReview)
    {
        // A positive domain candidate is never an authorization grant.
        public bool AuthorizesPublication => false;
        public bool AuthorizesPrivateAccess => false;
        public bool AuthorizesPayment => false;
        public bool CertifiesJwtMysqlOrHttp => false;
        public bool ContainsPublishedContent => false;
    }

    public async ValueTask<Preview> PrepareAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        C0601PublicReadContext? server;
        try
        {
            server = await _routing.ReadCurrentAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("VERIFIED_SERVER_ROUTING_UNAVAILABLE");
        }
        cancellationToken.ThrowIfCancellationRequested();

        if (server is null || !server.ServerSourceVerified ||
            string.IsNullOrEmpty(server.ServerRouteKey) ||
            string.IsNullOrEmpty(server.ServerCanonicalHost))
            return Deny("TRUSTED_SERVER_ROUTE_REQUIRED");

        C0601PublicContentSnapshot? snapshot;
        try
        {
            // Never accept a client tenant, host, content identifier or revision.
            snapshot = await _content.ReadForRouteAsync(
                server.ServerRouteKey, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return Deny("VERIFIED_PLATFORM_SNAPSHOT_UNAVAILABLE");
        }
        cancellationToken.ThrowIfCancellationRequested();

        var decision = C0601LandingDomainRules.Evaluate(snapshot, server);
        return decision.IsCandidateForFurtherReview &&
               decision.Code == "CANDIDATE_ONLY_NO_PUBLICATION_GRANT"
            ? new Preview("PLATFORM_PUBLICATION_AND_LEGAL_GATES_STILL_REQUIRED", true)
            : Deny(decision.Code);
    }

    private static Preview Deny(string code) => new(code, false);
}
