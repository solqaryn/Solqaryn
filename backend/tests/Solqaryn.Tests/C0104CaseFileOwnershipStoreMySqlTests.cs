using System.Data;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Solqaryn.API.Security;
using Solqaryn.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Application.Security;
using Solqaryn.Application.DTOs;
using Solqaryn.Infrastructure.Repositories;
using Solqaryn.Infrastructure.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.AutomationV5;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

[Trait("Category", "Integration")]
public sealed class C0104CaseFileOwnershipStoreMySqlTests
{
    // HttpContextAccessor uses a static AsyncLocal across instances. Two simulated
    // requests in the same async flow must not overwrite each other's tenant marker.
    private sealed class IsolatedFixtureAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    // Explicit synthetic source for storage integration only: no production grant.
    private sealed class FixtureAuthority(int tenant, int branch, int actor)
        : V5CaseFileOwnershipStore.IWriteAuthority
    {
        public bool RevokeAfterWrite { get; set; }
        private int _calls;
        public ValueTask<V5CaseFileOwnershipStore.WriteAuthority?> ResolveAsync(
            AppDbContext db, V5CaseFileOwnershipStore.Selection selection, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Assert.NotNull(db.Database.CurrentTransaction);
            _calls++;
            return ValueTask.FromResult<V5CaseFileOwnershipStore.WriteAuthority?>(
                selection.ResourceType != "fixture" || (RevokeAfterWrite && _calls > 1)
                    ? null : new(actor, tenant, branch, selection.CaseId,
                        selection.ResourceType, selection.ResourceRef));
        }
    }

    [Fact]
    public async Task Store_IsolatesTenants_AppendsVersions_AndRollsBackRevocation()
    {
        var origin = new MySqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("Ephemeral MySQL required."));
        if (origin.Server is not ("127.0.0.1" or "localhost") || origin.Port != 3306)
            throw new InvalidOperationException("Only isolated localhost MySQL allowed.");
        origin.Database = "v44_c0104_store_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL(origin.ConnectionString, x => x.MigrationsAssembly("Solqaryn.Infrastructure.Migrations"))
            .Options;
        await using var setup = new AppDbContext(options);
        try
        {
            await Phase7MySqlTestDatabase.InitializeFreshAsync(setup);
            // Exercise only the additive migration's empty-schema downgrade/reapply.
            await setup.Database.MigrateAsync("20261117215700_V19WhatsAppE164LiteralPlus");
            Assert.Contains("20261126165100_V44CaseFileOwnershipStore",
                await setup.Database.GetPendingMigrationsAsync());
            await setup.Database.MigrateAsync();
            Assert.Empty(await setup.Database.GetPendingMigrationsAsync());
            var a = new Empresa("QA A");
            var b = new Empresa("QA B");
            setup.Set<Empresa>().AddRange(a,b);
            await setup.SaveChangesAsync();
            var branchA = new Sucursal { EmpresaId = a.Id, Codigo = "QA_A", Nombre = "QA A" };
            var branchB = new Sucursal { EmpresaId = b.Id, Codigo = "QA_B", Nombre = "QA B" };
            setup.Set<Sucursal>().AddRange(branchA,branchB);
            // Phase7 bootstrap does not guarantee a user. Seed only this disposable DB.
            var role = new Rol { Nombre = "QA Casefile", NombreNormalizado = "QA_CASEFILE",
                EsSistema = true, Activo = true };
            setup.Roles.Add(role);
            await setup.SaveChangesAsync();
            var user = new Usuario { NombreUsuario = "qa-casefile", NombreCompleto = "QA Casefile",
                PasswordHash = "fixture-no-login", RolId = role.Id, Activo = true };
            setup.Usuarios.Add(user);
            await setup.SaveChangesAsync();
            var actor = user.Id;
            setup.UsuarioEmpresas.AddRange(new UsuarioEmpresa(actor, a.Id, role.Id),
                new UsuarioEmpresa(actor, b.Id, role.Id));
            await setup.SaveChangesAsync();
            IHttpContextAccessor Access(int tenant)
            {
                var h = new DefaultHttpContext();
                h.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = tenant;
                return new IsolatedFixtureAccessor { HttpContext = h };
            }
            await using var dbA = new AppDbContext(options, tenantHttpContextAccessor: Access(a.Id));
            await using var dbB = new AppDbContext(options, tenantHttpContextAccessor: Access(b.Id));
            Assert.Equal(a.Id, dbA.AuthorizedCommercialEmpresaId);
            Assert.Equal(b.Id, dbB.AuthorizedCommercialEmpresaId);
            var storeA = new V5CaseFileOwnershipStore(dbA, new FixtureAuthority(a.Id, branchA.Id, actor));
            var storeB = new V5CaseFileOwnershipStore(dbB, new FixtureAuthority(b.Id, branchB.Id, actor));
            var one = new V5CaseFileOwnershipStore.Selection(1,"fixture","r1",null,new string('a',64),"create");
            var two = one with { CaseId = 2, ResourceRef = "r2" };
            Assert.Equal(1L, (await storeA.AppendAsync(one)).Version);
            Assert.Equal(1L, (await storeA.AppendAsync(two)).Version);
            Assert.Equal(1L, (await storeB.AppendAsync(one)).Version);
            Assert.Null(await storeB.ReadCandidateAsync(a.Id,1,branchA.Id));
            Assert.Null(await storeA.ReadCandidateAsync(a.Id,1,branchB.Id));
            Assert.Null(await storeB.ReadCandidateAsync(b.Id,2,branchB.Id));
            Assert.Equal(2L, (await storeA.AppendAsync(one with { ExpectedVersion = 1 })).Version);
            Assert.Equal("CASEFILE_VERSION_CONFLICT",
                (await storeA.AppendAsync(one with { ExpectedVersion = 1 })).Code);
            Assert.Equal(2L,(await storeA.ReadCandidateAsync(a.Id,1,branchA.Id))!.Version);
            Assert.Equal(1L,(await storeB.ReadCandidateAsync(b.Id,1,branchB.Id))!.Version);
            // The audit consumer must be able to retain the actual P01/P02 candidate
            // lock in its own transaction; standalone reads cannot establish this.
            Assert.Null(await V5CaseFileOwnershipStore.ReadCandidateInCurrentTransactionAsync(
                dbA, a.Id, 1, branchA.Id));
            await using (var candidateTx = await dbA.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
            {
                Assert.Null(await V5CaseFileOwnershipStore.ReadCandidateInCurrentTransactionAsync(
                    dbA, b.Id, 1, branchB.Id));
                Assert.Null(await V5CaseFileOwnershipStore.ReadCandidateInCurrentTransactionAsync(
                    dbA, a.Id, 1, branchB.Id));
                var locked = await V5CaseFileOwnershipStore.ReadCandidateInCurrentTransactionAsync(
                    dbA, a.Id, 1, branchA.Id);
                Assert.Equal(2L, locked!.Version);
                Assert.Equal("r1", locked.ResourceRef);
                Assert.Same(candidateTx, dbA.Database.CurrentTransaction);
                await using var competing = new MySqlConnection(origin.ConnectionString);
                await competing.OpenAsync();
                await using var contender = competing.CreateCommand();
                contender.CommandText = "SET SESSION innodb_lock_wait_timeout=1";
                await contender.ExecuteNonQueryAsync();
                contender.CommandText = """
                    UPDATE V5CaseFileOwnerships SET RecursoRef=RecursoRef
                    WHERE EmpresaId=@tenant AND Id=@case
                    """;
                contender.Parameters.AddWithValue("@tenant", a.Id);
                contender.Parameters.AddWithValue("@case", 1);
                var blocked = await Assert.ThrowsAsync<MySqlException>(async () =>
                {
                    await contender.ExecuteNonQueryAsync();
                });
                Assert.Equal(1205, blocked.Number);
                Assert.Equal(locked, await V5CaseFileOwnershipStore.ReadCandidateInCurrentTransactionAsync(
                    dbA, a.Id, 1, branchA.Id));
                await candidateTx.RollbackAsync();
                // The second session can now obtain the lock; no source row is changed.
                await contender.ExecuteNonQueryAsync();
            }
            Assert.Equal(2L, (await storeA.ReadCandidateAsync(a.Id, 1, branchA.Id))!.Version);
            // Supplemental API12 grant lock: explicit fixture grants only. The
            // test is a real SQL boundary test, not an authenticated HTTP proof.
            Assert.False(await V5CaseFileAuditGrantLock.TryAcquireAsync(
                dbA, actor, role.Id, a.Id, branchA.Id, role.Id));
            await using (var noGrant = await dbA.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
            {
                Assert.False(await V5CaseFileAuditGrantLock.TryAcquireAsync(
                    dbA, actor, role.Id, a.Id, branchA.Id, role.Id));
                await noGrant.RollbackAsync();
            }
            async Task<Permiso> FixturePermission(ModuloSistema modulo, AccionPermiso accion)
            {
                var existing = await setup.Permisos.IgnoreQueryFilters()
                    .SingleOrDefaultAsync(p => p.Modulo == modulo && p.Accion == accion);
                if (existing is not null)
                {
                    Assert.True(existing.Activo && !existing.Eliminado);
                    Assert.Equal(AmbitoAutorizacion.Empresa, existing.Ambito);
                    return existing;
                }
                var created = new Permiso { Codigo = $"QA.{modulo}.{accion}",
                    Nombre = $"QA {modulo} {accion}", Modulo = modulo, Accion = accion,
                    Ambito = AmbitoAutorizacion.Empresa, Activo = true };
                setup.Permisos.Add(created);
                await setup.SaveChangesAsync();
                return created;
            }
            var auditPermission = await FixturePermission(ModuloSistema.Auditoria, AccionPermiso.ReadAuditCorrelation);
            var branchPermission = await FixturePermission(ModuloSistema.Sucursales, AccionPermiso.Ver);
            var grant = new RolPermiso { RolId = role.Id, PermisoId = auditPermission.Id };
            setup.RolPermisos.AddRange(grant, new RolPermiso { RolId = role.Id, PermisoId = branchPermission.Id });
            await setup.SaveChangesAsync();
            // The receipt writer is a real SQL storage primitive. This explicit
            // synthetic resolver is NOT an authenticated commercial resource mapping.
            var auditCandidate = (await storeA.ReadCandidateAsync(a.Id, 1, branchA.Id))!;
            var otherCandidate = (await storeB.ReadCandidateAsync(b.Id, 1, branchB.Id))!;
            var correlation = Guid.NewGuid();
            int resourceChecks = 0;
            ValueTask<bool> VerifyFixtureResource(AppDbContext context,
                V5CaseFileOwnershipStore.Candidate candidate, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                Assert.Same(dbA, context);
                Assert.NotNull(context.Database.CurrentTransaction);
                resourceChecks++;
                return ValueTask.FromResult(candidate == auditCandidate);
            }
            Assert.Null(await V5CaseFileAuditReceiptPersistence.AppendEnterpriseInCurrentTransactionAsync(
                dbA, auditCandidate, actor, role.Id, role.Id, correlation, VerifyFixtureResource));
            await using (var receiptTx = await dbA.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
            {
                Assert.Null(await V5CaseFileAuditReceiptPersistence.AppendEnterpriseInCurrentTransactionAsync(
                    dbA, otherCandidate, actor, role.Id, role.Id, correlation, VerifyFixtureResource));
                Assert.Equal(0, resourceChecks);
                var receiptId = await V5CaseFileAuditReceiptPersistence.AppendEnterpriseInCurrentTransactionAsync(
                    dbA, auditCandidate, actor, role.Id, role.Id, correlation, VerifyFixtureResource);
                Assert.NotNull(receiptId);
                Assert.Equal(2, resourceChecks);
                Assert.Same(receiptTx, dbA.Database.CurrentTransaction);
                var audit = await dbA.Set<RegistroAuditoria>().AsNoTracking()
                    .SingleAsync(r => r.Id == receiptId.Value);
                Assert.Equal(a.Id, audit.EmpresaId);
                Assert.Equal(actor, audit.UsuarioId);
                Assert.Equal(AmbitoAutorizacion.Empresa, audit.AmbitoAutoridad);
                Assert.Equal(OrigenAutorizacion.MembresiaEmpresa, audit.OrigenAutorizacion);
                Assert.Equal(AccionPermiso.ReadAuditCorrelation, audit.Accion);
                Assert.Equal(correlation.ToString("N"), audit.CorrelationId);
                Assert.Equal("actor-autenticado", audit.NombreUsuario);
                Assert.Null(audit.Ip);
                Assert.Null(audit.UserAgent);
                Assert.Null(audit.ValoresAnteriores);
                Assert.Null(audit.Motivo);
                Assert.Null(audit.Error);
                using var payload = System.Text.Json.JsonDocument.Parse(audit.ValoresNuevos!);
                Assert.Equal(2, payload.RootElement.EnumerateObject().Count());
                Assert.Equal(branchA.Id, payload.RootElement.GetProperty("SucursalId").GetInt32());
                Assert.Equal(auditCandidate.Version, payload.RootElement.GetProperty("Version").GetInt64());
                await receiptTx.RollbackAsync();
            }
            Assert.False(await dbA.Set<RegistroAuditoria>().AsNoTracking()
                .AnyAsync(r => r.CorrelationId == correlation.ToString("N")));
            var revokedDuringReceipt = Guid.NewGuid();
            int rechecks = 0;
            ValueTask<bool> RevokedAfterIo(AppDbContext context,
                V5CaseFileOwnershipStore.Candidate candidate, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                return ValueTask.FromResult(++rechecks == 1 && candidate == auditCandidate);
            }
            await using (var receiptTx = await dbA.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
            {
                Assert.Null(await V5CaseFileAuditReceiptPersistence.AppendEnterpriseInCurrentTransactionAsync(
                    dbA, auditCandidate, actor, role.Id, role.Id, revokedDuringReceipt, RevokedAfterIo));
                Assert.Equal(2, rechecks);
                await receiptTx.RollbackAsync();
            }
            Assert.False(await dbA.Set<RegistroAuditoria>().AsNoTracking()
                .AnyAsync(r => r.CorrelationId == revokedDuringReceipt.ToString("N")));
            var committedCorrelation = Guid.NewGuid();
            await using (var receiptTx = await dbA.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
            {
                Assert.NotNull(await V5CaseFileAuditReceiptPersistence.AppendEnterpriseInCurrentTransactionAsync(
                    dbA, auditCandidate, actor, role.Id, role.Id, committedCorrelation, VerifyFixtureResource));
                await receiptTx.CommitAsync();
            }
            Assert.True(await dbA.Set<RegistroAuditoria>().AsNoTracking()
                .AnyAsync(r => r.EmpresaId == a.Id && r.CorrelationId == committedCorrelation.ToString("N")));
            Assert.False(await dbB.Set<RegistroAuditoria>().AsNoTracking()
                .AnyAsync(r => r.EmpresaId == b.Id && r.CorrelationId == committedCorrelation.ToString("N")));
            // Real JwtBearer signature validation plus real Oracle grant/receipt locks.
            // Preliminary permission projection and resource mapping are explicit fixtures:
            // this is not a commercial HTTP/DI or authentic-resource certification.
            async Task CheckAuthenticatedReceipt(int tenant, int branch, int caseId,
                bool changeSessionDuringIo, bool shouldCommit)
            {
                var requestAccessor = Access(tenant);
                var request = requestAccessor.HttpContext!;
                var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
                var signed = new JwtSecurityToken("casefile-fixture", "casefile-fixture",
                    new[]
                    {
                        new Claim("id", actor.ToString(CultureInfo.InvariantCulture)),
                        new Claim("rolId", role.Id.ToString(CultureInfo.InvariantCulture)),
                        new Claim("tenantId", tenant.ToString(CultureInfo.InvariantCulture)),
                        new Claim("branchId", branch.ToString(CultureInfo.InvariantCulture)),
                        new Claim("tenantRoleId", role.Id.ToString(CultureInfo.InvariantCulture))
                    }, expires: DateTime.UtcNow.AddMinutes(5),
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
                using var provider = new ServiceCollection().AddLogging()
                    .AddAuthentication(TenantSessionToken.Scheme)
                    .AddJwtBearer(TenantSessionToken.Scheme, jwt =>
                    {
                        jwt.MapInboundClaims = false;
                        jwt.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true, IssuerSigningKey = key,
                            ValidateIssuer = true, ValidIssuer = "casefile-fixture",
                            ValidateAudience = true, ValidAudience = "casefile-fixture",
                            ValidateLifetime = true, ClockSkew = TimeSpan.Zero
                        };
                    }).Services.BuildServiceProvider();
                await using var requestScope = provider.CreateAsyncScope();
                request.RequestServices = requestScope.ServiceProvider;
                request.Request.Headers["Authorization"] = "Bearer " + new JwtSecurityTokenHandler().WriteToken(signed);
                var authenticated = await request.AuthenticateAsync(TenantSessionToken.Scheme);
                Assert.True(authenticated.Succeeded);
                request.User = authenticated.Principal!;
                var preliminary = new Mock<IPermisoService>(MockBehavior.Strict);
                preliminary.Setup(p => p.GetMisPermisosContextoAsync(actor, role.Id, tenant,
                    branch, role.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new MisPermisosDto
                    {
                        Rol = "fixture", Permisos = new List<string> { "Auditoria:ReadAuditCorrelation" }
                    });
                var authority = new V5CaseFileAuditCorrelationAuthority(requestAccessor, preliminary.Object);
                await using var requestDb = new AppDbContext(options, tenantHttpContextAccessor: requestAccessor);
                var operation = Guid.NewGuid();
                var checks = 0;
                ValueTask<bool> VerifyFixture(AppDbContext context,
                    V5CaseFileOwnershipStore.Candidate candidate, CancellationToken ct)
                {
                    ct.ThrowIfCancellationRequested();
                    Assert.Same(requestDb, context);
                    Assert.NotNull(context.Database.CurrentTransaction);
                    checks++;
                    if (changeSessionDuringIo && checks == 2)
                        request.User = new ClaimsPrincipal(new ClaimsIdentity());
                    return ValueTask.FromResult(candidate.TenantId == tenant &&
                        candidate.BranchId == branch && candidate.CaseId == caseId &&
                        candidate.ResourceType == "fixture");
                }

                var result = await authority.AppendAuthorizedReceiptAsync(
                    requestDb, caseId, operation, VerifyFixture);
                Assert.Equal(shouldCommit ? "CASEFILE_AUDIT_RECEIPT_WRITTEN" : "CASEFILE_AUDIT_DENIED", result.Code);
                Assert.Equal(shouldCommit, result.ReceiptId.HasValue);
                Assert.Null(requestDb.Database.CurrentTransaction);
                var durable = await requestDb.Set<RegistroAuditoria>().AsNoTracking()
                    .Where(r => r.CorrelationId == operation.ToString("N")).ToListAsync();
                if (shouldCommit)
                {
                    var row = Assert.Single(durable);
                    Assert.Equal(tenant, row.EmpresaId);
                    Assert.Equal(caseId, row.ReferenciaId);
                    Assert.Equal(AccionPermiso.ReadAuditCorrelation, row.Accion);
                    Assert.Equal("actor-autenticado", row.NombreUsuario);
                    Assert.Null(row.Ip);
                    Assert.Null(row.UserAgent);
                    Assert.Equal(2, checks);
                }
                else
                {
                    Assert.Empty(durable);
                    Assert.Empty(requestDb.ChangeTracker.Entries());
                    if (changeSessionDuringIo) Assert.Equal(2, checks);
                }
            }
            await CheckAuthenticatedReceipt(a.Id, branchA.Id, 1, false, true);
            await CheckAuthenticatedReceipt(a.Id, branchA.Id, 2, false, true);
            await CheckAuthenticatedReceipt(b.Id, branchB.Id, 1, false, true);
            await CheckAuthenticatedReceipt(b.Id, branchB.Id, 2, false, false);
            await CheckAuthenticatedReceipt(a.Id, branchA.Id, 1, true, false);

            // Legacy readers must not enumerate, count or export committed API12 receipts.
            var ordinaryCorrelation = Guid.NewGuid().ToString("N");
            dbA.Set<RegistroAuditoria>().Add(new RegistroAuditoria
            {
                Fecha = DateTime.UtcNow, UsuarioId = actor, NombreUsuario = "fixture-actor",
                EmpresaId = a.Id, AmbitoAutoridad = AmbitoAutorizacion.Empresa,
                OrigenAutorizacion = OrigenAutorizacion.MembresiaEmpresa,
                Modulo = ModuloSistema.Auditoria, Accion = AccionPermiso.Ver,
                Entidad = "FixtureOrdinaryAudit", Descripcion = "ordinary fixture",
                CorrelationId = ordinaryCorrelation, Resultado = "Exito"
            });
            await dbA.SaveChangesAsync();
            var legacyA = new AuditoriaRepository(dbA);
            var legacyB = new AuditoriaRepository(dbB);
            foreach (var legacy in new[] { legacyA, legacyB })
            {
                var hidden = await legacy.GetFilteredAsync(new AuditoriaFiltroDto
                {
                    Modulo = "Auditoria", Accion = "ReadAuditCorrelation"
                });
                Assert.Equal(0, hidden.TotalCount);
                Assert.Empty(hidden.Items);
            }
            var ordinaryFilter = new AuditoriaFiltroDto { Entidad = "FixtureOrdinaryAudit" };
            var visible = await legacyA.GetFilteredAsync(ordinaryFilter);
            Assert.Equal(1, visible.TotalCount);
            Assert.Equal(ordinaryCorrelation, Assert.Single(visible.Items).CorrelationId);
            Assert.Equal(0, (await legacyB.GetFilteredAsync(ordinaryFilter)).TotalCount);
            var period = new ReporteAdministrativoFiltroDto
            {
                Desde = DateTime.UtcNow.Date.AddDays(-1), Hasta = DateTime.UtcNow.Date.AddDays(1)
            };
            foreach (var report in new[] { new ReporteAdministrativoService(dbA),
                         new ReporteAdministrativoService(dbB) })
            {
                var summary = await report.ObtenerResumenAuditoriaAsync(period);
                Assert.DoesNotContain(summary.PorAccion, row => row.Accion == "ReadAuditCorrelation");
                var export = await report.ExportarAsync("auditoria", "csv", period);
                var csv = System.Text.Encoding.UTF8.GetString(export.Contenido);
                Assert.DoesNotContain(committedCorrelation.ToString("N"), csv, StringComparison.Ordinal);
                Assert.DoesNotContain("ReadAuditCorrelation", csv, StringComparison.Ordinal);
            }
            var ordinaryExport = await new ReporteAdministrativoService(dbA).ExportarAsync("auditoria", "csv", period);
            Assert.Contains(ordinaryCorrelation, System.Text.Encoding.UTF8.GetString(ordinaryExport.Contenido), StringComparison.Ordinal);
            Assert.True(await dbA.Set<RegistroAuditoria>().AsNoTracking()
                .AnyAsync(r => r.EmpresaId == a.Id && r.CorrelationId == committedCorrelation.ToString("N")));

            await using (var grantTx = await dbA.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
            {
                Assert.False(await V5CaseFileAuditGrantLock.TryAcquireAsync(
                    dbA, actor, role.Id, b.Id, branchB.Id, role.Id));
                Assert.False(await V5CaseFileAuditGrantLock.TryAcquireAsync(
                    dbA, actor, role.Id, a.Id, branchB.Id, role.Id));
                Assert.True(await V5CaseFileAuditGrantLock.TryAcquireAsync(
                    dbA, actor, role.Id, a.Id, branchA.Id, role.Id));
                Assert.Same(grantTx, dbA.Database.CurrentTransaction);
                await using var revoker = new MySqlConnection(origin.ConnectionString);
                await revoker.OpenAsync();
                await using var revokeCommand = revoker.CreateCommand();
                revokeCommand.CommandText = "SET SESSION innodb_lock_wait_timeout=1";
                await revokeCommand.ExecuteNonQueryAsync();
                revokeCommand.CommandText = "DELETE FROM RolPermisos WHERE Id=@grant";
                revokeCommand.Parameters.AddWithValue("@grant", grant.Id);
                var blockedRevoke = await Assert.ThrowsAsync<MySqlException>(async () =>
                {
                    await revokeCommand.ExecuteNonQueryAsync();
                });
                Assert.Equal(1205, blockedRevoke.Number);
                await grantTx.RollbackAsync();
                Assert.Equal(1, await revokeCommand.ExecuteNonQueryAsync());
            }
            await using (var revokedGrantTx = await dbA.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
            {
                Assert.False(await V5CaseFileAuditGrantLock.TryAcquireAsync(
                    dbA, actor, role.Id, a.Id, branchA.Id, role.Id));
                var priorChecks = resourceChecks;
                Assert.Null(await V5CaseFileAuditReceiptPersistence.AppendEnterpriseInCurrentTransactionAsync(
                    dbA, auditCandidate, actor, role.Id, role.Id, Guid.NewGuid(), VerifyFixtureResource));
                Assert.Equal(priorChecks, resourceChecks);
                await revokedGrantTx.RollbackAsync();
            }
            await CheckAuthenticatedReceipt(a.Id, branchA.Id, 1, false, false);
            var revoke = new FixtureAuthority(a.Id,branchA.Id,actor) { RevokeAfterWrite = true };
            var revokedStore = new V5CaseFileOwnershipStore(dbA,revoke);
            var three = one with { CaseId = 3, ResourceRef = "r3" };
            Assert.Equal("CASEFILE_WRITE_DENIED",(await revokedStore.AppendAsync(three)).Code);
            Assert.Null(await storeA.ReadCandidateAsync(a.Id,3,branchA.Id));
            Assert.Equal(1L,(await storeA.AppendAsync(three)).Version);
            Assert.Equal("CASEFILE_WRITE_DENIED",
                (await storeA.AppendAsync(one with { CaseId = 4, ResourceType = "unmapped" })).Code);
            Assert.Null(await storeA.ReadCandidateAsync(a.Id,4,branchA.Id));

            // ascii_bin is PAD SPACE; the durable receipt must compare exact bytes.
            // Exercise real MySQL trigger-induced divergence in the disposable fixture.
            var five = one with { CaseId = 5, ResourceRef = "r5" };
            await setup.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER V44C0104TamperOwnership BEFORE INSERT ON V5CaseFileOwnerships
                FOR EACH ROW SET NEW.RecursoRef = CONCAT(NEW.RecursoRef, ' ')
                """);
            try
            {
                Assert.Equal("CASEFILE_WRITE_FAILED", (await storeA.AppendAsync(five)).Code);
                Assert.Null(await storeA.ReadCandidateAsync(a.Id,5,branchA.Id));
            }
            finally { await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER V44C0104TamperOwnership"); }
            Assert.Equal(1L, (await storeA.AppendAsync(five)).Version);

            await setup.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER V44C0104TamperReason BEFORE INSERT ON V5CaseFileVersions
                FOR EACH ROW SET NEW.ReasonCode = CONCAT(NEW.ReasonCode, ' ')
                """);
            try
            {
                Assert.Equal("CASEFILE_WRITE_FAILED",
                    (await storeA.AppendAsync(five with { ExpectedVersion = 1 })).Code);
                Assert.Equal(1L, (await storeA.ReadCandidateAsync(a.Id,5,branchA.Id))!.Version);
            }
            finally { await setup.Database.ExecuteSqlRawAsync("DROP TRIGGER V44C0104TamperReason"); }
            Assert.Equal(2L, (await storeA.AppendAsync(five with { ExpectedVersion = 1 })).Version);

            // Simulate a malformed legacy/externally corrupted candidate only in the fixture.
            await setup.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE V5CaseFileOwnerships SET RecursoRef=CONCAT(RecursoRef, ' ')
                WHERE EmpresaId={a.Id} AND Id={5}
                """);
            Assert.Null(await storeA.ReadCandidateAsync(a.Id,5,branchA.Id));
            Assert.Equal(1L, (await storeB.ReadCandidateAsync(b.Id,1,branchB.Id))!.Version);
        }
        finally { await setup.Database.EnsureDeletedAsync(); }
    }
}
