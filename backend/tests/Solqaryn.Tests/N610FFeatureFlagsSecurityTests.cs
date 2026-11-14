using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// N6.10.F security contract for SaaS feature flags. These tests lock the API
/// surface fail-closed: authenticated access plus tenant-scoped relational
/// permissions are mandatory for every SaaS endpoint, including entitlements.
/// </summary>
public sealed class N610FFeatureFlagsSecurityTests
{
    [Fact]
    public void Controller_RequiresAuthenticatedUser()
    {
        Assert.NotNull(typeof(SuscripcionesSaaSController)
            .GetCustomAttribute<AuthorizeAttribute>());
    }

    [Theory]
    [InlineData(nameof(SuscripcionesSaaSController.Onboarding), AccionPermiso.Crear)]
    [InlineData(nameof(SuscripcionesSaaSController.ObtenerActual), AccionPermiso.Ver)]
    [InlineData(nameof(SuscripcionesSaaSController.ObtenerLimites), AccionPermiso.Ver)]
    [InlineData(nameof(SuscripcionesSaaSController.EvaluarModulo), AccionPermiso.Ver)]
    public void Endpoints_RequireConfiguracionPermission(string methodName, AccionPermiso expectedAction)
    {
        var method = typeof(SuscripcionesSaaSController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(x => x.Name == methodName);

        var attribute = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(attribute);

        var moduloField = typeof(RequierePermisoAttribute)
            .GetField("_modulo", BindingFlags.Instance | BindingFlags.NonPublic);
        var accionField = typeof(RequierePermisoAttribute)
            .GetField("_accion", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(moduloField);
        Assert.NotNull(accionField);
        Assert.Equal(ModuloSistema.Configuracion, (ModuloSistema)moduloField!.GetValue(attribute)!);
        Assert.Equal(expectedAction, (AccionPermiso)accionField!.GetValue(attribute)!);
    }
}
