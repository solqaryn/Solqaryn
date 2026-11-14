using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>
/// Casos de uso físicos de la transferencia. Estado documental, existencias y
/// Kardex cambian dentro de la misma transacción para impedir tránsito huérfano.
/// </summary>
public sealed class TransferenciaInventarioMovimientoService : ITransferenciaInventarioMovimientoService
{
    private readonly ITransferenciaInventarioRepository _repository;
    private readonly TransferenciaInventarioExistenciaService _existencias;
    private readonly TransferenciaKardexMovimientoRegistrar _kardex;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public TransferenciaInventarioMovimientoService(
        ITransferenciaInventarioRepository repository,
        TransferenciaInventarioExistenciaService existencias,
        IKardexMovimientoWriter kardexWriter,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _existencias = existencias;
        _kardex = new TransferenciaKardexMovimientoRegistrar(kardexWriter);
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<TransferenciaInventarioDto?> DespacharAsync(
        int id,
        DespacharTransferenciaInventarioDto dto)
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
            var lockSet = await _existencias.BloquearParaDespachoAsync(transferencia);
            var transiciones = await _existencias.AplicarDespachoCompletoAsync(lockSet, transferencia);
            await _kardex.RegistrarDespachoAsync(
                transferencia,
                transiciones,
                usuarioId,
                _currentUser.NombreUsuario);

            transferencia.MarcarEnTransito(usuarioId, DateTime.UtcNow);
            MarcarActualizacion(transferencia, usuarioId);
            _repository.Update(transferencia);
            await _repository.SaveChangesAsync();
            resultado = transferencia;
        });

        return resultado is null ? null : Map(resultado);
    }

    public async Task<TransferenciaInventarioDto?> RecibirAsync(
        int id,
        RecibirTransferenciaInventarioDto dto)
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
            if (transferencia.Detalles.Any(x => !x.RecepcionCerrada))
                throw new BusinessRuleException("Todos los detalles deben cerrar su recepción antes de completar la transferencia.");

            var lockSet = await _existencias.BloquearParaRecepcionAsync(transferencia);
            var transiciones = await _existencias.AplicarRecepcionAsync(lockSet, transferencia);
            await _kardex.RegistrarRecepcionAsync(
                transferencia,
                transiciones,
                usuarioId,
                _currentUser.NombreUsuario);

            transferencia.Recibir(usuarioId, DateTime.UtcNow);
            MarcarActualizacion(transferencia, usuarioId);
            _repository.Update(transferencia);
            await _repository.SaveChangesAsync();
            resultado = transferencia;
        });

        return resultado is null ? null : Map(resultado);
    }

    public async Task<TransferenciaInventarioDto?> CancelarAsync(
        int id,
        CancelarTransferenciaInventarioDto dto)
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
            if (transferencia.Estado == EstadoTransferenciaInventario.Recibida)
                throw new BusinessRuleException("Una transferencia recibida no puede cancelarse.");

            if (transferencia.Estado == EstadoTransferenciaInventario.EnTransito)
            {
                var lockSet = await _existencias.BloquearParaCancelacionEnTransitoAsync(transferencia);
                var transiciones = await _existencias.AplicarCancelacionEnTransitoAsync(lockSet, transferencia);
                await _kardex.RegistrarCancelacionAsync(
                    transferencia,
                    transiciones,
                    usuarioId,
                    _currentUser.NombreUsuario);
            }

            try
            {
                transferencia.Cancelar(usuarioId, dto.Motivo, DateTime.UtcNow);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessRuleException(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                throw new BusinessRuleException(ex.Message);
            }

            MarcarActualizacion(transferencia, usuarioId);
            _repository.Update(transferencia);
            await _repository.SaveChangesAsync();
            resultado = transferencia;
        });

        return resultado is null ? null : Map(resultado);
    }

    private static void AplicarDespacho(
        TransferenciaInventario transferencia,
        DespacharTransferenciaInventarioDto dto)
    {
        ValidarCoberturaDetalles(transferencia, dto.Detalles.Select(x => x.DetalleId));
        var mapa = dto.Detalles.ToDictionary(x => x.DetalleId);
        foreach (var detalle in transferencia.Detalles)
            detalle.RegistrarDespacho(mapa[detalle.Id].CantidadDespachada);
    }

    private static void AplicarRecepcion(
        TransferenciaInventario transferencia,
        RecibirTransferenciaInventarioDto dto)
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

    private static void ValidarCoberturaDetalles(
        TransferenciaInventario transferencia,
        IEnumerable<int> ids)
    {
        var lista = ids.ToList();
        if (lista.Count != lista.Distinct().Count())
            throw new BusinessRuleException("La operación contiene detalles duplicados.");
        var esperados = transferencia.Detalles.Select(x => x.Id).OrderBy(x => x).ToArray();
        var recibidos = lista.OrderBy(x => x).ToArray();
        if (!esperados.SequenceEqual(recibidos))
            throw new BusinessRuleException("La operación debe informar exactamente todos los detalles de la transferencia.");
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
