using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

public sealed class EstadoFinancieroService : IEstadoFinancieroService
{
    private readonly AppDbContext _context;
    private readonly IPeriodoContableRepository _periodos;
    private readonly IMovimientoFinancieroRepository _movimientosFinancieros;

    public EstadoFinancieroService(
        AppDbContext context,
        IPeriodoContableRepository periodos,
        IMovimientoFinancieroRepository movimientosFinancieros)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _periodos = periodos ?? throw new ArgumentNullException(nameof(periodos));
        _movimientosFinancieros = movimientosFinancieros ?? throw new ArgumentNullException(nameof(movimientosFinancieros));
    }

    public async Task<EstadoFinancieroDto> GenerarAsync(
        TipoEstadoFinanciero tipo,
        EstadoFinancieroFiltroDto filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (!Enum.IsDefined(tipo))
            throw new BusinessRuleException("El tipo de estado financiero no es válido.");

        var (fechaInicio, fechaFin) = await ResolverRangoAsync(filtro, cancellationToken);
        if (tipo == TipoEstadoFinanciero.FlujoEfectivo)
            return await GenerarFlujoEfectivoAsync(fechaInicio, fechaFin);

        var cuentas = await _context.Set<CuentaContable>()
            .AsNoTracking()
            .OrderBy(c => c.Codigo)
            .ToListAsync(cancellationToken);
        var cuentasPorId = cuentas.ToDictionary(c => c.Id);

        var query = _context.AsientoDetalles
            .AsNoTracking()
            .Where(d => d.AsientoContable != null && d.AsientoContable.Fecha <= fechaFin);

        if (tipo != TipoEstadoFinanciero.BalanceGeneral)
            query = query.Where(d => d.AsientoContable!.Fecha >= fechaInicio);

        var movimientos = await query
            .Select(d => new MovimientoContableRow
            {
                AsientoContableId = d.AsientoContableId,
                CuentaContableId = d.CuentaContableId,
                Fecha = d.AsientoContable!.Fecha,
                Numero = d.AsientoContable.Numero,
                Concepto = d.AsientoContable.Concepto,
                Debe = d.Debe,
                Haber = d.Haber
            })
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.AsientoContableId)
            .ToListAsync(cancellationToken);

        return tipo switch
        {
            TipoEstadoFinanciero.BalanceGeneral => GenerarBalanceGeneral(fechaInicio, fechaFin, movimientos, cuentasPorId),
            TipoEstadoFinanciero.EstadoResultados => GenerarEstadoResultados(fechaInicio, fechaFin, movimientos, cuentasPorId),
            TipoEstadoFinanciero.BalanceComprobacion => GenerarBalanceComprobacion(fechaInicio, fechaFin, movimientos, cuentasPorId),
            TipoEstadoFinanciero.LibroDiario => GenerarLibroDiario(fechaInicio, fechaFin, movimientos, cuentasPorId),
            TipoEstadoFinanciero.LibroMayor => GenerarLibroMayor(fechaInicio, fechaFin, movimientos, cuentasPorId),
            _ => throw new BusinessRuleException("El tipo de estado financiero no está soportado.")
        };
    }

    private async Task<(DateTime Inicio, DateTime Fin)> ResolverRangoAsync(
        EstadoFinancieroFiltroDto filtro,
        CancellationToken cancellationToken)
    {
        var porPeriodo = filtro.PeriodoContableId.HasValue;
        var porFechas = filtro.FechaDesde.HasValue || filtro.FechaHasta.HasValue;

        if (porPeriodo == porFechas)
            throw new BusinessRuleException("Debe indicar exactamente un PeriodoContableId o un rango FechaDesde/FechaHasta.");

        DateTime inicio;
        DateTime fin;

        if (porPeriodo)
        {
            if (filtro.PeriodoContableId!.Value <= 0)
                throw new BusinessRuleException("PeriodoContableId debe ser mayor a cero.");

            var periodo = await _periodos.GetByIdAsync(filtro.PeriodoContableId.Value, false, cancellationToken)
                ?? throw new ResourceNotFoundException($"No se encontró el período contable con ID {filtro.PeriodoContableId.Value}.");
            inicio = periodo.FechaInicio;
            fin = periodo.FechaFin;
        }
        else
        {
            if (!filtro.FechaDesde.HasValue || !filtro.FechaHasta.HasValue)
                throw new BusinessRuleException("FechaDesde y FechaHasta son obligatorias cuando no se usa PeriodoContableId.");
            inicio = filtro.FechaDesde.Value;
            fin = filtro.FechaHasta.Value;
        }

        if (inicio > fin)
            throw new BusinessRuleException("FechaDesde no puede ser posterior a FechaHasta.");

        return (inicio, fin);
    }

    private static EstadoFinancieroDto GenerarBalanceGeneral(
        DateTime inicio,
        DateTime fin,
        IReadOnlyList<MovimientoContableRow> movimientos,
        IReadOnlyDictionary<int, CuentaContable> cuentas)
    {
        var permitidos = new HashSet<TipoCuentaContable>
        {
            TipoCuentaContable.Activo,
            TipoCuentaContable.Pasivo,
            TipoCuentaContable.Patrimonio
        };
        var lineas = AgruparPorCuenta(movimientos, cuentas, permitidos);

        decimal TotalTipo(TipoCuentaContable tipo) => lineas
            .Where(l => cuentas.TryGetValue(l.CuentaContableId, out var c) && c.Tipo == tipo)
            .Sum(l => l.Saldo);

        return Crear(
            "Balance General",
            inicio,
            fin,
            lineas,
            new[]
            {
                Total("Total Activos", TotalTipo(TipoCuentaContable.Activo)),
                Total("Total Pasivos", TotalTipo(TipoCuentaContable.Pasivo)),
                Total("Total Patrimonio", TotalTipo(TipoCuentaContable.Patrimonio)),
                Total("Ecuación contable", TotalTipo(TipoCuentaContable.Activo) - TotalTipo(TipoCuentaContable.Pasivo) - TotalTipo(TipoCuentaContable.Patrimonio))
            });
    }

    private static EstadoFinancieroDto GenerarEstadoResultados(
        DateTime inicio,
        DateTime fin,
        IReadOnlyList<MovimientoContableRow> movimientos,
        IReadOnlyDictionary<int, CuentaContable> cuentas)
    {
        var permitidos = new HashSet<TipoCuentaContable>
        {
            TipoCuentaContable.Ingreso,
            TipoCuentaContable.Gasto,
            TipoCuentaContable.Costo
        };
        var lineas = AgruparPorCuenta(movimientos, cuentas, permitidos);

        decimal TotalTipo(TipoCuentaContable tipo) => lineas
            .Where(l => cuentas.TryGetValue(l.CuentaContableId, out var c) && c.Tipo == tipo)
            .Sum(l => l.Saldo);

        var ingresos = TotalTipo(TipoCuentaContable.Ingreso);
        var gastos = TotalTipo(TipoCuentaContable.Gasto);
        var costos = TotalTipo(TipoCuentaContable.Costo);

        return Crear(
            "Estado de Resultados",
            inicio,
            fin,
            lineas,
            new[]
            {
                Total("Total Ingresos", ingresos),
                Total("Total Gastos", gastos),
                Total("Total Costos", costos),
                Total("Utilidad/Pérdida Neta", ingresos - gastos - costos)
            });
    }

    private static EstadoFinancieroDto GenerarBalanceComprobacion(
        DateTime inicio,
        DateTime fin,
        IReadOnlyList<MovimientoContableRow> movimientos,
        IReadOnlyDictionary<int, CuentaContable> cuentas)
    {
        var lineas = AgruparPorCuenta(movimientos, cuentas, null);
        var debe = movimientos.Sum(x => x.Debe);
        var haber = movimientos.Sum(x => x.Haber);
        return Crear(
            "Balance de Comprobación",
            inicio,
            fin,
            lineas,
            new[] { Total("Total Debe", debe), Total("Total Haber", haber), Total("Diferencia", debe - haber) });
    }

    private static EstadoFinancieroDto GenerarLibroMayor(
        DateTime inicio,
        DateTime fin,
        IReadOnlyList<MovimientoContableRow> movimientos,
        IReadOnlyDictionary<int, CuentaContable> cuentas)
    {
        var lineas = AgruparPorCuenta(movimientos, cuentas, null);
        return Crear(
            "Libro Mayor",
            inicio,
            fin,
            lineas,
            new[]
            {
                Total("Total Debe", movimientos.Sum(x => x.Debe)),
                Total("Total Haber", movimientos.Sum(x => x.Haber))
            });
    }

    private static EstadoFinancieroDto GenerarLibroDiario(
        DateTime inicio,
        DateTime fin,
        IReadOnlyList<MovimientoContableRow> movimientos,
        IReadOnlyDictionary<int, CuentaContable> cuentas)
    {
        var lineas = movimientos
            .Where(m => cuentas.ContainsKey(m.CuentaContableId))
            .Select(m =>
            {
                var cuenta = cuentas[m.CuentaContableId];
                var asiento = string.IsNullOrWhiteSpace(m.Numero) ? m.Concepto : $"{m.Numero} · {m.Concepto}";
                return new EstadoFinancieroLineaDto
                {
                    CuentaContableId = cuenta.Id,
                    CuentaCodigo = cuenta.Codigo,
                    CuentaNombre = $"{cuenta.Nombre} — {asiento}",
                    Saldo = m.Debe - m.Haber,
                    EsRaiz = cuenta.EsRaiz
                };
            })
            .ToList();

        var debe = movimientos.Sum(x => x.Debe);
        var haber = movimientos.Sum(x => x.Haber);
        return Crear(
            "Libro Diario",
            inicio,
            fin,
            lineas,
            new[] { Total("Total Debe", debe), Total("Total Haber", haber), Total("Diferencia", debe - haber) });
    }

    private async Task<EstadoFinancieroDto> GenerarFlujoEfectivoAsync(DateTime inicio, DateTime fin)
    {
        var movimientos = (await _movimientosFinancieros.GetFilteredAsync(inicio, fin))
            .Where(m => m.Estado == EstadoMovimientoFinanciero.Pagado)
            .ToList();

        var ingresos = movimientos.Where(m => m.Tipo == TipoMovimientoFinanciero.Ingreso).Sum(m => m.Monto);
        var egresos = movimientos.Where(m => m.Tipo == TipoMovimientoFinanciero.Egreso).Sum(m => m.Monto);
        var ajustes = movimientos.Where(m => m.Tipo == TipoMovimientoFinanciero.Ajuste).Sum(m => m.Monto);

        var lineas = new List<EstadoFinancieroLineaDto>
        {
            FlujoLinea("FLUJO-ING", "Ingresos de efectivo", ingresos),
            FlujoLinea("FLUJO-EGR", "Egresos de efectivo", -egresos),
            FlujoLinea("FLUJO-AJU", "Ajustes de efectivo", ajustes)
        };

        return Crear(
            "Flujo de Efectivo",
            inicio,
            fin,
            lineas,
            new[]
            {
                Total("Total Ingresos", ingresos),
                Total("Total Egresos", egresos),
                Total("Total Ajustes", ajustes),
                Total("Flujo Neto", ingresos - egresos + ajustes)
            });
    }

    private static List<EstadoFinancieroLineaDto> AgruparPorCuenta(
        IReadOnlyList<MovimientoContableRow> movimientos,
        IReadOnlyDictionary<int, CuentaContable> cuentas,
        ISet<TipoCuentaContable>? tiposPermitidos)
    {
        var lineas = new List<EstadoFinancieroLineaDto>();
        foreach (var grupo in movimientos.GroupBy(x => x.CuentaContableId))
        {
            if (!cuentas.TryGetValue(grupo.Key, out var cuenta))
                continue;
            if (tiposPermitidos is not null && !tiposPermitidos.Contains(cuenta.Tipo))
                continue;

            var saldo = CalcularSaldo(cuenta.Tipo, grupo.Sum(x => x.Debe), grupo.Sum(x => x.Haber));
            lineas.Add(new EstadoFinancieroLineaDto
            {
                CuentaContableId = cuenta.Id,
                CuentaCodigo = cuenta.Codigo,
                CuentaNombre = cuenta.Nombre,
                Saldo = saldo,
                EsRaiz = cuenta.EsRaiz
            });
        }

        return lineas.OrderBy(x => x.CuentaCodigo).ToList();
    }

    private static decimal CalcularSaldo(TipoCuentaContable tipo, decimal debe, decimal haber) =>
        tipo is TipoCuentaContable.Activo or TipoCuentaContable.Gasto or TipoCuentaContable.Costo
            ? debe - haber
            : haber - debe;

    private static EstadoFinancieroDto Crear(
        string nombre,
        DateTime inicio,
        DateTime fin,
        IReadOnlyList<EstadoFinancieroLineaDto> lineas,
        IReadOnlyList<EstadoFinancieroTotalDto> totales) =>
        new() { Nombre = nombre, FechaInicio = inicio, FechaFin = fin, Lineas = lineas, Totales = totales };

    private static EstadoFinancieroTotalDto Total(string etiqueta, decimal valor) =>
        new() { Etiqueta = etiqueta, Valor = valor };

    private static EstadoFinancieroLineaDto FlujoLinea(string codigo, string nombre, decimal saldo) =>
        new() { CuentaContableId = 0, CuentaCodigo = codigo, CuentaNombre = nombre, Saldo = saldo, EsRaiz = true };

    private sealed class MovimientoContableRow
    {
        public int AsientoContableId { get; init; }
        public int CuentaContableId { get; init; }
        public DateTime Fecha { get; init; }
        public string? Numero { get; init; }
        public string Concepto { get; init; } = string.Empty;
        public decimal Debe { get; init; }
        public decimal Haber { get; init; }
    }
}
