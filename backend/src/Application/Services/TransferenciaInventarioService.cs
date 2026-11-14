using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>
/// Casos de uso de la transferencia empresarial. Este servicio conserva las
/// transiciones de estado dentro de una transacción y evita repetir una misma
/// transición cuando el cliente reintenta la petición.
///
/// El movimiento físico de existencias se integra antes de exponer Despachar y
/// Recibir por API; mientras tanto la clase permanece sin registro DI/controller.
/// </summary>
public sealed class TransferenciaInventarioService : ITransferenciaInventarioService
{
    private readonly ITransferenciaInventarioRepository _repository;
    private readonly IAlmacenRepository _almacenes;
    private readonly IProductoVarianteRepository _variantes;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public TransferenciaInventarioService(
        ITransferenciaInventarioRepository repository,
        IAlmacenRepository almacenes,
        IProductoVarianteRepository variantes,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _almacenes = almacenes;
        _variantes = variantes;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<TransferenciaInventarioDto>> GetPagedAsync(TransferenciaInventarioFiltroDto filtro)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var (items, totalCount) = await _repository.GetPagedAsync(filtro);
        return new PagedResult<TransferenciaInventarioDto>
        {
            Items = items.Select(Map).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TransferenciaInventarioDto?> GetByIdAsync(int id)
    {
        if (id <= 0) return null;
        var transferencia = await _repository.GetByIdAsync(id);
        return transferencia is null ? null : Map(transferencia);
    }

    public async Task<TransferenciaInventarioDto> CreateAsync(CreateTransferenciaInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var usuarioId = ObtenerUsuarioId();
        await ValidarTopologiaAsync(dto.AlmacenOrigenId, dto.AlmacenDestinoId);
        var detalles = await ConstruirDetallesAsync(dto.Detalles, usuarioId);
        var ahora = DateTime.UtcNow;
        var transferencia = new TransferenciaInventario
        {
            Numero = await GenerarNumeroAsync(ahora),
            AlmacenOrigenId = dto.AlmacenOrigenId,
            AlmacenDestinoId = dto.AlmacenDestinoId,
            Observaciones = NormalizarOpcional(dto.Observaciones),
            CreadoPorUsuarioId = usuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario,
            ActualizadoPorUsuarioId = usuarioId,
            ActualizadoPorNombreUsuario = _currentUser.NombreUsuario,
            FechaCreacion = ahora,
            FechaActualizacion = ahora,
            Detalles = detalles
        };
        transferencia.ValidarTopologia();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _repository.AddAsync(transferencia);
            await _repository.SaveChangesAsync();
        });

        var persistida = await _repository.GetByIdAsync(transferencia.Id) ?? transferencia;
        return Map(persistida);
    }

    public async Task<TransferenciaInventarioDto?> UpdateAsync(int id, UpdateTransferenciaInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (id <= 0) return null;
        var usuarioId = ObtenerUsuarioId();
        await ValidarTopologiaAsync(dto.AlmacenOrigenId, dto.AlmacenDestinoId);
        var nuevosDetalles = await ConstruirDetallesAsync(dto.Detalles, usuarioId);
        TransferenciaInventario? actualizada = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var transferencia = await _repository.GetByIdForUpdateAsync(id);
            if (transferencia is null) return;
            if (transferencia.Estado != EstadoTransferenciaInventario.Borrador)
                throw new BusinessRuleException("Solo una transferencia en borrador puede editarse.");

            transferencia.AlmacenOrigenId = dto.AlmacenOrigenId;
            transferencia.AlmacenDestinoId = dto.AlmacenDestinoId;
            transferencia.Observaciones = NormalizarOpcional(dto.Observaciones);
            transferencia.Detalles.Clear();
            foreach (var detalle in nuevosDetalles)
                transferencia.Detalles.Add(detalle);

            MarcarActualizacion(transferencia, usuarioId);
            transferencia.ValidarTopologia();
            _repository.Update(transferencia);
            await _repository.SaveChangesAsync();
            actualizada = transferencia;
        });

        if (actualizada is null) return null;
        return Map(await _repository.GetByIdAsync(id) ?? actualizada);
    }

    public async Task<TransferenciaInventarioDto?> SolicitarAsync(int id)
    {
        if (id <= 0) return null;
        var usuarioId = ObtenerUsuarioId();
        TransferenciaInventario? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var transferencia = await _repository.GetByIdForUpdateAsync(id);
            if (transferencia is null) return;
            if (transferencia.Estado == EstadoTransferenciaInventario.Solicitada)
            {
                resultado = transferencia;
                return;
            }

            transferencia.Solicitar(usuarioId, DateTime.UtcNow);
            MarcarActualizacion(transferencia, usuarioId);
            _repository.Update(transferencia);
            await _repository.SaveChangesAsync();
            resultado = transferencia;
        });

        return resultado is null ? null : Map(resultado);
    }

    public async Task<TransferenciaInventarioDto?> AprobarAsync(int id, AprobarTransferenciaInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (id <= 0) return null;
        var usuarioId = ObtenerUsuarioId();
        TransferenciaInventario? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var transferencia = await _repository.GetByIdForUpdateAsync(id);
            if (transferencia is null) return;
            if (transferencia.Estado == EstadoTransferenciaInventario.Aprobada)
            {
                resultado = transferencia;
                return;
            }
            if (transferencia.Estado != EstadoTransferenciaInventario.Solicitada)
                throw new BusinessRuleException("Solo una transferencia solicitada puede aprobarse.");

            AplicarAprobacion(transferencia, dto);
            transferencia.Aprobar(usuarioId, DateTime.UtcNow);
            MarcarActualizacion(transferencia, usuarioId);
            _repository.Update(transferencia);
            await _repository.SaveChangesAsync();
            resultado = transferencia;
        });

        return resultado is null ? null : Map(resultado);
    }

    public async Task<TransferenciaInventarioDto?> DespacharAsync(int id, DespacharTransferenciaInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (id <= 0) return null;
        var usuarioId = ObtenerUsuarioId();
        TransferenciaInventario? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var transferencia = await _repository.GetByIdForUpdateAsync(id);
            if (transferencia is null) return;
            if (transferencia.Estado == EstadoTransferenciaInventario.EnTransito)
            {
                resultado = transferencia;
                return;
            }
            if (transferencia.Estado != EstadoTransferenciaInventario.Aprobada)
                throw new BusinessRuleException("Solo una transferencia aprobada puede despacharse.");

            AplicarDespacho(transferencia, dto);
            transferencia.MarcarEnTransito(usuarioId, DateTime.UtcNow);
            MarcarActualizacion(transferencia, usuarioId);
            _repository.Update(transferencia);
            await _repository.SaveChangesAsync();
            resultado = transferencia;
        });

        return resultado is null ? null : Map(resultado);
    }

    public async Task<TransferenciaInventarioDto?> RecibirAsync(int id, RecibirTransferenciaInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (id <= 0) return null;
        var usuarioId = ObtenerUsuarioId();
        TransferenciaInventario? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var transferencia = await _repository.GetByIdForUpdateAsync(id);
            if (transferencia is null) return;
            if (transferencia.Estado == EstadoTransferenciaInventario.Recibida)
            {
                resultado = transferencia;
                return;
            }
            if (transferencia.Estado != EstadoTransferenciaInventario.EnTransito)
                throw new BusinessRuleException("Solo una transferencia en tránsito puede recibirse.");

            AplicarRecepcion(transferencia, dto);
            transferencia.Recibir(usuarioId, DateTime.UtcNow);
            MarcarActualizacion(transferencia, usuarioId);
            _repository.Update(transferencia);
            await _repository.SaveChangesAsync();
            resultado = transferencia;
        });

        return resultado is null ? null : Map(resultado);
    }

    public async Task<TransferenciaInventarioDto?> CancelarAsync(int id, CancelarTransferenciaInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (id <= 0) return null;
        var usuarioId = ObtenerUsuarioId();
        TransferenciaInventario? resultado = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var transferencia = await _repository.GetByIdForUpdateAsync(id);
            if (transferencia is null) return;
            if (transferencia.Estado == EstadoTransferenciaInventario.Cancelada)
            {
                resultado = transferencia;
                return;
            }

            transferencia.Cancelar(usuarioId, dto.Motivo, DateTime.UtcNow);
            MarcarActualizacion(transferencia, usuarioId);
            _repository.Update(transferencia);
            await _repository.SaveChangesAsync();
            resultado = transferencia;
        });

        return resultado is null ? null : Map(resultado);
    }

    private async Task ValidarTopologiaAsync(int almacenOrigenId, int almacenDestinoId)
    {
        if (almacenOrigenId <= 0 || almacenDestinoId <= 0)
            throw new BusinessRuleException("Los almacenes de origen y destino son obligatorios.");
        if (almacenOrigenId == almacenDestinoId)
            throw new BusinessRuleException("El almacén de origen y destino deben ser distintos.");

        var origen = await _almacenes.GetByIdAsync(almacenOrigenId);
        var destino = await _almacenes.GetByIdAsync(almacenDestinoId);
        if (origen is null || origen.Eliminado || !origen.Activo)
            throw new BusinessRuleException("El almacén de origen no existe o no está activo.");
        if (destino is null || destino.Eliminado || !destino.Activo)
            throw new BusinessRuleException("El almacén de destino no existe o no está activo.");
    }

    private async Task<List<TransferenciaInventarioDetalle>> ConstruirDetallesAsync(
        IReadOnlyCollection<TransferenciaInventarioDetalleInputDto>? inputs,
        int usuarioId)
    {
        if (inputs is null || inputs.Count == 0)
            throw new BusinessRuleException("La transferencia debe contener al menos un detalle.");

        var duplicada = inputs.GroupBy(x => new { x.ProductoVarianteId, x.UbicacionOrigenId, x.UbicacionDestinoId })
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicada is not null)
            throw new BusinessRuleException("No puede repetirse la misma variante y par de ubicaciones dentro de la transferencia.");

        var detalles = new List<TransferenciaInventarioDetalle>(inputs.Count);
        foreach (var input in inputs)
        {
            if (input.ProductoVarianteId <= 0)
                throw new BusinessRuleException("Cada detalle debe indicar una variante válida.");
            if (input.CantidadSolicitada <= 0)
                throw new BusinessRuleException("La cantidad solicitada debe ser mayor que cero.");

            var variante = await _variantes.GetByIdAsync(input.ProductoVarianteId);
            if (variante is null || variante.Eliminado || !variante.Activo)
                throw new BusinessRuleException($"La variante {input.ProductoVarianteId} no existe o no está activa.");

            var detalle = new TransferenciaInventarioDetalle
            {
                ProductoVarianteId = variante.Id,
                UbicacionOrigenId = input.UbicacionOrigenId,
                UbicacionDestinoId = input.UbicacionDestinoId,
                ProductoSkuSnapshot = variante.Sku,
                ProductoMarcaSnapshot = variante.Marca?.Nombre,
                ProductoModeloSnapshot = variante.Modelo?.Nombre,
                ProductoColorSnapshot = variante.Color?.Nombre,
                ProductoTallaSnapshot = variante.Talla?.Nombre,
                CreadoPorUsuarioId = usuarioId,
                CreadoPorNombreUsuario = _currentUser.NombreUsuario,
                ActualizadoPorUsuarioId = usuarioId,
                ActualizadoPorNombreUsuario = _currentUser.NombreUsuario
            };
            detalle.EstablecerCantidadSolicitada(input.CantidadSolicitada);
            detalles.Add(detalle);
        }

        return detalles;
    }

    private static void AplicarAprobacion(TransferenciaInventario transferencia, AprobarTransferenciaInventarioDto dto)
    {
        ValidarCoberturaDetalles(transferencia, dto.Detalles.Select(x => x.DetalleId));
        var mapa = dto.Detalles.ToDictionary(x => x.DetalleId);
        foreach (var detalle in transferencia.Detalles)
            detalle.AprobarCantidad(mapa[detalle.Id].CantidadAprobada);
    }

    private static void AplicarDespacho(TransferenciaInventario transferencia, DespacharTransferenciaInventarioDto dto)
    {
        ValidarCoberturaDetalles(transferencia, dto.Detalles.Select(x => x.DetalleId));
        var mapa = dto.Detalles.ToDictionary(x => x.DetalleId);
        foreach (var detalle in transferencia.Detalles)
            detalle.RegistrarDespacho(mapa[detalle.Id].CantidadDespachada);
    }

    private static void AplicarRecepcion(TransferenciaInventario transferencia, RecibirTransferenciaInventarioDto dto)
    {
        ValidarCoberturaDetalles(transferencia, dto.Detalles.Select(x => x.DetalleId));
        var mapa = dto.Detalles.ToDictionary(x => x.DetalleId);
        foreach (var detalle in transferencia.Detalles)
        {
            var input = mapa[detalle.Id];
            detalle.RegistrarRecepcion(
                input.CantidadRecibida,
                input.CantidadFaltante,
                input.CantidadDanada,
                input.CantidadSobrante);
        }
    }

    private static void ValidarCoberturaDetalles(TransferenciaInventario transferencia, IEnumerable<int> ids)
    {
        var lista = ids.ToList();
        if (lista.Count != lista.Distinct().Count())
            throw new BusinessRuleException("La operación contiene detalles duplicados.");
        var esperados = transferencia.Detalles.Select(x => x.Id).OrderBy(x => x).ToArray();
        var recibidos = lista.OrderBy(x => x).ToArray();
        if (!esperados.SequenceEqual(recibidos))
            throw new BusinessRuleException("La operación debe informar exactamente todos los detalles de la transferencia.");
    }

    private async Task<string> GenerarNumeroAsync(DateTime fechaUtc)
    {
        for (var intento = 0; intento < 5; intento++)
        {
            var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            var numero = $"TRF-{fechaUtc:yyyyMMddHHmmssfff}-{sufijo}";
            if (!await _repository.ExisteNumeroAsync(numero))
                return numero;
        }

        throw new BusinessRuleException("No fue posible generar un número único de transferencia.");
    }

    private int ObtenerUsuarioId()
    {
        if (!_currentUser.EstaAutenticado || !_currentUser.UsuarioId.HasValue || _currentUser.UsuarioId.Value <= 0)
            throw new BusinessRuleException("La operación requiere un usuario autenticado válido.");
        return _currentUser.UsuarioId.Value;
    }

    private void MarcarActualizacion(TransferenciaInventario transferencia, int usuarioId)
    {
        transferencia.ActualizadoPorUsuarioId = usuarioId;
        transferencia.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        transferencia.FechaActualizacion = DateTime.UtcNow;
    }

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static TransferenciaInventarioDto Map(TransferenciaInventario transferencia) => new()
    {
        Id = transferencia.Id,
        Numero = transferencia.Numero,
        AlmacenOrigenId = transferencia.AlmacenOrigenId,
        AlmacenOrigenNombre = transferencia.AlmacenOrigen?.Nombre,
        AlmacenDestinoId = transferencia.AlmacenDestinoId,
        AlmacenDestinoNombre = transferencia.AlmacenDestino?.Nombre,
        Estado = transferencia.Estado.ToString(),
        Observaciones = transferencia.Observaciones,
        FechaSolicitud = transferencia.FechaSolicitud,
        FechaAprobacion = transferencia.FechaAprobacion,
        FechaDespacho = transferencia.FechaDespacho,
        FechaRecepcion = transferencia.FechaRecepcion,
        FechaCancelacion = transferencia.FechaCancelacion,
        MotivoCancelacion = transferencia.MotivoCancelacion,
        Detalles = transferencia.Detalles.OrderBy(x => x.Id).Select(x => new TransferenciaInventarioDetalleDto
        {
            Id = x.Id,
            ProductoVarianteId = x.ProductoVarianteId,
            UbicacionOrigenId = x.UbicacionOrigenId,
            UbicacionDestinoId = x.UbicacionDestinoId,
            CantidadSolicitada = x.CantidadSolicitada,
            CantidadAprobada = x.CantidadAprobada,
            CantidadDespachada = x.CantidadDespachada,
            CantidadRecibida = x.CantidadRecibida,
            CantidadFaltante = x.CantidadFaltante,
            CantidadSobrante = x.CantidadSobrante,
            CantidadDanada = x.CantidadDanada,
            ProductoSkuSnapshot = x.ProductoSkuSnapshot,
            ProductoMarcaSnapshot = x.ProductoMarcaSnapshot,
            ProductoModeloSnapshot = x.ProductoModeloSnapshot,
            ProductoColorSnapshot = x.ProductoColorSnapshot,
            ProductoTallaSnapshot = x.ProductoTallaSnapshot
        }).ToList()
    };
}
