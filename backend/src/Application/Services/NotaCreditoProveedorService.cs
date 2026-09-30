using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Application.Services;

public sealed class NotaCreditoProveedorService : INotaCreditoProveedorService
{
    private const string EntidadAuditoria = "NotaCreditoProveedor";
    private const string NumeroConstraint = "UX_NotasCreditoProveedor_Proveedor_Numero";

    private readonly INotaCreditoProveedorRepository _repository;
    private readonly IFacturaProveedorRepository _facturas;
    private readonly IProveedorRepository _proveedores;
    private readonly IDevolucionProveedorRepository _devoluciones;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;
    private readonly ILogger<NotaCreditoProveedorService> _logger;

    public NotaCreditoProveedorService(
        INotaCreditoProveedorRepository repository,
        IFacturaProveedorRepository facturas,
        IProveedorRepository proveedores,
        IDevolucionProveedorRepository devoluciones,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria,
        ILogger<NotaCreditoProveedorService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _facturas = facturas ?? throw new ArgumentNullException(nameof(facturas));
        _proveedores = proveedores ?? throw new ArgumentNullException(nameof(proveedores));
        _devoluciones = devoluciones ?? throw new ArgumentNullException(nameof(devoluciones));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResult<NotaCreditoProveedorDto>> GetPagedAsync(NotaCreditoProveedorFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        NormalizarFiltro(filtro);
        var (items, total) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<NotaCreditoProveedorDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = filtro.Page,
            PageSize = filtro.PageSize
        };
    }

    public async Task<NotaCreditoProveedorDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la nota de crédito debe ser válido.");

        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : Map(entity);
    }

    public async Task<NotaCreditoProveedorDto> CreateAsync(CreateNotaCreditoProveedorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        NormalizarYValidar(dto);
        var usuarioId = ObtenerUsuarioId();

        var factura = await ObtenerFacturaRegistradaAsync(dto.FacturaProveedorId);
        var proveedor = await _proveedores.GetByIdAsync(factura.ProveedorId)
            ?? throw new BusinessRuleException("El proveedor asociado a la factura no existe.");
        await ValidarDevolucionOpcionalAsync(dto.DevolucionProveedorId, factura);

        var numero = NormalizarNumero(dto.NumeroNotaCredito);
        var existente = await _repository.GetByProveedorNumeroAsync(factura.ProveedorId, numero);
        if (existente is not null)
            return Map(ResolverReintento(existente, dto, factura.ProveedorId));

        NotaCreditoProveedor? creada = null;
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var concurrente = await _repository.GetByProveedorNumeroAsync(factura.ProveedorId, numero, tracking: true);
                if (concurrente is not null)
                {
                    creada = ResolverReintento(concurrente, dto, factura.ProveedorId);
                    return;
                }

                var ahora = DateTime.UtcNow;
                var entity = new NotaCreditoProveedor
                {
                    NumeroNotaCredito = numero,
                    ProveedorId = factura.ProveedorId,
                    FacturaProveedorId = dto.FacturaProveedorId,
                    DevolucionProveedorId = dto.DevolucionProveedorId,
                    ProveedorNombreSnapshot = proveedor.Nombre.Trim(),
                    Moneda = NormalizarMoneda(dto.Moneda),
                    FechaEmisionUtc = dto.FechaEmisionUtc,
                    ReferenciaFiscal = Normalizar(dto.ReferenciaFiscal),
                    Motivo = dto.Motivo.Trim(),
                    Observaciones = Normalizar(dto.Observaciones),
                    SubtotalCredito = dto.SubtotalCredito,
                    ImpuestoCredito = dto.ImpuestoCredito,
                    FechaCreacion = ahora,
                    FechaActualizacion = ahora,
                    CreadoPorUsuarioId = usuarioId,
                    CreadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario)
                };
                ValidarDominio(entity.ValidarDocumento);

                await _repository.AddAsync(entity);
                await _repository.SaveChangesAsync();
                await RegistrarAuditoriaAsync(
                    AccionPermiso.Crear,
                    "Creación de nota de crédito de proveedor.",
                    entity.Id,
                    valoresNuevos: Snapshot(entity));
                creada = entity;
            });
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == NumeroConstraint)
        {
            _logger.LogWarning(ex, "Colisión concurrente al crear nota de crédito del proveedor {ProveedorId}.", factura.ProveedorId);
            var concurrente = await _repository.GetByProveedorNumeroAsync(factura.ProveedorId, numero)
                ?? throw new ConflictException("La nota de crédito fue creada concurrentemente y no pudo recuperarse de forma segura.");
            creada = ResolverReintento(concurrente, dto, factura.ProveedorId);
        }

        return Map(creada ?? throw new InvalidOperationException("La creación de la nota de crédito no produjo un resultado."));
    }

    public async Task<NotaCreditoProveedorDto> UpdateAsync(int id, UpdateNotaCreditoProveedorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        NormalizarYValidar(dto);
        var usuarioId = ObtenerUsuarioId();

        NotaCreditoProveedor? actualizada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await ObtenerBloqueadaAsync(id);
            ValidarDominio(entity.AsegurarEditable);
            var anterior = Snapshot(entity);

            var numero = NormalizarNumero(dto.NumeroNotaCredito);
            var conflicto = await _repository.GetByProveedorNumeroAsync(entity.ProveedorId, numero, tracking: true);
            if (conflicto is not null && conflicto.Id != entity.Id)
                throw new ConflictException("El número de nota de crédito ya existe para el proveedor indicado.");

            entity.NumeroNotaCredito = numero;
            entity.FechaEmisionUtc = dto.FechaEmisionUtc;
            entity.Moneda = NormalizarMoneda(dto.Moneda);
            entity.ReferenciaFiscal = Normalizar(dto.ReferenciaFiscal);
            entity.Motivo = dto.Motivo.Trim();
            entity.Observaciones = Normalizar(dto.Observaciones);
            entity.SubtotalCredito = dto.SubtotalCredito;
            entity.ImpuestoCredito = dto.ImpuestoCredito;
            entity.FechaActualizacion = DateTime.UtcNow;
            entity.ActualizadoPorUsuarioId = usuarioId;
            entity.ActualizadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario);
            ValidarDominio(entity.ValidarDocumento);

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Editar,
                "Edición de nota de crédito de proveedor en borrador.",
                entity.Id,
                anterior,
                Snapshot(entity));
            actualizada = entity;
        });

        return Map(actualizada ?? throw new InvalidOperationException("La actualización de la nota de crédito no produjo un resultado."));
    }

    public async Task<NotaCreditoProveedorDto> RegistrarAsync(int id)
    {
        NotaCreditoProveedor? registrada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await ObtenerBloqueadaAsync(id);
            if (entity.Estado == EstadoNotaCreditoProveedor.Registrada)
            {
                registrada = entity;
                return;
            }
            if (entity.Estado != EstadoNotaCreditoProveedor.Borrador)
                throw new BusinessRuleException("Solo una nota de crédito de proveedor en borrador puede registrarse.");

            var factura = await ObtenerFacturaRegistradaAsync(entity.FacturaProveedorId);
            if (factura.ProveedorId != entity.ProveedorId)
                throw new BusinessRuleException("La nota de crédito no coincide con el proveedor de la factura.");
            await ValidarDevolucionOpcionalAsync(entity.DevolucionProveedorId, factura);

            var creditoPrevio = await _repository.GetCreditoRegistradoAcumuladoPorFacturaAsync(factura.Id, entity.Id);
            if (creditoPrevio + entity.TotalCredito > factura.Total)
                throw new BusinessRuleException("El crédito acumulado no puede superar el total registrado de la factura de proveedor.");

            var anterior = Snapshot(entity);
            var usuarioId = ObtenerUsuarioId();
            ValidarDominio(() => entity.Registrar(usuarioId, NombreUsuarioAuditoria(), DateTime.UtcNow));
            entity.FechaActualizacion = DateTime.UtcNow;
            entity.ActualizadoPorUsuarioId = usuarioId;
            entity.ActualizadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario);

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Confirmar,
                "Registro de nota de crédito de proveedor.",
                entity.Id,
                anterior,
                Snapshot(entity));
            registrada = entity;
        });

        return Map(registrada ?? throw new InvalidOperationException("El registro de la nota de crédito no produjo un resultado."));
    }

    public async Task<NotaCreditoProveedorDto> AnularAsync(int id, AnularNotaCreditoProveedorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var motivo = Normalizar(dto.Motivo) ?? throw new BusinessRuleException("El motivo de anulación es obligatorio.");
        if (motivo.Length > 500)
            throw new BusinessRuleException("El motivo de anulación no puede superar 500 caracteres.");

        NotaCreditoProveedor? anulada = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await ObtenerBloqueadaAsync(id);
            if (entity.Estado == EstadoNotaCreditoProveedor.Anulada)
            {
                anulada = entity;
                return;
            }
            if (entity.Estado != EstadoNotaCreditoProveedor.Registrada)
                throw new BusinessRuleException("Solo una nota de crédito de proveedor registrada puede anularse.");

            var anterior = Snapshot(entity);
            var usuarioId = ObtenerUsuarioId();
            ValidarDominio(() => entity.Anular(usuarioId, motivo, DateTime.UtcNow));
            entity.FechaActualizacion = DateTime.UtcNow;
            entity.ActualizadoPorUsuarioId = usuarioId;
            entity.ActualizadoPorNombreUsuario = Normalizar(_currentUser.NombreUsuario);

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaAsync(
                AccionPermiso.Anular,
                "Anulación de nota de crédito de proveedor.",
                entity.Id,
                anterior,
                Snapshot(entity),
                motivo);
            anulada = entity;
        });

        return Map(anulada ?? throw new InvalidOperationException("La anulación de la nota de crédito no produjo un resultado."));
    }

    private async Task<FacturaProveedor> ObtenerFacturaRegistradaAsync(int facturaId)
    {
        if (facturaId <= 0)
            throw new BusinessRuleException("La factura de proveedor debe ser válida.");

        var factura = await _facturas.GetByIdAsync(facturaId)
            ?? throw new BusinessRuleException("La factura de proveedor especificada no existe.");
        if (factura.Estado != EstadoFacturaProveedor.Registrada)
            throw new BusinessRuleException("La nota de crédito sólo puede respaldarse en una factura de proveedor registrada.");
        return factura;
    }

    private async Task ValidarDevolucionOpcionalAsync(int? devolucionId, FacturaProveedor factura)
    {
        if (!devolucionId.HasValue)
            return;

        var devolucion = await _devoluciones.GetByIdAsync(devolucionId.Value)
            ?? throw new BusinessRuleException("La devolución de proveedor especificada no existe.");
        if (devolucion.Estado != EstadoDevolucionProveedor.Confirmada)
            throw new BusinessRuleException("La devolución vinculada debe estar confirmada.");
        if (devolucion.FacturaProveedorId != factura.Id || devolucion.ProveedorId != factura.ProveedorId)
            throw new BusinessRuleException("La devolución vinculada no corresponde a la factura y proveedor indicados.");
    }

    private async Task<NotaCreditoProveedor> ObtenerBloqueadaAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la nota de crédito debe ser válido.");
        return await _repository.GetByIdForUpdateAsync(id)
            ?? throw new ResourceNotFoundException("Nota de crédito de proveedor no encontrada.");
    }

    private static NotaCreditoProveedor ResolverReintento(
        NotaCreditoProveedor existente,
        CreateNotaCreditoProveedorDto dto,
        int proveedorId)
    {
        if (existente.ProveedorId != proveedorId
            || existente.FacturaProveedorId != dto.FacturaProveedorId
            || existente.DevolucionProveedorId != dto.DevolucionProveedorId
            || !string.Equals(existente.NumeroNotaCredito, NormalizarNumero(dto.NumeroNotaCredito), StringComparison.Ordinal)
            || !string.Equals(existente.Moneda, NormalizarMoneda(dto.Moneda), StringComparison.Ordinal)
            || existente.FechaEmisionUtc != dto.FechaEmisionUtc
            || !string.Equals(existente.ReferenciaFiscal, Normalizar(dto.ReferenciaFiscal), StringComparison.Ordinal)
            || !string.Equals(existente.Motivo, dto.Motivo.Trim(), StringComparison.Ordinal)
            || !string.Equals(existente.Observaciones, Normalizar(dto.Observaciones), StringComparison.Ordinal)
            || existente.SubtotalCredito != dto.SubtotalCredito
            || existente.ImpuestoCredito != dto.ImpuestoCredito)
        {
            throw new ConflictException("El número de nota de crédito ya existe para el proveedor con un payload diferente.");
        }

        return existente;
    }

    private static void NormalizarYValidar(CreateNotaCreditoProveedorDto dto)
    {
        ValidarFechaUtc(dto.FechaEmisionUtc);
        ValidarCabecera(dto.NumeroNotaCredito, dto.Moneda, dto.Motivo, dto.ReferenciaFiscal, dto.Observaciones, dto.SubtotalCredito, dto.ImpuestoCredito);
        if (dto.FacturaProveedorId <= 0)
            throw new BusinessRuleException("La factura de proveedor debe ser válida.");
        if (dto.DevolucionProveedorId is <= 0)
            throw new BusinessRuleException("La devolución de proveedor, cuando se informa, debe ser válida.");
    }

    private static void NormalizarYValidar(UpdateNotaCreditoProveedorDto dto)
    {
        ValidarFechaUtc(dto.FechaEmisionUtc);
        ValidarCabecera(dto.NumeroNotaCredito, dto.Moneda, dto.Motivo, dto.ReferenciaFiscal, dto.Observaciones, dto.SubtotalCredito, dto.ImpuestoCredito);
    }

    private static void ValidarCabecera(
        string numero, string moneda, string motivo, string? referenciaFiscal, string? observaciones,
        decimal subtotal, decimal impuesto)
    {
        var numeroNormalizado = Normalizar(numero);
        if (numeroNormalizado is null || numeroNormalizado.Length > 80)
            throw new BusinessRuleException("El número de nota de crédito es obligatorio y no puede superar 80 caracteres.");
        var monedaNormalizada = Normalizar(moneda);
        if (monedaNormalizada is null || monedaNormalizada.Length != 3)
            throw new BusinessRuleException("La moneda debe usar un código ISO de tres caracteres.");
        var motivoNormalizado = Normalizar(motivo);
        if (motivoNormalizado is null || motivoNormalizado.Length > 500)
            throw new BusinessRuleException("El motivo es obligatorio y no puede superar 500 caracteres.");
        if (Normalizar(referenciaFiscal)?.Length > 120)
            throw new BusinessRuleException("La referencia fiscal no puede superar 120 caracteres.");
        if (Normalizar(observaciones)?.Length > 1000)
            throw new BusinessRuleException("Las observaciones no pueden superar 1000 caracteres.");
        if (subtotal < 0m || impuesto < 0m || decimal.Round(subtotal + impuesto, 4, MidpointRounding.AwayFromZero) <= 0m)
            throw new BusinessRuleException("El total acreditado debe ser mayor que cero y sus componentes no pueden ser negativos.");
    }

    private static void ValidarFechaUtc(DateTime value)
    {
        if (value == default)
            throw new BusinessRuleException("La fecha de emisión es obligatoria.");
        if (value.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException("La fecha de emisión debe expresarse en UTC.");
    }

    private static void NormalizarFiltro(NotaCreditoProveedorFiltroDto filtro)
    {
        if (filtro.ProveedorId is <= 0 || filtro.FacturaProveedorId is <= 0 || filtro.DevolucionProveedorId is <= 0)
            throw new BusinessRuleException("Los filtros de identificadores deben ser válidos.");
        if (filtro.Desde.HasValue)
            ValidarFechaUtc(filtro.Desde.Value);
        if (filtro.Hasta.HasValue)
            ValidarFechaUtc(filtro.Hasta.Value);
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde > filtro.Hasta)
            throw new BusinessRuleException("El rango de fechas es inválido.");

        filtro.Page = Math.Max(1, filtro.Page);
        filtro.PageSize = Math.Clamp(filtro.PageSize, 1, 100);
        filtro.Numero = Normalizar(filtro.Numero);
        filtro.Search = Normalizar(filtro.Search);
        filtro.SortBy = Normalizar(filtro.SortBy);
        filtro.SortDirection = string.Equals(filtro.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
    }

    private int ObtenerUsuarioId() => _currentUser.EstaAutenticado && _currentUser.UsuarioId is > 0
        ? _currentUser.UsuarioId.Value
        : throw new ForbiddenAccessException("No existe un usuario autenticado válido para ejecutar la operación.");

    private string? NombreUsuarioAuditoria() =>
        Normalizar(_currentUser.NombreCompleto) ?? Normalizar(_currentUser.NombreUsuario);

    private Task RegistrarAuditoriaAsync(
        AccionPermiso accion,
        string descripcion,
        int referenciaId,
        object? valoresAnteriores = null,
        object? valoresNuevos = null,
        string? motivo = null) =>
        _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Compras,
            accion,
            descripcion,
            referenciaId,
            EntidadAuditoria,
            valoresAnteriores,
            valoresNuevos,
            motivo);

    private static object Snapshot(NotaCreditoProveedor x) => new
    {
        x.Id,
        x.NumeroNotaCredito,
        x.ProveedorId,
        x.FacturaProveedorId,
        x.DevolucionProveedorId,
        x.Moneda,
        x.FechaEmisionUtc,
        x.ReferenciaFiscal,
        x.Motivo,
        x.SubtotalCredito,
        x.ImpuestoCredito,
        x.TotalCredito,
        x.Estado
    };

    private static NotaCreditoProveedorDto Map(NotaCreditoProveedor x) => new()
    {
        Id = x.Id,
        NumeroNotaCredito = x.NumeroNotaCredito,
        ProveedorId = x.ProveedorId,
        FacturaProveedorId = x.FacturaProveedorId,
        DevolucionProveedorId = x.DevolucionProveedorId,
        ProveedorNombreSnapshot = x.ProveedorNombreSnapshot,
        Moneda = x.Moneda,
        FechaEmisionUtc = x.FechaEmisionUtc,
        ReferenciaFiscal = x.ReferenciaFiscal,
        Motivo = x.Motivo,
        Observaciones = x.Observaciones,
        SubtotalCredito = x.SubtotalCredito,
        ImpuestoCredito = x.ImpuestoCredito,
        TotalCredito = x.TotalCredito,
        Estado = x.Estado,
        FechaRegistroUtc = x.FechaRegistroUtc,
        RegistradaPorUsuarioId = x.RegistradaPorUsuarioId,
        RegistradaPorNombreSnapshot = x.RegistradaPorNombreSnapshot,
        FechaAnulacionUtc = x.FechaAnulacionUtc,
        AnuladaPorUsuarioId = x.AnuladaPorUsuarioId,
        MotivoAnulacion = x.MotivoAnulacion
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

    private static string NormalizarNumero(string value) =>
        value.Trim().ToUpperInvariant();

    private static string NormalizarMoneda(string value) =>
        value.Trim().ToUpperInvariant();

    private static string? Normalizar(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
