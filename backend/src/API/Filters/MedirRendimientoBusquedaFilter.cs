using System.Collections;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Solqaryn.API.Filters;

/// <summary>
/// Métrica segura para endpoints operativos de búsqueda y escaneo.
/// Nunca registra términos, SKU, códigos de barras, teléfonos ni correos.
/// </summary>
public sealed class MedirRendimientoBusquedaFilter : IAsyncActionFilter
{
    private static readonly IReadOnlyDictionary<string, string> RutasMedidas =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/productos"] = "search",
            ["/sucursales"] = "buscar",
            ["/almacenes"] = "buscar",
            ["/clientes/buscar"] = "termino",
            ["/proveedores/buscar"] = "termino",
            ["/ventas/productos/buscar"] = "termino",
            ["/ventas/productos/por-codigo"] = "codigo",
            ["/compras/productos/buscar"] = "termino",
            ["/compras/productos/por-codigo"] = "codigo"
        };

    private static readonly BusquedaRendimientoMetricas Metricas = new();
    private readonly ILogger<MedirRendimientoBusquedaFilter> _logger;

    public MedirRendimientoBusquedaFilter(ILogger<MedirRendimientoBusquedaFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var ruta = context.HttpContext.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
        if (!RutasMedidas.TryGetValue(ruta, out var parametroConsulta))
        {
            await next();
            return;
        }

        var reloj = Stopwatch.StartNew();
        var ejecutado = await next();
        reloj.Stop();

        var request = context.HttpContext.Request;
        var longitudTermino = request.Query.TryGetValue(parametroConsulta, out var valores)
            ? valores.FirstOrDefault()?.Trim().Length ?? 0
            : 0;
        var estadoHttp = ObtenerEstadoHttp(ejecutado);
        var cantidadResultados = ObtenerCantidadResultados(ejecutado.Result, estadoHttp);
        var resumen = Metricas.Registrar(ruta, reloj.ElapsedMilliseconds);

        _logger.LogInformation(
            "BusquedaOperativa Ruta={Ruta} DuracionMs={DuracionMs} P50Ms={P50Ms} P95Ms={P95Ms} Muestras={Muestras} LongitudTermino={LongitudTermino} CantidadResultados={CantidadResultados} EstadoHTTP={EstadoHTTP} CorrelationId={CorrelationId}",
            ruta,
            reloj.ElapsedMilliseconds,
            resumen.P50Ms,
            resumen.P95Ms,
            resumen.Muestras,
            longitudTermino,
            cantidadResultados,
            estadoHttp,
            context.HttpContext.TraceIdentifier);
    }

    private static int ObtenerEstadoHttp(ActionExecutedContext context)
    {
        if (context.Exception is not null && !context.ExceptionHandled)
            return StatusCodes.Status500InternalServerError;

        return context.Result switch
        {
            ObjectResult objectResult when objectResult.StatusCode.HasValue => objectResult.StatusCode.Value,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            _ => StatusCodes.Status200OK
        };
    }

    private static int ObtenerCantidadResultados(IActionResult? result, int estadoHttp)
    {
        if (estadoHttp is < 200 or >= 300 || result is not ObjectResult objectResult || objectResult.Value is null)
            return 0;

        var data = objectResult.Value.GetType().GetProperty("Data")?.GetValue(objectResult.Value);
        if (data is null) return 0;
        if (data is string) return 1;
        if (data is ICollection collection) return collection.Count;

        var items = data.GetType().GetProperty("Items")?.GetValue(data);
        if (items is ICollection itemsCollection)
            return itemsCollection.Count;
        if (items is IEnumerable itemsEnumerable)
        {
            var itemsCount = 0;
            foreach (var _ in itemsEnumerable) itemsCount++;
            return itemsCount;
        }

        if (data is IEnumerable enumerable)
        {
            var count = 0;
            foreach (var _ in enumerable) count++;
            return count;
        }

        return 1;
    }
}
