using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N31CotizacionApiContractTests
{
    [Fact]
    public void Controller_usa_servicio_inyectado_explicito()
    {
        var constructor = Assert.Single(typeof(CotizacionesController).GetConstructors());
        var parametro = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(ICotizacionService), parametro.ParameterType);
    }

    [Theory]
    [InlineData(nameof(CotizacionesController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(CotizacionesController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(CotizacionesController.Crear), AccionPermiso.Crear)]
    [InlineData(nameof(CotizacionesController.Actualizar), AccionPermiso.Editar)]
    [InlineData(nameof(CotizacionesController.Eliminar), AccionPermiso.EliminarPermanente)]
    [InlineData(nameof(CotizacionesController.Enviar), AccionPermiso.Confirmar)]
    [InlineData(nameof(CotizacionesController.Aceptar), AccionPermiso.Aprobar)]
    [InlineData(nameof(CotizacionesController.Rechazar), AccionPermiso.Rechazar)]
    [InlineData(nameof(CotizacionesController.Convertir), AccionPermiso.Confirmar)]
    [InlineData(nameof(CotizacionesController.Duplicar), AccionPermiso.Duplicar)]
    public void Endpoints_mantienen_rbac_ventas_exacto(string metodo, AccionPermiso accionEsperada)
    {
        var method = typeof(CotizacionesController).GetMethod(metodo);
        Assert.NotNull(method);

        var permiso = Assert.Single(method!.CustomAttributes.Where(x => x.AttributeType == typeof(RequierePermisoAttribute)));
        Assert.Equal(2, permiso.ConstructorArguments.Count);
        Assert.Equal(ModuloSistema.Ventas, (ModuloSistema)permiso.ConstructorArguments[0].Value!);
        Assert.Equal(accionEsperada, (AccionPermiso)permiso.ConstructorArguments[1].Value!);
    }

    [Fact]
    public void Controller_y_endpoints_no_permiten_bypass_anonimo()
    {
        var type = typeof(CotizacionesController);

        Assert.NotNull(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).SingleOrDefault());
        Assert.Empty(type.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));

        foreach (var method in type.GetMethods().Where(x => x.DeclaringType == type))
            Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }

    [Fact]
    public void CatalogoVentas_incluye_todos_los_permisos_runtime_de_cotizaciones()
    {
        var acciones = Assert.Single(
            CatalogoPermisosBase.Definicion.Where(x => x.Modulo == ModuloSistema.Ventas)).Acciones;

        foreach (var accion in new[]
        {
            AccionPermiso.Ver,
            AccionPermiso.Crear,
            AccionPermiso.Editar,
            AccionPermiso.EliminarPermanente,
            AccionPermiso.Confirmar,
            AccionPermiso.Aprobar,
            AccionPermiso.Rechazar,
            AccionPermiso.Duplicar
        })
        {
            Assert.Contains(accion, acciones);
        }
    }
}
