using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

public class CostoEnvioService : ICostoEnvioService
{
    private readonly ICostoEnvioRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public CostoEnvioService(ICostoEnvioRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<List<CostoEnvioDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(ToDto).ToList();

    public async Task<CostoEnvioDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item is null ? null : ToDto(item);
    }

    public async Task<CostoEnvioDto?> GetPredeterminadoVigenteAsync()
    {
        var item = await _repository.GetPredeterminadoVigenteAsync(DateTime.UtcNow);
        return item is null ? null : ToDto(item);
    }

    public async Task<CostoEnvioDto> CreateAsync(GuardarCostoEnvioDto dto)
    {
        Validar(dto);
        var nombre = dto.Nombre.Trim();
        if (await _repository.ExisteNombreAsync(nombre.ToUpperInvariant()))
            throw new BusinessRuleException("Ya existe un costo de envío con ese nombre.");
        if (dto.EsPredeterminado)
            await _repository.DesmarcarPredeterminadosAsync();

        var item = new CostoEnvio
        {
            Nombre = nombre,
            Descripcion = Normalizar(dto.Descripcion, 500),
            Monto = Math.Round(dto.Monto, 2, MidpointRounding.AwayFromZero),
            VigenteDesde = dto.VigenteDesde?.ToUniversalTime(),
            VigenteHasta = dto.VigenteHasta?.ToUniversalTime(),
            Prioridad = dto.Prioridad,
            EsPredeterminado = dto.EsPredeterminado,
            Activo = dto.Activo,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };
        await _repository.AddAsync(item);
        await _repository.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<CostoEnvioDto?> UpdateAsync(int id, GuardarCostoEnvioDto dto)
    {
        Validar(dto);
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return null;
        var nombre = dto.Nombre.Trim();
        if (await _repository.ExisteNombreAsync(nombre.ToUpperInvariant(), id))
            throw new BusinessRuleException("Ya existe un costo de envío con ese nombre.");
        if (dto.EsPredeterminado)
            await _repository.DesmarcarPredeterminadosAsync(id);

        item.Nombre = nombre;
        item.Descripcion = Normalizar(dto.Descripcion, 500);
        item.Monto = Math.Round(dto.Monto, 2, MidpointRounding.AwayFromZero);
        item.VigenteDesde = dto.VigenteDesde?.ToUniversalTime();
        item.VigenteHasta = dto.VigenteHasta?.ToUniversalTime();
        item.Prioridad = dto.Prioridad;
        item.EsPredeterminado = dto.EsPredeterminado;
        item.Activo = dto.Activo;
        item.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        item.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        item.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(item);
        await _repository.SaveChangesAsync();
        return ToDto(item);
    }

    public async Task<bool> CambiarEstadoAsync(int id, bool activo)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return false;
        item.Activo = activo;
        if (!activo) item.EsPredeterminado = false;
        item.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(item);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return false;
        item.Eliminado = true;
        item.Activo = false;
        item.EsPredeterminado = false;
        item.FechaEliminacion = DateTime.UtcNow;
        item.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        item.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(item);
        await _repository.SaveChangesAsync();
        return true;
    }

    private static void Validar(GuardarCostoEnvioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new BusinessRuleException("El nombre es obligatorio.");
        if (dto.Nombre.Trim().Length > 150)
            throw new BusinessRuleException("El nombre no puede superar 150 caracteres.");
        if (dto.Monto < 0)
            throw new BusinessRuleException("El monto no puede ser negativo.");
        if (dto.VigenteDesde.HasValue && dto.VigenteHasta.HasValue && dto.VigenteHasta < dto.VigenteDesde)
            throw new BusinessRuleException("La fecha final de vigencia no puede ser anterior a la fecha inicial.");
    }

    private static string? Normalizar(string? valor, int maximo)
    {
        var limpio = string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        return limpio is not null && limpio.Length > maximo ? limpio[..maximo] : limpio;
    }

    private static CostoEnvioDto ToDto(CostoEnvio x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Monto = x.Monto,
        VigenteDesde = x.VigenteDesde,
        VigenteHasta = x.VigenteHasta,
        Prioridad = x.Prioridad,
        EsPredeterminado = x.EsPredeterminado,
        Activo = x.Activo,
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };
}
