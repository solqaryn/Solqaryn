using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class ReportesInventarioKardexSecurityContractTests
{
    [Fact]
    public void Controller_RequiereAutenticacion()
    {
        Assert.NotNull(typeof(ReportesInventarioKardexController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void Get_RequierePermisoConsultarHistorial()
    {
        var method = typeof(ReportesInventarioKardexController).GetMethod(nameof(ReportesInventarioKardexController.Get));
        Assert.NotNull(method);

        var permiso = method!.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.MovimientosInventario, (ModuloSistema?)moduloField!.GetValue(permiso));
        Assert.Equal(AccionPermiso.ConsultarHistorial, (AccionPermiso?)accionField!.GetValue(permiso));
    }

    [Fact]
    public void Constructor_ExigeAuditoriaYScopeServerSide()
    {
        var constructor = Assert.Single(typeof(ReportesInventarioKardexController).GetConstructors());
        var parameterTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();

        Assert.Contains(typeof(IUsuarioScopeService), parameterTypes);
        Assert.Contains(typeof(IAuditoriaService), parameterTypes);
    }

    [Fact]
    public async Task ScopeFisicoExplicitoSinScopeResuelto_FallaCerrado()
    {
        var filtro = new ReporteInventarioKardexFiltroDto { AlmacenId = 7 };

        var permitido = await ReporteInventarioScopeGuard.CanUseExplicitPhysicalScopeAsync(
            filtro,
            new NullScopeService());

        Assert.False(permitido);
    }

    private sealed class NullScopeService : IUsuarioScopeService
    {
        public Task<UsuarioScopeActual?> ObtenerActualAsync() => Task.FromResult<UsuarioScopeActual?>(null);
    }
}
