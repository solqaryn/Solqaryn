using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class CuentaPorPagarService : ICuentaPorPagarService
{
    private const string EntidadAuditoria = "CuentaPorPagar";
    private readonly ICuentaPorPagarRepository _repository;
    private readonly IFacturaProveedorRepository _facturas;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public CuentaPorPagarService(
        ICuentaPorPagarRepository repository,
        IFacturaProveedorRepository facturas,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _facturas = facturas ?? throw new ArgumentNullException(nameof(facturas));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<PagedResult<CuentaPorPagarDto>> GetPagedAsync(CuentaPorPagarFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        NormalizarFiltro(filtro);
        var (items, total) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<CuentaPorPagarDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = filtro.Page,
            PageSize = filtro.PageSize
        };
    }

    public async Task<CuentaPorPagarDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la cuenta por pagar debe ser válido.");

        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : Map(entity);
    }

    public async Task<CuentaPorPagarDto> GenerarAsync(GenerarCuentaPorPagarDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var usuarioId = ObtenerUsuarioId();

        if (dto.FacturaProveedorId <= 0)
            throw new BusinessRuleException("La factura de proveedor debe ser válida.");
        if (!Enum.IsDefined(typeof(CondicionPagoProveedor), dto.CondicionPago))
            throw new BusinessRuleException("La condición de pago no es válida.");

        var factura = await _facturas.GetByIdAsync(dto.FacturaProveedorId)
            ?? throw new ResourceNotFoundException("Factura de proveedor no encontrada.");
        if (factura.Estado != EstadoFacturaProveedor.Registrada)
            throw new BusinessRuleException("La obligación sólo puede generarse desde una factura de proveedor registrada.");
        if (factura.Total <= 0m)
            throw new BusinessRuleException("La factura registrada debe tener un total positivo.");

        var vencimiento = ResolverVencimiento(dto, factura);
        var existente = await _repository.GetByFacturaProveedorIdAsync(factura.Id);
        if (existente is not null)
            return Map(ResolverReintentoGeneracion(existente, factura, dto.CondicionPago, vencimiento));

        CuentaPorPagar? creada = null;
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var concurrente = await _repository.GetByFacturaProveedorIdAsync(factura.Id, tracking: true);
                if (concurrente is not null)
                {
                    creada = ResolverReintentoGeneracion(concurrente, factura, dto.CondicionPago, vencimiento);
                    return;
                }

                var ahora = DateTime.UtcNow;
                var entity = new CuentaPorPagar
                {
                    FacturaProveedorId = factura.Id,
                    ProveedorId = factura.ProveedorId,
                    Moneda = factura.Moneda.Trim().ToUpperInvariant(),
                    CondicionPago = dto.CondicionPago,
                    FechaEmisionUtc = factura.FechaEmisionUtc,
                    FechaVencimientoUtc = vencimiento,
                    MontoOriginal = factura.Total,
                    FechaCreacion = ahora,
                    FechaActualizacion = ahora,
                    CreadoPorUsuarioId = usuarioId,
                    CreadoPorNombreUsuario = NombreUsuarioAuditoria()
                };
                ValidarDominio(entity.Validar);

                await _repository.AddAsync(entity);
                await _repository.SaveChangesAsync();
                await RegistrarAuditoriaAsync(
                    AccionPermiso.Crear,
                    "Generación de cuenta por pagar desde factura de proveedor registrada.",
                    entity.Id,
                    valoresNuevos: Snapshot(entity));
                creada = entity;
            });
        }
        catch (UniqueConstraintViolationException)
        {
            var concurrente = await _repository.GetByFacturaProveedorIdAsync(factura.Id)
                ?? throw new ConflictException("La cuenta por pagar fue creada concurrentemente y no pudo recuperarse de forma segura.");
            creada = ResolverReintentoGeneracion(concurrente, factura, dto.CondicionPago, vencimiento);
        }

        return Map(creada ?? throw new InvalidOperationException("La generación de la cuenta por pagar no produjo un resultado."));
    }

    public async Task<CuentaPorPagarDto> AplicarAsync(int id, AplicarCuentaPorPagarDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ObtenerUsuarioId();
        ValidarFechaUtc(dto.FechaAplicacionUtc, "La fecha de aplicación");
        if (!Enum.IsDefined(typeof(TipoAplicacionCuentaPorPagar), dto.Tipo))
            throw new BusinessRuleException("El tipo de aplicación no es válido.");
        if (dto.Monto <= 0m)
            throw new BusinessRuleException("El monto aplicado debe ser mayor que cero.");
        if (string.IsNullOrWhiteSpace(dto.IdempotencyKey) || dto.IdempotencyKey.Trim().Length > 128)
            throw new BusinessRuleException("La clave de idempotencia es obligatoria y no puede superar 128 caracteres.");

        CuentaPorPagar? actualizada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await ObtenerBloqueadaAsync(id);
            var clave = dto.IdempotencyKey.Trim();
            var referencia = string.IsNullOrWhiteSpace(dto.ReferenciaExterna) ? null : dto.ReferenciaExterna.Trim();
            var replay = entity.Aplicaciones.FirstOrDefault(x => x.IdempotencyKey == clave);
            if (replay is not null)
            {
                if (replay.Tipo != dto.Tipo || replay.Monto != dto.Monto
                    || !string.Equals(replay.ReferenciaExterna, referencia, StringComparison.Ordinal))
                    throw new ConflictException("La clave de idempotencia ya fue utilizada con un payload diferente.");

                actualizada = entity;
                return;
            }

            var anterior = Snapshot(entity);
            AplicacionCuentaPorPagar aplicacion;
            try
            {
                aplicacion = entity.Aplicar(
                    dto.Tipo,
                    dto.Monto,
                    clave,
                    dto.FechaAplicacionUtc,
                    referencia);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                throw new BusinessRuleException(ex.Message);
            }

            var usuarioId = ObtenerUsuarioId();
            var ahora = DateTime.UtcNow;
            aplicacion.CreadoPorUsuarioId = aplicacion.CreadoPorUsuarioId is > 0 ? aplicacion.CreadoPorUsuarioId : usuarioId;
            aplicacion.CreadoPorNombreUsuario ??= NombreUsuarioAuditoria();
            aplicacion.FechaCreacion = aplicacion.FechaCreacion == default ? ahora : aplicacion.FechaCreacion;
            aplicacion.FechaActualizacion = ahora;
            entity.FechaActualizacion = ahora;
            entity.ActualizadoPorUsuarioId = usuarioId;
            entity.ActualizadoPorNombreUsuario = NombreUsuarioAuditoria();

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Editar,
                $"Aplicación {dto.Tipo} sobre cuenta por pagar.",
                entity.Id,
                anterior,
                Snapshot(entity));
            actualizada = entity;
        });

        return Map(actualizada ?? throw new InvalidOperationException("La aplicación no produjo un resultado."));
    }

    public async Task<CuentaPorPagarDto> RevertirAplicacionAsync(int id, RevertirAplicacionCuentaPorPagarDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ObtenerUsuarioId();
        ValidarFechaUtc(dto.FechaReversionUtc, "La fecha de reversión");

        if (string.IsNullOrWhiteSpace(dto.IdempotencyKey))
            throw new BusinessRuleException("La clave de idempotencia de la aplicación es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.Motivo) || dto.Motivo.Trim().Length > 500)
            throw new BusinessRuleException("El motivo de reversión es obligatorio y no puede superar 500 caracteres.");

        CuentaPorPagar? actualizada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await ObtenerBloqueadaAsync(id);
            var clave = dto.IdempotencyKey.Trim();
            var existente = entity.Aplicaciones.SingleOrDefault(x => x.IdempotencyKey == clave)
                ?? throw new BusinessRuleException("La aplicación indicada no existe.");
            if (existente.Revertida)
            {
                actualizada = entity;
                return;
            }

            var anterior = Snapshot(entity);
            try
            {
                entity.RevertirAplicacion(clave, dto.Motivo, dto.FechaReversionUtc);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                throw new BusinessRuleException(ex.Message);
            }

            entity.FechaActualizacion = DateTime.UtcNow;
            entity.ActualizadoPorUsuarioId = ObtenerUsuarioId();
            entity.ActualizadoPorNombreUsuario = NombreUsuarioAuditoria();

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Editar,
                "Reversión de aplicación de cuenta por pagar.",
                entity.Id,
                anterior,
                Snapshot(entity),
                dto.Motivo.Trim());
            actualizada = entity;
        });

        return Map(actualizada ?? throw new InvalidOperationException("La reversión no produjo un resultado."));
    }

    public async Task<CuentaPorPagarDto> AnularAsync(int id, AnularCuentaPorPagarDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ObtenerUsuarioId();
        ValidarFechaUtc(dto.FechaAnulacionUtc, "La fecha de anulación");
        if (string.IsNullOrWhiteSpace(dto.Motivo) || dto.Motivo.Trim().Length > 500)
            throw new BusinessRuleException("El motivo de anulación es obligatorio y no puede superar 500 caracteres.");

        CuentaPorPagar? anulada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await ObtenerBloqueadaAsync(id);
            if (entity.Estado == EstadoCuentaPorPagar.Anulada)
            {
                anulada = entity;
                return;
            }

            var anterior = Snapshot(entity);
            try
            {
                entity.Anular(dto.Motivo, dto.FechaAnulacionUtc);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                throw new BusinessRuleException(ex.Message);
            }

            entity.FechaActualizacion = DateTime.UtcNow;
            entity.ActualizadoPorUsuarioId = ObtenerUsuarioId();
            entity.ActualizadoPorNombreUsuario = NombreUsuarioAuditoria();

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Anular,
                "Anulación de cuenta por pagar.",
                entity.Id,
                anterior,
                Snapshot(entity),
                dto.Motivo.Trim());
            anulada = entity;
        });

        return Map(anulada ?? throw new InvalidOperationException("La anulación no produjo un resultado."));
    }

    private async Task<CuentaPorPagar> ObtenerBloqueadaAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la cuenta por pagar debe ser válido.");

        return await _repository.GetByIdForUpdateAsync(id)
            ?? throw new ResourceNotFoundException("Cuenta por pagar no encontrada.");
    }

    private static DateTime ResolverVencimiento(GenerarCuentaPorPagarDto dto, FacturaProveedor factura)
    {
        if (factura.FechaEmisionUtc.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException("La fecha de emisión de la factura debe expresarse en UTC.");

        if (dto.CondicionPago == CondicionPagoProveedor.Contado)
        {
            if (dto.FechaVencimientoUtc.HasValue && dto.FechaVencimientoUtc.Value != factura.FechaEmisionUtc)
                throw new BusinessRuleException("Una cuenta por pagar de contado debe vencer en la fecha de emisión.");
            return factura.FechaEmisionUtc;
        }

        var vencimiento = dto.FechaVencimientoUtc ?? factura.FechaVencimientoUtc
            ?? throw new BusinessRuleException("Una cuenta por pagar a crédito requiere fecha de vencimiento.");
        ValidarFechaUtc(vencimiento, "La fecha de vencimiento");
        if (vencimiento <= factura.FechaEmisionUtc)
            throw new BusinessRuleException("Una cuenta por pagar a crédito debe vencer después de la fecha de emisión.");
        return vencimiento;
    }

    private static CuentaPorPagar ResolverReintentoGeneracion(
        CuentaPorPagar existente,
        FacturaProveedor factura,
        CondicionPagoProveedor condicion,
        DateTime vencimiento)
    {
        if (existente.FacturaProveedorId != factura.Id
            || existente.ProveedorId != factura.ProveedorId
            || !string.Equals(existente.Moneda, factura.Moneda.Trim().ToUpperInvariant(), StringComparison.Ordinal)
            || existente.CondicionPago != condicion
            || existente.FechaEmisionUtc != factura.FechaEmisionUtc
            || existente.FechaVencimientoUtc != vencimiento
            || existente.MontoOriginal != factura.Total)
        {
            throw new ConflictException("La factura de proveedor ya tiene una cuenta por pagar con parámetros diferentes.");
        }

        return existente;
    }

    private static void NormalizarFiltro(CuentaPorPagarFiltroDto filtro)
    {
        if (filtro.ProveedorId is <= 0 || filtro.FacturaProveedorId is <= 0)
            throw new BusinessRuleException("Los filtros de identificadores deben ser válidos.");
        if (filtro.Estado.HasValue && !Enum.IsDefined(typeof(EstadoCuentaPorPagar), filtro.Estado.Value))
            throw new BusinessRuleException("El estado de cuenta por pagar no es válido.");
        if (filtro.CondicionPago.HasValue && !Enum.IsDefined(typeof(CondicionPagoProveedor), filtro.CondicionPago.Value))
            throw new BusinessRuleException("La condición de pago no es válida.");
        if (filtro.VenceDesdeUtc.HasValue)
            ValidarFechaUtc(filtro.VenceDesdeUtc.Value, "La fecha inicial de vencimiento");
        if (filtro.VenceHastaUtc.HasValue)
            ValidarFechaUtc(filtro.VenceHastaUtc.Value, "La fecha final de vencimiento");
        if (filtro.VenceDesdeUtc.HasValue && filtro.VenceHastaUtc.HasValue && filtro.VenceDesdeUtc > filtro.VenceHastaUtc)
            throw new BusinessRuleException("El rango de vencimiento es inválido.");
        if (!string.IsNullOrWhiteSpace(filtro.Moneda) && filtro.Moneda.Trim().Length != 3)
            throw new BusinessRuleException("El filtro de moneda debe usar un código ISO de tres caracteres.");

        filtro.Page = Math.Max(1, filtro.Page);
        filtro.PageSize = Math.Clamp(filtro.PageSize, 1, 100);
        filtro.Moneda = string.IsNullOrWhiteSpace(filtro.Moneda) ? null : filtro.Moneda.Trim().ToUpperInvariant();
        filtro.SortDirection = string.Equals(filtro.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
    }

    private int ObtenerUsuarioId() => _currentUser.EstaAutenticado && _currentUser.UsuarioId is > 0
        ? _currentUser.UsuarioId.Value
        : throw new ForbiddenAccessException("No existe un usuario autenticado válido para ejecutar la operación.");

    private string? NombreUsuarioAuditoria() =>
        !string.IsNullOrWhiteSpace(_currentUser.NombreCompleto)
            ? _currentUser.NombreCompleto.Trim()
            : string.IsNullOrWhiteSpace(_currentUser.NombreUsuario) ? null : _currentUser.NombreUsuario.Trim();

    private Task RegistrarAuditoriaAsync(
        AccionPermiso accion,
        string descripcion,
        int referenciaId,
        object? valoresAnteriores = null,
        object? valoresNuevos = null,
        string? motivo = null) =>
        _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Finanzas,
            accion,
            descripcion,
            referenciaId,
            EntidadAuditoria,
            valoresAnteriores,
            valoresNuevos,
            motivo);

    private static object Snapshot(CuentaPorPagar x) => new
    {
        x.Id,
        x.FacturaProveedorId,
        x.ProveedorId,
        x.Moneda,
        x.CondicionPago,
        x.FechaEmisionUtc,
        x.FechaVencimientoUtc,
        x.MontoOriginal,
        x.MontoAplicado,
        x.Saldo,
        x.Estado
    };

    private static CuentaPorPagarDto Map(CuentaPorPagar x) => new()
    {
        Id = x.Id,
        FacturaProveedorId = x.FacturaProveedorId,
        ProveedorId = x.ProveedorId,
        Moneda = x.Moneda,
        CondicionPago = x.CondicionPago,
        FechaEmisionUtc = x.FechaEmisionUtc,
        FechaVencimientoUtc = x.FechaVencimientoUtc,
        MontoOriginal = x.MontoOriginal,
        MontoAplicado = x.MontoAplicado,
        Saldo = x.Saldo,
        Estado = x.Estado,
        FechaAnulacionUtc = x.FechaAnulacionUtc,
        MotivoAnulacion = x.MotivoAnulacion,
        Aplicaciones = x.Aplicaciones
            .OrderBy(a => a.FechaAplicacionUtc)
            .ThenBy(a => a.Id)
            .Select(a => new AplicacionCuentaPorPagarDto
            {
                Id = a.Id,
                Tipo = a.Tipo,
                Monto = a.Monto,
                IdempotencyKey = a.IdempotencyKey,
                ReferenciaExterna = a.ReferenciaExterna,
                FechaAplicacionUtc = a.FechaAplicacionUtc,
                Revertida = a.Revertida,
                FechaReversionUtc = a.FechaReversionUtc,
                MotivoReversion = a.MotivoReversion
            })
            .ToList()
    };

    private static void ValidarDominio(Action action)
    {
        try
        {
            action();
        }
        catch (BusinessRuleException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            throw new BusinessRuleException(ex.Message);
        }
    }

    private static void ValidarFechaUtc(DateTime value, string campo)
    {
        if (value == default || value.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException($"{campo} debe expresarse en UTC.");
    }
}
