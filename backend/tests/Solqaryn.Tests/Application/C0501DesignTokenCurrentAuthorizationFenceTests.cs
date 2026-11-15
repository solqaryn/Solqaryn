using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;
using Xunit;
using Fence = Solqaryn.Application.AutomationV5.C0501DesignTokenCurrentAuthorizationFence;

namespace Solqaryn.Tests.Application;

public sealed class C0501DesignTokenCurrentAuthorizationFenceTests
{
    private const string Idem = "2c85f621-314a-4f81-88c4-8054e61f812d";
    private sealed class IdentitySource : IIdentidadUniversalSnapshotSource
    {
        public IdentidadUniversalSnapshot? Snapshot { get; set; }
        public int Reads { get; private set; }
        public Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
            int user, int tenant, int? branch, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            return Task.FromResult(Snapshot);
        }
    }
    private sealed class ThemeSource : IC0501VerifiedThemeStateReader
    {
        public Fence.VerifiedThemeState? State { get; set; }
        public int Reads { get; private set; }
        public int? RequestedTenant { get; private set; }
        public bool Throws { get; set; }
        public ValueTask<Fence.VerifiedThemeState?> ReadCurrentAsync(
            int tenant, string key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            RequestedTenant = tenant;
            if (Throws) throw new InvalidOperationException("theme store unavailable");
            return ValueTask.FromResult(State);
        }
    }

    private static (ContextoTenantActual Actor, IdentidadUniversalSnapshot Snapshot) Identity()
    {
        var user = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var company = new Empresa("Tenant A") { Id = 11 };
        var member = new UsuarioEmpresa(7, 11, 19);
        var role = new Rol { Id = 19, EmpresaId = 11, Activo = true };
        var permit = new Permiso
        {
            Id = 49, Modulo = ModuloSistema.Configuracion, Accion = AccionPermiso.Editar,
            Ambito = AmbitoAutorizacion.Empresa, Activo = true
        };
        role.Permisos.Add(new RolPermiso
        { RolId = 19, RolEntidad = role, PermisoId = 49, Permiso = permit });
        var actor = IdentidadUniversalReglas.CrearContextoVerificado(
            user, company, member, role);
        return (actor, new IdentidadUniversalSnapshot(user, company, member, role, null));
    }

    private static Fence.VerifiedThemeState State() =>
        new(11, "primary-color", 7, 9,
            new HashSet<string>(StringComparer.Ordinal) { "primary-color" },
            true, true, true);

    private static C0501DesignTokenApplicationService.ClientIntent Intent() =>
        new(11, 9, 7, "primary-color", "COLOR", "#123AbC", Idem);

    private static (Fence Fence, ContextoTenantActual Actor,
        IdentitySource Identities, ThemeSource Themes, IdentidadUniversalSnapshot Snapshot) Setup()
    {
        var (actor, snapshot) = Identity();
        var identities = new IdentitySource { Snapshot = snapshot };
        var themes = new ThemeSource { State = State() };
        var fence = new Fence(new IdentidadUniversalAuthorizationGuard(identities), themes);
        return (fence, actor, identities, themes, snapshot);
    }

    [Fact] public async Task CurrentRelationalPermissionAndBackendThemeStillGrantNothing()
    {
        var t = Setup();
        var result = await t.Fence.InspectAsync(t.Actor, Intent());
        Assert.Equal("AUTHENTICATED_THEME_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.RequiresSeparatelyAuthenticatedThemeAdapter);
        Assert.False(result.GrantsAccess);
        Assert.False(result.WritesTheme);
        Assert.False(result.CanOverrideTenant);
        Assert.False(result.ChangesProviderState);
        Assert.False(result.CertifiesHttpJwtOrMySql);
        Assert.Equal(1, t.Identities.Reads);
        Assert.Equal(1, t.Themes.Reads);
        Assert.Equal(11, t.Themes.RequestedTenant);
    }

    [Fact] public async Task NullIntentOrIdentityDoesNotTouchDataSources()
    {
        var t = Setup();
        Assert.Equal("TRUSTED_THEME_INTENT_REQUIRED", (await t.Fence.InspectAsync(null, Intent())).Code);
        Assert.Equal("TRUSTED_THEME_INTENT_REQUIRED", (await t.Fence.InspectAsync(t.Actor, null)).Code);
        Assert.Equal(0, t.Identities.Reads);
        Assert.Equal(0, t.Themes.Reads);
    }

    [Fact] public async Task ClientTenantBIsDeniedBeforeAnyRead()
    {
        var t = Setup();
        Assert.Equal("CLIENT_TENANT_ASSERTION_DENIED",
            (await t.Fence.InspectAsync(t.Actor, Intent() with { ClaimedTenantId = 22 })).Code);
        Assert.Equal(0, t.Identities.Reads);
    }

    [Fact] public async Task ClientGlobalMutationAndDirectWriteNeverReachData()
    {
        var t = Setup();
        Assert.Equal("CLIENT_MUTATION_OR_GLOBAL_GRANT_DENIED",
            (await t.Fence.InspectAsync(t.Actor, Intent() with { PlatformCatalogMutation = true })).Code);
        Assert.Equal("CLIENT_MUTATION_OR_GLOBAL_GRANT_DENIED",
            (await t.Fence.InspectAsync(t.Actor, Intent() with { DirectWrite = true })).Code);
        Assert.Equal(0, t.Identities.Reads);
    }

    [Fact] public async Task RevokedMembershipAndRoleDoNotReadThemeCatalog()
    {
        var t = Setup();
        t.Snapshot.Membresia.Desactivar();
        Assert.Equal("CURRENT_RELATIONAL_CONFIGURATION_RBAC_REQUIRED",
            (await t.Fence.InspectAsync(t.Actor, Intent())).Code);
        Assert.Equal(0, t.Themes.Reads);
    }

    [Fact] public async Task AdministratorWithoutEffectiveConfigurationEditIsDenied()
    {
        var t = Setup();
        t.Snapshot.Rol.EsAdministrador = true;
        t.Snapshot.Rol.Permisos.Clear();
        Assert.Equal("CURRENT_RELATIONAL_CONFIGURATION_RBAC_REQUIRED",
            (await t.Fence.InspectAsync(t.Actor, Intent())).Code);
        Assert.Equal(0, t.Themes.Reads);
    }

    [Fact] public async Task ForeignTenantOrWrongCatalogKeyCannotBeLaundered()
    {
        foreach (var state in new[]
        {
            State() with { TenantId = 22 },
            State() with { ExactTokenKey = "Primary-Color" }
        })
        {
            var t = Setup(); t.Themes.State = state;
            Assert.Equal("CURRENT_VERIFIED_THEME_POLICY_REQUIRED",
                (await t.Fence.InspectAsync(t.Actor, Intent())).Code);
        }
    }

    [Fact] public async Task MissingBackendProvenanceAndJwtCurrentMustDeny()
    {
        foreach (var state in new[]
        {
            State() with { OwnerVerifiedFromPersistence = false },
            State() with { CatalogReadFromTrustedBackend = false },
            State() with { JwtValidatedByBackendAndNotRevoked = false },
            State() with { ApprovedTokenKeys = null }
        })
        {
            var t = Setup(); t.Themes.State = state;
            Assert.Equal("CURRENT_VERIFIED_THEME_POLICY_REQUIRED",
                (await t.Fence.InspectAsync(t.Actor, Intent())).Code);
        }
    }

    [Fact] public async Task ServerSessionEpochAndRevisionMustMatchRequestedVersion()
    {
        var t = Setup();
        Assert.Equal("STALE_OR_INVALID_SESSION_EPOCH",
            (await t.Fence.InspectAsync(t.Actor, Intent() with { SessionEpoch = 8 })).Code);
        Assert.Equal("THEME_REVISION_CONFLICT",
            (await t.Fence.InspectAsync(t.Actor, Intent() with { ExpectedRevision = 8 })).Code);
    }

    [Fact] public async Task UnsafeCssAndNonOrdinalApprovedKeyAreDenied()
    {
        var t = Setup();
        Assert.Equal("UNSAFE_OR_UNTYPED_VALUE",
            (await t.Fence.InspectAsync(t.Actor, Intent() with { TokenValue = "url(javascript:alert(1))" })).Code);
        t.Themes.State = State() with
        { ApprovedTokenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PRIMARY-COLOR" } };
        Assert.Equal("UNKNOWN_OR_UNSAFE_TOKEN",
            (await t.Fence.InspectAsync(t.Actor, Intent())).Code);
    }

    [Fact] public async Task MissingPolicyAndProviderFailureFailClosed()
    {
        var t = Setup();
        t.Themes.State = null;
        Assert.Equal("CURRENT_VERIFIED_THEME_POLICY_REQUIRED",
            (await t.Fence.InspectAsync(t.Actor, Intent())).Code);
        t.Themes.Throws = true;
        Assert.Equal("TRUSTED_THEME_STATE_UNAVAILABLE",
            (await t.Fence.InspectAsync(t.Actor, Intent())).Code);
    }

    [Fact] public async Task CancelledReviewNeverReadsIdentityOrCatalog()
    {
        var t = Setup();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await t.Fence.InspectAsync(t.Actor, Intent(), cts.Token));
        Assert.Equal(0, t.Identities.Reads);
        Assert.Equal(0, t.Themes.Reads);
    }
}
