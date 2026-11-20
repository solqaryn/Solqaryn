using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Solqaryn.Application.AutomationV5;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C1103OutboxEventDomainRules;
using Gate = Solqaryn.Application.AutomationV5.C1103OutboxAuthorizationGate;

namespace Solqaryn.Tests.Application;

public sealed class C1103M0303OutboxAuthorizationGateTests
{
    private const string Issuer = "solqaryn-outbox-ephemeral-test";
    private const string Audience = "solqaryn-outbox-ephemeral-test";
    private static readonly SymmetricSecurityKey Key = new(
        Encoding.UTF8.GetBytes("c1103m0303-only-ephemeral-test-signing-key-2026"));
    private static readonly DateTime Now = new(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);

    private static Rules.EventDraft Draft(int tenant = 101) =>
        new(tenant, Guid.Parse("22222222-2222-4222-8222-222222222222"),
            "opaque-unique-key-123456789", "ventas.factura.emitida",
            new string('a', 64), Now.AddMinutes(-1), "correlation", 1, 0, 3);

    private static Gate.CurrentAuthority Valid(int tenant = 101) =>
        new(42, tenant, true, true, true, false, true, Now,
            new HashSet<string>(StringComparer.Ordinal) { "ventas.factura.emitida" });

    private sealed class Reader : IC1103OutboxCurrentMembershipReader
    {
        public Gate.CurrentAuthority? Authority { get; set; } = Valid();
        public Exception? Error { get; set; }
        public int Calls { get; private set; }
        public int RequestedTenant { get; private set; }
        public int User { get; private set; }

        public ValueTask<Gate.CurrentAuthority?> ReadCurrentAsync(
            int authenticatedUserId, int requestedTenantId, CancellationToken cancellationToken)
        {
            Calls++;
            RequestedTenant = requestedTenantId;
            User = authenticatedUserId;
            cancellationToken.ThrowIfCancellationRequested();
            if (Error is not null) throw Error;
            return ValueTask.FromResult(Authority);
        }
    }

    private static string Token(
        string? userId = "42", SecurityKey? key = null,
        DateTime? expires = null, bool duplicate = false)
    {
        var claims = new List<Claim>();
        if (userId is not null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        if (duplicate) claims.Add(new Claim(ClaimTypes.NameIdentifier, "43"));
        var jwt = new JwtSecurityToken(Issuer, Audience, claims,
            notBefore: DateTime.UtcNow.AddMinutes(-2),
            expires: expires ?? DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key ?? Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static ClaimsPrincipal Validate(string token, SecurityKey? key = null)
    {
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        return handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key ?? Key,
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.Zero
        }, out _);
    }

    [Fact]
    public async Task SignedJwtAndLiveMembershipOnlyGrantNonWritingPreflight()
    {
        var reader = new Reader();
        var result = await new Gate(reader).InspectAsync(Validate(Token()), Draft());
        Assert.Equal("OUTBOX_AUTH_PREFLIGHT_ONLY", result.Code);
        Assert.True(result.PermitsDomainPreflightOnly);
        Assert.False(result.AuthorizesPersistence);
        Assert.False(result.AuthorizesDispatch);
        Assert.False(result.CertifiesSignedJwtHttpOrMysql);
        Assert.Equal(42, reader.User);
        Assert.Equal(101, reader.RequestedTenant);
    }

    [Fact]
    public void ForgedJwtSignatureRejectedBeforeGate()
    {
        var forged = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "c1103m0303-forged-ephemeral-test-signing-key-2026"));
        Assert.ThrowsAny<SecurityTokenException>(() => Validate(Token(key: forged)));
    }

    [Fact]
    public void ExpiredJwtRejectedBeforeGate()
    {
        Assert.ThrowsAny<SecurityTokenException>(() =>
            Validate(Token(expires: DateTime.UtcNow.AddMinutes(-1))));
    }

    [Fact]
    public async Task AnonymousOrMissingPrincipalDeniedBeforeReader()
    {
        var reader = new Reader();
        var gate = new Gate(reader);
        Assert.Equal("OUTBOX_AUTH_DENIED", (await gate.InspectAsync(null, Draft())).Code);
        Assert.Equal("OUTBOX_AUTH_DENIED", (await gate.InspectAsync(
            new ClaimsPrincipal(new ClaimsIdentity()), Draft())).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-an-int")]
    public async Task MissingOrInvalidJwtUserDeniedWithoutReader(string? user)
    {
        var reader = new Reader();
        Assert.Equal("OUTBOX_AUTH_DENIED",
            (await new Gate(reader).InspectAsync(Validate(Token(user)), Draft())).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task DuplicateJwtUserClaimsDenied()
    {
        var reader = new Reader();
        Assert.Equal("OUTBOX_AUTH_DENIED",
            (await new Gate(reader).InspectAsync(Validate(Token(duplicate: true)), Draft())).Code);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task ForeignTenantAtoBRejectedWithFreshMembership()
    {
        var reader = new Reader();
        var verdict = await new Gate(reader).InspectAsync(Validate(Token()), Draft(202));
        Assert.Equal("OUTBOX_AUTH_DENIED", verdict.Code);
        Assert.Equal(202, reader.RequestedTenant);
        Assert.False(verdict.AuthorizesDispatch);
    }

    [Theory]
    [InlineData("trust")]
    [InlineData("member")]
    [InlineData("grant")]
    [InlineData("revoked")]
    [InlineData("ownership")]
    [InlineData("user")]
    [InlineData("time")]
    public async Task UntrustedRevokedOrForeignServerSnapshotDenied(string mutation)
    {
        var reader = new Reader();
        reader.Authority = mutation switch
        {
            "trust" => Valid() with { ServerTrusted = false },
            "member" => Valid() with { MembershipActive = false },
            "grant" => Valid() with { RoleGrantActive = false },
            "revoked" => Valid() with { PermissionRevoked = true },
            "ownership" => Valid() with { TenantOwnedByUser = false },
            "user" => Valid() with { UserId = 999 },
            _ => Valid() with { VerifiedNowUtc = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified) }
        };
        var verdict = await new Gate(reader).InspectAsync(Validate(Token()), Draft());
        Assert.Equal("OUTBOX_AUTH_DENIED", verdict.Code);
        Assert.False(verdict.AuthorizesPersistence);
    }

    [Fact]
    public async Task EventTypeCannotBeElevatedByClient()
    {
        var verdict = await new Gate(new Reader()).InspectAsync(
            Validate(Token()), Draft() with { EventType = "admin.secrets.export" });
        Assert.Equal("EVENT_ID_OR_TYPE_DENIED", verdict.Code);
    }

    [Fact]
    public async Task ReplayedIntentNeverDispatchesAgain()
    {
        var draft = Draft();
        var existing = new Rules.ExistingIntent(101, draft.EventoId, draft.IdempotencyKey!,
            draft.EventType!, draft.PayloadSha256!, 1, false, false);
        var verdict = await new Gate(new Reader { Authority = Valid() with { Existing = existing } })
            .InspectAsync(Validate(Token()), draft);
        Assert.Equal("EXACT_REPLAY_NO_NEW_DISPATCH", verdict.Code);
        Assert.False(verdict.AuthorizesDispatch);
    }

    [Fact]
    public async Task ReaderExceptionIsOpaque()
    {
        var reader = new Reader { Error = new InvalidOperationException("mysql://secret-test") };
        var verdict = await new Gate(reader).InspectAsync(Validate(Token()), Draft());
        Assert.Equal("OUTBOX_AUTH_BACKEND_UNAVAILABLE", verdict.Code);
        Assert.DoesNotContain("secret", verdict.Code);
        Assert.False(verdict.AuthorizesPersistence);
    }

    [Fact]
    public async Task CancellationPropagatesWithoutReader()
    {
        var reader = new Reader();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await new Gate(reader).InspectAsync(Validate(Token()), Draft(), cts.Token));
        Assert.Equal(0, reader.Calls);
    }
}
