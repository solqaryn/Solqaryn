using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Cajas;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>
/// N4.1.D application boundary for Caja, hardened by N4.1.F with strict audit
/// inside the same unit-of-work transaction as every business mutation and
/// fail-closed RBAC enforcement using the authoritative generic permission contract.
/// Caja-specific actions are not invented: the service reuses AccionPermiso and
/// ModuloSistema.Caja.
/// </summary>
public sealed class CajaService : ICajaService
{
    private readonly ICajaRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;
    private readonly IPermisoService _permisos;

    public CajaService(
        ICajaRepository repository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria,
        IPermisoService permisos)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _permisos = permisos ?? throw new ArgumentNullException(nameof(permisos));
    }

    public async Task<CajaDto> GetCajaByIdAsync(int id)
    {
        await AutorizarAsync(AccionPermiso.Ver);
        ValidarId(id, "caja");
        var caja = await _repository.GetCajaByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Caja con Id {id} no encontrada.");
        return Map(caja);
    }

    public async Task<CajaSesionDto> GetSesionByIdAsync(int id)
    {
        await AutorizarAsync(AccionPermiso.Ver);
        ValidarId(id, "sesión de caja");
        var sesion = await _repository.GetSesionByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Sesión de caja con Id {id} no encontrada.");
        return Map(sesion);
    }

    public async Task<CajaSesionDto?> GetSesionActivaAsync(int cajaId)
    {
        await AutorizarAsync(AccionPermiso.Ver);
        ValidarId(cajaId, "caja");
        var caja = await _repository.GetCajaByIdAsync(cajaId)
            ?? throw new ResourceNotFoundException($"Caja con Id {cajaId} no encontrada.");
        var sesion = await _repository.GetSesionActivaByCajaIdAsync(caja.Id);
        return sesion is null ? null : Map(sesion);
    }

    public async Task<CajaDto> CrearCajaAsync(CrearCajaDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequerirUsuarioId();
        await AutorizarAsync(AccionPermiso.Crear);
        var id = 0;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            Caja caja;
            try
            {
                caja = new Caja(dto.Nombre);
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                throw Regla(ex);
            }

            await _repository.AddCajaAsync(caja);
            await _repository.SaveChangesAsync();
            id = caja.Id;
            await AuditarEstrictoAsync(
                AccionPermiso.Crear,
                $"Caja creada: {caja.Nombre}",
                caja.Id,
                "Caja");
        });
        return await ObtenerCajaResultadoAsync(id);
    }

    public async Task<CajaDto> ActivarCajaAsync(int id)
    {
        RequerirUsuarioId();
        await AutorizarAsync(AccionPermiso.Activar);
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var caja = await ObtenerCajaParaActualizarAsync(id);
            caja.Activar();
            _repository.UpdateCaja(caja);
            await _repository.SaveChangesAsync();
            await AuditarEstrictoAsync(
                AccionPermiso.Activar,
                $"Caja activada: {caja.Nombre}",
                caja.Id,
                "Caja");
        });
        return await ObtenerCajaResultadoAsync(id);
    }

    public async Task<CajaDto> DesactivarCajaAsync(int id)
    {
        RequerirUsuarioId();
        await AutorizarAsync(AccionPermiso.Desactivar);
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var caja = await ObtenerCajaParaActualizarAsync(id);
            try
            {
                caja.Desactivar();
            }
            catch (InvalidOperationException ex)
            {
                throw Regla(ex);
            }
            _repository.UpdateCaja(caja);
            await _repository.SaveChangesAsync();
            await AuditarEstrictoAsync(
                AccionPermiso.Desactivar,
                $"Caja desactivada: {caja.Nombre}",
                caja.Id,
                "Caja");
        });
        return await ObtenerCajaResultadoAsync(id);
    }

    public async Task<CajaSesionDto> AbrirSesionAsync(int cajaId, AbrirCajaSesionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var usuarioId = RequerirUsuarioId();
        await AutorizarAsync(AccionPermiso.Crear);
        var sesionId = 0;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var caja = await ObtenerCajaParaActualizarAsync(cajaId);
            CajaSesion sesion;
            try
            {
                sesion = new CajaSesion(caja.Id, usuarioId, dto.FondoInicial);
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                throw Regla(ex);
            }

            await _repository.AddSesionAsync(sesion);
            await _repository.SaveChangesAsync();

            try
            {
                caja.RegistrarSesionActiva(sesion.Id);
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                throw Regla(ex);
            }

            _repository.UpdateCaja(caja);
            await _repository.SaveChangesAsync();
            sesionId = sesion.Id;
            await AuditarEstrictoAsync(
                AccionPermiso.Crear,
                $"Sesión de caja abierta para Caja {caja.Id}",
                sesion.Id,
                "CajaSesion");
        });

        return await ObtenerSesionResultadoAsync(sesionId);
    }

    public async Task<CajaSesionDto> IniciarOperacionesAsync(int sesionId)
    {
        RequerirUsuarioId();
        await AutorizarAsync(AccionPermiso.Actualizar);
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var sesion = await ObtenerSesionParaActualizarAsync(sesionId);
            try
            {
                sesion.IniciarOperaciones();
            }
            catch (InvalidOperationException ex)
            {
                throw Regla(ex);
            }
            _repository.UpdateSesion(sesion);
            await _repository.SaveChangesAsync();
            await AuditarEstrictoAsync(
                AccionPermiso.Actualizar,
                $"Operaciones iniciadas en sesión de caja {sesion.Id}",
                sesion.Id,
                "CajaSesion");
        });
        return await ObtenerSesionResultadoAsync(sesionId);
    }

    public async Task<CajaSesionDto> RegistrarMovimientoAsync(int sesionId, RegistrarMovimientoCajaDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequerirUsuarioId();
        await AutorizarAsync(AccionPermiso.Crear);
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var sesion = await ObtenerSesionParaActualizarAsync(sesionId);
            try
            {
                sesion.RegistrarMovimiento(dto.Tipo, dto.Monto, dto.Referencia);
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                throw Regla(ex);
            }
            _repository.UpdateSesion(sesion);
            await _repository.SaveChangesAsync();
            await AuditarEstrictoAsync(
                AccionPermiso.Crear,
                $"Movimiento registrado en sesión de caja {sesion.Id}",
                sesion.Id,
                "CajaSesion");
        });
        return await ObtenerSesionResultadoAsync(sesionId);
    }

    public async Task<CajaSesionDto> IniciarArqueoAsync(int sesionId)
    {
        RequerirUsuarioId();
        await AutorizarAsync(AccionPermiso.Actualizar);
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var sesion = await ObtenerSesionParaActualizarAsync(sesionId);
            try
            {
                sesion.IniciarArqueo();
            }
            catch (InvalidOperationException ex)
            {
                throw Regla(ex);
            }
            _repository.UpdateSesion(sesion);
            await _repository.SaveChangesAsync();
            await AuditarEstrictoAsync(
                AccionPermiso.Actualizar,
                $"Arqueo iniciado en sesión de caja {sesion.Id}",
                sesion.Id,
                "CajaSesion");
        });
        return await ObtenerSesionResultadoAsync(sesionId);
    }

    public async Task<CajaSesionDto> CerrarSesionAsync(int sesionId, CerrarCajaSesionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequerirUsuarioId();
        await AutorizarAsync(AccionPermiso.Cerrar);
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var sesion = await ObtenerSesionParaActualizarAsync(sesionId);
            var caja = await ObtenerCajaParaActualizarAsync(sesion.CajaId);
            try
            {
                sesion.Cerrar(dto.SaldoContado, dto.Observaciones);
                caja.LiberarSesionActiva(sesion.Id);
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            {
                throw Regla(ex);
            }

            _repository.UpdateSesion(sesion);
            _repository.UpdateCaja(caja);
            await _repository.SaveChangesAsync();
            await AuditarEstrictoAsync(
                AccionPermiso.Cerrar,
                $"Sesión de caja cerrada: {sesion.Id}",
                sesion.Id,
                "CajaSesion");
        });
        return await ObtenerSesionResultadoAsync(sesionId);
    }

    private Task AutorizarAsync(AccionPermiso accion) =>
        _permisos.VerificarPermisoAsync(ModuloSistema.Caja, accion);

    private async Task AuditarEstrictoAsync(
        AccionPermiso accion,
        string descripcion,
        int referenciaId,
        string entidad)
    {
        await _auditoria.RegistrarEstrictoAsync(
            ModuloSistema.Caja,
            accion,
            descripcion,
            referenciaId,
            entidad: entidad);
    }

    private async Task<CajaDto> ObtenerCajaResultadoAsync(int id)
    {
        ValidarId(id, "caja");
        var caja = await _repository.GetCajaByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Caja con Id {id} no encontrada.");
        return Map(caja);
    }

    private async Task<CajaSesionDto> ObtenerSesionResultadoAsync(int id)
    {
        ValidarId(id, "sesión de caja");
        var sesion = await _repository.GetSesionByIdAsync(id)
            ?? throw new ResourceNotFoundException($"Sesión de caja con Id {id} no encontrada.");
        return Map(sesion);
    }

    private async Task<Caja> ObtenerCajaParaActualizarAsync(int id)
    {
        ValidarId(id, "caja");
        return await _repository.GetCajaByIdForUpdateAsync(id)
            ?? throw new ResourceNotFoundException($"Caja con Id {id} no encontrada.");
    }

    private async Task<CajaSesion> ObtenerSesionParaActualizarAsync(int id)
    {
        ValidarId(id, "sesión de caja");
        return await _repository.GetSesionByIdForUpdateAsync(id)
            ?? throw new ResourceNotFoundException($"Sesión de caja con Id {id} no encontrada.");
    }

    private int RequerirUsuarioId()
    {
        if (!_currentUser.EstaAutenticado || _currentUser.UsuarioId is not > 0)
            throw new ForbiddenAccessException("La operación de Caja requiere un usuario autenticado.");
        return _currentUser.UsuarioId.Value;
    }

    private static void ValidarId(int id, string entidad)
    {
        if (id <= 0) throw new BusinessRuleException($"El identificador de {entidad} debe ser mayor que cero.");
    }

    private static BusinessRuleException Regla(Exception ex) => new(ex.Message);

    private static CajaDto Map(Caja caja) => new()
    {
        Id = caja.Id,
        Nombre = caja.Nombre,
        Estado = caja.Estado,
        SesionActivaId = caja.SesionActivaId
    };

    private static CajaSesionDto Map(CajaSesion sesion) => new()
    {
        Id = sesion.Id,
        CajaId = sesion.CajaId,
        UsuarioId = sesion.UsuarioId,
        FechaApertura = sesion.FechaApertura,
        FechaCierre = sesion.FechaCierre,
        Estado = sesion.Estado,
        FondoInicial = sesion.FondoInicial,
        TotalIngresos = sesion.TotalIngresos,
        TotalRetiros = sesion.TotalRetiros,
        TotalDepositos = sesion.TotalDepositos,
        SaldoEsperado = sesion.SaldoEsperado,
        SaldoContado = sesion.SaldoContado,
        Diferencia = sesion.Diferencia,
        ObservacionesArqueo = sesion.ObservacionesArqueo,
        Movimientos = sesion.Movimientos.Select(Map).ToList()
    };

    private static CajaMovimientoDto Map(CajaMovimiento movimiento) => new()
    {
        Id = movimiento.Id,
        CajaSesionId = movimiento.CajaSesionId,
        UsuarioId = movimiento.UsuarioId,
        Tipo = movimiento.Tipo,
        Monto = movimiento.Monto,
        Referencia = movimiento.Referencia,
        FechaOperacion = movimiento.FechaOperacion,
        ImpactoSaldo = movimiento.ImpactoSaldo
    };
}
