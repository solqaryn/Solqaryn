namespace Solqaryn.Domain.Enums;

public enum AccionPermiso
{
    Ver = 1,
    Crear = 2,
    Editar = 3,
    /// Conservado únicamente para contratos históricos. Los módulos modernos
    /// deben diferenciar EliminarLogico/EliminarPermanente cuando aplique.
    Eliminar = 4,
    Confirmar = 5,
    Anular = 6,
    Actualizar = 7,
    Activar = 8,
    Desactivar = 9,
    EliminarLogico = 10,
    EliminarPermanente = 11,
    Aprobar = 12,
    Rechazar = 13,
    Exportar = 14,
    Imprimir = 15,
    Administrar = 16,
    AsignarRol = 17,
    RestablecerContrasena = 18,
    CambiarEstado = 19,
    ConsultarHistorial = 20,
    Aplicar = 21,
    Duplicar = 22,
    Compartir = 23,
    AjustarStock = 24,
    RegistrarConsumo = 25,
    ExonerarEnvio = 26,
    Importar = 27,
    Cerrar = 28,
    Reabrir = 29
}
