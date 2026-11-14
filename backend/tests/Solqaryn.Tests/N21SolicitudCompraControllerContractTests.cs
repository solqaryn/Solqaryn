using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N21SolicitudCompraControllerContractTests
{
    [Fact]
    public void Controller_exige_autenticacion_y_ruta_canonica()
    {
        var type = typeof(SolicitudesCompraController);

        Assert.NotNull(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true).SingleOrDefault());
        Assert.NotNull(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        Assert.Equal("solicitudes-compra", route.Template);
    }

    public static IEnumerable<object?[]> AccionesProtegidas()
    {
        yield return new object?[] { nameof(SolicitudesCompraController.Buscar), typeof(HttpGetAttribute), null, AccionPermiso.Ver };
        yield return new object?[] { nameof(SolicitudesCompraController.GetById), typeof(HttpGetAttribute), "{id:int}", AccionPermiso.Ver };
        yield return new object?[] { nameof(SolicitudesCompraController.Create), typeof(HttpPostAttribute), null, AccionPermiso.Crear };
        yield return new object?[] { nameof(SolicitudesCompraController.Update), typeof(HttpPutAttribute), "{id:int}", AccionPermiso.Editar };
        yield return new object?[] { nameof(SolicitudesCompraController.Enviar), typeof(HttpPostAttribute), "{id:int}/enviar", AccionPermiso.Confirmar };
        yield return new object?[] { nameof(SolicitudesCompraController.Aprobar), typeof(HttpPostAttribute), "{id:int}/aprobar", AccionPermiso.Aprobar };
        yield return new object?[] { nameof(SolicitudesCompraController.Rechazar), typeof(HttpPostAttribute), "{id:int}/rechazar", AccionPermiso.Rechazar };
    }

    [Theory]
    [MemberData(nameof(AccionesProtegidas))]
    public void Cada_endpoint_conserva_verbo_ruta_y_permiso_compras(
        string methodName,
        Type httpAttributeType,
        string? template,
        AccionPermiso accion)
    {
        var method = typeof(SolicitudesCompraController).GetMethod(methodName);
        Assert.NotNull(method);

        var http = Assert.Single(method!.GetCustomAttributes(inherit: true)
            .OfType<HttpMethodAttribute>()
            .Where(attribute => attribute.GetType() == httpAttributeType));
        Assert.Equal(template, http.Template);

        var permiso = Assert.Single(method.CustomAttributes
            .Where(attribute => attribute.AttributeType == typeof(RequierePermisoAttribute)));
        Assert.Equal(2, permiso.ConstructorArguments.Count);
        Assert.Equal((int)ModuloSistema.Compras, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)accion, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Fact]
    public void Ningun_endpoint_publico_puede_degradarse_a_allow_anonymous()
    {
        var methods = typeof(SolicitudesCompraController)
            .GetMethods()
            .Where(method => method.DeclaringType == typeof(SolicitudesCompraController) && method.IsPublic);

        Assert.DoesNotContain(methods,
            method => method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Any());
    }
}
