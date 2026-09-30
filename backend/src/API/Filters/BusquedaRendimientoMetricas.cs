namespace Solqaryn.API.Filters;

public sealed record BusquedaRendimientoResumen(int Muestras, long P50Ms, long P95Ms);

/// <summary>
/// Mantiene una ventana acotada de latencias por ruta para exponer p50/p95
/// sin persistir términos de búsqueda ni identificadores sensibles.
/// </summary>
public sealed class BusquedaRendimientoMetricas
{
    private const int CapacidadPorRuta = 200;
    private readonly object _sync = new();
    private readonly Dictionary<string, Queue<long>> _muestras = new(StringComparer.OrdinalIgnoreCase);

    public BusquedaRendimientoResumen Registrar(string ruta, long duracionMs)
    {
        var duracionSegura = Math.Max(0, duracionMs);
        lock (_sync)
        {
            if (!_muestras.TryGetValue(ruta, out var cola))
            {
                cola = new Queue<long>(CapacidadPorRuta);
                _muestras[ruta] = cola;
            }

            cola.Enqueue(duracionSegura);
            while (cola.Count > CapacidadPorRuta)
                cola.Dequeue();

            var ordenadas = cola.OrderBy(x => x).ToArray();
            return new BusquedaRendimientoResumen(
                ordenadas.Length,
                Percentil(ordenadas, 0.50),
                Percentil(ordenadas, 0.95));
        }
    }

    internal static long Percentil(IReadOnlyList<long> valoresOrdenados, double percentil)
    {
        if (valoresOrdenados.Count == 0)
            return 0;

        var p = Math.Clamp(percentil, 0d, 1d);
        var indice = (int)Math.Ceiling(p * valoresOrdenados.Count) - 1;
        indice = Math.Clamp(indice, 0, valoresOrdenados.Count - 1);
        return valoresOrdenados[indice];
    }
}
