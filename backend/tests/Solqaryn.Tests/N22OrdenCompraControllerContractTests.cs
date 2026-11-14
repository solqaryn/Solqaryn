using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N22OrdenCompraControllerContractTests
{
    [Fact]
    public void Controller_exige_autenticacion_y_ruta_canonica()
    {
        var type = typeof(OrdenesCompraController);
        Assert.NotNull(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true).SingleOrDefault());
        Assert.NotNull(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        Assert.Equal("ordenes-compra", route.Template);
    }

    public static IEnumerable<object?[]> AccionesProtegidas()
    {
        yield return new object?[] { nameof(OrdenesCompraController.Buscar), typeof(HttpGetAttribute), null, AccionPermiso.Ver };
        yield return new object?[] { nameof(OrdenesCompraController.GetById), typeof(HttpGetAttribute), "{id:int}", AccionPermiso.Ver };
        yield return new object?[] { nameof(OrdenesCompraController.Create), typeof(HttpPostAttribute), null, AccionPermiso.Crear };
        yield return new object?[] { nameof(OrdenesCompraController.Update), typeof(HttpPutAttribute), "{id:int}", AccionPermiso.Editar };
        yield return new object?[] { nameof(OrdenesCompraController.EnviarAprobacion), typeof(HttpPostAttribute), "{id:int}/enviar-aprobacion", AccionPermiso.Confirmar };
        yield return new object?[] { nameof(OrdenesCompraController.Aprobar), typeof(HttpPostAttribute), "{id:int}/aprobar", AccionPermiso.Aprobar };
        yield return new object?[] { nameof(OrdenesCompraController.Cancelar), typeof(HttpPostAttribute), "{id:int}/cancelar", AccionPermiso.Anular };
    }

    [Theory]
    [MemberData(nameof(AccionesProtegidas))]
    public void Cada_endpoint_conserva_verbo_ruta_y_permiso_compras(
        string methodName,
        Type httpAttributeType,
        string? template,
        AccionPermiso accion)
    {
        var method = typeof(OrdenesCompraController).GetMethod(methodName);
        Assert.NotNull(method);
        var http = Assert.Single(method!.GetCustomAttributes(inherit: true)
            .OfType<HttpMethodAttribute>()
            .Where(attribute => attribute.GetType() == httpAttributeType));
        Assert.Equal(template, http.Template);

        var permiso = Assert.Single(method.CustomAttributes
            .Where(attribute => attribute.AttributeType == typeof(RequierePermisoAttribute)));
        Assert.Equal((int)ModuloSistema.Compras, Convert.ToInt32(permiso.ConstructorArguments[0].Value));
        Assert.Equal((int)accion, Convert.ToInt32(permiso.ConstructorArguments[1].Value));
    }

    [Fact]
    public void Crear_exige_header_idempotency_key()
    {
        var method = typeof(OrdenesCompraController).GetMethod(nameof(OrdenesCompraController.Create));
        Assert.NotNull(method);
        var parameter = Assert.Single(method!.GetParameters().Where(p => p.Name == "idempotencyKey"));
        var header = Assert.Single(parameter.GetCustomAttributes(typeof(FromHeaderAttribute), inherit: true).Cast<FromHeaderAttribute>());
        Assert.Equal("Idempotency-Key", header.Name);
    }

    [Fact]
    public void Ningun_endpoint_publico_puede_degradarse_a_allow_anonymous()
    {
        var methods = typeof(OrdenesCompraController)
            .GetMethods()
            .Where(method => method.DeclaringType == typeof(OrdenesCompraController) && method.IsPublic);
        Assert.DoesNotContain(methods, method => method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).Any());
    }

    [Fact]
    public void API_no_expone_recepcion_ni_mutaciones_de_inventario_en_N22()
    {
        var nombres = typeof(OrdenesCompraController).GetMethods()
            .Where(m => m.DeclaringType == typeof(OrdenesCompraController))
            .Select(m => m.Name)
            .ToArray();
        Assert.DoesNotContain(nombres, n => n.Contains("Recibir", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, n => n.Contains("Inventario", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, n => n.Contains("Kardex", StringComparison.OrdinalIgnoreCase));
    }
}
