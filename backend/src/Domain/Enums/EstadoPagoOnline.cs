namespace Solqaryn.Domain.Enums;

/// <summary>
/// Estado normalizado de un pago online, independiente del proveedor externo.
/// </summary>
public enum EstadoPagoOnline
{
    Pendiente = 1,
    Pagado = 2,
    Fallido = 3
}
