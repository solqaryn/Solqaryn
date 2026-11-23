using Moq;
using Microsoft.Extensions.Configuration;
using Solqaryn.Infrastructure.Services;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// Enterprise-owned roles must retain a live matching membership before
/// product login and refresh can issue tokens. Repository-mocked service tests:
/// not signed JWT, HTTP, MySQL or browser integration certification.
/// </summary>
public sealed class C0301M0303TenantOwnedRoleTokenGateTests
{
    private const int UserId = 704;
    private const int CompanyId = 101;
    private const int RoleId = 17;

    private static Usuario Actor(bool owned = true) => new()
    {
        Id = UserId,
        NombreUsuario = "tenant-operator",
        NombreCompleto = "Tenant operator",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("valid-test-password"),
        RolId = RoleId,
        RolEntidad = new Rol
        {
            Id = RoleId,
            Nombre = "Operador",
            Activo = true,
            Ambito = AmbitoAutorizacion.Empresa,
            EmpresaId = owned ? CompanyId : null,
            EsSistema = !owned
        }
    };

    private static (AuthService service, Mock<IUsuarioRepository> repo,
        Mock<IJwtService> jwt) Make(Usuario actor, UsuarioEmpresa? membership)
    {
        var repo = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        repo.Setup(x => x.GetByIdAsync(actor.Id)).ReturnsAsync(actor);
        repo.Setup(x => x.GetByNombreUsuarioAsync(actor.NombreUsuario))
            .ReturnsAsync(actor);
        repo.Setup(x => x.GetEmpresaAsync(actor.Id, CompanyId))
            .ReturnsAsync(membership);
        repo.Setup(x => x.GetEmpresasAsync(actor.Id))
            .ReturnsAsync(membership is null
                ? new List<UsuarioEmpresa>()
                : new List<UsuarioEmpresa> { membership });
        var jwt = new Mock<IJwtService>(MockBehavior.Strict);
        jwt.Setup(x => x.GenerarToken(actor))
            .Returns(("unsigned-test-token", DateTime.UtcNow.AddMinutes(35)));
        return (new AuthService(repo.Object, Mock.Of<IRolRepository>(),
            jwt.Object), repo, jwt);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("revoked")]
    [InlineData("foreign-user")]
    [InlineData("foreign-company")]
    [InlineData("wrong-role")]
    public async Task RenewalRejectsInvalidTenantMembershipWithoutJwt(string scenario)
    {
        var actor = Actor();
        UsuarioEmpresa? member = scenario switch
        {
            "missing" => null,
            "revoked" => new UsuarioEmpresa(UserId, CompanyId, RoleId),
            "foreign-user" => new UsuarioEmpresa(UserId + 1, CompanyId, RoleId),
            "foreign-company" => new UsuarioEmpresa(UserId, CompanyId + 1, RoleId),
            "wrong-role" => new UsuarioEmpresa(UserId, CompanyId, RoleId + 1),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        if (scenario == "revoked") member!.Desactivar();
        var (service, repo, jwt) = Make(actor, member);

        Assert.Null(await service.RenovarAsync(UserId));
        repo.Verify(x => x.GetEmpresaAsync(UserId, CompanyId), Times.Once);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task LoginRejectsRevokedMembershipEvenWithRealBcryptPassword()
    {
        var actor = Actor();
        var member = new UsuarioEmpresa(UserId, CompanyId, RoleId);
        member.Desactivar();
        var (service, repo, jwt) = Make(actor, member);

        var result = await service.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = actor.NombreUsuario,
            Password = "valid-test-password"
        });

        Assert.Null(result);
        repo.Verify(x => x.GetEmpresaAsync(UserId, CompanyId), Times.Once);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task LiveMatchingMembershipPreservesLoginAndRenewal()
    {
        var actor = Actor();
        var (service, repo, jwt) = Make(actor,
            new UsuarioEmpresa(UserId, CompanyId, RoleId));

        var login = await service.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = actor.NombreUsuario,
            Password = "valid-test-password"
        });
        var renew = await service.RenovarAsync(UserId);

        Assert.NotNull(login);
        Assert.NotNull(renew);
        Assert.Equal("unsigned-test-token", login.Token);
        Assert.Equal("unsigned-test-token", renew.Token);
        repo.Verify(x => x.GetEmpresaAsync(UserId, CompanyId), Times.Exactly(2));
        jwt.Verify(x => x.GenerarToken(actor), Times.Exactly(2));
    }

    [Fact]
    public async Task SystemEnterpriseTemplateRequiresCurrentMembershipBeforeToken()
    {
        var actor = Actor(owned: false);
        var (service, repo, jwt) = Make(actor, null);

        Assert.Null(await service.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = actor.NombreUsuario,
            Password = "valid-test-password"
        }));
        Assert.Null(await service.RenovarAsync(UserId));
        repo.Verify(x => x.GetEmpresasAsync(UserId), Times.Exactly(2));
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Theory]
    [InlineData("revoked")]
    [InlineData("foreign-user")]
    [InlineData("wrong-role")]
    public async Task SystemEnterpriseTemplateRejectsNonmatchingMembership(string scenario)
    {
        var actor = Actor(owned: false);
        var member = scenario switch
        {
            "foreign-user" => new UsuarioEmpresa(UserId + 1, CompanyId, RoleId),
            "wrong-role" => new UsuarioEmpresa(UserId, CompanyId, RoleId + 1),
            _ => new UsuarioEmpresa(UserId, CompanyId, RoleId)
        };
        if (scenario == "revoked") member.Desactivar();
        var (service, repo, jwt) = Make(actor, member);

        Assert.Null(await service.RenovarAsync(UserId));
        repo.Verify(x => x.GetEmpresasAsync(UserId), Times.Once);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task MatchingSystemEnterpriseTemplateMembershipAllowsLoginAndRenewal()
    {
        var actor = Actor(owned: false);
        var (service, repo, jwt) = Make(actor,
            new UsuarioEmpresa(UserId, CompanyId, RoleId));

        Assert.NotNull(await service.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = actor.NombreUsuario,
            Password = "valid-test-password"
        }));
        Assert.NotNull(await service.RenovarAsync(UserId));
        repo.Verify(x => x.GetEmpresasAsync(UserId), Times.Exactly(2));
        jwt.Verify(x => x.GenerarToken(actor), Times.Exactly(2));
    }

    [Fact]
    public async Task InconsistentRoleEntityCannotMintTenantOwnedToken()
    {
        var actor = Actor();
        actor.RolId = RoleId + 1;
        var (service, repo, jwt) = Make(actor,
            new UsuarioEmpresa(UserId, CompanyId, RoleId));

        Assert.Null(await service.RenovarAsync(UserId));
        repo.Verify(x => x.GetEmpresaAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task EnterpriseSystemTemplateWithMismatchedSqlRoleRejectsProductLoginAndRefresh()
    {
        var actor = Actor(owned: false);
        actor.RolId = RoleId + 1;
        var (service, repo, jwt) = Make(actor, null);

        Assert.Null(await service.LoginAsync(new LoginRequestDto
        {
            NombreUsuario = actor.NombreUsuario,
            Password = "valid-test-password"
        }));
        Assert.Null(await service.RenovarAsync(UserId));

        repo.Verify(x => x.GetEmpresaAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
        jwt.Verify(x => x.GenerarToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public void DirectJwtSignerRejectsMismatchedSqlRoleBeforeProducingSignedToken()
    {
        var actor = Actor(owned: false);
        actor.RolId = RoleId + 1;
        var settings = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "Only-Test-Key-Longer-Than-32-Characters-2026",
                ["Jwt:Issuer"] = "Solqaryn.Unit.Tests",
                ["Jwt:Audience"] = "Solqaryn.Unit.Tests"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            new JwtService(settings).GenerarToken(actor));
    }
}
