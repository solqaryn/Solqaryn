using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class CategoriaService : ICategoriaService
{
    private readonly ICategoriaRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public CategoriaService(ICategoriaRepository repository, ICurrentUserService currentUser, IAuditoriaService auditoria)
    {
        _repository = repository;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<List<CategoriaDto>> GetAllAsync()
    {
        var categorias = await _repository.GetAllAsync();
        return categorias.Select(c => ToDto(c)).ToList();
    }

    public async Task<List<CategoriaDto>> GetActivasAsync()
    {
        var categorias = await _repository.GetActivasAsync();
        return categorias.Select(c => ToDto(c, incluirConteo: false)).ToList();
    }

    public async Task<CategoriaDto?> GetByIdAsync(int id)
    {
        var categoria = await _repository.GetByIdConProductosAsync(id);
        return categoria is null ? null : ToDto(categoria);
    }

    public async Task<CategoriaDto> CreateAsync(CreateCategoriaDto dto)
    {
        if (await _repository.ExisteNombreAsync(dto.Nombre))
            throw new BusinessRuleException($"Ya existe una categoría con el nombre '{dto.Nombre}'.");

        var categoria = new Categoria
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            Activa = true,
            Eliminada = false,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(categoria);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Categorias, AccionPermiso.Crear, $"Categoría creada: {categoria.Nombre}", categoria.Id);

        return ToDto(categoria);
    }

    public async Task<CategoriaDto?> UpdateAsync(int id, UpdateCategoriaDto dto)
    {
        var categoria = await _repository.GetByIdAsync(id);
        if (categoria is null) return null;

        if (await _repository.ExisteNombreAsync(dto.Nombre, id))
            throw new BusinessRuleException($"Ya existe una categoría con el nombre '{dto.Nombre}'.");

        categoria.Nombre = dto.Nombre.Trim();
        categoria.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        // El estado se modifica únicamente mediante Activar/Desactivar, nunca
        // como efecto lateral del permiso Editar.
        categoria.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        categoria.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        categoria.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(categoria);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Categorias, AccionPermiso.Editar, $"Categoría actualizada: {categoria.Nombre}", categoria.Id);

        return ToDto(categoria);
    }

    public async Task<CategoriaDto?> CambiarEstadoAsync(int id, bool activa)
    {
        var categoria = await _repository.GetByIdAsync(id);
        if (categoria is null) return null;
        if (categoria.Activa == activa) return ToDto(categoria);

        categoria.Activa = activa;
        categoria.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        categoria.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        categoria.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(categoria);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.Categorias,
            activa ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Categoría {(activa ? "activada" : "desactivada")}: {categoria.Nombre}",
            categoria.Id);

        return ToDto(categoria);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var categoria = await _repository.GetByIdConProductosAsync(id);
        if (categoria is null) return false;

        categoria.Activa = false;
        categoria.Eliminada = true;
        categoria.FechaEliminacion = DateTime.UtcNow;
        categoria.EliminadaPorUsuarioId = _currentUser.UsuarioId;
        categoria.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        categoria.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        categoria.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(categoria);
        var eliminado = await _repository.SaveChangesAsync();
        if (eliminado)
            await _auditoria.RegistrarAsync(ModuloSistema.Categorias, AccionPermiso.EliminarLogico, $"Categoría eliminada lógicamente: {categoria.Nombre}", categoria.Id);
        return eliminado;
    }

    private static CategoriaDto ToDto(Categoria c, bool incluirConteo = true) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Descripcion = c.Descripcion,
        Activa = c.Activa,
        TotalProductos = incluirConteo ? c.Productos?.Count ?? 0 : 0,
        CreadoPorNombreUsuario = c.CreadoPorNombreUsuario,
        ActualizadoPorNombreUsuario = c.ActualizadoPorNombreUsuario,
        FechaCreacion = c.FechaCreacion,
        FechaActualizacion = c.FechaActualizacion
    };
}
