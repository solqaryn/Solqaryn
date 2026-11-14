using System.Reflection;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N16TransferenciaSecurityContractTests
{
    [Fact]
    public void Controller_RequiereAutenticacionGlobal()
    {
        Assert.NotNull(typeof(TransferenciasInventarioController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void Controller_ExponeAuditoriaComoDependenciaInyectable()
    {
        var constructor = Assert.Single(typeof(TransferenciasInventarioController).GetConstructors());
        var parametroAuditoria = Assert.Single(
            constructor.GetParameters().Where(x => x.ParameterType == typeof(IAuditoriaService)));

        Assert.True(parametroAuditoria.HasDefaultValue);
        Assert.Null(parametroAuditoria.DefaultValue);
    }

    [Theory]
    [InlineData(nameof(TransferenciasInventarioController.Buscar), AccionPermiso.Ver)]
    [InlineData(nameof(TransferenciasInventarioController.GetById), AccionPermiso.Ver)]
    [InlineData(nameof(TransferenciasInventarioController.Create), AccionPermiso.Crear)]
    [InlineData(nameof(TransferenciasInventarioController.Update), AccionPermiso.Editar)]
    [InlineData(nameof(TransferenciasInventarioController.Solicitar), AccionPermiso.CambiarEstado)]
    [InlineData(nameof(TransferenciasInventarioController.Aprobar), AccionPermiso.Aprobar)]
    [InlineData(nameof(TransferenciasInventarioController.Despachar), AccionPermiso.Confirmar)]
    [InlineData(nameof(TransferenciasInventarioController.Recibir), AccionPermiso.Confirmar)]
    [InlineData(nameof(TransferenciasInventarioController.Cancelar), AccionPermiso.Anular)]
    public void Endpoints_UsanPermisoRelacionalDeMovimientosInventario(string metodo, AccionPermiso accionEsperada)
    {
        var method = typeof(TransferenciasInventarioController).GetMethod(metodo)
            ?? throw new InvalidOperationException($"No existe el endpoint {metodo}.");
        var permiso = method.GetCustomAttribute<RequierePermisoAttribute>();

        Assert.NotNull(permiso);

        var modulo = (ModuloSistema)typeof(RequierePermisoAttribute)
            .GetField("_modulo", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(permiso)!;
        var accion = (AccionPermiso)typeof(RequierePermisoAttribute)
            .GetField("_accion", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(permiso)!;

        Assert.Equal(ModuloSistema.MovimientosInventario, modulo);
        Assert.Equal(accionEsperada, accion);
    }

    [Fact]
    public void Lifecycle_NoDegradaOperacionesSensiblesAPermisoGenerico()
    {
        var sensibles = new[]
        {
            nameof(TransferenciasInventarioController.Aprobar),
            nameof(TransferenciasInventarioController.Despachar),
            nameof(TransferenciasInventarioController.Recibir),
            nameof(TransferenciasInventarioController.Cancelar)
        };

        foreach (var metodo in sensibles)
        {
            var permiso = typeof(TransferenciasInventarioController)
                .GetMethod(metodo)!
                .GetCustomAttribute<RequierePermisoAttribute>()!;
            var accion = (AccionPermiso)typeof(RequierePermisoAttribute)
                .GetField("_accion", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(permiso)!;

            Assert.DoesNotContain(accion, new[] { AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar });
        }
    }
}
