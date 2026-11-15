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

        public Task<UsuarioScopeActual?> ObtenerActualAsync() =>
            Task.FromResult<UsuarioScopeActual?>(null);

        public Task<UsuarioTenantScopeActual?> ObtenerActualAsync(
            int empresaId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Actual);
        }

        public Task<IReadOnlyList<EmpresaSeleccionable>> ListarEmpresasSeleccionablesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Seleccionables);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Obtener_IdInvalido_RechazaAntesDeConsultarMembresias(int empresaId)
    {
        var scope = new ScopeStub { Actual = new UsuarioTenantScopeActual(7, 11, 19, "Operador", false) };
        var action = await new TenantContextController(scope).Obtener(empresaId, CancellationToken.None);

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
        var action = await new TenantContextController(scope).Obtener(22, CancellationToken.None);

        Assert.IsType<ForbidResult>(action);
        Assert.Equal(1, scope.Calls);
    }

    [Fact]
    public async Task Obtener_ConMembresiaVigente_DevuelveDtoVerificado()
    {
        var scope = new ScopeStub { Actual = new UsuarioTenantScopeActual(7, 11, 19, "Operador", false) };
        var action = await new TenantContextController(scope).Obtener(11, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var response = Assert.IsType<ApiResponse<UsuarioTenantScopeActual>>(ok.Value);
        Assert.True(response.Success);
        Assert.NotNull(response.Data);
        Assert.Equal(7, response.Data.UsuarioId);
        Assert.Equal(11, response.Data.EmpresaId);
        Assert.Equal(19, response.Data.RolId);
        Assert.Empty(response.Errors);
        Assert.Equal(1, scope.Calls);
    }

    [Fact]
    public async Task Selector_DtoContieneSoloLaListaDevueltaPorServicio()
    {
        var scope = new ScopeStub
        {
            Seleccionables = new[] { new EmpresaSeleccionable(11, "A") }
        };
        var action = await new TenantContextController(scope).ListarEmpresas(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action);
        var response = Assert.IsType<ApiResponse<IReadOnlyList<EmpresaSeleccionable>>>(ok.Value);
        Assert.True(response.Success);
        var empresa = Assert.Single(response.Data!);
        Assert.Equal(11, empresa.EmpresaId);
        Assert.Equal("A", empresa.Nombre);
    }
}
