using Solqaryn.API.Controllers;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P2-001.D: directed test for the two new commercial read routes.
/// Delegates to the existing P2-001.C metadata assertions; does not prove JWT/HTTP runtime.
/// </summary>
public sealed class P2001DProveedoresComprasLecturaTests
{
    [Theory]
    [InlineData(typeof(ProveedoresController), nameof(ProveedoresController.GetAll),
        "proveedores", ModuloSistema.Proveedores)]
    [InlineData(typeof(ComprasController), nameof(ComprasController.GetPaged),
        "compras", ModuloSistema.Compras)]
    public void Proveedores_y_compras_exigen_autorizacion_y_permiso_Ver(
        Type controlador, string accion, string ruta, ModuloSistema modulo)
    {
        new P2001CMatrizComercialLecturaTests()
            .Lectura_comercial_no_puede_perder_autenticacion_o_permiso(
                controlador, accion, ruta, modulo);
    }
}
