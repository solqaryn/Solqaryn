using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Common;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed partial class AjusteInventarioService : IAjusteInventarioService
{
    private readonly IAjusteInventarioRepository _repository;
    private readonly IProductoRepository _productoRepository;
    private readonly IProductoVarianteRepository _productoVarianteRepository;
    private readonly IMovimientoInventarioRepository _movimientoInventarioRepository;
    private readonly IInventarioConcurrencyService _inventarioConcurrency;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly IExistenciaVarianteConcurrencyService? _existenciaVarianteConcurrency;

    public AjusteInventarioService(
        IAjusteInventarioRepository repository,
        IProductoRepository productoRepository,
        IProductoVarianteRepository productoVarianteRepository,
        IMovimientoInventarioRepository movimientoInventarioRepository,
        IInventarioConcurrencyService inventarioConcurrency,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria,
        IExistenciaVarianteConcurrencyService? existenciaVarianteConcurrency = null)
    {
        _repository = repository;
        _productoRepository = productoRepository;
        _productoVarianteRepository = productoVarianteRepository;
        _movimientoInventarioRepository = movimientoInventarioRepository;
        _inventarioConcurrency = inventarioConcurrency;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _existenciaVarianteConcurrency = existenciaVarianteConcurrency;
    }

    public async Task<List<AjusteInventarioDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(ToDto).ToList();

    public async Task<AjusteInventarioDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var ajuste = await _repository.GetByIdAsync(id);
        return ajuste is null ? null : ToDto(ajuste);
    }

    public async Task<AjusteInventarioDto> CreateAsync(CreateAjusteInventarioDto dto)
    {
        ValidarCabecera(dto.Motivo, dto.Observaciones, dto.Detalles);
        var (usuarioId, nombreUsuario) = ObtenerUsuarioActual();
        AjusteInventario? creado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            creado = await CrearBorradorInternoAsync(dto, usuarioId, nombreUsuario);
        });

        creado ??= await _repository.GetByIdAsync(creado?.Id ?? 0)
            ?? throw new InvalidOperationException("No se pudo recuperar el ajuste recién creado.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Crear,
            $"Ajuste de inventario creado como borrador: {creado.NumeroAjuste}",
            creado.Id,
            entidad: nameof(AjusteInventario));

        return ToDto(creado);
    }

    public async Task<AjusteInventarioDto?> UpdateAsync(int id, UpdateAjusteInventarioDto dto)
    {
        ValidarCabecera(dto.Motivo, dto.Observaciones, dto.Detalles);
        var (usuarioId, nombreUsuario) = ObtenerUsuarioActual();
        var encontrado = false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var ajuste = await _repository.GetByIdForUpdateAsync(id);
            if (ajuste is null) return;
            encontrado = true;

            if (ajuste.Estado != EstadoAjusteInventario.Borrador)
                throw new BusinessRuleException("Solo los ajustes en estado Borrador pueden editarse.");

            ajuste.FechaAjuste = dto.FechaAjuste ?? ajuste.FechaAjuste;
            ajuste.Motivo = dto.Motivo.Trim();
            ajuste.Observaciones = NormalizarOpcional(dto.Observaciones);
            ajuste.ActualizadoPorUsuarioId = usuarioId;
            ajuste.ActualizadoPorNombreUsuario = nombreUsuario;
            ajuste.FechaActualizacion = DateTime.UtcNow;

            ajuste.Detalles.Clear();
            await ReemplazarDetallesAsync(ajuste, dto.Detalles);
            _repository.Update(ajuste);
            await _repository.SaveChangesAsync();
        });

        if (!encontrado) return null;

        var actualizado = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("No se pudo recuperar el ajuste actualizado.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Inventario,
            AccionPermiso.Editar,
            $"Ajuste de inventario actualizado: {actualizado.NumeroAjuste}",
            actualizado.Id,
            entidad: nameof(AjusteInventario));

        return ToDto(actualizado);
    }

    public async Task<AjusteInventarioDto?> ConfirmarAsync(int id)
    {
        var (usuarioId, nombreUsuario) = ObtenerUsuarioActual();
        var encontrado = false;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var ajuste = await _repository.GetByIdForUpdateAsync(id);
            if (ajuste is null) return;
            encontrado = true;
            await ConfirmarInternoAsync(ajuste, usuarioId, nombreUsuario, null);
        });

        if (!encontrado) return null;

        var confirmado = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("No se pudo recuperar el ajuste confirmado.");

        return ToDto(confirmado);
    }

    public async Task<AjusteStockResultadoDto> AjustarStockCompatibilidadAsync(
        int productoId,
        int? varianteId,
        AjusteStockRequest request)
    {
        ValidarSolicitudCompatibilidad(productoId, varianteId, request);
        var (usuarioId, nombreUsuario) = ObtenerUsuarioActual();
        var motivo = request.Motivo.Trim();
        AjusteInventario? confirmado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            confirmado = await CrearBorradorInternoAsync(
                new CreateAjusteInventarioDto
                {
                    Motivo = motivo,
                    Observaciones = "Creado y confirmado atómicamente por adaptador de compatibilidad legacy.",
                    Detalles =
                    {
                        new AjusteInventarioDetalleInputDto
                        {
                            ProductoId = productoId,
                            ProductoVarianteId = varianteId,
                            AlmacenId = request.AlmacenId,
                            UbicacionAlmacenId = request.UbicacionAlmacenId,
                            CantidadObjetivo = request.CantidadNueva
                        }
                    }
                },
                usuarioId,
                nombreUsuario);

            var cantidadesEsperadas = new Dictionary<(int ProductoId, int? ProductoVarianteId), int>
            {
                [(productoId, varianteId)] = request.CantidadActualEsperada
            };

            await ConfirmarInternoAsync(
                confirmado,
                usuarioId,
                nombreUsuario,
                cantidadesEsperadas);
        });

        var ajuste = confirmado
            ?? throw new InvalidOperationException("No se pudo materializar el ajuste formal de compatibilidad.");
        var detalle = ajuste.Detalles.Single(d =>
            d.ProductoId == productoId && d.ProductoVarianteId == varianteId);

        var cantidadAnterior = detalle.CantidadAnteriorSnapshot
            ?? throw new InvalidOperationException("El ajuste confirmado no materializó el stock anterior.");
        var cantidadNueva = detalle.CantidadNuevaSnapshot
            ?? throw new InvalidOperationException("El ajuste confirmado no materializó el stock nuevo.");

        return new AjusteStockResultadoDto
        {
            ProductoId = productoId,
            ProductoVarianteId = varianteId,
            CantidadAnterior = cantidadAnterior,
            CantidadNueva = cantidadNueva,
            Diferencia = detalle.DiferenciaSnapshot ?? cantidadNueva - cantidadAnterior,
            Motivo = motivo
        };
    }

}
