using Solqaryn.Infrastructure.AutomationV5;
using App = Solqaryn.Application.AutomationV5.C0501DesignTokenApplicationService;
using Rules = Solqaryn.Domain.AutomationV5.C0501DesignTokenDomainRules;
using Xunit;
using Worker = Solqaryn.Infrastructure.AutomationV5.C0501DesignTokenBoundedReviewWorker;

namespace Solqaryn.Tests.Infrastructure;

public sealed class C0501DesignTokenBoundedReviewWorkerTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private static App.ActorSnapshot Actor() =>
        new(new Rules.TrustedContext(7, 11, true, true, true, false, 7, 9),
            true, true, true, true);

    private static App.CatalogSnapshot Catalog() =>
        new(11, "primary-color", 7,
            new HashSet<string>(StringComparer.Ordinal) { "primary-color" },
            true, true);

    private static Worker Create(App.ActorSnapshot? actor = null,
        App.CatalogSnapshot? catalog = null) =>
        new(new C0501DesignTokenIsolatedSnapshotAdapter(
            actor ?? Actor(), catalog ?? Catalog(), Sha));

    private static App.ClientIntent Intent() =>
        new(11, 9, 7, "primary-color", "COLOR", "#123AbC",
            "2c85f621-314a-4f81-88c4-8054e61f812d");

    private static Worker.Input Input(string attempt = "attempt-001",
        App.ClientIntent? intent = null) => new(attempt, intent ?? Intent());

    private static ValueTask<Worker.Result> Check(Worker worker,
        IReadOnlyList<Worker.Input>? batch = null, int actor = 7,
        int tenant = 11, long revision = 7, int epoch = 9,
        string? sha = Sha, CancellationToken ct = default) =>
        worker.PreviewAsync(batch ?? new[] { Input() },
            actor, tenant, revision, epoch, sha, ct);

    [Fact]
    public async Task ValidIsolatedTokenRequiresReauthenticatedBackendAndCannotApplyCss()
    {
        var r = await Check(Create());
        Assert.Equal("THEME_PREVIEW_ONLY_REAUTH_REQUIRED", r.Code);
        Assert.Single(r.Items);
        Assert.Equal(0, r.Items[0].Ordinal);
        Assert.Equal("AUTHENTICATED_THEME_ADAPTER_REQUIRED",
            r.Items[0].DecisionCode);
        Assert.False(r.AuthorizesThemeChange);
        Assert.False(r.AuthorizesTenantAccess);
        Assert.False(r.PersistsAnything);
        Assert.False(r.ReadsOrWritesMysql);
        Assert.False(r.AppliesCss);
        Assert.False(r.StartsHostedWorker);
        Assert.False(r.CertifiesAccessibility);
        Assert.False(r.CertifiesProduction);
        Assert.DoesNotContain("primary-color", r.ToString());
        Assert.DoesNotContain("#123AbC", r.ToString());
    }

    [Fact]
    public async Task EmptyAndOversizedBatchFailBeforeAnyResults()
    {
        Assert.Equal("THEME_BATCH_REQUIRED_OR_TOO_LARGE",
            (await Create().PreviewAsync(null, 7, 11, 7, 9, Sha)).Code);
        Assert.Equal("THEME_BATCH_REQUIRED_OR_TOO_LARGE",
            (await Create().PreviewAsync(Array.Empty<Worker.Input>(), 7, 11, 7, 9, Sha)).Code);
        var big = Enumerable.Range(0, 13)
            .Select(i => Input($"attempt-{i:000}")).ToArray();
        Assert.Equal("THEME_BATCH_REQUIRED_OR_TOO_LARGE",
            (await Check(Create(), big)).Code);
    }

    [Fact]
    public async Task DuplicateAttemptOrTokenDeniedAtomically()
    {
        Assert.Equal("THEME_DUPLICATE_ATTEMPT_DENIED",
            (await Check(Create(), new[] { Input(), Input() })).Code);
        var mixed = new[] { Input("attempt-001"), Input("attempt-002") };
        var r = await Check(Create(), mixed);
        Assert.Equal("THEME_DUPLICATE_TOKEN_DENIED", r.Code);
        Assert.Empty(r.Items);
    }

    [Fact]
    public async Task ForeignTenantAndStaleRevisionAreDeniedBeforeAdapter()
    {
        Assert.Equal("THEME_UNTRUSTED_BATCH_DENIED",
            (await Check(Create(), new[] { Input(intent:
                Intent() with { ClaimedTenantId = 22 }) })).Code);
        Assert.Equal("THEME_UNTRUSTED_BATCH_DENIED",
            (await Check(Create(), new[] { Input(intent:
                Intent() with { ExpectedRevision = 6 }) })).Code);
    }

    [Fact]
    public async Task DirectOrPlatformWritesAreNeverAccepted()
    {
        Assert.Equal("THEME_UNTRUSTED_BATCH_DENIED",
            (await Check(Create(), new[] { Input(intent:
                Intent() with { DirectWrite = true }) })).Code);
        Assert.Equal("THEME_UNTRUSTED_BATCH_DENIED",
            (await Check(Create(), new[] { Input(intent:
                Intent() with { PlatformCatalogMutation = true }) })).Code);
    }

    [Fact]
    public async Task WrongActorTenantEpochOrShaNeverReturnPreview()
    {
        foreach (var result in new[] {
            await Check(Create(), actor: 99),
            await Check(Create(), tenant: 22),
            await Check(Create(), epoch: 8),
            await Check(Create(), sha: OtherSha)
        })
        {
            Assert.NotEqual("THEME_PREVIEW_ONLY_REAUTH_REQUIRED", result.Code);
            Assert.Empty(result.Items);
        }
        Assert.Equal("THEME_TRUSTED_CONTEXT_REQUIRED",
            (await Check(Create(), sha: "bad")).Code);
    }

    [Fact]
    public async Task RevokedMembershipAndRoleAreDenied()
    {
        var revoked = Actor() with { Context =
            Actor().Context with { RoleRevoked = true } };
        var expired = Actor() with { Context =
            Actor().Context with { MembershipCurrent = false } };
        Assert.Equal("THEME_AUTHORITY_DENIED",
            (await Check(Create(actor: revoked))).Code);
        Assert.Equal("THEME_AUTHORITY_DENIED",
            (await Check(Create(actor: expired))).Code);
    }

    [Fact]
    public async Task UnsafeCssValueAndWrongOrdinalTokenFailClosed()
    {
        var unsafeValue = Intent() with { TokenValue = "url(javascript:alert(1))" };
        Assert.Equal("THEME_AUTHORITY_DENIED",
            (await Check(Create(), new[] { Input(intent: unsafeValue) })).Code);
        var wrongKey = Intent() with { TokenKey = "Primary-Color" };
        Assert.Equal("THEME_UNTRUSTED_BATCH_DENIED",
            (await Check(Create(), new[] { Input(intent: wrongKey) })).Code);
    }

    [Fact]
    public async Task WrongCatalogOwnershipAndCaseInsensitiveKeysCannotPass()
    {
        Assert.Equal("THEME_AUTHORITY_DENIED",
            (await Check(Create(catalog: Catalog() with { OwnerVerified = false }))).Code);
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PRIMARY-COLOR" };
        Assert.Equal("THEME_AUTHORITY_DENIED",
            (await Check(Create(catalog: Catalog() with { ApprovedKeys = keys }))).Code);
    }

    [Fact]
    public async Task MalformedAttemptIsDeniedWithoutPartialResult()
    {
        var r = await Check(Create(), new[] { Input("bad key") });
        Assert.Equal("THEME_UNTRUSTED_BATCH_DENIED", r.Code);
        Assert.Empty(r.Items);
    }

    [Fact]
    public async Task WrongSessionAndBackendJwtAreRejected()
    {
        Assert.Equal("THEME_AUTHORITY_DENIED",
            (await Check(Create(actor: Actor() with { JwtFresh = false }))).Code);
        Assert.Equal("THEME_UNTRUSTED_BATCH_DENIED",
            (await Check(Create(), new[] { Input(intent:
                Intent() with { SessionEpoch = 8 }) })).Code);
    }

    [Fact]
    public async Task CancellationPropagatesNoFakePartialSuccess()
    {
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Check(Create(), ct: new CancellationToken(true)));
    }

    [Fact]
    public void RequiresIsolatedAdapter()
    {
        Assert.Throws<ArgumentNullException>(() => new Worker(null!));
    }
}
