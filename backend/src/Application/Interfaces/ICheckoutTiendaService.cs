using Solqaryn.Application.DTOs;

namespace Solqaryn.Application.Interfaces;

public interface ICheckoutTiendaService
{
    Task<CheckoutTiendaResultado> ValidarAsync(ValidarCheckoutTiendaDto? dto);
}

public sealed record CheckoutTiendaResultado(CheckoutTiendaValidadoDto? Datos, string Mensaje, bool EsConflicto)
{
    public static CheckoutTiendaResultado Invalido(string mensaje) => new(null, mensaje, false);
    public static CheckoutTiendaResultado Conflict(string mensaje) => new(null, mensaje, true);
    public static CheckoutTiendaResultado Ok(CheckoutTiendaValidadoDto datos, string mensaje) => new(datos, mensaje, false);
}
