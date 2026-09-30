using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N710FFacturacionFiscalSecurityAuditTests
{
    [Fact]
    public void Controller_Remains_Authenticated_And_Permission_Gated()
    {
        var type = typeof(FacturacionFiscalController);

        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(type.GetCustomAttribute<AllowAnonymousAttribute>());

        var permiso = type.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permiso);

        var moduloField = typeof(RequierePermisoAttribute)
            .GetField("_modulo", BindingFlags.NonPublic | BindingFlags.Instance);
        var accionField = typeof(RequierePermisoAttribute)
            .GetField("_accion", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Equal(ModuloSistema.Facturacion, (ModuloSistema?)moduloField?.GetValue(permiso));
        Assert.Equal(AccionPermiso.Crear, (AccionPermiso?)accionField?.GetValue(permiso));

        var emitir = type.GetMethod(nameof(FacturacionFiscalController.Emitir));
        Assert.NotNull(emitir);
        Assert.NotNull(emitir!.GetCustomAttribute<HttpPostAttribute>());
        Assert.Null(emitir.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Controller_Requires_TenantScope_And_Audit_Dependencies()
    {
        var constructor = Assert.Single(typeof(FacturacionFiscalController).GetConstructors());
        var parameterTypes = constructor.GetParameters().Select(x => x.ParameterType).ToArray();

        Assert.Contains(typeof(IUsuarioScopeService), parameterTypes);
        Assert.Contains(typeof(IAuditoriaService), parameterTypes);
    }
}
