using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public class N48FAuthorizationPolicyDriftTests
{
    private static void AssertRequiresAuthorizeAndPermiso(MethodInfo method, ModuloSistema expectedModulo, AccionPermiso expectedAccion)
    {
        var classAuthorize = method.DeclaringType?.GetCustomAttribute<AuthorizeAttribute>();
        var methodAuthorize = method.GetCustomAttribute<AuthorizeAttribute>();
        var allowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>();

        Assert.Null(allowAnonymous);
        Assert.True(classAuthorize != null || methodAuthorize != null, $"Method {method.Name} on {method.DeclaringType?.Name} must have [Authorize]");

        var methodPermiso = method.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(methodPermiso);

        var moduloField = typeof(RequierePermisoAttribute).GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute).GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);

        var actualModulo = (ModuloSistema?)moduloField?.GetValue(methodPermiso);
        var actualAccion = (AccionPermiso?)accionField?.GetValue(methodPermiso);

        Assert.Equal(expectedModulo, actualModulo);
        Assert.Equal(expectedAccion, actualAccion);
    }

    [Fact]
    public void AsientosContablesController_Endpoints_HaveCorrectPolicies()
    {
        var type = typeof(AsientosContablesController);

        AssertRequiresAuthorizeAndPermiso(type.GetMethod("GetAll")!, ModuloSistema.Finanzas, AccionPermiso.Ver);
        AssertRequiresAuthorizeAndPermiso(type.GetMethod("GetById")!, ModuloSistema.Finanzas, AccionPermiso.Ver);
        AssertRequiresAuthorizeAndPermiso(type.GetMethod("Create")!, ModuloSistema.Finanzas, AccionPermiso.Crear);
    }

    [Fact]
    public void ContabilizacionController_Endpoints_HaveCorrectPolicies()
    {
        var type = typeof(ContabilizacionController);

        AssertRequiresAuthorizeAndPermiso(type.GetMethod("Contabilizar")!, ModuloSistema.Finanzas, AccionPermiso.Crear);
    }

    [Fact]
    public void CuentaContableController_Endpoints_HaveCorrectPolicies()
    {
        var type = typeof(CuentaContableController);

        AssertRequiresAuthorizeAndPermiso(type.GetMethod("GetAll")!, ModuloSistema.Finanzas, AccionPermiso.Ver);
        AssertRequiresAuthorizeAndPermiso(type.GetMethod("GetRaices")!, ModuloSistema.Finanzas, AccionPermiso.Ver);
        AssertRequiresAuthorizeAndPermiso(type.GetMethod("GetById")!, ModuloSistema.Finanzas, AccionPermiso.Ver);
        AssertRequiresAuthorizeAndPermiso(type.GetMethod("Create")!, ModuloSistema.Finanzas, AccionPermiso.Crear);
        AssertRequiresAuthorizeAndPermiso(type.GetMethod("Update")!, ModuloSistema.Finanzas, AccionPermiso.Editar);
    }
}
