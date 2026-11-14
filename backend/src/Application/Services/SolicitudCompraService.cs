using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class SolicitudCompraService : ISolicitudCompraService
{
    private const string EntidadAuditoria = "SolicitudCompra";

    private readonly ISolicitudCompraRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public SolicitudCompraService(
        ISolicitudCompraRepository repository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _auditoria = auditoria;
    }

    public async Task<PagedResult<SolicitudCompraDto>> GetPagedAsync(SolicitudCompraFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        if (filtro.Desde.HasValue && filtro.Hasta.HasValue && filtro.Desde.Value > filtro.Hasta.Value)
            throw new ArgumentException("El rango de fechas es inválido.", nameof(filtro));

        filtro.Page = Math.Max(1, filtro.Page);
        filtro.PageSize = Math.Clamp(filtro.PageSize, 1, 100);
        var (items, total) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<SolicitudCompraDto>
        {
            Items = items.Select(Map).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = total
        };
    }

    public async Task<SolicitudCompraDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var solicitud = await _repository.GetByIdAsync(id);
        return solicitud is null ? null : Map(solicitud);
    }

    public async Task<SolicitudCompraDto> CreateAsync(CreateSolicitudCompraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidarDetalles(dto.Detalles);

        var solicitud = new SolicitudCompra
        {
            NumeroSolicitud = await GenerarNumeroAsync(),
            ProveedorId = dto.ProveedorId,
            Notas = Normalizar(dto.Notas),
            Detalles = dto.Detalles.Select(CrearDetalle).ToList()
        };
        solicitud.ValidarDocumento();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _repository.AddAsync(solicitud);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaEstrictoAsync(
                AccionPermiso.Crear,
                "Solicitud de compra creada",
                solicitud,
                valoresNuevos: SnapshotAuditoria(solicitud));
        });

        return Map(solicitud);
    }

    public async Task<SolicitudCompraDto> UpdateAsync(int id, UpdateSolicitudCompraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidarId(id);
        ValidarDetalles(dto.Detalles);

        // Se valida todo el nuevo documento antes de abrir la transacción y antes
        // de reemplazar el detalle persistido, evitando mutaciones parciales.
        var nuevosDetalles = dto.Detalles.Select(CrearDetalle).ToList();
        foreach (var detalle in nuevosDetalles) detalle.Validar();

        SolicitudCompra? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var solicitud = await RequerirForUpdateAsync(id);
            solicitud.AsegurarEditable();
            var anterior = SnapshotAuditoria(solicitud);

            solicitud.ProveedorId = dto.ProveedorId;
            solicitud.Notas = Normalizar(dto.Notas);
            solicitud.Detalles.Clear();
            foreach (var detalle in nuevosDetalles) solicitud.Detalles.Add(detalle);
            solicitud.ValidarDocumento();

            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaEstrictoAsync(
                AccionPermiso.Editar,
                "Solicitud de compra editada",
                solicitud,
                anterior,
                SnapshotAuditoria(solicitud));
            resultado = solicitud;
        });

        return Map(resultado!);
    }

    public async Task<SolicitudCompraDto> EnviarAsync(int id)
    {
        ValidarId(id);
        var (usuarioId, nombre) = RequerirUsuario();
        SolicitudCompra? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var solicitud = await RequerirForUpdateAsync(id);
            var anterior = SnapshotAuditoria(solicitud);
            solicitud.Solicitar(usuarioId, nombre, DateTime.UtcNow);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaEstrictoAsync(
                AccionPermiso.Confirmar,
                "Solicitud de compra enviada a aprobación",
                solicitud,
                anterior,
                SnapshotAuditoria(solicitud));
            resultado = solicitud;
        });

        return Map(resultado!);
    }

    public async Task<SolicitudCompraDto> AprobarAsync(int id)
    {
        ValidarId(id);
        var (usuarioId, nombre) = RequerirUsuario();
        SolicitudCompra? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var solicitud = await RequerirForUpdateAsync(id);
            var anterior = SnapshotAuditoria(solicitud);
            solicitud.Aprobar(usuarioId, nombre, DateTime.UtcNow);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaEstrictoAsync(
                AccionPermiso.Aprobar,
                "Solicitud de compra aprobada",
                solicitud,
                anterior,
                SnapshotAuditoria(solicitud));
            resultado = solicitud;
        });

        return Map(resultado!);
    }

    public async Task<SolicitudCompraDto> RechazarAsync(int id, RechazarSolicitudCompraDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidarId(id);
        var motivo = Normalizar(dto.Motivo);
        if (motivo is null)
            throw new ArgumentException("El motivo de rechazo es obligatorio.", nameof(dto));

        var (usuarioId, nombre) = RequerirUsuario();
        SolicitudCompra? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var solicitud = await RequerirForUpdateAsync(id);
            var anterior = SnapshotAuditoria(solicitud);
            solicitud.Rechazar(usuarioId, nombre, motivo, DateTime.UtcNow);
            await _repository.SaveChangesAsync();
            await RegistrarAuditoriaEstrictoAsync(
                AccionPermiso.Rechazar,
                "Solicitud de compra rechazada",
                solicitud,
                anterior,
                SnapshotAuditoria(solicitud),
                motivo);
            resultado = solicitud;
        });

        return Map(resultado!);
    }

    private Task RegistrarAuditoriaEstrictoAsync(
        AccionPermiso accion,
        string descripcion,
        SolicitudCompra solicitud,
        object? valoresAnteriores = null,
        object? valoresNuevos = null,
        string? motivo = null) =>
        _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Compras,
            accion,
            descripcion,
            referenciaId: solicitud.Id,
            entidad: EntidadAuditoria,
            valoresAnteriores: valoresAnteriores,
            valoresNuevos: valoresNuevos,
            motivo: motivo);

    // La bitácora conserva únicamente metadatos necesarios para reconstruir el
    // lifecycle. Notas y observaciones libres se excluyen para no replicar texto
    // potencialmente sensible en el subsistema de auditoría.
    private static object SnapshotAuditoria(SolicitudCompra solicitud) => new
    {
        solicitud.NumeroSolicitud,
        Estado = solicitud.Estado.ToString(),
        solicitud.ProveedorId,
        Lineas = solicitud.Detalles.Count,
        solicitud.FechaSolicitudUtc,
        solicitud.SolicitadaPorUsuarioId,
        solicitud.FechaDecisionUtc,
        solicitud.DecididaPorUsuarioId
    };

    private async Task<SolicitudCompra> RequerirForUpdateAsync(int id) =>
        await _repository.GetByIdForUpdateAsync(id)
            ?? throw new KeyNotFoundException("Solicitud de compra no encontrada.");

    private static void ValidarId(int id)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
    }

    private (int UsuarioId, string? Nombre) RequerirUsuario()
    {
        if (!_currentUser.EstaAutenticado || _currentUser.UsuarioId is not > 0)
            throw new InvalidOperationException("Se requiere un usuario autenticado para esta transición.");
        return (_currentUser.UsuarioId.Value, _currentUser.NombreCompleto ?? _currentUser.NombreUsuario);
    }

    private async Task<string> GenerarNumeroAsync()
    {
        for (var intento = 0; intento < 5; intento++)
        {
            var numero = $"SC-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..32].ToUpperInvariant();
            if (!await _repository.ExisteNumeroAsync(numero)) return numero;
        }
        throw new InvalidOperationException("No fue posible generar un número único de solicitud de compra.");
    }

    private static void ValidarDetalles(IReadOnlyCollection<SolicitudCompraDetalleInputDto>? detalles)
    {
        if (detalles is null || detalles.Count == 0)
            throw new ArgumentException("La solicitud debe contener al menos un detalle.", nameof(detalles));
        foreach (var input in detalles)
        {
            if (input.ProductoId <= 0) throw new ArgumentException("Cada detalle debe indicar un producto válido.", nameof(detalles));
            if (input.ProductoVarianteId is <= 0) throw new ArgumentException("La variante debe ser válida cuando se especifica.", nameof(detalles));
            if (input.CantidadSolicitada <= 0) throw new ArgumentException("La cantidad solicitada debe ser mayor que cero.", nameof(detalles));
            if (input.CostoEstimadoUnitario < 0) throw new ArgumentException("El costo estimado no puede ser negativo.", nameof(detalles));
        }
    }

    private static SolicitudCompraDetalle CrearDetalle(SolicitudCompraDetalleInputDto input)
    {
        var detalle = new SolicitudCompraDetalle
        {
            ProductoId = input.ProductoId,
            ProductoVarianteId = input.ProductoVarianteId,
            Observacion = Normalizar(input.Observacion)
        };
        detalle.EstablecerCantidad(input.CantidadSolicitada);
        detalle.EstablecerCostoEstimado(input.CostoEstimadoUnitario);
        return detalle;
    }

    private static SolicitudCompraDto Map(SolicitudCompra solicitud) => new()
    {
        Id = solicitud.Id,
        NumeroSolicitud = solicitud.NumeroSolicitud,
        Estado = solicitud.Estado.ToString(),
        ProveedorId = solicitud.ProveedorId,
        ProveedorNombre = solicitud.Proveedor?.Nombre,
        Notas = solicitud.Notas,
        FechaSolicitudUtc = solicitud.FechaSolicitudUtc,
        SolicitadaPorUsuarioId = solicitud.SolicitadaPorUsuarioId,
        SolicitadaPorNombreSnapshot = solicitud.SolicitadaPorNombreSnapshot,
        FechaDecisionUtc = solicitud.FechaDecisionUtc,
        DecididaPorUsuarioId = solicitud.DecididaPorUsuarioId,
        DecididaPorNombreSnapshot = solicitud.DecididaPorNombreSnapshot,
        MotivoRechazo = solicitud.MotivoRechazo,
        Detalles = solicitud.Detalles.Select(d => new SolicitudCompraDetalleDto
        {
            Id = d.Id,
            ProductoId = d.ProductoId,
            ProductoVarianteId = d.ProductoVarianteId,
            CantidadSolicitada = d.CantidadSolicitada,
            CostoEstimadoUnitario = d.CostoEstimadoUnitario,
            Observacion = d.Observacion,
            ProductoSkuSnapshot = d.ProductoSkuSnapshot,
            ProductoNombreSnapshot = d.ProductoNombreSnapshot,
            ProductoMarcaSnapshot = d.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = d.ProductoModeloSnapshot,
            ProductoColorSnapshot = d.ProductoColorSnapshot,
            ProductoTallaSnapshot = d.ProductoTallaSnapshot
        }).ToList()
    };

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
