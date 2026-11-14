using System.Globalization;
using System.Reflection;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

public class N04CatalogoPermisosRuntimeTests
{
    [Fact]
    public void AtributosRuntime_SoloExigenPermisosPresentesEnCatalogoBase()
    {
        var catalogo = CatalogoPermisosBase.Definicion
            .SelectMany(x => x.Acciones.Select(accion => (x.Modulo, Accion: accion)))
            .ToHashSet();

        var assemblyApi = typeof(RequierePermisoAttribute).Assembly;
        var exigidos = new List<(ModuloSistema Modulo, AccionPermiso Accion, string Origen)>();

        foreach (var controller in assemblyApi.GetTypes()
                     .Where(t => t.IsClass && !t.IsAbstract &&
                                 t.Namespace?.StartsWith("Solqaryn.API.Controllers", StringComparison.Ordinal) == true))
        {
            exigidos.AddRange(ExtraerPermisos(controller, controller.FullName ?? controller.Name));

            foreach (var metodo in controller.GetMethods(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                exigidos.AddRange(ExtraerPermisos(
                    metodo,
                    $"{controller.FullName ?? controller.Name}.{metodo.Name}"));
            }
        }

        var faltantes = exigidos
            .Where(x => !catalogo.Contains((x.Modulo, x.Accion)))
            .Distinct()
            .OrderBy(x => x.Modulo)
            .ThenBy(x => x.Accion)
            .ThenBy(x => x.Origen)
            .ToList();

        Assert.True(
            faltantes.Count == 0,
            "Hay permisos exigidos por atributos runtime que no pueden seedearse/concederse desde CatalogoPermisosBase: " +
            string.Join(", ", faltantes.Select(x => $"{x.Modulo}:{x.Accion} ({x.Origen})")));
    }

    [Fact]
    public void Facturacion_IncluyeAccionesRequeridasPorControllersRuntime()
    {
        var acciones = CatalogoPermisosBase.Definicion
            .Single(x => x.Modulo == ModuloSistema.Facturacion)
            .Acciones;

        foreach (var accion in new[]
                 {
                     AccionPermiso.Ver,
                     AccionPermiso.Exportar,
                     AccionPermiso.Imprimir,
                     AccionPermiso.Compartir,
                     AccionPermiso.Administrar,
                     AccionPermiso.Aplicar,
                     AccionPermiso.Anular,
                     AccionPermiso.CambiarEstado
                 })
        {
            Assert.Contains(accion, acciones);
        }
    }

    [Fact]
    public void MovimientosInventario_IncluyeCerrarParaConteosFisicos()
    {
        var acciones = CatalogoPermisosBase.Definicion
            .Single(x => x.Modulo == ModuloSistema.MovimientosInventario)
            .Acciones;

        Assert.Contains(AccionPermiso.Cerrar, acciones);
    }

    private static IEnumerable<(ModuloSistema Modulo, AccionPermiso Accion, string Origen)> ExtraerPermisos(
        MemberInfo miembro,
        string origen)
    {
        foreach (var atributo in miembro.CustomAttributes.Where(a =>
                     a.AttributeType == typeof(RequierePermisoAttribute) ||
                     a.AttributeType == typeof(RequiereAlgunoPermisoAttribute)))
        {
            var modulo = (ModuloSistema)Convert.ToInt32(
                atributo.ConstructorArguments[0].Value,
                CultureInfo.InvariantCulture);

            if (atributo.AttributeType == typeof(RequierePermisoAttribute))
            {
                yield return (
                    modulo,
                    (AccionPermiso)Convert.ToInt32(
                        atributo.ConstructorArguments[1].Value,
                        CultureInfo.InvariantCulture),
                    origen);
                continue;
            }

            if (atributo.ConstructorArguments[1].Value is not IEnumerable<CustomAttributeTypedArgument> acciones)
                throw new InvalidOperationException($"No se pudo leer RequiereAlgunoPermisoAttribute en {origen}.");

            foreach (var accion in acciones)
            {
                yield return (
                    modulo,
                    (AccionPermiso)Convert.ToInt32(accion.Value, CultureInfo.InvariantCulture),
                    origen);
            }
        }
    }
}
