namespace Solqaryn.Domain.Enums;

/// <summary>
/// Estado durable de un mensaje de outbox. El dispatcher solo puede intentar
/// mensajes Pendiente/Fallido cuya ventana de disponibilidad ya venció.
/// </summary>
public enum EstadoMensajeOutbox
{
    Pendiente = 0,
    Procesando = 1,
    Entregado = 2,
    Fallido = 3,
    DeadLetter = 4
}
