using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.API.Security;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Xunit;
using Rules = Solqaryn.Domain.Security.C0303SecurityAuditDomainRules;

namespace Solqaryn.Tests;

/// <summary>
/// Physical MySQL/Oracle EF proof for C03-03.M03.03 audit-read preflight.
/// JWT bearer verification is real but HTTP transport is a test context, not
/// a hosted HTTP endpoint. No durable event sink or production grant is proven.
/// </summary>
[Trait("Category", "Integration")]
public sealed class C0303M0303VerifiedAuditMysql84IntegrationTests
{
    private static readonly SymmetricSecurityKey Signing = new(
        Encoding.UTF8.GetBytes("solqaryn-c0303-m0303-audit-mysql84-test-only-2026!"));

    [Fact]
    public async Task PhysicalMysql84SignedJwtTenantAbGrantRevocationAndRollbackFailClosed()
    {
        var raw = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("EPHEMERAL_MYSQL84_REQUIRED");
        var source = new MySqlConnectionStringBuilder(raw);
        if (source.Server is not ("127.0.0.1" or "localhost") ||
            source.Port != 3306 || source.UserID != "root" ||
            !source.Database.StartsWith("solqaryn_", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only CI disposable MySQL 8.4 localhost is allowed");

        var connection = new MySqlConnectionStringBuilder(raw)
        {
            Database = "v25_c0303_m0303_" + Guid.NewGuid().ToString("N")
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(connection, o => o.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;

        try
        {
            await using var db = new AppDbContext(options);
            await Phase7MySqlTestDatabase.InitializeFreshAsync(db);
            await db.Database.OpenConnectionAsync();
            try
            {
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT VERSION()";
                var actualVersion = Convert.ToString(await command.ExecuteScalarAsync());
                Assert.StartsWith("8.4.", actualVersion, StringComparison.Ordinal);
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }

            var tenantA = new Empresa("Audit CI tenant A");
            var tenantB = new Empresa("Audit CI tenant B");
            db.Set<Empresa>().AddRange(tenantA, tenantB);
            await db.SaveChangesAsync();
            Assert.NotEqual(tenantA.Id, tenantB.Id);

            var roleA = new Rol
            {
                Nombre = "Audit CI Administrator A",
                NombreNormalizado = "AUDIT_CI_ADMIN_A",
                EmpresaId = tenantA.Id,
                Ambito = AmbitoAutorizacion.Empresa,
                EsAdministrador = true,
                Activo = true
            };
            var roleB = new Rol
            {
                Nombre = "Audit CI Administrator B",
                NombreNormalizado = "AUDIT_CI_ADMIN_B",
                EmpresaId = tenantB.Id,
                Ambito = AmbitoAutorizacion.Empresa,
                EsAdministrador = true,
                Activo = true
            };
            db.Roles.AddRange(roleA, roleB);
            await db.SaveChangesAsync();

            var username = "auditci_" + Guid.NewGuid().ToString("N")[..12];
            var actor = new Usuario
            {
                NombreUsuario = username,
                NombreCompleto = "Synthetic audit actor",
                PasswordHash = "unusable-ci-only",
                RolId = roleA.Id,
                Activo = true
            };
            var permission = new Permiso
            {
                Codigo = "C0303_MYSQL_AUDITORIA_VER",
                Nombre = "C0303 disposable audit read",
                Modulo = ModuloSistema.Auditoria,
                Accion = AccionPermiso.Ver,
                Ambito = AmbitoAutorizacion.Empresa,
                Activo = true
            };
            db.Usuarios.Add(actor);
            db.Permisos.Add(permission);
            await db.SaveChangesAsync();
            db.UsuarioEmpresas.AddRange(
                new UsuarioEmpresa(actor.Id, tenantA.Id, roleA.Id),
                new UsuarioEmpresa(actor.Id, tenantB.Id, roleB.Id));
            db.RolPermisos.Add(new RolPermiso
            {
                RolId = roleA.Id,
                PermisoId = permission.Id
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var current = new Mock<ICurrentUserService>(MockBehavior.Strict);
            current.SetupGet(x => x.EstaAutenticado).Returns(true);
            current.SetupGet(x => x.UsuarioId).Returns(actor.Id);
            current.SetupGet(x => x.NombreUsuario).Returns(username);
            var scope = new UsuarioScopeService(db, current.Object);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.MapInboundClaims = false;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "c0303-mysql-ci",
                        ValidateAudience = true,
                        ValidAudience = "c0303-mysql-ci",
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = Signing,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
            await using var provider = services.BuildServiceProvider();
            var jwt = new JwtSecurityToken(
                issuer: "c0303-mysql-ci", audience: "c0303-mysql-ci",
                claims: [
                    new Claim("id", actor.Id.ToString()),
                    new Claim("nombreUsuario", username),
                    new Claim("sub", username)
                ],
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddMinutes(8),
                signingCredentials: new SigningCredentials(Signing, SecurityAlgorithms.HmacSha256));
            var http = new DefaultHttpContext { RequestServices = provider };
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("id", actor.Id.ToString())], "synthetic-not-a-grant"));
            http.Request.Headers.Authorization =
                "Bearer " + new JwtSecurityTokenHandler().WriteToken(jwt);
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenantA.Id;
            http.Request.RouteValues["empresaId"] = tenantA.Id.ToString();

            var accessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
            accessor.SetupGet(x => x.HttpContext).Returns(http);
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["SecurityAudit:EnableReadOnlyAuthorizationProbe"] = "true"
                }).Build();
            var reader = new C0303VerifiedAuditTenantReaders(
                accessor.Object, current.Object, scope, db, config);

            // The real MySQL membership/grant join must attest a *read-only*
            // candidate for tenant A; it must never certify a durable audit sink.
            var positive = await reader.ReadCurrentAsync(default);
            Assert.NotNull(positive);
            Assert.Equal(tenantA.Id, positive.TenantId);
            Assert.False(positive.TransactionalSinkVerified);
            Assert.NotNull(await reader.ReadCurrentAsync(
                actor.Id, tenantA.Id, Rules.Action.AuditRead, default));

            // Route spoofing, membership B and critical changes cannot borrow A.
            http.Request.RouteValues["empresaId"] = tenantB.Id.ToString();
            Assert.Null(await reader.ReadCurrentAsync(default));
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenantB.Id;
            Assert.Null(await reader.ReadCurrentAsync(default)); // B has no grant
            http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenantA.Id;
            http.Request.RouteValues["empresaId"] = tenantA.Id.ToString();
            Assert.Null(await reader.ReadCurrentAsync(
                actor.Id, tenantB.Id, Rules.Action.AuditRead, default));
            Assert.Null(await reader.ReadCurrentAsync(
                actor.Id, tenantA.Id, Rules.Action.CriticalChange, default));

            // Physical transaction rejects the grant while revoked and reverts
            // the change without leaving a stale denial/allowance in SQL.
            await using (var tx = await db.Database.BeginTransactionAsync())
            {
                var persisted = await db.Permisos.IgnoreQueryFilters()
                    .SingleAsync(p => p.Id == permission.Id);
                persisted.Activo = false;
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
                Assert.Null(await reader.ReadCurrentAsync(default));
                await tx.RollbackAsync();
            }
            db.ChangeTracker.Clear();
            Assert.NotNull(await reader.ReadCurrentAsync(default));

            // Committed revocation is observed even with an otherwise valid JWT.
            var membership = await db.UsuarioEmpresas.IgnoreQueryFilters()
                .SingleAsync(m => m.UsuarioId == actor.Id &&
                                  m.EmpresaId == tenantA.Id);
            membership.Desactivar();
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            Assert.Null(await reader.ReadCurrentAsync(default));
            Assert.Null(await reader.ReadCurrentAsync(
                actor.Id, tenantA.Id, Rules.Action.AuditRead, default));
        }
        finally
        {
            await using var cleanup = new AppDbContext(options);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}
