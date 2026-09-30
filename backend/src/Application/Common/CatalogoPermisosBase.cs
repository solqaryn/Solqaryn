using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Common;

/// <summary>
/// Define el catálogo de permisos de sistema que debe existir en base de datos.
/// No autoriza solicitudes: la autorización efectiva depende exclusivamente de
/// RolPermiso -> Permiso persistidos.
/// </summary>
public static class CatalogoPermisosBase
{
    private static readonly AccionPermiso[] AccionesMantenimiento =
    {
        AccionPermiso.Ver,
        AccionPermiso.Crear,
        AccionPermiso.Editar,
        AccionPermiso.Activar,
        AccionPermiso.Desactivar,
        AccionPermiso.EliminarLogico
    };

    public static readonly AccionPermiso[] AccionesRbacRequeridas =
    {
        AccionPermiso.Ver,
        AccionPermiso.Crear,
        AccionPermiso.Editar,
        AccionPermiso.EliminarLogico,
        AccionPermiso.Confirmar,
        AccionPermiso.Anular,
        AccionPermiso.Aprobar,
        AccionPermiso.Rechazar,
        AccionPermiso.Imprimir,
        AccionPermiso.Exportar,
        AccionPermiso.Importar,
        AccionPermiso.Administrar,
        AccionPermiso.Cerrar,
        AccionPermiso.Reabrir
    };

    public static readonly (ModuloSistema Modulo, AccionPermiso[] Acciones)[] Definicion =
    {
        (ModuloSistema.Dashboard, new[] { AccionPermiso.Ver }),
        (ModuloSistema.Productos, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar, AccionPermiso.Actualizar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Exportar, AccionPermiso.Duplicar
        }),
        (ModuloSistema.Categorias, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente
        }),
        (ModuloSistema.Colores, AccionesMantenimiento),
        (ModuloSistema.Tallas, AccionesMantenimiento),
        (ModuloSistema.Marcas, AccionesMantenimiento),
        (ModuloSistema.Modelos, AccionesMantenimiento),
        (ModuloSistema.MetodosPago, AccionesMantenimiento),
        (ModuloSistema.Sucursales, AccionesMantenimiento),
        (ModuloSistema.Almacenes, AccionesMantenimiento),
        (ModuloSistema.UbicacionesAlmacen, AccionesMantenimiento),
        (ModuloSistema.Clientes, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.ConsultarHistorial
        }),
        (ModuloSistema.TiposClientes, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.ConsultarHistorial
        }),
        (ModuloSistema.Proveedores, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.ConsultarHistorial
        }),
        (ModuloSistema.Compras, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Confirmar, AccionPermiso.Anular, AccionPermiso.Aprobar, AccionPermiso.Rechazar,
            AccionPermiso.Cerrar, AccionPermiso.Reabrir, AccionPermiso.EliminarLogico,
            AccionPermiso.Exportar, AccionPermiso.Imprimir, AccionPermiso.ConsultarHistorial
        }),
        (ModuloSistema.Ventas, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Confirmar, AccionPermiso.Anular, AccionPermiso.Aprobar, AccionPermiso.Rechazar,
            AccionPermiso.Cerrar, AccionPermiso.Reabrir, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Duplicar,
            AccionPermiso.Exportar, AccionPermiso.Imprimir, AccionPermiso.ConsultarHistorial,
            AccionPermiso.ExonerarEnvio
        }),
        (ModuloSistema.Facturacion, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Exportar, AccionPermiso.Imprimir,
            AccionPermiso.Compartir, AccionPermiso.Administrar, AccionPermiso.Aplicar,
            AccionPermiso.Anular, AccionPermiso.CambiarEstado
        }),
        (ModuloSistema.Finanzas, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.Anular, AccionPermiso.Aprobar, AccionPermiso.Exportar,
            AccionPermiso.Importar, AccionPermiso.Imprimir, AccionPermiso.Administrar,
            AccionPermiso.Cerrar, AccionPermiso.Reabrir
        }),
        (ModuloSistema.Inventario, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Confirmar, AccionPermiso.Anular, AccionPermiso.Exportar
        }),
        (ModuloSistema.MovimientosInventario, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Confirmar, AccionPermiso.Anular, AccionPermiso.Aprobar,
            AccionPermiso.Cerrar, AccionPermiso.CambiarEstado, AccionPermiso.Exportar,
            AccionPermiso.ConsultarHistorial
        }),
        (ModuloSistema.InsumosAdministrativos, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.AjustarStock, AccionPermiso.RegistrarConsumo,
            AccionPermiso.ConsultarHistorial, AccionPermiso.Exportar
        }),
        (ModuloSistema.CargasMasivas, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Importar, AccionPermiso.Confirmar,
            AccionPermiso.Exportar, AccionPermiso.ConsultarHistorial
        }),
        (ModuloSistema.Usuarios, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.AsignarRol,
            AccionPermiso.RestablecerContrasena, AccionPermiso.CambiarEstado,
            AccionPermiso.EliminarLogico
        }),
        (ModuloSistema.Roles, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Duplicar, AccionPermiso.ConsultarHistorial
        }),
        (ModuloSistema.Permisos, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Duplicar,
            AccionPermiso.Administrar, AccionPermiso.ConsultarHistorial
        }),
        (ModuloSistema.Caja, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Activar,
            AccionPermiso.Desactivar, AccionPermiso.Actualizar, AccionPermiso.Cerrar
        }),
        (ModuloSistema.Auditoria, new[] { AccionPermiso.Ver, AccionPermiso.Exportar }),
        (ModuloSistema.ReportesAdministrativos, new[] { AccionPermiso.Ver, AccionPermiso.Exportar, AccionPermiso.Imprimir }),
        (ModuloSistema.Configuracion, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar,
            AccionPermiso.Administrar, AccionPermiso.Cerrar
        }),
        (ModuloSistema.Descuentos, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Duplicar, AccionPermiso.Aplicar,
            AccionPermiso.ConsultarHistorial
        }),
        (ModuloSistema.Impuestos, new[]
        {
            AccionPermiso.Ver, AccionPermiso.Crear, AccionPermiso.Editar,
            AccionPermiso.Activar, AccionPermiso.Desactivar, AccionPermiso.EliminarLogico,
            AccionPermiso.EliminarPermanente, AccionPermiso.Duplicar, AccionPermiso.Aplicar,
            AccionPermiso.ConsultarHistorial
        })
    };

    public static readonly (ModuloSistema Modulo, AccionPermiso Accion)[] NuevosPermisosAdministrador =
        Definicion.SelectMany(d => d.Acciones.Select(a => (d.Modulo, a))).ToArray();

    public static readonly (ModuloSistema Modulo, AccionPermiso Accion)[] DefaultVendedor = { };
}
