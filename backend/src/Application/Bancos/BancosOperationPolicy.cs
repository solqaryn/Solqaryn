using Solqaryn.Domain.Entities.Bancos;
using Solqaryn.Domain.Enums.Bancos;

namespace Solqaryn.Application.Bancos;

public static class BancosOperationPolicy
{
    public static void ValidarOperacionBancaria(
        CuentaBancaria origen,
        CuentaBancaria? destino,
        TipoOperacionBancaria tipo,
        decimal monto)
    {
        ArgumentNullException.ThrowIfNull(origen);

        if (origen.Estado != EstadoCuentaBancaria.Activa)
            throw new InvalidOperationException("La cuenta origen debe estar activa para operar.");

        if (monto <= 0m)
            throw new ArgumentOutOfRangeException(nameof(monto), "El monto debe ser mayor que cero.");

        if (tipo != TipoOperacionBancaria.Transferencia && destino is not null)
            throw new ArgumentException("Solo las transferencias permiten una cuenta destino.", nameof(destino));

        if (tipo == TipoOperacionBancaria.Transferencia)
        {
            if (destino is null)
                throw new ArgumentException("Una transferencia requiere una cuenta destino.", nameof(destino));

            if (destino.Estado != EstadoCuentaBancaria.Activa)
                throw new InvalidOperationException("La cuenta destino debe estar activa para recibir transferencias.");

            if (ReferenceEquals(origen, destino))
                throw new InvalidOperationException("La cuenta origen y destino de una transferencia deben ser distintas.");

            if (origen.Id > 0 && destino.Id > 0 && origen.Id == destino.Id)
                throw new InvalidOperationException("La cuenta origen y destino de una transferencia deben ser distintas.");

            if (!string.Equals(origen.Moneda, destino.Moneda, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La moneda de la cuenta origen y destino deben coincidir para una transferencia.");
        }

        origen.ValidarOperacion(tipo, monto, destino?.Id);
    }
}
