using Solqaryn.Infrastructure.AutomationV5;
using Rules = Solqaryn.Domain.AutomationV5.V5CommercialDocumentDomainRules;
using Adapter = Solqaryn.Infrastructure.AutomationV5.V5CommercialDocumentIsolatedReadAdapter;
using Xunit;

namespace Solqaryn.Tests.Infrastructure;

public sealed class V5CommercialDocumentIsolatedReadAdapterTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string OtherSha = "abcdef0123456789abcdef0123456789abcdef01";

    private sealed class Principals : IV5CommercialTrustedPrincipalSource
    {
        public Adapter.PrincipalSnapshot? Snapshot { get; set; } = ValidPrincipal();
        public int Calls { get; private set; }
        public bool Throws { get; set; }
        public ValueTask<Adapter.PrincipalSnapshot?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            if (Throws) throw new InvalidOperationException("secret internal backend detail");
            return ValueTask.FromResult(Snapshot);
        }
    }

    private sealed class Documents : IV5CommercialTenantDocumentSource
    {
        public Adapter.DocumentSnapshot? Snapshot { get; set; } = ValidDocument();
        public int Calls { get; private set; }
        public int? SeenTenant { get; private set; }
        public int? SeenId { get; private set; }
        public bool Throws { get; set; }
        public ValueTask<Adapter.DocumentSnapshot?> ReadAsync(
            int tenantId, int documentId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Calls++;
            SeenTenant = tenantId;
            SeenId = documentId;
            if (Throws) throw new InvalidOperationException("secret tenant data");
            return ValueTask.FromResult(Snapshot);
        }
    }

    private static Adapter.PrincipalSnapshot ValidPrincipal() =>
        new(7, 11, Sha, "ci:commercial:tenant-a", true, true, true, true,
            false, new HashSet<Rules.Operation> { Rules.Operation.ConfirmOrder },
            false, false);

    private static Adapter.DocumentSnapshot ValidDocument() =>
        new(42, 11, 11, 11, "BORRADOR", "HNL", 125.50m, 4, Sha, true, false);

    private static Rules.Command ValidCommand() =>
        new(Rules.Operation.ConfirmOrder, 4, 125.50m, "HNL",
            "tenant-a-confirm-4", new string('a', 64));

    [Fact]
    public async Task CurrentTrustedTenantAndPersistedOwnerOnlyPermitFurtherReview()
    {
        var principals = new Principals();
        var documents = new Documents();
        var result = await new Adapter(principals, documents)
            .InspectAsync(42, ValidCommand(), Sha);
        Assert.Equal("DURABLE_COMMERCIAL_ADAPTER_AND_A5_REQUIRED", result.Code);
        Assert.True(result.NeedsDurableAuthenticatedTransaction);
        Assert.Equal(11, documents.SeenTenant);
        Assert.Equal(42, documents.SeenId);
        Assert.False(result.GrantsMutation);
        Assert.False(result.GrantsPayment);
        Assert.False(result.GrantsFiscalIssuance);
        Assert.False(result.GrantsExport);
        Assert.False(result.GrantsCrossTenantRead);
    }

    [Fact]
    public async Task InvalidResourceOrShaNeverCallsBackendSources()
    {
        var p = new Principals();
        var d = new Documents();
        var adapter = new Adapter(p, d);
        Assert.Equal("VALID_RESOURCE_COMMAND_AND_SHA_REQUIRED",
            (await adapter.InspectAsync(0, ValidCommand(), Sha)).Code);
        Assert.Equal("VALID_RESOURCE_COMMAND_AND_SHA_REQUIRED",
            (await adapter.InspectAsync(42, null, Sha)).Code);
        Assert.Equal("VALID_RESOURCE_COMMAND_AND_SHA_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), "wrong")).Code);
        Assert.Equal(0, p.Calls);
        Assert.Equal(0, d.Calls);
    }

    [Theory]
    [InlineData(false, true, true, true, false)]
    [InlineData(true, false, true, true, false)]
    [InlineData(true, true, false, true, false)]
    [InlineData(true, true, true, false, false)]
    [InlineData(true, true, true, true, true)]
    public async Task RevokedOrUnverifiedPrincipalCannotReadDocument(
        bool auth, bool membershipRead, bool active, bool grantsRead, bool revoked)
    {
        var p = new Principals { Snapshot = ValidPrincipal() with
        {
            AuthenticatedByBackend = auth,
            MembershipReadFromBackend = membershipRead,
            MembershipActive = active,
            GrantsReadFromBackend = grantsRead,
            GrantsRevoked = revoked
        }};
        var d = new Documents();
        Assert.Equal("CURRENT_SERVER_IDENTITY_AND_GRANT_REQUIRED",
            (await new Adapter(p, d).InspectAsync(42, ValidCommand(), Sha)).Code);
        Assert.Equal(0, d.Calls);
    }

    [Fact]
    public async Task MissingGrantWrongShaOrNullBackendPrincipalNeverReadsDocument()
    {
        var p = new Principals();
        var d = new Documents();
        var adapter = new Adapter(p, d);
        p.Snapshot = null;
        Assert.Equal("CURRENT_SERVER_IDENTITY_AND_GRANT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
        p.Snapshot = ValidPrincipal() with
        {
            CurrentGrantedOperations = new HashSet<Rules.Operation>()
        };
        Assert.Equal("CURRENT_SERVER_IDENTITY_AND_GRANT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
        p.Snapshot = ValidPrincipal() with { SourceSha = OtherSha };
        Assert.Equal("CURRENT_SERVER_IDENTITY_AND_GRANT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
        Assert.Equal(0, d.Calls);
    }

    [Fact]
    public async Task ForeignTenantAndNullLegacyOwnershipAreAlwaysDenied()
    {
        var d = new Documents();
        var adapter = new Adapter(new Principals(), d);
        d.Snapshot = ValidDocument() with { DocumentTenantId = 22 };
        Assert.Equal("PERSISTED_TENANT_DOCUMENT_SNAPSHOT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
        d.Snapshot = ValidDocument() with { CustomerTenantId = null };
        Assert.Equal("PERSISTED_TENANT_DOCUMENT_SNAPSHOT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
        d.Snapshot = ValidDocument() with { ParentTenantId = 22 };
        Assert.Equal("PERSISTED_TENANT_DOCUMENT_SNAPSHOT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
        Assert.Equal(11, d.SeenTenant);
    }

    [Fact]
    public async Task WrongDocumentVersionShaAndUnverifiedOwnerAreDenied()
    {
        var d = new Documents();
        var adapter = new Adapter(new Principals(), d);
        d.Snapshot = ValidDocument() with { DocumentId = 99 };
        Assert.Equal("PERSISTED_TENANT_DOCUMENT_SNAPSHOT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
        d.Snapshot = ValidDocument() with { Version = 0 };
        Assert.Equal("PERSISTED_TENANT_DOCUMENT_SNAPSHOT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
        d.Snapshot = ValidDocument() with { SourceSha = OtherSha };
        Assert.Equal("PERSISTED_TENANT_DOCUMENT_SNAPSHOT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
        d.Snapshot = ValidDocument() with { OwnershipReadFromPersistence = false };
        Assert.Equal("PERSISTED_TENANT_DOCUMENT_SNAPSHOT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
        d.Snapshot = ValidDocument() with { UnattributedLegacy = true };
        Assert.Equal("PERSISTED_TENANT_DOCUMENT_SNAPSHOT_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
    }

    [Fact]
    public async Task StaleVersionMoneyOrReferenceCannotReachDurableExecution()
    {
        var p = new Principals();
        var d = new Documents();
        var adapter = new Adapter(p, d);
        Assert.Equal("STALE_DOCUMENT_VERSION",
            (await adapter.InspectAsync(42,
                ValidCommand() with { ExpectedVersion = 3 }, Sha)).Code);
        Assert.Equal("INVALID_DOCUMENT_AMOUNT",
            (await adapter.InspectAsync(42,
                ValidCommand() with { Amount = 125.51m }, Sha)).Code);
        p.Snapshot = ValidPrincipal() with { EvidenceReference = "token@tenant-b" };
        Assert.Equal("COMMERCIAL_EVIDENCE_REFERENCE_REQUIRED",
            (await adapter.InspectAsync(42, ValidCommand(), Sha)).Code);
    }

    [Fact]
    public async Task SourceFailuresAreOpaqueAndNeverGrantPermission()
    {
        var p = new Principals { Throws = true };
        var d = new Documents();
        Assert.Equal("TRUSTED_PRINCIPAL_SOURCE_UNAVAILABLE",
            (await new Adapter(p, d).InspectAsync(42, ValidCommand(), Sha)).Code);
        Assert.Equal(0, d.Calls);
        p.Throws = false;
        d.Throws = true;
        Assert.Equal("TENANT_DOCUMENT_SOURCE_UNAVAILABLE",
            (await new Adapter(p, d).InspectAsync(42, ValidCommand(), Sha)).Code);
    }

    [Fact]
    public async Task CancellationIsPropagatedAndDoesNotTouchSources()
    {
        var p = new Principals();
        var d = new Documents();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await new Adapter(p, d).InspectAsync(42, ValidCommand(), Sha, cts.Token));
        Assert.Equal(0, p.Calls);
        Assert.Equal(0, d.Calls);
    }

    [Fact]
    public void MissingSourceIsRejectedBeforeUse()
    {
        Assert.Throws<ArgumentNullException>(() => new Adapter(null!, new Documents()));
        Assert.Throws<ArgumentNullException>(() => new Adapter(new Principals(), null!));
    }
}
