using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N32PedidoVentaApiContractTests
{
    [Fact]
    public void Controller_usa_servicio_inyectado_explicito()
    {
        var constructor = Assert.Single(typeof(PedidosVentaController).GetConstructors());
        var parametro = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(IPedidoVentaService), parametro.ParameterType);
    }

    [Theory]
    [InlineData(nameof(PedidosVentaController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(PedidosVentaController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(PedidosVentaController.Crear), AccionPermiso.Crear)]
    [InlineData(nameof(PedidosVentaController.Actualizar), AccionPermiso.Editar)]
    [InlineData(nameof(PedidosVentaController.Confirmar), AccionPermiso.Confirmar)]
    [InlineData(nameof(PedidosVentaController.Anular), AccionPermiso.Anular)]
    public void Endpoints_mantienen_rbac_ventas_exacto(string metodo, AccionPermiso accionEsperada)
    {
        var method = typeof(PedidosVentaController).GetMethod(metodo);
        Assert.NotNull(method);

        var permiso = Assert.Single(method!.CustomAttributes.Where(x => x.AttributeType == typeof(RequierePermisoAttribute)));
        Assert.Equal(2, permiso.ConstructorArguments.Count);
        Assert.Equal(ModuloSistema.Ventas, (ModuloSistema)permiso.ConstructorArguments[0].Value!);
        Assert.Equal(accionEsperada, (AccionPermiso)permiso.ConstructorArguments[1].Value!);
    }

    [Fact]
    public void Controller_y_endpoints_no_permiten_bypass_anonimo()
    {
        var type = typeof(PedidosVentaController);

        Assert.NotNull(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.Empty(type.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));

        foreach (var method in type.GetMethods().Where(x => x.DeclaringType == type))
            Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }

    [Fact]
    public void Crear_exige_header_idempotency_key()
    {
        var method = typeof(PedidosVentaController).GetMethod(nameof(PedidosVentaController.Crear));
        Assert.NotNull(method);

        var header = Assert.Single(method!.GetParameters()[1].CustomAttributes.Where(x => x.AttributeType == typeof(FromHeaderAttribute)));
        var name = header.NamedArguments.Single(x => x.MemberName == nameof(FromHeaderAttribute.Name)).TypedValue.Value;

        Assert.Equal("Idempotency-Key", name);
    }

    [Fact]
    public void CatalogoVentas_incluye_permisos_runtime_de_pedidos()
    {
        var acciones = Assert.Single(
            CatalogoPermisosBase.Definicion.Where(x => x.Modulo == ModuloSistema.Ventas)).Acciones;

        foreach (var accion in new[]
        {
            AccionPermiso.Ver,
            AccionPermiso.Crear,
            AccionPermiso.Editar,
            AccionPermiso.Confirmar,
            AccionPermiso.Anular
        })
        {
            Assert.Contains(accion, acciones);
        }
    }
}
