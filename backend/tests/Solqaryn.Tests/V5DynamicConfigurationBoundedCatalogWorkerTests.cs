using System.Collections;
using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.V5DynamicConfigurationDomainRules;
using Worker = Solqaryn.Application.AutomationV5.V5DynamicConfigurationBoundedCatalogWorker;
using Preflight = Solqaryn.Application.AutomationV5.V5DynamicConfigurationApplicationPreflight;

namespace Solqaryn.Tests;

public sealed class V5DynamicConfigurationBoundedCatalogWorkerTests
{
    private static readonly string Sha = new('a', 40);

    private sealed class Actors : IV5DynamicConfigurationCurrentActorReader
    {
        public Preflight.ActorEvidence? Evidence { get; set; } = Active();
        public int Calls { get; private set; }
        public ValueTask<Preflight.ActorEvidence?> ReadCurrentAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(Evidence);
        }
    }

    private sealed class Catalog : IV5DynamicConfigurationServerCatalogReader
    {
        public readonly Dictionary<string, Preflight.CatalogEvidence> Entries =
            new(StringComparer.Ordinal);
        public readonly List<(int Tenant, string Key)> Calls = new();
        public bool ThrowOnRead { get; set; }
        public ValueTask<Preflight.CatalogEvidence?> ReadAsync(
            int tenant, string key, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Calls.Add((tenant, key));
            if (ThrowOnRead) throw new InvalidOperationException("provider diagnostic");
            return ValueTask.FromResult(
                Entries.TryGetValue(key, out var result) ? result : null);
        }
    }

    private static Preflight.ActorEvidence Active(
        int tenant = 7, bool membership = true, bool permission = true, bool revoked = false) =>
        new(12, tenant, Sha, true, true,
            new Rules.TrustedAuthority(tenant, true, membership, permission,
                true, revoked, false, false));

    private static Preflight.CatalogEvidence Entry(
        string key, int tenant = 7, bool secret = false, string? sha = null) =>
        new(tenant, sha ?? Sha, true, true, 3,
            new Rules.CatalogKey(key, Rules.ValueKind.SafeString,
                Rules.ConfigScope.Tenant, secret, 60, null, null),
            new HashSet<string>(new[] { key }, StringComparer.Ordinal));

    private static Worker.Request Request(params string[] keys) => new(7, Sha, keys);

    [Fact]
    public async Task Valid_catalog_is_deterministic_but_grants_no_permissions()
    {
        var catalog = new Catalog();
        catalog.Entries["screen.beta"] = Entry("screen.beta");
        catalog.Entries["screen.alpha"] = Entry("screen.alpha");
        var result = await new Worker(new Actors(), catalog).ProjectAsync(
            Request("screen.beta", "screen.alpha"));
        Assert.Equal("CONFIG_BATCH_PROJECTION_ONLY", result.Code);
        Assert.Equal(new[] { "screen.alpha", "screen.beta" },
            result.Descriptors.Select(x => x.Key));
        Assert.All(catalog.Calls, c => Assert.Equal(7, c.Tenant));
        Assert.False(result.GrantsRead);
        Assert.False(result.GrantsWrite);
        Assert.False(result.GrantsPlatformScope);
        Assert.False(result.CertifiesDurableCas);
        Assert.False(result.CertifiesJwtOrPersistence);
    }

    [Fact]
    public async Task Tenant_B_is_denied_before_catalog_io()
    {
        var catalog = new Catalog();
        var result = await new Worker(new Actors(), catalog).ProjectAsync(
            new Worker.Request(8, Sha, new[] { "screen.alpha" }));
        Assert.Equal("CONFIG_BATCH_CURRENT_TENANT_AUTHORITY_REQUIRED", result.Code);
        Assert.Empty(catalog.Calls);
    }

    [Theory]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task Inactive_revoked_or_ungranted_actor_is_denied(
        bool membership, bool permission, bool revoked)
    {
        var catalog = new Catalog();
        var actors = new Actors { Evidence = Active(membership: membership,
            permission: permission, revoked: revoked) };
        var result = await new Worker(actors, catalog).ProjectAsync(Request("screen.alpha"));
        Assert.Equal("CONFIG_BATCH_CURRENT_TENANT_AUTHORITY_REQUIRED", result.Code);
        Assert.Empty(catalog.Calls);
    }

    [Theory]
    [InlineData("security.jwt")]
    [InlineData("screen..alpha")]
    [InlineData("screen/key")]
    [InlineData("auth.bypass")]
    [InlineData("other@tenant")]
    public async Task Reserved_or_injected_key_is_rejected_before_provider(string key)
    {
        var actors = new Actors();
        var catalog = new Catalog();
        var result = await new Worker(actors, catalog).ProjectAsync(Request(key));
        Assert.Equal("CONFIG_BATCH_KEY_INVALID_OR_DUPLICATE", result.Code);
        Assert.Equal(0, actors.Calls);
    }

    [Fact]
    public async Task Duplicate_keys_fail_without_partial_results()
    {
        var catalog = new Catalog();
        var result = await new Worker(new Actors(), catalog).ProjectAsync(
            Request("screen.alpha", "screen.alpha"));
        Assert.Equal("CONFIG_BATCH_KEY_INVALID_OR_DUPLICATE", result.Code);
        Assert.Empty(catalog.Calls);
    }

    [Fact]
    public async Task Sixteen_is_accepted_and_seventeen_is_rejected()
    {
        var keys = Enumerable.Range(1, Worker.MaximumKeys).Select(i => "screen." + i).ToArray();
        var catalog = new Catalog();
        foreach (var key in keys) catalog.Entries[key] = Entry(key);
        var worker = new Worker(new Actors(), catalog);
        Assert.Equal(16, (await worker.ProjectAsync(Request(keys))).Descriptors.Count);
        var denied = await worker.ProjectAsync(Request(keys.Append("screen.17").ToArray()));
        Assert.Equal("CONFIG_BATCH_REQUEST_INVALID", denied.Code);
        Assert.Empty(denied.Descriptors);
    }

    [Fact]
    public async Task Cross_tenant_or_secret_catalog_entry_fails_all_or_nothing()
    {
        var catalog = new Catalog();
        catalog.Entries["screen.alpha"] = Entry("screen.alpha");
        catalog.Entries["screen.beta"] = Entry("screen.beta", tenant: 8);
        var worker = new Worker(new Actors(), catalog);
        Assert.Equal("CONFIG_BATCH_TENANT_CATALOG_REJECTED",
            (await worker.ProjectAsync(Request("screen.alpha", "screen.beta"))).Code);
        catalog.Entries["screen.beta"] = Entry("screen.beta", secret: true);
        var denied = await worker.ProjectAsync(Request("screen.alpha", "screen.beta"));
        Assert.Equal("CONFIG_BATCH_TENANT_CATALOG_REJECTED", denied.Code);
        Assert.Empty(denied.Descriptors);
    }

    [Fact]
    public async Task Stale_source_sha_must_be_rejected()
    {
        var catalog = new Catalog();
        catalog.Entries["screen.alpha"] = Entry("screen.alpha", sha: new string('b', 40));
        var result = await new Worker(new Actors(), catalog).ProjectAsync(Request("screen.alpha"));
        Assert.Equal("CONFIG_BATCH_TENANT_CATALOG_REJECTED", result.Code);
    }

    [Fact]
    public async Task Provider_failure_is_sanitized()
    {
        var result = await new Worker(new Actors(),
            new Catalog { ThrowOnRead = true }).ProjectAsync(Request("screen.alpha"));
        Assert.Equal("CONFIG_BATCH_CATALOG_SOURCE_UNAVAILABLE", result.Code);
        Assert.Empty(result.Descriptors);
    }

    private sealed class ThrowingApprovedKeys : IReadOnlySet<string>
    {
        public int Count => 1;
        public bool Contains(string item) => string.Equals(item, "screen.alpha", StringComparison.Ordinal);
        public IEnumerator<string> GetEnumerator() => Enumerate().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static IEnumerable<string> Enumerate()
        {
            yield return "screen.not-approved";
            throw new InvalidOperationException("private provider diagnostic must not escape");
        }

        public bool IsProperSubsetOf(IEnumerable<string> other) => throw new NotSupportedException();
        public bool IsProperSupersetOf(IEnumerable<string> other) => throw new NotSupportedException();
        public bool IsSubsetOf(IEnumerable<string> other) => throw new NotSupportedException();
        public bool IsSupersetOf(IEnumerable<string> other) => throw new NotSupportedException();
        public bool Overlaps(IEnumerable<string> other) => throw new NotSupportedException();
        public bool SetEquals(IEnumerable<string> other) => throw new NotSupportedException();
    }

    [Fact]
    public async Task Approved_keys_enumerator_failure_is_sanitized_without_partial_metadata()
    {
        var catalog = new Catalog();
        catalog.Entries["screen.alpha"] = Entry("screen.alpha") with
        {
            ApprovedKeys = new ThrowingApprovedKeys()
        };
        var result = await new Worker(new Actors(), catalog).ProjectAsync(
            Request("screen.alpha"));
        Assert.Equal("CONFIG_BATCH_CATALOG_SOURCE_UNAVAILABLE", result.Code);
        Assert.Empty(result.Descriptors);
        Assert.Single(catalog.Calls);
        Assert.Equal(7, catalog.Calls[0].Tenant);
    }

    [Fact]
    public async Task Approved_keys_without_expected_key_are_denied()
    {
        var catalog = new Catalog();
        catalog.Entries["screen.alpha"] = Entry("screen.alpha") with
        {
            ApprovedKeys = new HashSet<string>(new[] { "screen.beta" }, StringComparer.Ordinal)
        };
        var result = await new Worker(new Actors(), catalog).ProjectAsync(
            Request("screen.alpha"));
        Assert.Equal("CONFIG_BATCH_TENANT_CATALOG_REJECTED", result.Code);
        Assert.Empty(result.Descriptors);
    }

    [Fact]
    public async Task Caller_cannot_mutate_result_or_its_source()
    {
        var catalog = new Catalog();
        catalog.Entries["screen.alpha"] = Entry("screen.alpha");
        var keys = new List<string> { "screen.alpha" };
        var result = await new Worker(new Actors(), catalog).ProjectAsync(
            new Worker.Request(7, Sha, keys));
        keys.Clear();
        Assert.Single(result.Descriptors);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<Worker.Descriptor>)result.Descriptors).Clear());
        Assert.Single(result.Descriptors);
    }

    [Fact]
    public async Task Cancellation_is_propagated_without_provider_calls()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var actors = new Actors();
        var catalog = new Catalog();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await new Worker(actors, catalog).ProjectAsync(
                Request("screen.alpha"), source.Token);
        });
        Assert.Equal(0, actors.Calls);
        Assert.Empty(catalog.Calls);
    }
}
