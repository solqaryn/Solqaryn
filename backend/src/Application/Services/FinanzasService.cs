using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using CatalogoMetodoPago = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Application.Services;

public class FinanzasService : IFinanzasService
{
    private readonly IMovimientoFinancieroRepository _movimientoRepository;
    private readonly IRevisionFinancieraRepository _revisionRepository;
    private readonly IVentaRepository _ventaRepository;
    private readonly ICompraRepository _compraRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUsuarioScopeService _usuarioScope;

    public FinanzasService(
        IMovimientoFinancieroRepository movimientoRepository,
        IRevisionFinancieraRepository revisionRepository,
        IVentaRepository ventaRepository,
        ICompraRepository compraRepository,
        IProductoRepository productoRepository,
        ICurrentUserService currentUser,
        IUsuarioScopeService usuarioScope)
    {
        _movimientoRepository = movimientoRepository;
        _revisionRepository = revisionRepository;
        _ventaRepository = ventaRepository;
        _compraRepository = compraRepository;
        _productoRepository = productoRepository;
        _currentUser = currentUser;
        _usuarioScope = usuarioScope;
    }

    public async Task<FinanzasResumenDto> GetResumenAsync()
    {
        var alcance = await ObtenerAlcanceObligatorioAsync();
        var esAdministrador = alcance.EsAdministrador;
        var movimientos = await _movimientoRepository.GetFilteredAsync(null, null);
        var noAnulados = movimientos.Where(m => m.Estado != EstadoMovimientoFinanciero.Anulado).ToList();

        var ingresosTotales = noAnulados.Where(m => m.Tipo == TipoMovimientoFinanciero.Ingreso).Sum(m => m.Monto);
        var egresosTotales = noAnulados.Where(m => m.Tipo == TipoMovimientoFinanciero.Egreso).Sum(m => m.Monto);

        var utilidadBruta = esAdministrador
            ? await _ventaRepository.GetUtilidadBrutaTotalAsync()
            : 0m;
        var margenUtilidadBruta = esAdministrador && ingresosTotales > 0
            ? decimal.Round(utilidadBruta / ingresosTotales * 100m, 2)
            : 0m;

        var gastosOperativos = esAdministrador
            ? noAnulados.Where(m =>
                    !m.EsAutomatico &&
                    m.Tipo == TipoMovimientoFinanciero.Egreso &&
                    m.Categoria == CategoriaMovimientoFinanciero.GastoOperativo)
                .Sum(m => m.Monto)
            : 0m;
        var utilidadNeta = esAdministrador ? utilidadBruta - gastosOperativos : 0m;

        var valorInventarioCostoMercaderia = esAdministrador
            ? await _productoRepository.GetValorTotalCostoPorTipoAsync(TipoInventario.MercaderiaVenta)
            : 0m;
        var valorInventarioCostoInsumos = esAdministrador
            ? await _productoRepository.GetValorTotalCostoPorTipoAsync(TipoInventario.InsumoAdministrativo)
            : 0m;
        var valorInventarioCosto = valorInventarioCostoMercaderia + valorInventarioCostoInsumos;
        var valorPotencialVentaMercaderia = esAdministrador
            ? await _productoRepository.GetValorTotalPrecioPorTipoAsync(TipoInventario.MercaderiaVenta)
            : 0m;
        var valorPotencialVenta = valorPotencialVentaMercaderia;
        var utilidadInventarioPotencial = esAdministrador
            ? valorPotencialVentaMercaderia - valorInventarioCostoMercaderia
            : 0m;
        var margenInventarioPotencial = esAdministrador && valorPotencialVentaMercaderia > 0
            ? decimal.Round(utilidadInventarioPotencial / valorPotencialVentaMercaderia * 100m, 2)
            : 0m;

        var cuentasPorCobrar = await _ventaRepository.GetCuentasPorCobrarAsync();
        var ventasDelMes = await _ventaRepository.GetTotalDelMesAsync();
        var ingresosDelMes = await _ventaRepository.GetIngresosDelMesAsync();

        var cuentasPorPagar = esAdministrador
            ? await _compraRepository.GetCuentasPorPagarAsync()
            : 0m;
        var comprasDelMes = esAdministrador
            ? await _compraRepository.GetTotalDelMesAsync()
            : 0;
        var ultimaRevision = esAdministrador
            ? await _revisionRepository.GetUltimaAsync()
            : null;

        return new FinanzasResumenDto
        {
            IngresosTotales = ingresosTotales,
            EgresosTotales = egresosTotales,
            GastosOperativos = gastosOperativos,
            UtilidadBruta = utilidadBruta,
            MargenUtilidadBruta = margenUtilidadBruta,
            UtilidadNeta = utilidadNeta,
            ValorInventarioCosto = valorInventarioCosto,
            ValorInventarioCostoMercaderia = valorInventarioCostoMercaderia,
            ValorInventarioCostoInsumosAdministrativos = valorInventarioCostoInsumos,
            ValorPotencialVenta = valorPotencialVenta,
            ValorPotencialVentaMercaderia = valorPotencialVentaMercaderia,
            UtilidadInventarioPotencial = utilidadInventarioPotencial,
            MargenInventarioPotencial = margenInventarioPotencial,
            CuentasPorCobrar = cuentasPorCobrar,
            CuentasPorPagar = cuentasPorPagar,
            BalanceOperativo = ingresosTotales - egresosTotales,
            VentasDelMes = ventasDelMes,
            ComprasDelMes = comprasDelMes,
            IngresosDelMes = ingresosDelMes,
            UltimaRevision = ultimaRevision is null ? null : ToDto(ultimaRevision)
        };
    }

    public async Task<List<MovimientoFinancieroDto>> GetMovimientosAsync(DateTime? desde, DateTime? hasta)
    {
        await ObtenerAlcanceObligatorioAsync();
        var movimientos = await _movimientoRepository.GetFilteredAsync(desde, hasta);
        return movimientos.Select(ToDto).ToList();
    }

    public async Task<MovimientoFinancieroDto> RegistrarMovimientoManualAsync(CreateMovimientoManualDto dto)
    {
        await ObtenerAlcanceObligatorioAsync();

        if (dto.Monto <= 0)
            throw new BusinessRuleException("El monto debe ser mayor a 0.");
        if (string.IsNullOrWhiteSpace(dto.Concepto))
            throw new BusinessRuleException("El concepto es obligatorio.");

        if (!Enum.TryParse<TipoMovimientoFinanciero>(dto.Tipo, true, out var tipo) || !Enum.IsDefined(tipo))
            throw new BusinessRuleException("El tipo de movimiento financiero no es válido.");
        if (!Enum.TryParse<CategoriaMovimientoFinanciero>(dto.Categoria, true, out var categoria) || !Enum.IsDefined(categoria))
            throw new BusinessRuleException("La categoría financiera no es válida.");

        if (categoria == CategoriaMovimientoFinanciero.GastoOperativo && tipo != TipoMovimientoFinanciero.Egreso)
            throw new BusinessRuleException("Un gasto operativo debe registrarse como egreso.");

        if (categoria is CategoriaMovimientoFinanciero.Venta or CategoriaMovimientoFinanciero.Compra or CategoriaMovimientoFinanciero.Reversion)
            throw new BusinessRuleException("Las categorías Venta, Compra y Reversión son automáticas y no admiten registros manuales.");

        CatalogoMetodoPago? metodoPagoCatalogo = null;
        if (!string.IsNullOrWhiteSpace(dto.MetodoPago))
        {
            metodoPagoCatalogo = await _movimientoRepository.GetMetodoPagoPorCodigoONombreAsync(dto.MetodoPago.Trim())
                ?? throw new BusinessRuleException($"El método de pago '{dto.MetodoPago.Trim()}' no existe en el catálogo.");
        }

        var movimiento = new MovimientoFinanciero
        {
            Tipo = tipo,
            Categoria = categoria,
            Concepto = dto.Concepto.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            Monto = dto.Monto,
            Estado = EstadoMovimientoFinanciero.Pagado,
            MetodoPagoId = metodoPagoCatalogo?.Id,
            MetodoPagoCatalogo = metodoPagoCatalogo,
            EsAutomatico = false,
            ModuloOrigen = "Manual",
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _movimientoRepository.AddAsync(movimiento);
        await _movimientoRepository.SaveChangesAsync();

        return ToDto(movimiento);
    }

    public async Task<MovimientoFinancieroDto?> AnularMovimientoAsync(int id, string motivo)
    {
        await ObtenerAlcanceObligatorioAsync();
        var movimiento = await _movimientoRepository.GetByIdAsync(id);
        if (movimiento is null) return null;

        if (movimiento.EsAutomatico)
            throw new BusinessRuleException(
                "Los movimientos automáticos no se pueden anular directamente. Anula la compra o venta que los originó.");
        if (movimiento.Estado == EstadoMovimientoFinanciero.Anulado)
            throw new BusinessRuleException("Este movimiento ya está anulado.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de anulación es obligatorio.");

        movimiento.Estado = EstadoMovimientoFinanciero.Anulado;
        movimiento.AnuladoPorUsuarioId = _currentUser.UsuarioId;
        movimiento.AnuladoPorNombreUsuario = _currentUser.NombreUsuario;
        movimiento.FechaAnulacion = DateTime.UtcNow;
        movimiento.MotivoAnulacion = motivo;

        _movimientoRepository.Update(movimiento);
        await _movimientoRepository.SaveChangesAsync();

        return ToDto(movimiento);
    }

    public async Task<List<RevisionFinancieraDto>> GetRevisionesAsync()
    {
        var alcance = await ObtenerAlcanceObligatorioAsync();
        if (!alcance.EsAdministrador)
            return new List<RevisionFinancieraDto>();

        var revisiones = await _revisionRepository.GetAllAsync();
        return revisiones.Select(ToDto).ToList();
    }

    public async Task<RevisionFinancieraDto> RegistrarRevisionAsync(CreateRevisionFinancieraDto dto)
    {
        var alcance = await ObtenerAlcanceObligatorioAsync();
        if (!alcance.EsAdministrador)
            throw new BusinessRuleException("Solo un administrador puede registrar revisiones financieras.");

        if (dto.FechaHasta < dto.FechaDesde)
            throw new BusinessRuleException("La fecha 'hasta' no puede ser anterior a la fecha 'desde'.");

        if (!Enum.TryParse<EstadoRevisionFinanciera>(dto.EstadoRevision, true, out var estado))
            estado = EstadoRevisionFinanciera.Revisado;

        var revision = new RevisionFinanciera
        {
            FechaDesde = dto.FechaDesde,
            FechaHasta = dto.FechaHasta,
            EstadoRevision = estado,
            Observaciones = dto.Observaciones,
            RevisadoPorUsuarioId = alcance.UsuarioId,
            RevisadoPorNombreUsuario = _currentUser.NombreCompleto ?? _currentUser.NombreUsuario ?? "—",
            FechaRevision = DateTime.UtcNow
        };

        await _revisionRepository.AddAsync(revision);
        await _revisionRepository.SaveChangesAsync();

        return ToDto(revision);
    }

    private async Task<UsuarioScopeActual> ObtenerAlcanceObligatorioAsync() =>
        await _usuarioScope.ObtenerActualAsync()
        ?? throw new ForbiddenAccessException("No fue posible resolver el usuario autenticado y su rol vigente.");

    private static MovimientoFinancieroDto ToDto(MovimientoFinanciero m) => new()
    {
        Id = m.Id,
        Fecha = m.Fecha,
        Tipo = m.Tipo.ToString(),
        Categoria = m.Categoria.ToString(),
        Concepto = m.Concepto,
        Descripcion = m.Descripcion,
        Monto = m.Monto,
        Estado = m.Estado.ToString(),
        MetodoPago = m.MetodoPagoCatalogo?.Nombre,
        EsAutomatico = m.EsAutomatico,
        ModuloOrigen = m.ModuloOrigen,
        CreadoPorNombreUsuario = m.CreadoPorNombreUsuario,
        AnuladoPorNombreUsuario = m.AnuladoPorNombreUsuario,
        FechaAnulacion = m.FechaAnulacion,
        MotivoAnulacion = m.MotivoAnulacion
    };

    private static RevisionFinancieraDto ToDto(RevisionFinanciera r) => new()
    {
        Id = r.Id,
        FechaDesde = r.FechaDesde,
        FechaHasta = r.FechaHasta,
        RevisadoPorNombreUsuario = r.RevisadoPorNombreUsuario,
        FechaRevision = r.FechaRevision,
        EstadoRevision = r.EstadoRevision.ToString(),
        Observaciones = r.Observaciones
    };
}
