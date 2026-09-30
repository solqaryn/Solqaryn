using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _repository;
    private readonly ITipoClienteRepository _tipoClienteRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly ITipoClientePredeterminadoResolver _predeterminadoResolver;

    public ClienteService(
        IClienteRepository repository,
        ITipoClienteRepository tipoClienteRepository,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria,
        ITipoClientePredeterminadoResolver predeterminadoResolver)
    {
        _repository = repository;
        _tipoClienteRepository = tipoClienteRepository;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _predeterminadoResolver = predeterminadoResolver;
    }

    public async Task<List<ClienteDto>> GetAllAsync()
    {
        var clientes = await _repository.GetAllAsync();
        return clientes.Select(c => ToDto(c)).ToList();
    }

    public async Task<List<ClienteDto>> GetActivosAsync()
    {
        var clientes = await _repository.GetActivosAsync();
        return clientes.Select(c => ToDto(c, incluirVentas: false)).ToList();
    }

    public async Task<List<ClienteDto>> BuscarActivosAsync(string termino)
    {
        var clientes = await _repository.BuscarActivosAsync(termino);
        return clientes.Select(c => ToDto(c, incluirVentas: false)).ToList();
    }

    public async Task<ClienteDto?> GetByIdAsync(int id)
    {
        var cliente = await _repository.GetByIdConVentasAsync(id);
        return cliente is null ? null : ToDto(cliente);
    }

    public async Task<ClienteDto> CreateAsync(CreateClienteDto dto)
    {
        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new BusinessRuleException("El nombre del cliente es obligatorio.");

        var identidad = Limpiar(dto.IdentidadORTN);
        if (identidad is not null && await _repository.ExisteIdentidadAsync(identidad))
            throw new BusinessRuleException($"Ya existe un cliente con la identidad/RTN '{identidad}'.");

        int tipoClienteId;
        if (dto.TipoClienteId.HasValue && dto.TipoClienteId.Value > 0)
        {
            var tipo = await _tipoClienteRepository.GetByIdAsync(dto.TipoClienteId.Value);
            if (tipo is null || !tipo.Activo)
                throw new BusinessRuleException($"El tipo de cliente con ID {dto.TipoClienteId.Value} no existe o está inactivo.");
            tipoClienteId = tipo.Id;
        }
        else
        {
            tipoClienteId = await _predeterminadoResolver.ResolverIdPredeterminadoAsync();
        }

        var cliente = new Cliente
        {
            Nombre = nombre,
            Telefono = Limpiar(dto.Telefono),
            IdentidadORTN = identidad,
            Correo = Limpiar(dto.Correo),
            Direccion = Limpiar(dto.Direccion),
            Activo = true,
            TipoClienteId = tipoClienteId,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(cliente);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Clientes, AccionPermiso.Crear, $"Cliente creado: {cliente.Nombre}", cliente.Id);

        var updatedCliente = await _repository.GetByIdAsync(cliente.Id);
        return ToDto(updatedCliente ?? cliente);
    }

    public async Task<ClienteDto?> UpdateAsync(int id, UpdateClienteDto dto)
    {
        var cliente = await _repository.GetByIdAsync(id);
        if (cliente is null) return null;

        var nombre = dto.Nombre.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new BusinessRuleException("El nombre del cliente es obligatorio.");

        var identidad = Limpiar(dto.IdentidadORTN);
        if (identidad is not null && await _repository.ExisteIdentidadAsync(identidad, id))
            throw new BusinessRuleException($"Ya existe otro cliente con la identidad/RTN '{identidad}'.");

        if (dto.TipoClienteId.HasValue && dto.TipoClienteId.Value > 0)
        {
            var tipo = await _tipoClienteRepository.GetByIdAsync(dto.TipoClienteId.Value);
            if (tipo is null || !tipo.Activo)
                throw new BusinessRuleException($"El tipo de cliente con ID {dto.TipoClienteId.Value} no existe o está inactivo.");
            cliente.TipoClienteId = tipo.Id;
        }

        cliente.Nombre = nombre;
        cliente.Telefono = Limpiar(dto.Telefono);
        cliente.IdentidadORTN = identidad;
        cliente.Correo = Limpiar(dto.Correo);
        cliente.Direccion = Limpiar(dto.Direccion);
        cliente.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        cliente.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        cliente.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(cliente);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Clientes, AccionPermiso.Editar, $"Cliente actualizado: {cliente.Nombre}", cliente.Id);

        var updatedCliente = await _repository.GetByIdAsync(cliente.Id);
        return ToDto(updatedCliente ?? cliente);
    }

    public async Task<ClienteDto?> CambiarEstadoAsync(int id, bool activo)
    {
        var cliente = await _repository.GetByIdAsync(id);
        if (cliente is null) return null;
        if (cliente.Activo == activo) return ToDto(cliente, incluirVentas: false);

        cliente.Activo = activo;
        cliente.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        cliente.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        cliente.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(cliente);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.Clientes,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Cliente {(activo ? "activado" : "desactivado")}: {cliente.Nombre}",
            cliente.Id);

        return ToDto(cliente, incluirVentas: false);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cliente = await _repository.GetByIdConVentasAsync(id);
        if (cliente is null) return false;

        cliente.Activo = false;
        cliente.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        cliente.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        cliente.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(cliente);
        var eliminado = await _repository.SaveChangesAsync();
        if (eliminado)
            await _auditoria.RegistrarAsync(ModuloSistema.Clientes, AccionPermiso.EliminarLogico, $"Cliente desactivado como eliminación lógica: {cliente.Nombre}", id);
        return eliminado;
    }

    private static string? Limpiar(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ClienteDto ToDto(Cliente c, bool incluirVentas = true) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Telefono = c.Telefono,
        IdentidadORTN = c.IdentidadORTN,
        Correo = c.Correo,
        Direccion = c.Direccion,
        Activo = c.Activo,
        TotalVentas = incluirVentas ? c.Ventas?.Count(v => v.Estado != EstadoDocumento.Anulada) ?? 0 : 0,
        TotalVendido = incluirVentas ? c.Ventas?.Where(v => v.Estado == EstadoDocumento.Confirmada).Sum(v => v.Total) ?? 0 : 0,
        CreadoPorNombreUsuario = c.CreadoPorNombreUsuario,
        FechaCreacion = c.FechaCreacion,
        TipoClienteId = c.TipoClienteId,
        TipoClienteNombre = c.TipoCliente?.Nombre ?? "Sin clasificar",
        TipoClienteColorHex = c.TipoCliente?.ColorHex ?? "#9E9E9E"
    };
}
