namespace Solqaryn.Application.Common;

/// <summary>
/// Genera identificadores de correlación determinísticos para operaciones de inventario
/// que todavía no exponen una clave física completa. El mismo documento/operación
/// produce siempre el mismo valor, permitiendo trazabilidad e idempotencia lógica
/// sin inventar AlmacenId o UbicacionAlmacenId.
/// </summary>
public static class KardexCorrelationId
{
    public static string CompraConfirmar(int compraId) => Crear("compra", compraId, "confirmar");

    public static string CompraAnular(int compraId) => Crear("compra", compraId, "anular");

    public static string VentaConfirmar(int ventaId) => Crear("venta", ventaId, "confirmar");

    public static string VentaAnular(int ventaId) => Crear("venta", ventaId, "anular");

    public static string ConsumoConfirmar(int consumoInsumoId) => Crear("consumo", consumoInsumoId, "confirmar");

    public static string ConsumoAnular(int consumoInsumoId) => Crear("consumo", consumoInsumoId, "anular");

    public static string TransferenciaDespachar(int transferenciaId) =>
        Crear("transferencia", transferenciaId, "despachar");

    public static string TransferenciaRecibir(int transferenciaId) =>
        Crear("transferencia", transferenciaId, "recibir");

    public static string TransferenciaCancelar(int transferenciaId) =>
        Crear("transferencia", transferenciaId, "cancelar");

    private static string Crear(string modulo, int documentoId, string operacion)
    {
        if (documentoId <= 0)
            throw new ArgumentOutOfRangeException(nameof(documentoId), "El identificador del documento debe ser mayor que cero.");

        return $"{modulo}:{documentoId}:{operacion}";
    }
}
