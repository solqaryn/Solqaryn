using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class CreditoClienteService : ICreditoClienteService
{
    private readonly ICreditoClienteRepository _repository;
    private readonly IClienteRepository _clientes;
    private readonly IAuditoriaService _auditoria;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreditoClienteService(
        ICreditoClienteRepository repository,
        IClienteRepository clientes,
        IAuditoriaService auditoria,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _clientes = clientes ?? throw new ArgumentNullException(nameof(clientes));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreditoClienteDto> GetByIdAsync(int id)
    {
        if (id <= 0) throw new BusinessRuleException("El identificador de crédito debe ser mayor que cero.");
        var credito = await _repository.GetByIdAsync(id) ?? throw new ResourceNotFoundException($"Crédito de cliente con Id {id} no encontrado.");
        return Map(credito);
    }

    public async Task<IReadOnlyList<CreditoClienteDto>> GetByClienteIdAsync(int clienteId)
    {
        if (clienteId <= 0) throw new BusinessRuleException("ClienteId debe ser mayor que cero.");
        var items = await _repository.GetByClienteIdAsync(clienteId);
        return items.Select(Map).ToList();
    }

    public async Task<CreditoClienteDto> CrearAsync(CreateCreditoClienteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequerirUsuario();
        var id = 0;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var cliente = await _clientes.GetByIdAsync(dto.ClienteId) ?? throw new ResourceNotFoundException($"Cliente con Id {dto.ClienteId} no encontrado.");
            var credito = CreditoCliente.Crear(cliente, dto.Moneda, dto.LimiteCredito, dto.DiasCredito, dto.UmbralAlertaPorcentaje);
            await _repository.AddAsync(credito);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Clientes,
                AccionPermiso.Crear,
                "Configuración de crédito de cliente creada.",
                credito.Id,
                nameof(CreditoCliente),
                valoresNuevos: new { credito.ClienteId, credito.Moneda, credito.LimiteCredito, credito.DiasCredito, credito.UmbralAlertaPorcentaje });
            id = credito.Id;
        });
        return await GetByIdAsync(id);
    }

    public async Task<CreditoClienteDto> ActualizarPoliticaAsync(int id, UpdateCreditoClienteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var credito = await ObtenerParaActualizarAsync(id);
            credito.ActualizarPolitica(dto.Moneda, dto.LimiteCredito, dto.DiasCredito, dto.UmbralAlertaPorcentaje);
            _repository.Update(credito);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Clientes,
                AccionPermiso.Editar,
                "Política de crédito de cliente actualizada.",
                credito.Id,
                nameof(CreditoCliente),
                valoresNuevos: new { credito.Moneda, credito.LimiteCredito, credito.DiasCredito, credito.UmbralAlertaPorcentaje });
        });
        return await GetByIdAsync(id);
    }

    public async Task<CreditoClienteDto> AplicarBloqueoAutomaticoAsync(int id, AplicarBloqueoCreditoClienteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var credito = await ObtenerParaActualizarAsync(id);
            try
            {
                credito.AplicarBloqueoAutomatico(dto.Motivo, DateTime.UtcNow);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessRuleException(ex.Message);
            }
            _repository.Update(credito);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Clientes,
                AccionPermiso.Editar,
                "Bloqueo automático de crédito de cliente aplicado.",
                credito.Id,
                nameof(CreditoCliente),
                valoresNuevos: new { credito.BloqueadoAutomaticamente, credito.MotivoBloqueo, credito.BloqueadoUtc });
        });
        return await GetByIdAsync(id);
    }

    public async Task<CreditoClienteDto> LiberarBloqueoAutomaticoAsync(int id)
    {
        RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var credito = await ObtenerParaActualizarAsync(id);
            credito.LiberarBloqueoAutomatico(DateTime.UtcNow);
            _repository.Update(credito);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Clientes,
                AccionPermiso.Editar,
                "Bloqueo automático de crédito de cliente liberado.",
                credito.Id,
                nameof(CreditoCliente),
                valoresNuevos: new { credito.BloqueadoAutomaticamente, credito.MotivoBloqueo, credito.BloqueadoUtc });
        });
        return await GetByIdAsync(id);
    }

    public async Task<CreditoClienteDto> AutorizarExcepcionAsync(int id, AutorizarExcepcionCreditoClienteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var actor = RequerirUsuarioNombre();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var credito = await ObtenerParaActualizarAsync(id);
            try
            {
                credito.AutorizarExcepcion(dto.Monto, dto.VigenteHastaUtc, actor, DateTime.UtcNow);
            }
            catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
            {
                throw new BusinessRuleException(ex.Message);
            }
            _repository.Update(credito);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Clientes,
                AccionPermiso.Editar,
                "Excepción de crédito de cliente autorizada.",
                credito.Id,
                nameof(CreditoCliente),
                valoresNuevos: new { credito.MontoExcepcion, credito.ExcepcionVigenteHastaUtc, credito.ExcepcionAutorizadaPor, credito.ExcepcionAutorizadaUtc });
        });
        return await GetByIdAsync(id);
    }

    public async Task<CreditoClienteDto> RevocarExcepcionAsync(int id)
    {
        RequerirUsuario();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var credito = await ObtenerParaActualizarAsync(id);
            credito.RevocarExcepcion();
            _repository.Update(credito);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Clientes,
                AccionPermiso.Editar,
                "Excepción de crédito de cliente revocada.",
                credito.Id,
                nameof(CreditoCliente),
                valoresNuevos: new { credito.MontoExcepcion, credito.ExcepcionVigenteHastaUtc, credito.ExcepcionAutorizadaPor, credito.ExcepcionAutorizadaUtc });
        });
        return await GetByIdAsync(id);
    }

    private async Task<CreditoCliente> ObtenerParaActualizarAsync(int id)
    {
        if (id <= 0) throw new BusinessRuleException("El identificador de crédito debe ser mayor que cero.");
        return await _repository.GetByIdForUpdateAsync(id)
            ?? throw new ResourceNotFoundException($"Crédito de cliente con Id {id} no encontrado.");
    }

    private void RequerirUsuario()
    {
        if (_currentUser.UsuarioId is not > 0)
            throw new ForbiddenAccessException("La operación requiere un usuario autenticado.");
    }

    private string RequerirUsuarioNombre()
    {
        RequerirUsuario();
        var nombre = _currentUser.NombreCompleto?.Trim();
        if (string.IsNullOrWhiteSpace(nombre)) nombre = _currentUser.NombreUsuario?.Trim();
        return string.IsNullOrWhiteSpace(nombre)
            ? _currentUser.UsuarioId!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : nombre;
    }

    private static CreditoClienteDto Map(CreditoCliente x) => new()
    {
        Id = x.Id,
        ClienteId = x.ClienteId,
        Moneda = x.Moneda,
        LimiteCredito = x.LimiteCredito,
        DiasCredito = x.DiasCredito,
        UmbralAlertaPorcentaje = x.UmbralAlertaPorcentaje,
        BloqueadoAutomaticamente = x.BloqueadoAutomaticamente,
        MotivoBloqueo = x.MotivoBloqueo,
        BloqueadoUtc = x.BloqueadoUtc,
        MontoExcepcion = x.MontoExcepcion,
        ExcepcionVigenteHastaUtc = x.ExcepcionVigenteHastaUtc,
        ExcepcionAutorizadaPor = x.ExcepcionAutorizadaPor,
        ExcepcionAutorizadaUtc = x.ExcepcionAutorizadaUtc
    };
}
