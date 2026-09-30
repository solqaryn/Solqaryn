using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using MetodoPagoEntity = Solqaryn.Domain.Entities.Catalogos.MetodoPago;

namespace Solqaryn.Application.Services;

public sealed class MetodoPagoService : IMetodoPagoService
{
    private readonly IMetodoPagoRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    public MetodoPagoService(IMetodoPagoRepository repository, ICurrentUserService currentUser, IAuditoriaService auditoria, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MetodoPagoDto>> GetAllAsync() => (await _repository.GetAllAsync()).Select(ToDto).ToList();
    public async Task<List<MetodoPagoDto>> GetActivosAsync() => (await _repository.GetActivosAsync()).Select(ToDto).ToList();
    public async Task<MetodoPagoDto?> GetByIdAsync(int id) => (await _repository.GetByIdAsync(id)) is { } item ? ToDto(item) : null;

    public async Task<MetodoPagoDto> CreateAsync(CreateMetodoPagoDto dto)
    {
        var codigo = NormalizarCodigoEntrada(dto.Codigo);
        ValidarCampos(dto.Nombre, dto.Tipo, dto.Orden);
        if (await _repository.ExisteCodigoAsync(codigo)) throw new BusinessRuleException($"Ya existe un método de pago con código '{codigo}'.");

        var item = new MetodoPagoEntity
        {
            Codigo = codigo,
            Nombre = dto.Nombre.Trim(),
            Tipo = dto.Tipo.Trim(),
            Activo = dto.Activo,
            RequiereReferencia = dto.RequiereReferencia,
            RequiereBanco = dto.RequiereBanco,
            PermiteCambio = dto.PermiteCambio,
            Orden = dto.Orden,
            Metadata = dto.Metadata,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(ModuloSistema.MetodosPago, AccionPermiso.Crear, $"Método de pago creado: {item.Nombre} ({item.Codigo})", item.Id);
        });
        return ToDto(item);
    }

    public async Task<MetodoPagoDto?> UpdateAsync(int id, UpdateMetodoPagoDto dto)
    {
        ValidarCampos(dto.Nombre, dto.Tipo, dto.Orden);
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return null;
        var anteriores = ToDto(item);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            item.Nombre = dto.Nombre.Trim();
            item.Tipo = dto.Tipo.Trim();
            item.Activo = dto.Activo;
            item.RequiereReferencia = dto.RequiereReferencia;
            item.RequiereBanco = dto.RequiereBanco;
            item.PermiteCambio = dto.PermiteCambio;
            item.Orden = dto.Orden;
            item.Metadata = dto.Metadata;
            MarcarActualizacion(item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(ModuloSistema.MetodosPago, AccionPermiso.Editar, $"Método de pago actualizado: {item.Nombre} ({item.Codigo})", item.Id, valoresAnteriores: anteriores, valoresNuevos: ToDto(item));
        });

        return ToDto(item);
    }

    public async Task<MetodoPagoDto?> CambiarEstadoAsync(int id, bool activo)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return null;
        if (item.Activo == activo) return ToDto(item);
        var anteriores = ToDto(item);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            item.Activo = activo;
            MarcarActualizacion(item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.MetodosPago,
                activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
                $"Método de pago {(activo ? "activado" : "desactivado")}: {item.Nombre} ({item.Codigo})",
                item.Id,
                valoresAnteriores: anteriores,
                valoresNuevos: ToDto(item));
        });

        return ToDto(item);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return false;
        var anteriores = ToDto(item);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            item.Eliminado = true;
            item.Activo = false;
            item.FechaEliminacion = DateTime.UtcNow;
            item.EliminadoPorUsuarioId = _currentUser.UsuarioId;
            MarcarActualizacion(item);
            _repository.Update(item);
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.MetodosPago,
                AccionPermiso.EliminarLogico,
                $"Método de pago eliminado lógicamente: {item.Nombre} ({item.Codigo})",
                item.Id,
                valoresAnteriores: anteriores,
                valoresNuevos: ToDto(item));
        });

        return true;
    }

    public async Task ReordenarAsync(IReadOnlyCollection<ReordenarMetodoPagoDto> items)
    {
        if (items.Count == 0) throw new BusinessRuleException("Debe indicar al menos un método de pago para reordenar.");
        if (items.Any(x => x.Id <= 0 || x.Orden < 0)) throw new BusinessRuleException("Los identificadores y órdenes deben ser válidos.");
        if (items.Select(x => x.Id).Distinct().Count() != items.Count) throw new BusinessRuleException("No se permiten métodos de pago duplicados en el reordenamiento.");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var cambio in items)
            {
                var item = await _repository.GetByIdAsync(cambio.Id) ?? throw new BusinessRuleException($"No existe el método de pago {cambio.Id}.");
                item.Orden = cambio.Orden;
                MarcarActualizacion(item);
                _repository.Update(item);
            }
            await _repository.SaveChangesAsync();
            await _auditoria.RegistrarEstrictoAsync(ModuloSistema.MetodosPago, AccionPermiso.Editar, $"Reordenamiento de {items.Count} métodos de pago.", entidad: "MetodoPago");
        });
    }

    private void MarcarActualizacion(MetodoPagoEntity item)
    {
        item.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        item.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        item.FechaActualizacion = DateTime.UtcNow;
    }

    private static void ValidarCampos(string nombre, string tipo, int orden)
    {
        if (string.IsNullOrWhiteSpace(nombre)) throw new BusinessRuleException("El nombre del método de pago es obligatorio.");
        if (string.IsNullOrWhiteSpace(tipo)) throw new BusinessRuleException("El tipo del método de pago es obligatorio.");
        if (orden < 0) throw new BusinessRuleException("El orden no puede ser negativo.");
    }

    private static string NormalizarCodigoEntrada(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new BusinessRuleException("El código del método de pago es obligatorio.");
        var normalizado = codigo.Trim().ToUpperInvariant();
        if (normalizado.Length > 40 || normalizado.Any(c => !(char.IsLetterOrDigit(c) || c == '_'))) throw new BusinessRuleException("El código debe contener solo letras, números o guion bajo y tener máximo 40 caracteres.");
        return normalizado;
    }

    private static MetodoPagoDto ToDto(MetodoPagoEntity x) => new()
    {
        Id = x.Id, Codigo = x.Codigo, Nombre = x.Nombre, Tipo = x.Tipo, Activo = x.Activo,
        RequiereReferencia = x.RequiereReferencia, RequiereBanco = x.RequiereBanco,
        PermiteCambio = x.PermiteCambio, Orden = x.Orden, Metadata = x.Metadata
    };
}
