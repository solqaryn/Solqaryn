using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class C0301M0302LoginInteligenteApplicationServiceTests
{
    private const string Password = "Strong-Secret-42!";
    private static Usuario User() => new()
    {
        Id = 11, NombreUsuario = "demo", Activo = true,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password, workFactor: 4)
    };

    private static (C0301LoginInteligenteApplicationService Service,
        Mock<IUsuarioRepository> Users,
        Mock<IC0301ServerTenantPreferenceReader> Preference) Create(
        int[] tenants, int? preferred = null, Usuario? user = null)
    {
        user ??= User();
        var users = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        users.Setup(x => x.GetByNombreUsuarioAsync("demo")).ReturnsAsync(user);
        users.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        users.Setup(x => x.GetEmpresasAsync(user.Id)).ReturnsAsync(
            tenants.Select(id => new UsuarioEmpresa(user.Id, id, id + 100)).ToList());
        var companies = new Mock<IEmpresaRepository>(MockBehavior.Strict);
        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        foreach (var id in tenants.Distinct())
        {
            companies.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Empresa("Tenant") { Id = id });
            roles.Setup(x => x.GetByIdAsync(id + 100))
                .ReturnsAsync(new Rol { Id = id + 100, EmpresaId = id,
                    Activo = true, Ambito = AmbitoAutorizacion.Empresa });
        }
        var pref = new Mock<IC0301ServerTenantPreferenceReader>(MockBehavior.Strict);
        pref.Setup(x => x.ReadLastTenantIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(preferred);
        return (new(users.Object, companies.Object, roles.Object, pref.Object),
            users, pref);
    }

    [Fact]
    public async Task OneTenantRequiresServerEnforcementAndDoesNotIssueToken()
    {
        var t = Create([10]);
        var actual = await t.Service.PrepareAsync(" demo ", Password);
        Assert.Equal("SINGLE_TENANT_REQUIRES_SERVER_ENFORCEMENT", actual.Code);
        Assert.Equal(10, actual.CandidateTenantId);
        Assert.False(actual.IssuesJwt);
        Assert.False(actual.GrantsTenantAccess);
        t.Users.Verify(x => x.GetByNombreUsuarioAsync("demo"), Times.Once);
        t.Users.Verify(x => x.GetEmpresasAsync(11), Times.Once);
    }

    [Fact]
    public async Task ZeroMembershipsRejectWithoutPlatformFallback()
    {
        var t = Create([]);
        var actual = await t.Service.PrepareAsync("demo", Password);
        Assert.Equal("NO_VALID_ENTERPRISE_MEMBERSHIP", actual.Code);
        Assert.Null(actual.CandidateTenantId);
        Assert.Empty(actual.ValidTenantIds);
    }

    [Theory]
    [InlineData(20, "VALID_PREFERENCE_REQUIRES_SERVER_ENFORCEMENT", 20)]
    [InlineData(99, "EXPLICIT_TENANT_SELECTION_REQUIRED", null)]
    [InlineData(null, "EXPLICIT_TENANT_SELECTION_REQUIRED", null)]
    public async Task MultipleTenantsOnlyUseCurrentPersistedPreference(
        int? preference, string expected, int? candidate)
    {
        var t = Create([10, 20], preference);
        var actual = await t.Service.PrepareAsync("demo", Password);
        Assert.Equal(expected, actual.Code);
        Assert.Equal(candidate, actual.CandidateTenantId);
        Assert.Equal(2, actual.ValidTenantIds.Count);
        t.Preference.Verify(x => x.ReadLastTenantIdAsync(11,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WrongPasswordDoesNotReadMembershipsOrPreferences()
    {
        var t = Create([10]);
        Assert.Equal("INVALID_CREDENTIALS",
            (await t.Service.PrepareAsync("demo", "wrong")).Code);
        t.Users.Verify(x => x.GetEmpresasAsync(It.IsAny<int>()), Times.Never);
        t.Preference.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BlockedIdentityRejectedBeforePasswordAndMembershipLookup()
    {
        var user = User(); user.Bloqueado = true;
        var t = Create([10], user: user);
        Assert.Equal("INVALID_CREDENTIALS",
            (await t.Service.PrepareAsync("demo", Password)).Code);
        t.Users.Verify(x => x.GetEmpresasAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CrossTenantRoleCannotBecomeValidMember()
    {
        var t = Create([10]);
        // The Application service uses role.EsAsignableAEmpresa: a role owned by
        // another tenant can never add an authorized tenant option.
        Assert.False(new Rol { Id = 110, EmpresaId = 20,
            Ambito = AmbitoAutorizacion.Empresa }.EsAsignableAEmpresa(10));
        Assert.False((await t.Service.PrepareAsync("demo", Password)).GrantsTenantAccess);
    }

    [Fact]
    public async Task MembershipRepositoryFailureIsSafeAndDoesNotEmitToken()
    {
        var t = Create([10]);
        t.Users.Setup(x => x.GetEmpresasAsync(11))
            .ThrowsAsync(new InvalidOperationException("DB confidential error"));
        var actual = await t.Service.PrepareAsync("demo", Password);
        Assert.Equal("AUTHENTICATION_OR_MEMBERSHIP_SOURCE_UNAVAILABLE", actual.Code);
        Assert.DoesNotContain("confidential", actual.Code, StringComparison.OrdinalIgnoreCase);
        Assert.False(actual.IssuesJwt);
    }
}
