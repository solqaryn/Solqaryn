using Solqaryn.Application.AutomationV5;
using Xunit;
using Gate = Solqaryn.Application.AutomationV5.C0404PaymentAuthorizationGate;
namespace Solqaryn.Tests.Application;
public sealed class C0404M0303PaymentAuthorizationGateTests
{
    private sealed class Reader : IC0404PaymentAuthorizationEvidenceReader
    {
        public Gate.TrustedSnapshot? Snapshot { get; set; }
        public Exception? Error { get; set; }
        public int Calls { get; private set; }
        public Action? AfterRead { get; set; }
        public ValueTask<Gate.TrustedSnapshot?> ReadCurrentAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested(); Calls++;
            if (Error is not null) throw Error;
            var snapshot = Snapshot;
            AfterRead?.Invoke();
            return ValueTask.FromResult(snapshot);
        }
    }
    private static Gate.Request Create() => new(100, 92, Gate.Operation.CreateLink);
    private static Gate.TrustedSnapshot Trusted() => new()
    {
        ActorId = 71, TenantId = 100, InvoiceId = 92,
        JwtSignatureIssuerAudienceLifetimeVerifiedByBackend = true,
        CurrentSessionVerified = true, UserActive = true,
        AuthorityDatabaseAvailable = true, MembershipFromPersistence = true,
        MembershipActive = true, RoleAndGrantFromPersistence = true,
        TenantReadFencedByDatabase = true, InvoiceOwnershipVerifiedFromPersistence = true,
        AuthorizationRevisionFresh = true, FeatureEnabledByTrustedConfig = true,
        CreateGrant = true, RefundGrant = true, ReceiptAndLedgerVersionVerified = true
    };
    private static void NoEffects(Gate.Decision d)
    {
        Assert.False(d.AuthorizesFinancialMutation);
        Assert.False(d.AuthorizesProviderCall);
        Assert.False(d.AuthorizesLedgerWrite);
        Assert.False(d.InstallsTenantContext);
        Assert.False(d.CertifiesJwtOrDatabaseIntegration);
    }
    [Fact] public async Task VerifiedSnapshotAllowsOnlyPurePlanNoCharge()
    {
        var r = new Reader { Snapshot = Trusted() };
        var d = await new Gate(r).InspectAsync(Create());
        Assert.True(d.MayInspectPureApplicationPlan); NoEffects(d); Assert.Equal(1, r.Calls);
    }
    [Theory]
    [InlineData(0,92)][InlineData(-1,92)][InlineData(int.MinValue,92)][InlineData(100,0)][InlineData(100,-1)][InlineData(100,int.MinValue)]
    public async Task InvalidTenantOrInvoiceDeniedBeforeReader(int tenant,int invoice)
    {
        var r = new Reader { Snapshot = Trusted() };
        var d = await new Gate(r).InspectAsync(Create() with { TenantId = tenant, InvoiceId = invoice });
        Assert.False(d.MayInspectPureApplicationPlan); NoEffects(d); Assert.Equal(0,r.Calls);
    }
    [Fact] public async Task NullUnknownActionDeniedBeforeReader()
    {
        var r = new Reader { Snapshot = Trusted() }; var gate = new Gate(r);
        Assert.Equal("PAYMENT_AUTHORIZATION_DENIED",(await gate.InspectAsync(null)).Code);
        Assert.Equal("PAYMENT_AUTHORIZATION_DENIED",(await gate.InspectAsync(Create() with { Action = (Gate.Operation)999 })).Code);
        Assert.Equal(0,r.Calls);
    }
    [Fact] public async Task ClientCannotInjectRoleJwtMembershipHmacChargeOrLedgerBypass()
    {
        var r = new Reader { Snapshot = Trusted() }; var gate = new Gate(r);
        foreach(var req in new[]
        {
            Create() with { ClientClaimsRole = true }, Create() with { ClientSuppliesJwt = true },
            Create() with { ClientClaimsMembership = true }, Create() with { ClientSuppliesProviderSignature = true },
            Create() with { RequestsImmediateCharge = true }, Create() with { RequestsBypassOfLedger = true }
        })
        {
            var d = await gate.InspectAsync(req);
            Assert.Equal("PAYMENT_AUTHORIZATION_DENIED", d.Code); NoEffects(d);
        }
        Assert.Equal(0,r.Calls);
    }
    [Fact] public async Task CrossTenantAndForeignInvoiceDenied()
    {
        var r = new Reader { Snapshot = Trusted() }; var gate = new Gate(r);
        Assert.Equal("PAYMENT_AUTHORIZATION_DENIED",(await gate.InspectAsync(Create() with { TenantId = 200 })).Code);
        Assert.Equal("PAYMENT_AUTHORIZATION_DENIED",(await gate.InspectAsync(Create() with { InvoiceId = 999 })).Code);
    }
    [Fact] public async Task FakeJwtRevocationMissingDatabaseRoleGrantAndStaleContextFailClosed()
    {
        var r = new Reader { Snapshot = Trusted() }; var gate = new Gate(r);
        foreach(var s in new[]
        {
            Trusted() with { JwtSignatureIssuerAudienceLifetimeVerifiedByBackend = false },
            Trusted() with { CurrentSessionVerified = false }, Trusted() with { UserActive = false },
            Trusted() with { AuthorityDatabaseAvailable = false },
            Trusted() with { MembershipFromPersistence = false },
            Trusted() with { MembershipActive = false },
            Trusted() with { RoleAndGrantFromPersistence = false },
            Trusted() with { PermissionRevoked = true },
            Trusted() with { TenantReadFencedByDatabase = false },
            Trusted() with { InvoiceOwnershipVerifiedFromPersistence = false },
            Trusted() with { AuthorizationRevisionFresh = false },
            Trusted() with { FeatureEnabledByTrustedConfig = false },
            Trusted() with { CreateGrant = false }, Trusted() with { ActorId = 0 }
        })
        {
            r.Snapshot = s;
            var d = await gate.InspectAsync(Create());
            Assert.Equal("PAYMENT_AUTHORIZATION_DENIED",d.Code); NoEffects(d);
        }
    }
    [Fact] public async Task RefundNeedsDistinctGrantAndReceiptVersionNeverWrites()
    {
        var r = new Reader { Snapshot = Trusted() }; var gate = new Gate(r);
        var req = Create() with { Action = Gate.Operation.Refund };
        var valid = await gate.InspectAsync(req); Assert.True(valid.MayInspectPureApplicationPlan); NoEffects(valid);
        r.Snapshot = Trusted() with { RefundGrant = false };
        Assert.Equal("PAYMENT_AUTHORIZATION_DENIED",(await gate.InspectAsync(req)).Code);
        r.Snapshot = Trusted() with { ReceiptAndLedgerVersionVerified = false };
        Assert.Equal("PAYMENT_AUTHORIZATION_DENIED",(await gate.InspectAsync(req)).Code);
    }
    [Fact] public async Task UnavailableReaderAndExceptionNoSecretLeak()
    {
        var r = new Reader(); var gate = new Gate(r);
        Assert.Equal("PAYMENT_AUTHORIZATION_DENIED",(await gate.InspectAsync(Create())).Code);
        r.Error = new InvalidOperationException("mysql://secret bearer TOP_SECRET");
        var d = await gate.InspectAsync(Create());
        Assert.Equal("PAYMENT_AUTHORIZATION_DENIED",d.Code);
        Assert.DoesNotContain("TOP_SECRET",d.Code); NoEffects(d);
    }
    [Fact] public async Task CancelledCallCannotBeConvertedToAuthorization()
    {
        var r = new Reader { Snapshot = Trusted() };
        using var cts = new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await new Gate(r).InspectAsync(Create(),cts.Token));
        Assert.Equal(0,r.Calls);
    }
    [Fact] public async Task ReaderIgnoringMidflightCancellationCannotReturnTrustedPlan()
    {
        using var cts = new CancellationTokenSource();
        var reader = new Reader { Snapshot = Trusted(), AfterRead = () => cts.Cancel() };
        // Reader ignores the token and returns a formerly positive snapshot.
        // The gate itself MUST reject after the awaited read.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await new Gate(reader).InspectAsync(Create(), cts.Token));
        Assert.Equal(1, reader.Calls);
    }

}
