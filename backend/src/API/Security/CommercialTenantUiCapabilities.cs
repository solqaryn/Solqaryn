using Microsoft.Extensions.Configuration;
using Solqaryn.Application.DTOs;

namespace Solqaryn.API.Security;

/// <summary>
/// Proyección estricta de acciones/rutas visibles en Angular durante la cuarentena.
/// Se obtiene solo de flags existentes y NO autoriza rutas ni modifica el middleware.
/// La comprobación efectiva sigue en JWT, membresía, RBAC y guard de cada API.
/// </summary>
public static class CommercialTenantUiCapabilities
{
    public static void Apply(MisPermisosDto dto, IConfiguration config, bool multipleEmpresas)
    {
        dto.OperacionMultiEmpresaRestringida = multipleEmpresas;
        dto.CapacidadesHabilitadas = new List<string>();
        dto.RutasHabilitadas = new List<string>();
        if (!multipleEmpresas)
            return;

        var capacidades = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rutas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Habilitar(string modulo, string accion, params string[] angularRoutes)
        {
            capacidades.Add($"{modulo}:{accion}");
            foreach (var ruta in angularRoutes) rutas.Add(ruta);
        }

        // La lista UI es deliberadamente MENOR que la allowlist de API.
        // Sólo se anuncian rutas Angular con operación principal certificada,
        // nunca CRUD/exports, reports o jobs por inferencia de ser administrador.
        if (config.GetValue<bool>("MultiEmpresa:PrivateReadPilotEnabled"))
        {
            Habilitar("Productos", "Ver", "productos", "productos/:id");
            Habilitar("Clientes", "Ver", "clientes");
            Habilitar("Proveedores", "Ver", "proveedores");
            Habilitar("TiposClientes", "Ver", "tipo-clientes");
            Habilitar("Ventas", "Ver", "ventas", "ventas/:id");
            Habilitar("Compras", "Ver", "compras", "compras/:id");
            Habilitar("Facturacion", "Ver", "facturas/:id");
        }

        if (config.GetValue<bool>("MultiEmpresa:PrivateWritePilotEnabled"))
        {
            Habilitar("Clientes", "Crear", "clientes/nuevo");
            Habilitar("Proveedores", "Crear", "proveedores/nuevo");
            Habilitar("TiposClientes", "Crear", "tipo-clientes/nuevo");
        }

        if (config.GetValue<bool>("MultiEmpresa:PrivateMasterEditPilotEnabled"))
        {
            Habilitar("Clientes", "Editar", "clientes/:id/editar");
            Habilitar("Proveedores", "Editar", "proveedores/:id/editar");
        }

        if (config.GetValue<bool>("MultiEmpresa:PrivateDraftPilotEnabled"))
        {
            Habilitar("Productos", "Crear", "productos/nuevo");
            Habilitar("Ventas", "Crear", "ventas/nueva");
            Habilitar("Compras", "Crear", "compras/nueva");
        }

        if (config.GetValue<bool>("MultiEmpresa:PrivateInventarioReadPilotEnabled"))
            Habilitar("MovimientosInventario", "Ver", "inventario/movimientos");

        // Confirmación, pagos, PDF, descarga de media, reports y jobs son
        // capacidades de API puntuales: no equivalen a habilitar toda su UI.
        dto.CapacidadesHabilitadas = capacidades.OrderBy(x => x, StringComparer.Ordinal).ToList();
        dto.RutasHabilitadas = rutas.OrderBy(x => x, StringComparer.Ordinal).ToList();
    }
}
