using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>
/// Vertical N1.9.D/F para administrar la identidad trazable opt-in sin convertir
/// lotes/series en una segunda autoridad cuantitativa de inventario y dejando
/// evidencia de auditoría estricta en cada mutación empresarial.
/// </summary>
public sealed class TrazabilidadInventarioService : ITrazabilidadInventarioService
{
    private readonly ITrazabilidadInventarioRepository _repository;
    private readonly IProductoVarianteRepository _variantes;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public TrazabilidadInventarioService(
        ITrazabilidadInventarioRepository repository,
        IProductoVarianteRepository variantes,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _variantes = variantes ?? throw new ArgumentNullException(nameof(variantes));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<ConfiguracionTrazabilidadVarianteDto?> GetConfiguracionAsync(int productoVarianteId)
    {
        if (productoVarianteId <= 0) return null;
        var variante = await _variantes.GetByIdAsync(productoVarianteId);
        return variante is null ? null : MapConfiguracion(variante);
    }

    public async Task<ConfiguracionTrazabilidadVarianteDto> ConfigurarAsync(
        int productoVarianteId,
        ConfigurarTrazabilidadVarianteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (productoVarianteId <= 0)
            throw new BusinessRuleException("La variante indicada no es válida.");

        ConfiguracionTrazabilidadVarianteDto? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var variante = await _variantes.GetByIdForUpdateAsync(productoVarianteId)
                ?? throw new BusinessRuleException("La variante indicada no existe.");
            if (!variante.Activo || variante.Eliminado)
                throw new BusinessRuleException("La trazabilidad sólo puede configurarse en una variante activa.");

            if (EsMismaConfiguracion(variante, request))
            {
                resultado = MapConfiguracion(variante);
                return;
            }

            var anterior = new
            {
                variante.ControlaLote,
                variante.ControlaNumeroSerie,
                variante.ControlaFechaVencimiento,
                variante.DiasAlertaVencimiento
            };

            var habilitaDimension =
                (!variante.ControlaLote && request.ControlaLote) ||
                (!variante.ControlaNumeroSerie && request.ControlaNumeroSerie) ||
                (!variante.ControlaFechaVencimiento && request.ControlaFechaVencimiento);
            if (habilitaDimension && await _repository.TieneStockFisicoAsync(variante.Id))
            {
                throw new BusinessRuleException(
                    "No puede habilitarse una dimensión nueva de trazabilidad sobre una variante con stock existente. Registre primero una adopción/apertura trazable explícita.");
            }

            if (variante.ControlaLote && !request.ControlaLote && await _repository.TieneLotesActivosAsync(variante.Id))
                throw new BusinessRuleException("No puede deshabilitarse el control de lote mientras existan lotes activos.");
            if (variante.ControlaNumeroSerie && !request.ControlaNumeroSerie && await _repository.TieneSeriesActivasAsync(variante.Id))
                throw new BusinessRuleException("No puede deshabilitarse el control de series mientras existan series activas.");
            if (!variante.ControlaFechaVencimiento && request.ControlaFechaVencimiento &&
                await _repository.TieneLotesActivosSinVencimientoAsync(variante.Id))
                throw new BusinessRuleException("No puede habilitarse vencimiento mientras existan lotes activos sin fecha de vencimiento.");

            try
            {
                variante.ConfigurarTrazabilidad(
                    request.ControlaLote,
                    request.ControlaNumeroSerie,
                    request.ControlaFechaVencimiento,
                    request.DiasAlertaVencimiento);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentOutOfRangeException)
            {
                throw new BusinessRuleException(ex.Message);
            }

            _variantes.Update(variante);
            await _variantes.SaveChangesAsync();
            resultado = MapConfiguracion(variante);

            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.MovimientosInventario,
                AccionPermiso.Editar,
                "Configuración de trazabilidad de variante actualizada.",
                referenciaId: variante.Id,
                entidad: "ProductoVarianteTrazabilidad",
                valoresAnteriores: anterior,
                valoresNuevos: new
                {
                    variante.ControlaLote,
                    variante.ControlaNumeroSerie,
                    variante.ControlaFechaVencimiento,
                    variante.DiasAlertaVencimiento
                });
        });

        return resultado!;
    }

    public async Task<PagedResult<LoteInventarioDto>> GetLotesAsync(LoteInventarioQueryDto query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidarRangoFechas(query.VenceDesde, query.VenceHasta);
        var (items, total) = await _repository.GetLotesPagedAsync(query);
        return new PagedResult<LoteInventarioDto>
        {
            Items = items.Select(MapLote).ToList(),
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            TotalCount = total
        };
    }

    public async Task<LoteInventarioDto?> GetLoteByIdAsync(int id)
    {
        if (id <= 0) return null;
        var lote = await _repository.GetLoteByIdAsync(id);
        return lote is null ? null : MapLote(lote);
    }

    public async Task<LoteInventarioDto> CrearLoteAsync(CrearLoteInventarioRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ProductoVarianteId <= 0)
            throw new BusinessRuleException("La variante del lote es obligatoria.");

        LoteInventarioDto? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var variante = await _variantes.GetByIdForUpdateAsync(request.ProductoVarianteId)
                ?? throw new BusinessRuleException("La variante indicada no existe.");
            ValidarVarianteParaLote(variante, request.FechaVencimiento);

            var candidato = new LoteInventario { ProductoVarianteId = variante.Id };
            ConfigurarLote(candidato, request.Codigo, request.FechaFabricacion, request.FechaVencimiento, variante.ControlaFechaVencimiento);

            var existente = await _repository.GetLoteByCodigoAsync(variante.Id, candidato.Codigo, tracking: false);
            if (existente is not null)
            {
                resultado = ResolverLoteIdempotente(existente, candidato);
                return;
            }

            MarcarCreacion(candidato);
            if (!await _repository.TryAddLoteAsync(candidato))
            {
                existente = await _repository.GetLoteByCodigoAsync(variante.Id, candidato.Codigo, tracking: false)
                    ?? throw new BusinessRuleException("El lote fue registrado concurrentemente y no pudo recuperarse.");
                resultado = ResolverLoteIdempotente(existente, candidato);
                return;
            }

            resultado = MapLote(candidato);
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.MovimientosInventario,
                AccionPermiso.Crear,
                "Lote de inventario registrado.",
                referenciaId: candidato.Id,
                entidad: "LoteInventario",
                valoresNuevos: new
                {
                    candidato.ProductoVarianteId,
                    candidato.FechaFabricacion,
                    candidato.FechaVencimiento,
                    candidato.Activo
                });
        });
        return resultado!;
    }

    public async Task<LoteInventarioDto> ActualizarLoteAsync(int id, ActualizarLoteInventarioRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (id <= 0) throw new BusinessRuleException("El lote indicado no es válido.");

        LoteInventarioDto? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var lote = await _repository.GetLoteByIdAsync(id, tracking: true)
                ?? throw new BusinessRuleException("El lote indicado no existe.");
            if (!lote.Activo) throw new BusinessRuleException("Un lote inactivo no puede editarse.");

            var anterior = new
            {
                lote.ProductoVarianteId,
                lote.FechaFabricacion,
                lote.FechaVencimiento,
                lote.Activo
            };

            var variante = await _variantes.GetByIdForUpdateAsync(lote.ProductoVarianteId)
                ?? throw new BusinessRuleException("La variante asociada al lote no existe.");
            ValidarVarianteParaLote(variante, request.FechaVencimiento);

            var codigoNormalizado = NormalizarIdentidad(request.Codigo, "El código de lote es obligatorio.");
            var duplicado = await _repository.GetLoteByCodigoAsync(variante.Id, codigoNormalizado);
            if (duplicado is not null && duplicado.Id != lote.Id)
                throw new BusinessRuleException("Ya existe otro lote con el mismo código para la variante.");

            ConfigurarLote(lote, codigoNormalizado, request.FechaFabricacion, request.FechaVencimiento, variante.ControlaFechaVencimiento);
            MarcarActualizacion(lote);
            await _repository.SaveChangesAsync();
            resultado = MapLote(lote);

            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.MovimientosInventario,
                AccionPermiso.Editar,
                "Lote de inventario actualizado.",
                referenciaId: lote.Id,
                entidad: "LoteInventario",
                valoresAnteriores: anterior,
                valoresNuevos: new
                {
                    lote.ProductoVarianteId,
                    lote.FechaFabricacion,
                    lote.FechaVencimiento,
                    lote.Activo
                });
        });
        return resultado!;
    }

    public async Task<LoteInventarioDto> DesactivarLoteAsync(int id)
    {
        if (id <= 0) throw new BusinessRuleException("El lote indicado no es válido.");
        LoteInventarioDto? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var lote = await _repository.GetLoteByIdAsync(id, tracking: true)
                ?? throw new BusinessRuleException("El lote indicado no existe.");
            if (!lote.Activo)
            {
                resultado = MapLote(lote);
                return;
            }
            if (await _repository.TieneSeriesActivasEnLoteAsync(lote.Id))
                throw new BusinessRuleException("El lote no puede desactivarse mientras tenga series activas asociadas.");

            lote.Desactivar();
            MarcarActualizacion(lote);
            await _repository.SaveChangesAsync();
            resultado = MapLote(lote);

            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.MovimientosInventario,
                AccionPermiso.Anular,
                "Lote de inventario desactivado.",
                referenciaId: lote.Id,
                entidad: "LoteInventario",
                valoresNuevos: new { lote.ProductoVarianteId, lote.Activo });
        });
        return resultado!;
    }

    public async Task<PagedResult<SerieInventarioDto>> GetSeriesAsync(SerieInventarioQueryDto query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var (items, total) = await _repository.GetSeriesPagedAsync(query);
        return new PagedResult<SerieInventarioDto>
        {
            Items = items.Select(MapSerie).ToList(),
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 200),
            TotalCount = total
        };
    }

    public async Task<SerieInventarioDto?> GetSerieByIdAsync(int id)
    {
        if (id <= 0) return null;
        var serie = await _repository.GetSerieByIdAsync(id);
        return serie is null ? null : MapSerie(serie);
    }

    public async Task<SerieInventarioDto> CrearSerieAsync(CrearSerieInventarioRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ProductoVarianteId <= 0)
            throw new BusinessRuleException("La variante de la serie es obligatoria.");

        SerieInventarioDto? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var variante = await _variantes.GetByIdForUpdateAsync(request.ProductoVarianteId)
                ?? throw new BusinessRuleException("La variante indicada no existe.");
            if (!variante.Activo || variante.Eliminado || !variante.ControlaNumeroSerie)
                throw new BusinessRuleException("La variante no está activa o no tiene habilitado el control de número de serie.");
            if (request.LoteInventarioId.HasValue && !variante.ControlaLote)
                throw new BusinessRuleException("No puede vincularse un lote cuando la variante no controla lotes.");

            var candidato = new SerieInventario { ProductoVarianteId = variante.Id };
            try
            {
                candidato.ConfigurarIdentidad(request.NumeroSerie);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessRuleException(ex.Message);
            }

            if (request.LoteInventarioId.HasValue)
            {
                var lote = await _repository.GetLoteByIdAsync(request.LoteInventarioId.Value)
                    ?? throw new BusinessRuleException("El lote indicado no existe.");
                if (!lote.Activo) throw new BusinessRuleException("El lote indicado está inactivo.");
                try
                {
                    candidato.VincularLote(lote);
                }
                catch (InvalidOperationException ex)
                {
                    throw new BusinessRuleException(ex.Message);
                }
            }

            var existente = await _repository.GetSerieByNumeroAsync(candidato.NumeroSerie);
            if (existente is not null)
            {
                resultado = ResolverSerieIdempotente(existente, candidato);
                return;
            }

            MarcarCreacion(candidato);
            if (!await _repository.TryAddSerieAsync(candidato))
            {
                existente = await _repository.GetSerieByNumeroAsync(candidato.NumeroSerie)
                    ?? throw new BusinessRuleException("La serie fue registrada concurrentemente y no pudo recuperarse.");
                resultado = ResolverSerieIdempotente(existente, candidato);
                return;
            }

            resultado = MapSerie(candidato);
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.MovimientosInventario,
                AccionPermiso.Crear,
                "Serie de inventario registrada.",
                referenciaId: candidato.Id,
                entidad: "SerieInventario",
                valoresNuevos: new
                {
                    candidato.ProductoVarianteId,
                    candidato.LoteInventarioId,
                    candidato.Estado
                });
        });
        return resultado!;
    }

    public async Task<SerieInventarioDto> DarDeBajaSerieAsync(int id)
    {
        if (id <= 0) throw new BusinessRuleException("La serie indicada no es válida.");
        SerieInventarioDto? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var serie = await _repository.GetSerieByIdAsync(id, tracking: true)
                ?? throw new BusinessRuleException("La serie indicada no existe.");
            if (serie.Estado == EstadoSerieInventario.Baja)
            {
                resultado = MapSerie(serie);
                return;
            }

            var estadoAnterior = serie.Estado;
            try
            {
                serie.DarDeBaja();
            }
            catch (InvalidOperationException ex)
            {
                throw new BusinessRuleException(ex.Message);
            }
            MarcarActualizacion(serie);
            await _repository.SaveChangesAsync();
            resultado = MapSerie(serie);

            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.MovimientosInventario,
                AccionPermiso.Anular,
                "Serie de inventario dada de baja.",
                referenciaId: serie.Id,
                entidad: "SerieInventario",
                valoresAnteriores: new { Estado = estadoAnterior },
                valoresNuevos: new { serie.ProductoVarianteId, serie.LoteInventarioId, serie.Estado });
        });
        return resultado!;
    }

    private static void ValidarVarianteParaLote(ProductoVariante variante, DateTime? fechaVencimiento)
    {
        if (!variante.Activo || variante.Eliminado || !variante.ControlaLote)
            throw new BusinessRuleException("La variante no está activa o no tiene habilitado el control de lote.");
        if (!variante.ControlaFechaVencimiento && fechaVencimiento.HasValue)
            throw new BusinessRuleException("La variante no tiene habilitado el control de fecha de vencimiento.");
    }

    private static void ConfigurarLote(
        LoteInventario lote,
        string codigo,
        DateTime? fechaFabricacion,
        DateTime? fechaVencimiento,
        bool requiereVencimiento)
    {
        try
        {
            lote.ConfigurarIdentidad(codigo, fechaFabricacion, fechaVencimiento, requiereVencimiento);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }

    private static LoteInventarioDto ResolverLoteIdempotente(LoteInventario existente, LoteInventario candidato)
    {
        if (!existente.Activo)
            throw new BusinessRuleException("Ya existe un lote inactivo con ese código; no se reactiva implícitamente.");
        if (existente.FechaFabricacion?.Date != candidato.FechaFabricacion?.Date ||
            existente.FechaVencimiento?.Date != candidato.FechaVencimiento?.Date)
            throw new BusinessRuleException("La clave idempotente del lote ya existe con datos diferentes.");
        return MapLote(existente);
    }

    private static SerieInventarioDto ResolverSerieIdempotente(SerieInventario existente, SerieInventario candidato)
    {
        if (existente.ProductoVarianteId != candidato.ProductoVarianteId ||
            existente.LoteInventarioId != candidato.LoteInventarioId)
            throw new BusinessRuleException("El número de serie ya existe con una identidad logística diferente.");
        return MapSerie(existente);
    }

    private static bool EsMismaConfiguracion(ProductoVariante variante, ConfigurarTrazabilidadVarianteRequest request) =>
        variante.ControlaLote == request.ControlaLote &&
        variante.ControlaNumeroSerie == request.ControlaNumeroSerie &&
        variante.ControlaFechaVencimiento == request.ControlaFechaVencimiento &&
        variante.DiasAlertaVencimiento == request.DiasAlertaVencimiento;

    private static void ValidarRangoFechas(DateTime? desde, DateTime? hasta)
    {
        if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            throw new BusinessRuleException("La fecha inicial de vencimiento no puede ser posterior a la fecha final.");
    }

    private static string NormalizarIdentidad(string valor, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new BusinessRuleException(mensaje);
        return valor.Trim().ToUpperInvariant();
    }

    private void MarcarCreacion(LoteInventario entidad)
    {
        entidad.FechaCreacion = DateTime.UtcNow;
        entidad.FechaActualizacion = entidad.FechaCreacion;
        entidad.CreadoPorUsuarioId = _currentUser.UsuarioId;
        entidad.CreadoPorNombreUsuario = _currentUser.NombreUsuario;
        entidad.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        entidad.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
    }

    private void MarcarCreacion(SerieInventario entidad)
    {
        entidad.FechaCreacion = DateTime.UtcNow;
        entidad.FechaActualizacion = entidad.FechaCreacion;
        entidad.CreadoPorUsuarioId = _currentUser.UsuarioId;
        entidad.CreadoPorNombreUsuario = _currentUser.NombreUsuario;
        entidad.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        entidad.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
    }

    private void MarcarActualizacion(LoteInventario entidad)
    {
        entidad.FechaActualizacion = DateTime.UtcNow;
        entidad.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        entidad.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
    }

    private void MarcarActualizacion(SerieInventario entidad)
    {
        entidad.FechaActualizacion = DateTime.UtcNow;
        entidad.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        entidad.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
    }

    private static ConfiguracionTrazabilidadVarianteDto MapConfiguracion(ProductoVariante variante) => new()
    {
        ProductoVarianteId = variante.Id,
        ControlaLote = variante.ControlaLote,
        ControlaNumeroSerie = variante.ControlaNumeroSerie,
        ControlaFechaVencimiento = variante.ControlaFechaVencimiento,
        DiasAlertaVencimiento = variante.DiasAlertaVencimiento
    };

    private static LoteInventarioDto MapLote(LoteInventario lote) => new()
    {
        Id = lote.Id,
        ProductoVarianteId = lote.ProductoVarianteId,
        Codigo = lote.Codigo,
        FechaFabricacion = lote.FechaFabricacion,
        FechaVencimiento = lote.FechaVencimiento,
        Activo = lote.Activo
    };

    private static SerieInventarioDto MapSerie(SerieInventario serie) => new()
    {
        Id = serie.Id,
        ProductoVarianteId = serie.ProductoVarianteId,
        LoteInventarioId = serie.LoteInventarioId,
        NumeroSerie = serie.NumeroSerie,
        Estado = serie.Estado
    };
}
