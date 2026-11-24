using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// C01-01.M05.02: contract tests of the real tenant-context controller.
/// Only controller actions and DTOs are exercised, not JWT middleware or EF.
/// </summary>
public sealed class TenantContextV5ContractTests
{
    private sealed class ScopeStub : IUsuarioScopeService
    {
        public int Calls { get; private set; }
        public UsuarioTenantScopeActual? Actual { get; set; }
        public IReadOnlyList<EmpresaSeleccionable> Seleccionables { get; set; } =
            Array.Empty<EmpresaSeleccionable>();
        public int SelectorCalls { get; private set; }
        public bool BaseVigente { get; set; } = true;
        public Action? OnSecondBaseCheck { get; set; }
        private int _baseChecks;
        public Task<bool> SesionBaseVigenteAsync(int actorId, int rolId,
            CancellationToken cancellationToken = default)
        {
            if (++_baseChecks == 2) OnSecondBaseCheck?.Invoke();
            return Task.FromResult(BaseVigente && actorId == 7 && rolId == 19);
        }

        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);

        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Actual);
        }

        public Task<IReadOnlyList<EmpresaSeleccionable>> ListarEmpresasSeleccionablesAsync(
            CancellationToken cancellationToken = default)
        {
            SelectorCalls++;
            return Task.FromResult(Seleccionables);
        }
    }

    // A synthetic unit principal is NOT a real JWT signature or MySQL test.
    private static TenantContextController WithJwtIdentity(
        IUsuarioScopeService scope, string actorId = "7", string roleId = "19")
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("id", actorId),
            new Claim("rolId", roleId)
        }, "TestJwt"));
        return new TenantContextController(scope)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Obtener_IdInvalido_RechazaAntesDeConsultarMembresias(int empresaId)
    {
        var scope = new ScopeStub { Actual = new UsuarioTenantScopeActual(7, 11, 19, "Operador", false) };
        var action = await WithJwtIdentity(scope).Obtener(empresaId, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(action);
        var response = Assert.IsType<ApiResponse<object>>(bad.Value);
        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Empty(response.Errors);
        Assert.Equal(0, scope.Calls);
    }

    [Fact]
    public async Task Obtener_SinMembresiaVerificable_NoRevelaExistenciaDeEmpresa()
    {
        var scope = new ScopeStub { Actual = null };
        var action = await WithJwtIdentity(scope).Obtener(22, CancellationToken.None);

        Assert.IsType<ForbidResult>(action);
        Assert.Equal(1, scope.Calls);
    }

    [Fact]
    public async Task Obtener_ConMembresiaVigente_DevuelveDtoVerificado()
    {
        var scope = new ScopeStub { Actual = new UsuarioTenantScopeActual(7, 11, 19, "Operador", false) };
        var action = await WithJwtIdentity(scope).Obtener(11, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var response = Assert.IsType<ApiResponse<UsuarioTenantScopeActual>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(7, response.Data.UsuarioId);
        Assert.Equal(11, response.Data.EmpresaId);
        Assert.Equal(19, response.Data.RolId);
        Assert.Empty(response.Errors);
        Assert.Equal(2, scope.Calls);
    }

    // El JWT identifica al actor y aporta un rol canónico, pero el rol efectivo
    // de la membresía SQL puede cambiar al seleccionar otra empresa. Mantener
    // las negativas de actor y tenant; no confundir rol SQL vigente con spoofing.
    [Theory]
    [InlineData(8, 11, 19)]
    [InlineData(7, 12, 19)]
    public async Task Obtener_ActorOTenantSqlNoCoincideConJwt_RechazaSinDatos(
        int actor, int tenant, int role)
    {
        var scope = new ScopeStub
        {
            Actual = new UsuarioTenantScopeActual(actor, tenant, role, "Operador", false)
        };
        var action = await WithJwtIdentity(scope).Obtener(11, CancellationToken.None);
        Assert.IsType<ForbidResult>(action);
        Assert.Equal(1, scope.Calls);
    }

    [Fact]
    public async Task Obtener_RolSqlVigenteDistintoAlJwt_UsaMembresiaEfectiva()
    {
        // Sólo prueba contrato controlador/DTO con principal sintético; la firma
        // JWT, autoridad SQL y aislamiento A/B requieren CI de integración real.
        var scope = new ScopeStub
        {
            Actual = new UsuarioTenantScopeActual(7, 11, 20, "Operador", false)
        };
        var action = await WithJwtIdentity(scope, "7", "19")
            .Obtener(11, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var response = Assert.IsType<ApiResponse<UsuarioTenantScopeActual>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(7, response.Data.UsuarioId);
        Assert.Equal(11, response.Data.EmpresaId);
        Assert.Equal(20, response.Data.RolId);
        Assert.Empty(response.Errors);
        Assert.Equal(2, scope.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Obtener_RevocacionOCambioRolDestinoDuranteRevalidacionBase_Deniega(
        bool revoke)
    {
        var scope = new ScopeStub
        {
            Actual = new UsuarioTenantScopeActual(7, 11, 20, "Operador", false)
        };
        scope.OnSecondBaseCheck = () => scope.Actual = revoke
            ? null
            : new UsuarioTenantScopeActual(7, 11, 21, "Nuevo rol", false);
        var action = await WithJwtIdentity(scope).Obtener(11, CancellationToken.None);
        Assert.IsType<ForbidResult>(action);
        Assert.Equal(2, scope.Calls);
    }

    [Theory]
    [InlineData("07", "19")]
    [InlineData("7", "019")]
    [InlineData("-7", "19")]
    [InlineData("7", "0")]
    public async Task Obtener_ClaimsNoCanonicos_NoConsultaMembresias(
        string actor, string role)
    {
        var scope = new ScopeStub();
        var action = await WithJwtIdentity(scope, actor, role)
            .Obtener(11, CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(action);
        Assert.Equal(0, scope.Calls);
    }

    [Fact]
    public async Task Obtener_DuplicacionDeActor_NoConsultaMembresias()
    {
        var scope = new ScopeStub();
        var controller = WithJwtIdentity(scope);
        ((ClaimsIdentity)controller.User.Identity!).AddClaim(new Claim("id", "8"));
        var action = await controller.Obtener(11, CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(action);
        Assert.Equal(0, scope.Calls);
    }


    [Theory]
    [InlineData("07", "19")]
    [InlineData("7", "019")]
    [InlineData("0", "19")]
    [InlineData("7", "0")]
    public async Task Selector_ClaimsNoCanonicos_RechazaSinConsultarCatalogo(
        string actor, string role)
    {
        var scope = new ScopeStub
        {
            Seleccionables = new[] { new EmpresaSeleccionable(11, "A") }
        };
        var action = await WithJwtIdentity(scope, actor, role)
            .ListarEmpresas(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(action);
        Assert.Equal(0, scope.SelectorCalls);
    }

    [Fact]
    public async Task Selector_RolDuplicado_RechazaSinConsultarCatalogo()
    {
        var scope = new ScopeStub
        {
            Seleccionables = new[] { new EmpresaSeleccionable(11, "A") }
        };
        var controller = WithJwtIdentity(scope);
        ((ClaimsIdentity)controller.User.Identity!).AddClaim(
            new Claim("rolId", "20"));

        var action = await controller.ListarEmpresas(CancellationToken.None);
        Assert.IsType<UnauthorizedObjectResult>(action);
        Assert.Equal(0, scope.SelectorCalls);
    }

    [Fact]
    public async Task RolBaseRevocado_NoConsultaNiExponeContexto()
    {
        var scope = new ScopeStub
        {
            BaseVigente = false,
            Actual = new UsuarioTenantScopeActual(7, 11, 20, "Operador", false),
            Seleccionables = new[] { new EmpresaSeleccionable(11, "A") }
        };
        var controller = WithJwtIdentity(scope);
        Assert.IsType<ForbidResult>(await controller.ListarEmpresas(CancellationToken.None));
        Assert.IsType<ForbidResult>(await controller.Obtener(11, CancellationToken.None));
        Assert.Equal(0, scope.SelectorCalls);
        Assert.Equal(0, scope.Calls);
    }

    [Fact]
    public async Task Selector_DtoContieneSoloLaListaDevueltaPorServicio()
    {
        var scope = new ScopeStub
        {
            Seleccionables = new[] { new EmpresaSeleccionable(11, "A") }
        };
        var action = await WithJwtIdentity(scope).ListarEmpresas(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<EmpresaSeleccionable>>>(ok.Value);
        Assert.True(response.Success);
        var empresa = Assert.Single(response.Data!);
        Assert.Equal(11, empresa.EmpresaId);
        Assert.Equal("A", empresa.Nombre);
    }
}
