using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class CotizacionService : ICotizacionService
{
    private readonly ICotizacionRepository _repository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _varianteRepository;
    private readonly IAuditoriaService _auditoriaService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CotizacionService(
        ICotizacionRepository repository,
        IClienteRepository clienteRepository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository varianteRepository,
        IAuditoriaService auditoriaService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clienteRepository = clienteRepository ?? throw new ArgumentNullException(nameof(clienteRepository));
        _productoRepository = productoRepository ?? throw new ArgumentNullException(nameof(productoRepository));
        _varianteRepository = varianteRepository ?? throw new ArgumentNullException(nameof(varianteRepository));
        _auditoriaService = auditoriaService ?? throw new ArgumentNullException(nameof(auditoriaService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CotizacionDto> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la cotización debe ser mayor que cero.");

        var cotizacion = await _repository.GetByIdAsync(id, asNoTracking: true)
            ?? throw new ResourceNotFoundException($"Cotización con Id {id} no encontrada.");

        return MapToDto(cotizacion);
    }

    public async Task<PagedResult<CotizacionDto>> GetPagedAsync(CotizacionFiltroDto request)
    {
        ValidarFiltro(request);
        var (items, total) = await _repository.GetPagedAsync(request);

        return new PagedResult<CotizacionDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = total
        };
    }

    public async Task<CotizacionDto> CrearAsync(CreateCotizacionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var usuarioId = RequerirUsuario();
        var cotizacionId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var cliente = await RequerirClienteActivoAsync(dto.ClienteId);
            var cotizacion = new Cotizacion
            {
                ClienteId = cliente.Id,
                ClienteNombreSnapshot = cliente.Nombre,
                ClienteDocumentoSnapshot = cliente.IdentidadORTN,
                Observaciones = NormalizarOpcional(dto.Observaciones),
                CreadoPorUsuarioId = usuarioId
            };

            foreach (var detalleDto in dto.Detalles ?? [])
            {
                cotizacion.Detalles.Add(await CrearDetalleDesdeDtoAsync(
                    detalleDto.ProductoId,
                    detalleDto.ProductoVarianteId,
                    detalleDto.Cantidad,
                    detalleDto.PrecioUnitario));
            }

            cotizacion.ValidarDocumento();
            await _repository.AddAsync(cotizacion);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Crear,
                "Cotización creada.",
                cotizacion.Id,
                nameof(Cotizacion),
                valoresNuevos: new { cotizacion.ClienteId, cotizacion.Estado, cotizacion.Total });

            cotizacionId = cotizacion.Id;
        });

        return await GetByIdAsync(cotizacionId);
    }

    public async Task<CotizacionDto> ActualizarAsync(UpdateCotizacionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Id <= 0)
            throw new BusinessRuleException("El identificador de la cotización debe ser mayor que cero.");

        var usuarioId = RequerirUsuario();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var cotizacion = await RequerirForUpdateAsync(dto.Id);
            cotizacion.AsegurarEditable();

            var valoresAnteriores = new
            {
                cotizacion.ClienteId,
                cotizacion.Observaciones,
                Total = cotizacion.Total,
                Detalles = cotizacion.Detalles.Count
            };

            if (cotizacion.ClienteId != dto.ClienteId)
            {
                var cliente = await RequerirClienteActivoAsync(dto.ClienteId);
                cotizacion.ClienteId = cliente.Id;
                cotizacion.ClienteNombreSnapshot = cliente.Nombre;
                cotizacion.ClienteDocumentoSnapshot = cliente.IdentidadORTN;
            }

            cotizacion.Observaciones = NormalizarOpcional(dto.Observaciones);

            var idsSolicitados = (dto.Detalles ?? [])
                .Where(x => x.Id.HasValue && x.Id.Value > 0)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            var detallesAEliminar = cotizacion.Detalles
                .Where(x => !idsSolicitados.Contains(x.Id))
                .ToList();

            foreach (var detalle in detallesAEliminar)
                cotizacion.Detalles.Remove(detalle);

            foreach (var detalleDto in dto.Detalles ?? [])
            {
                if (detalleDto.Id is > 0)
                {
                    var existente = cotizacion.Detalles.FirstOrDefault(x => x.Id == detalleDto.Id.Value)
                        ?? throw new BusinessRuleException(
                            $"El detalle {detalleDto.Id.Value} no pertenece a la cotización {cotizacion.Id}.");

                    var snapshot = await CrearDetalleDesdeDtoAsync(
                        detalleDto.ProductoId,
                        detalleDto.ProductoVarianteId,
                        detalleDto.Cantidad,
                        detalleDto.PrecioUnitario);
                    CopiarIdentidadProducto(snapshot, existente);
                    existente.EstablecerValores(snapshot.Cantidad, snapshot.PrecioUnitario);
                }
                else
                {
                    cotizacion.Detalles.Add(await CrearDetalleDesdeDtoAsync(
                        detalleDto.ProductoId,
                        detalleDto.ProductoVarianteId,
                        detalleDto.Cantidad,
                        detalleDto.PrecioUnitario));
                }
            }

            cotizacion.ValidarDocumento();
            cotizacion.ActualizadoPorUsuarioId = usuarioId;
            _repository.Update(cotizacion);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Editar,
                "Cotización actualizada.",
                cotizacion.Id,
                nameof(Cotizacion),
                valoresAnteriores,
                new { cotizacion.ClienteId, cotizacion.Observaciones, Total = cotizacion.Total, Detalles = cotizacion.Detalles.Count });
        });

        return await GetByIdAsync(dto.Id);
    }

    public async Task EliminarAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la cotización debe ser mayor que cero.");

        RequerirUsuario();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var cotizacion = await RequerirForUpdateAsync(id);
            cotizacion.AsegurarEditable();

            _repository.Remove(cotizacion);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.EliminarPermanente,
                "Cotización eliminada.",
                id,
                nameof(Cotizacion),
                valoresAnteriores: new { cotizacion.ClienteId, cotizacion.Estado, cotizacion.Total });
        });
    }

    public Task<CotizacionDto> EnviarAsync(int id) =>
        CambiarEstadoAsync(
            id,
            AccionPermiso.CambiarEstado,
            "Cotización enviada.",
            static (cotizacion, usuarioId) => cotizacion.Enviar(usuarioId, DateTime.UtcNow));

    public Task<CotizacionDto> AceptarAsync(int id) =>
        CambiarEstadoAsync(
            id,
            AccionPermiso.Aprobar,
            "Cotización aceptada.",
            static (cotizacion, usuarioId) => cotizacion.Aceptar(usuarioId, DateTime.UtcNow));

    public async Task<CotizacionDto> RechazarAsync(int id, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new BusinessRuleException("El motivo de rechazo es obligatorio.");

        return await CambiarEstadoAsync(
            id,
            AccionPermiso.Rechazar,
            "Cotización rechazada.",
            (cotizacion, usuarioId) => cotizacion.Rechazar(usuarioId, motivo, DateTime.UtcNow),
            motivo.Trim());
    }

    public Task<CotizacionDto> ConvertirAsync(int id) =>
        CambiarEstadoAsync(
            id,
            AccionPermiso.Aplicar,
            "Cotización convertida.",
            static (cotizacion, usuarioId) => cotizacion.Convertir(usuarioId, DateTime.UtcNow));

    public async Task<CotizacionDto> DuplicarComoBorradorAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la cotización debe ser mayor que cero.");

        var usuarioId = RequerirUsuario();
        var nuevaId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var original = await _repository.GetByIdAsync(id, asNoTracking: true)
                ?? throw new ResourceNotFoundException($"Cotización con Id {id} no encontrada.");

            var copia = original.DuplicarComoBorrador();
            copia.CreadoPorUsuarioId = usuarioId;

            await _repository.AddAsync(copia);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Duplicar,
                $"Cotización duplicada como borrador desde la cotización {id}.",
                copia.Id,
                nameof(Cotizacion),
                valoresNuevos: new { OrigenId = id, copia.ClienteId, copia.Total });

            nuevaId = copia.Id;
        });

        return await GetByIdAsync(nuevaId);
    }

    private async Task<CotizacionDto> CambiarEstadoAsync(
        int id,
        AccionPermiso accion,
        string descripcion,
        Action<Cotizacion, int> mutacion,
        string? motivo = null)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la cotización debe ser mayor que cero.");

        var usuarioId = RequerirUsuario();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var cotizacion = await RequerirForUpdateAsync(id);
            var estadoAnterior = cotizacion.Estado;

            mutacion(cotizacion, usuarioId);
            _repository.Update(cotizacion);
            await _repository.SaveChangesAsync();

            await _auditoriaService.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                accion,
                descripcion,
                cotizacion.Id,
                nameof(Cotizacion),
                new { Estado = estadoAnterior },
                new { cotizacion.Estado },
                motivo);
        });

        return await GetByIdAsync(id);
    }

    private async Task<Cotizacion> RequerirForUpdateAsync(int id) =>
        await _repository.GetByIdForUpdateAsync(id)
        ?? throw new ResourceNotFoundException($"Cotización con Id {id} no encontrada.");

    private int RequerirUsuario() =>
        _currentUserService.UsuarioId is > 0
            ? _currentUserService.UsuarioId.Value
            : throw new ForbiddenAccessException("La operación requiere un usuario autenticado.");

    private async Task<Cliente> RequerirClienteActivoAsync(int clienteId)
    {
        if (clienteId <= 0)
            throw new BusinessRuleException("El cliente es obligatorio.");

        var cliente = await _clienteRepository.GetByIdAsync(clienteId)
            ?? throw new BusinessRuleException($"Cliente con Id {clienteId} no encontrado.");

        if (!cliente.Activo)
            throw new BusinessRuleException($"El cliente {cliente.Nombre} está inactivo.");

        return cliente;
    }

    private static void ValidarFiltro(CotizacionFiltroDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.ClienteId is <= 0)
            throw new BusinessRuleException("ClienteId debe ser mayor que cero.");

        if (request.Estado.HasValue && !Enum.IsDefined(request.Estado.Value))
            throw new BusinessRuleException("El estado de cotización no es válido.");

        ValidarFechaUtc(request.FechaDesdeUtc, nameof(request.FechaDesdeUtc));
        ValidarFechaUtc(request.FechaHastaUtc, nameof(request.FechaHastaUtc));

        if (request.FechaDesdeUtc.HasValue &&
            request.FechaHastaUtc.HasValue &&
            request.FechaDesdeUtc.Value > request.FechaHastaUtc.Value)
            throw new BusinessRuleException("FechaDesdeUtc no puede ser posterior a FechaHastaUtc.");

        if (!string.IsNullOrWhiteSpace(request.SortDirection) &&
            !string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("SortDirection debe ser 'asc' o 'desc'.");
    }

    private static void ValidarFechaUtc(DateTime? fecha, string nombre)
    {
        if (fecha.HasValue && fecha.Value.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException($"{nombre} debe estar expresada en UTC.");
    }

    private async Task<CotizacionDetalle> CrearDetalleDesdeDtoAsync(
        int productoId,
        int? varianteId,
        decimal cantidad,
        decimal precioUnitario)
    {
        var producto = await _productoRepository.GetByIdAsync(productoId)
            ?? throw new BusinessRuleException($"Producto con Id {productoId} no encontrado.");

        if (!producto.Activo || producto.Eliminado)
            throw new BusinessRuleException($"El producto {producto.Nombre} no está disponible.");

        ProductoVariante? variante = null;
        if (varianteId.HasValue)
        {
            variante = await _varianteRepository.GetByIdAsync(varianteId.Value)
                ?? throw new BusinessRuleException($"Variante con Id {varianteId.Value} no encontrada.");

            if (variante.ProductoId != productoId)
                throw new BusinessRuleException($"La variante {varianteId.Value} no pertenece al producto {productoId}.");

            if (!variante.Activo || variante.Eliminado)
                throw new BusinessRuleException($"La variante {varianteId.Value} no está disponible.");
        }

        var detalle = new CotizacionDetalle
        {
            ProductoId = producto.Id,
            ProductoVarianteId = variante?.Id,
            ProductoSkuSnapshot = variante?.Sku,
            ProductoNombreSnapshot = producto.Nombre,
            ProductoMarcaSnapshot = variante?.Marca?.Nombre ?? producto.Marca,
            ProductoModeloSnapshot = variante?.Modelo?.Nombre ?? producto.Modelo,
            ProductoColorSnapshot = variante?.Color?.Nombre,
            ProductoTallaSnapshot = variante?.Talla?.Nombre
        };

        var precioAutorizado = variante?.Precio ?? producto.Precio;
        if (precioAutorizado <= 0)
            throw new BusinessRuleException(
                $"El producto '{producto.Nombre}' no tiene un precio de venta vigente configurado.");

        // precioUnitario se conserva en la firma para compatibilidad de contrato,
        // pero nunca es fuente de verdad del precio comercial.
        detalle.EstablecerValores(cantidad, precioAutorizado);
        return detalle;
    }

    private static void CopiarIdentidadProducto(CotizacionDetalle origen, CotizacionDetalle destino)
    {
        destino.ProductoId = origen.ProductoId;
        destino.ProductoVarianteId = origen.ProductoVarianteId;
        destino.ProductoSkuSnapshot = origen.ProductoSkuSnapshot;
        destino.ProductoNombreSnapshot = origen.ProductoNombreSnapshot;
        destino.ProductoMarcaSnapshot = origen.ProductoMarcaSnapshot;
        destino.ProductoModeloSnapshot = origen.ProductoModeloSnapshot;
        destino.ProductoColorSnapshot = origen.ProductoColorSnapshot;
        destino.ProductoTallaSnapshot = origen.ProductoTallaSnapshot;
    }

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static CotizacionDto MapToDto(Cotizacion entidad) => new()
    {
        Id = entidad.Id,
        Estado = entidad.Estado,
        ClienteId = entidad.ClienteId,
        ClienteNombreSnapshot = entidad.ClienteNombreSnapshot,
        ClienteDocumentoSnapshot = entidad.ClienteDocumentoSnapshot,
        Observaciones = entidad.Observaciones,
        Total = entidad.Total,
        FechaEnvioUtc = entidad.FechaEnvioUtc,
        EnviadaPorUsuarioId = entidad.EnviadaPorUsuarioId,
        FechaAceptacionUtc = entidad.FechaAceptacionUtc,
        AceptadaPorUsuarioId = entidad.AceptadaPorUsuarioId,
        FechaRechazoUtc = entidad.FechaRechazoUtc,
        RechazadaPorUsuarioId = entidad.RechazadaPorUsuarioId,
        MotivoRechazo = entidad.MotivoRechazo,
        FechaConversionUtc = entidad.FechaConversionUtc,
        ConvertidaPorUsuarioId = entidad.ConvertidaPorUsuarioId,
        CreatedAt = entidad.FechaCreacion,
        CreadoPorUsuarioId = entidad.CreadoPorUsuarioId,
        UpdatedAt = entidad.FechaActualizacion,
        ActualizadoPorUsuarioId = entidad.ActualizadoPorUsuarioId,
        Detalles = entidad.Detalles.Select(d => new CotizacionDetalleDto
        {
            Id = d.Id,
            ProductoId = d.ProductoId,
            ProductoVarianteId = d.ProductoVarianteId,
            ProductoSkuSnapshot = d.ProductoSkuSnapshot,
            ProductoNombreSnapshot = d.ProductoNombreSnapshot,
            ProductoMarcaSnapshot = d.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = d.ProductoModeloSnapshot,
            ProductoColorSnapshot = d.ProductoColorSnapshot,
            ProductoTallaSnapshot = d.ProductoTallaSnapshot,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Total = d.Total
        }).ToList()
    };
}
