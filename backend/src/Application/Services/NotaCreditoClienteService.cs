using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class NotaCreditoClienteService : INotaCreditoClienteService
{
    private readonly INotaCreditoClienteRepository _repository;
    private readonly IFacturaRepository _facturas;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public NotaCreditoClienteService(
        INotaCreditoClienteRepository repository,
        IFacturaRepository facturas,
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

    public async Task<NotaCreditoClienteDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new BusinessRuleException("El identificador de la nota de crédito debe ser mayor que cero.");

        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : Map(entity);
    }

    public async Task<NotaCreditoClienteDto> CreateAsync(CreateNotaCreditoClienteDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.FacturaId <= 0)
            throw new BusinessRuleException("FacturaId debe ser mayor que cero.");
        if (dto.MontoCredito <= 0m)
            throw new BusinessRuleException("MontoCredito debe ser mayor que cero.");
        if (string.IsNullOrWhiteSpace(dto.Motivo))
            throw new BusinessRuleException("El motivo es obligatorio.");

        var (usuarioId, nombreUsuario) = RequerirUsuario();
        NotaCreditoCliente? creada = null;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var factura = await _facturas.GetByIdAsync(dto.FacturaId)
                ?? throw new ResourceNotFoundException($"Factura con Id {dto.FacturaId} no encontrada.");

            try
            {
                creada = NotaCreditoCliente.CrearDesdeFactura(
                    factura,
                    dto.MontoCredito,
                    dto.Motivo,
                    dto.Observaciones);
                creada.CreadoPorUsuarioId = usuarioId;
                creada.CreadoPorNombreUsuario = nombreUsuario;
                creada.FechaCreacion = DateTime.UtcNow;
                creada.FechaActualizacion = creada.FechaCreacion;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                throw new BusinessRuleException(ex.Message);
            }

            await _repository.AddAsync(creada);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.Ventas,
                AccionPermiso.Crear,
                "Creación de nota de crédito de cliente.",
                creada.Id,
                nameof(NotaCreditoCliente),
                valoresNuevos: new
                {
                    creada.FacturaId,
                    creada.VentaId,
                    creada.Moneda,
                    creada.MontoCredito,
                    creada.Motivo
                });
        });

        return Map(creada ?? throw new InvalidOperationException("La creación de la nota de crédito no produjo un resultado."));
    }

    private (int UsuarioId, string NombreUsuario) RequerirUsuario()
    {
        if (!_currentUser.EstaAutenticado || _currentUser.UsuarioId is not > 0)
            throw new ForbiddenAccessException("La operación requiere un usuario autenticado.");

        var nombre = _currentUser.NombreCompleto?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            nombre = _currentUser.NombreUsuario?.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ForbiddenAccessException("No se pudo resolver la identidad del usuario autenticado.");

        return (_currentUser.UsuarioId.Value, nombre);
    }

    private static NotaCreditoClienteDto Map(NotaCreditoCliente entity) => new()
    {
        Id = entity.Id,
        FacturaId = entity.FacturaId,
        VentaId = entity.VentaId,
        Moneda = entity.Moneda,
        MontoCredito = entity.MontoCredito,
        Motivo = entity.Motivo,
        Observaciones = entity.Observaciones,
        FechaCreacion = entity.FechaCreacion,
        FechaActualizacion = entity.FechaActualizacion,
        CreadoPorUsuarioId = entity.CreadoPorUsuarioId,
        CreadoPorNombreUsuario = entity.CreadoPorNombreUsuario
    };
}
