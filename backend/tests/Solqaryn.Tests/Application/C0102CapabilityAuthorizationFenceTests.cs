using Solqaryn.Application.AutomationV5;
using Solqaryn.Application.Security;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Domain.Security;
using Xunit;
using Rules = Solqaryn.Domain.AutomationV5.C0102CapabilityDomainRules;
using Fence = Solqaryn.Application.AutomationV5.C0102CapabilityAuthorizationFence;

namespace Solqaryn.Tests.Application;

public sealed class C0102CapabilityAuthorizationFenceTests
{
    private const string Capability = "orders.read";
    private static readonly IReadOnlySet<string> Allowed = new HashSet<string> { Capability };
    private static readonly IReadOnlySet<string> Empty = new HashSet<string>();

    private sealed class IdentitySource : IIdentidadUniversalSnapshotSource
    {
        public IdentidadUniversalSnapshot? Current { get; set; }
        public int Reads { get; private set; }
        public Task<IdentidadUniversalSnapshot?> ObtenerActualAsync(
            int userId, int tenantId, int? branchId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            return Task.FromResult(Current);
        }
    }

    private sealed class PolicySource : IC0102VerifiedCapabilityPolicyReader
    {
        public Fence.VerifiedResourcePolicy? Current { get; set; }
        public int Reads { get; private set; }
        public int? SelectedTenant { get; private set; }
        public bool ThrowOnRead { get; set; }
        public ValueTask<Fence.VerifiedResourcePolicy?> ReadCurrentAsync(
            int authenticatedTenantId, int resourceId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Reads++;
            SelectedTenant = authenticatedTenantId;
            if (ThrowOnRead) throw new InvalidOperationException("source unavailable");
            return ValueTask.FromResult(Current);
        }
    }

    private static (ContextoTenantActual Actor, IdentidadUniversalSnapshot Snapshot) Member()
    {
        var user = new Usuario { Id = 7, Activo = true, RolId = 999 };
        var company = new Empresa("Tenant A") { Id = 11 };
        var membership = new UsuarioEmpresa(7, 11, 19);
        var role = new Rol { Id = 19, EmpresaId = 11, Activo = true };
        var permission = new Permiso
        {
            Id = 47, Modulo = ModuloSistema.Usuarios,
            Accion = AccionPermiso.Ver, Ambito = AmbitoAutorizacion.Empresa,
            Activo = true
        };
        role.Permisos.Add(new RolPermiso
        {
            RolId = 19, RolEntidad = role,
            PermisoId = 47, Permiso = permission
        });
        var actor = IdentidadUniversalReglas.CrearContextoVerificado(
            user, company, membership, role);
        return (actor, new IdentidadUniversalSnapshot(
            user, company, membership, role,
            new Sucursal { Id = 3, EmpresaId = 11 }));
    }

    private static Rules.VerifiedPolicy Policy() => new(
        11, 11, 7, 19, 3, 3, "pro", "retail",
        true, true, true, false, true, true, true,
        Allowed, Allowed, Allowed, Allowed, Empty);

    private static Fence.VerifiedResourcePolicy Evidence() =>
        new(101, 11, 7, true, true, Policy());

    private static Fence.Intent Request() =>
        new(101, 11, 3, Capability, ModuloSistema.Usuarios, AccionPermiso.Ver);

    private static (Fence Gate, IdentitySource Identity, PolicySource Policy,
        ContextoTenantActual Actor, IdentidadUniversalSnapshot Snapshot) Setup()
    {
        var (actor, snapshot) = Member();
        var identity = new IdentitySource { Current = snapshot };
        var policy = new PolicySource { Current = Evidence() };
        return (new Fence(new IdentidadUniversalAuthorizationGuard(identity), policy),
            identity, policy, actor, snapshot);
    }

    [Fact] public async Task VerifiedMembershipRbacAndCapabilitiesStillDoNotGrantResourceAccess()
    {
        var t = Setup();
        var result = await t.Gate.InspectAsync(t.Actor, Request());
        Assert.Equal("AUTHENTICATED_CAPABILITY_RESOURCE_ADAPTER_REQUIRED", result.Code);
        Assert.True(result.NeedsAuthenticatedResourceAdapter);
        Assert.False(result.GrantsResourceAccess);
        Assert.False(result.GrantsCapability);
        Assert.False(result.WritesResource);
        Assert.False(result.CanExportOrActivate);
        Assert.False(result.CanOverrideTenant);
        Assert.Equal(1, t.Identity.Reads);
        Assert.Equal(1, t.Policy.Reads);
        Assert.Equal(11, t.Policy.SelectedTenant);
    }

    [Fact] public async Task MissingActorOrIntentNeverCallsBackendSources()
    {
        var t = Setup();
        Assert.Equal("TRUSTED_CAPABILITY_INTENT_REQUIRED",
            (await t.Gate.InspectAsync(null, Request())).Code);
        Assert.Equal("TRUSTED_CAPABILITY_INTENT_REQUIRED",
            (await t.Gate.InspectAsync(t.Actor, null)).Code);
        Assert.Equal(0, t.Identity.Reads);
        Assert.Equal(0, t.Policy.Reads);
    }

    [Fact] public async Task ForgedCompanyIsDeniedBeforeAnyBackendQuery()
    {
        var t = Setup();
        var result = await t.Gate.InspectAsync(t.Actor,
            Request() with { TenantAssertion = 22 });
        Assert.Equal("CROSS_TENANT_CAPABILITY_ASSERTION_DENIED", result.Code);
        Assert.Equal(0, t.Identity.Reads);
        Assert.Equal(0, t.Policy.Reads);
    }

    [Fact] public async Task InvalidResourceBranchAndUnknownPermissionAreRejectedEarly()
    {
        var t = Setup();
        foreach (var request in new[]
        {
            Request() with { ResourceId = 0 },
            Request() with { BranchAssertion = 0 },
            Request() with { Module = (ModuloSistema)999 },
            Request() with { Action = (AccionPermiso)999 },
            Request() with { CapabilityKey = "Orders.Read" },
            Request() with { CapabilityKey = "orders read" }
        })
        {
            Assert.Equal("TRUSTED_CAPABILITY_INTENT_REQUIRED",
                (await t.Gate.InspectAsync(t.Actor, request)).Code);
        }
        Assert.Equal(0, t.Identity.Reads);
    }

    [Fact] public async Task ClientCannotSupplyPolicyGrantOrMutation()
    {
        var t = Setup();
        Assert.Equal("CLIENT_GRANT_OR_MUTATION_DENIED",
            (await t.Gate.InspectAsync(t.Actor,
                Request() with { ClientSuppliesGrantsOrPolicy = true })).Code);
        Assert.Equal("CLIENT_GRANT_OR_MUTATION_DENIED",
            (await t.Gate.InspectAsync(t.Actor,
                Request() with { RequestsWriteExportOrActivation = true })).Code);
        Assert.Equal(0, t.Identity.Reads);
    }

    [Fact] public async Task RevokedMembershipCannotReuseOldRoleOrSnapshot()
    {
        var t = Setup();
        t.Snapshot.Membresia.Desactivar();
        Assert.Equal("CURRENT_MEMBERSHIP_AND_RELATIONAL_RBAC_REQUIRED",
            (await t.Gate.InspectAsync(t.Actor, Request())).Code);
        Assert.Equal(1, t.Identity.Reads);
        Assert.Equal(0, t.Policy.Reads);
    }

    [Fact] public async Task AdministratorWithoutExplicitRelationalGrantIsDenied()
    {
        var t = Setup();
        t.Snapshot.Rol.EsAdministrador = true;
        t.Snapshot.Rol.Permisos.Clear();
        Assert.Equal("CURRENT_MEMBERSHIP_AND_RELATIONAL_RBAC_REQUIRED",
            (await t.Gate.InspectAsync(t.Actor, Request())).Code);
        Assert.Equal(0, t.Policy.Reads);
    }

    [Fact] public async Task ResourceOwnedByTenantBIsDeniedEvenWhenClientAssertsA()
    {
        var t = Setup();
        t.Policy.Current = Evidence() with { OwnerTenantId = 22 };
        Assert.Equal("VERIFIED_RESOURCE_CAPABILITY_POLICY_REQUIRED",
            (await t.Gate.InspectAsync(t.Actor, Request())).Code);
        Assert.False((await t.Gate.InspectAsync(t.Actor, Request())).GrantsResourceAccess);
    }

    [Fact] public async Task ForgedResourceOrActorEvidenceAndUntrustedPolicyAreDenied()
    {
        foreach (var evidence in new[]
        {
            Evidence() with { ResourceId = 102 },
            Evidence() with { VerifiedActorId = 99 },
            Evidence() with { OwnershipReadFromTrustedPersistence = false },
            Evidence() with { CurrentCapabilityCatalogReadFromBackend = false },
            Evidence() with { Policy = Policy() with { ResourceBranchId = 4 } }
        })
        {
            var t = Setup();
            t.Policy.Current = evidence;
            Assert.Equal("VERIFIED_RESOURCE_CAPABILITY_POLICY_REQUIRED",
                (await t.Gate.InspectAsync(t.Actor, Request())).Code);
        }
    }

    [Fact] public async Task RevokedCommercialEntitlementCannotElevateRole()
    {
        foreach (var policy in new[]
        {
            Policy() with { PermissionRevoked = true },
            Policy() with { ExplicitRelationalRbacGrant = false },
            Policy() with { PlanCurrent = false },
            Policy() with { DeniedCapabilities = Allowed },
            Policy() with { TenantCapabilities = Empty }
        })
        {
            var t = Setup();
            t.Policy.Current = Evidence() with { Policy = policy };
            var result = await t.Gate.InspectAsync(t.Actor, Request());
            Assert.False(result.NeedsAuthenticatedResourceAdapter);
            Assert.False(result.GrantsResourceAccess);
        }
    }

    [Fact] public async Task SnapshotMissingAndReaderFailuresAreFailClosed()
    {
        var t = Setup();
        t.Policy.Current = null;
        Assert.Equal("VERIFIED_RESOURCE_CAPABILITY_POLICY_REQUIRED",
            (await t.Gate.InspectAsync(t.Actor, Request())).Code);
        t.Policy.ThrowOnRead = true;
        Assert.Equal("TRUSTED_CAPABILITY_SOURCE_UNAVAILABLE",
            (await t.Gate.InspectAsync(t.Actor, Request())).Code);
    }

    [Fact] public async Task CancelledOperationNeverReadsSources()
    {
        var t = Setup();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await t.Gate.InspectAsync(t.Actor, Request(), cts.Token));
        Assert.Equal(0, t.Identity.Reads);
        Assert.Equal(0, t.Policy.Reads);
    }
}
