using System.Text.RegularExpressions;

namespace Solqaryn.Domain.AutomationV5;

/// <summary>
/// Inert, versioned input from a future trusted platform adapter. It is not
/// proof of publication, commercial approval, JWT or server authorization.
/// </summary>
public sealed record C0601PublicContentSnapshot(
    string RouteKey,
    string CanonicalHost,
    string ContentKey,
    long Revision,
    DateTime ValidFromUtc,
    DateTime ValidUntilUtc,
    bool PlatformApproved,
    bool Active,
    bool SourceVersionVerified,
    bool Draft,
    bool ContainsTenantData,
    bool ContainsUnapprovedPrice,
    bool ContainsUnapprovedLegalText,
    bool ContainsExecutableMarkup);

/// <summary>
/// All values must be produced by a trusted server adapter, not client input.
/// This record does not authenticate an actor or provide authorization.
/// </summary>
public sealed record C0601PublicReadContext(
    string ServerRouteKey,
    string ServerCanonicalHost,
    DateTime NowUtc,
    bool ServerSourceVerified);

public sealed record C0601PublicContentDecision(string Code, bool IsCandidateForFurtherReview)
{
    public bool AuthorizesPublication => false;
    public bool AuthorizesPrivateAccess => false;
    public bool AuthorizesPayment => false;
    public bool CertifiesHttpOrMysql => false;
}

/// <summary>
/// C06-01.M03.01: pure, fail-closed server-side domain predicates for future
/// public-site content. API, persistence, JWT, publishing CAS, contact, legal
/// and payments belong to later separately authorized microtasks.
/// </summary>
public static class C0601LandingDomainRules
{
    private static readonly Regex RoutePattern = new(
        @"^/[a-z0-9]+(?:-[a-z0-9]+)*(?:/[a-z0-9]+(?:-[a-z0-9]+)*){0,2}\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ContentKeyPattern = new(
        @"^[a-z0-9]+(?:[._-][a-z0-9]+)*\z",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> ReservedPrivateRoots = new(StringComparer.Ordinal)
    {
        "api", "admin", "auth", "dashboard", "erp", "facturas", "login", "plataforma", "tienda"
    };

    private static C0601PublicContentDecision Reject(string code) => new(code, false);
    private static bool AsciiAlphanumeric(char c) =>
        (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');

    private static bool IsCanonicalHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host) || host.Length > 253 ||
            host.Contains('/') || host.Contains(':') || host.Contains('@'))
            return false;

        var labels = host.Split('.');
        return labels.Length >= 2 && labels.All(label =>
            label.Length is >= 1 and <= 63 &&
            AsciiAlphanumeric(label[0]) &&
            AsciiAlphanumeric(label[^1]) &&
            label.All(c => AsciiAlphanumeric(c) || c == '-'));
    }

    public static C0601PublicContentDecision Evaluate(
        C0601PublicContentSnapshot? publication,
        C0601PublicReadContext? context)
    {
        if (publication is null || context is null || !context.ServerSourceVerified)
            return Reject("TRUSTED_SERVER_SOURCE_REQUIRED");

        if (context.NowUtc.Kind != DateTimeKind.Utc ||
            publication.ValidFromUtc.Kind != DateTimeKind.Utc ||
            publication.ValidUntilUtc.Kind != DateTimeKind.Utc)
            return Reject("UTC_EVIDENCE_REQUIRED");

        if (string.IsNullOrEmpty(publication.RouteKey) ||
            publication.RouteKey.Length > 160 ||
            !RoutePattern.IsMatch(publication.RouteKey) ||
            ReservedPrivateRoots.Contains(publication.RouteKey.Split('/')[1]) ||
            !IsCanonicalHost(publication.CanonicalHost) ||
            !IsCanonicalHost(context.ServerCanonicalHost) ||
            !string.Equals(publication.RouteKey, context.ServerRouteKey, StringComparison.Ordinal) ||
            !string.Equals(publication.CanonicalHost, context.ServerCanonicalHost, StringComparison.Ordinal))
            return Reject("CANONICAL_ROUTE_HOST_MISMATCH");

        if (publication.Revision <= 0 ||
            string.IsNullOrEmpty(publication.ContentKey) ||
            publication.ContentKey.Length > 80 ||
            !ContentKeyPattern.IsMatch(publication.ContentKey))
            return Reject("VERSIONED_CONTENT_KEY_REQUIRED");

        if (!publication.PlatformApproved || !publication.SourceVersionVerified ||
            !publication.Active || publication.Draft)
            return Reject("PLATFORM_APPROVAL_REQUIRED");

        if (publication.ContainsTenantData || publication.ContainsUnapprovedPrice ||
            publication.ContainsUnapprovedLegalText || publication.ContainsExecutableMarkup)
            return Reject("UNSAFE_OR_UNAPPROVED_CONTENT");

        if (publication.ValidUntilUtc <= publication.ValidFromUtc ||
            context.NowUtc < publication.ValidFromUtc ||
            context.NowUtc >= publication.ValidUntilUtc)
            return Reject("PUBLICATION_WINDOW_INVALID");

        // Candidate ONLY. The actual server must still prove platform ownership,
        // commercial/legal decisions, authentication, revisions and publishing CAS.
        return new C0601PublicContentDecision("CANDIDATE_ONLY_NO_PUBLICATION_GRANT", true);
    }
}
