using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class CentroCostoService : ICentroCostoService
{
    private const int TamanoPaginaMaximo = 100;
    private readonly ICentroCostoRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public CentroCostoService(
        ICentroCostoRepository repository,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<(List<CentroCostoDto> Items, int Total)> BuscarAsync(string? termino, TipoCentroCosto? tipo, int? sucursalId, bool? activo, int pagina, int tamanoPagina)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, TamanoPaginaMaximo);
        var (items, total) = await _repository.BuscarAsync(Limpiar(termino), tipo, sucursalId, activo, pagina, tamanoPagina);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<List<CentroCostoDto>> GetActivosAsync(TipoCentroCosto? tipo = null, int? sucursalId = null)
        => (await _repository.GetActivosAsync(tipo, sucursalId)).Select(ToDto).ToList();

    public async Task<CentroCostoDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<CentroCostoDto> CreateAsync(CreateCentroCostoDto dto)
    {
        var codigo = NormalizarCodigo(dto.Codigo);
        if (await _repository.ExisteCodigoAsync(codigo))
            throw new BusinessRuleException($"Ya existe un centro de costo activo con el código '{codigo}'.");

        ValidarAsociacion(dto.Tipo, dto.SucursalId);
        var entity = new CentroCosto
        {
            Codigo = codigo,
            Nombre = NormalizarRequerido(dto.Nombre, "El nombre del centro de costo es obligatorio."),
            Descripcion = Limpiar(dto.Descripcion),
            Tipo = dto.Tipo,
            SucursalId = dto.SucursalId,
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(entity);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No fue posible guardar el centro de costo.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Crear,
            $"Centro de costo creado: {entity.Codigo} - {entity.Nombre}",
            entity.Id,
            entidad: nameof(CentroCosto),
            valoresNuevos: new { entity.Codigo, entity.Nombre, entity.Tipo, entity.SucursalId, entity.Activo });

        return ToDto(entity);
    }

    public async Task<CentroCostoDto?> UpdateAsync(int id, UpdateCentroCostoDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return null;

        var anteriores = new
        {
            entity.Codigo,
            entity.Nombre,
            entity.Descripcion,
            entity.Tipo,
            entity.SucursalId,
            entity.Activo
        };

        var codigo = NormalizarCodigo(dto.Codigo);
        if (await _repository.ExisteCodigoAsync(codigo, id))
            throw new BusinessRuleException($"Ya existe otro centro de costo activo con el código '{codigo}'.");

        ValidarAsociacion(dto.Tipo, dto.SucursalId);
        entity.Codigo = codigo;
        entity.Nombre = NormalizarRequerido(dto.Nombre, "El nombre del centro de costo es obligatorio.");
        entity.Descripcion = Limpiar(dto.Descripcion);
        entity.Tipo = dto.Tipo;
        entity.SucursalId = dto.SucursalId;
        entity.Activo = dto.Activo;
        entity.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        entity.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        entity.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(entity);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No fue posible actualizar el centro de costo.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.Editar,
            $"Centro de costo actualizado: {entity.Codigo} - {entity.Nombre}",
            entity.Id,
            entidad: nameof(CentroCosto),
            valoresAnteriores: anteriores,
            valoresNuevos: new { entity.Codigo, entity.Nombre, entity.Descripcion, entity.Tipo, entity.SucursalId, entity.Activo });

        return ToDto(entity);
    }

    public async Task<CentroCostoDto?> CambiarEstadoAsync(int id, bool activo)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return null;
        if (entity.Activo == activo) return ToDto(entity);

        if (activo) entity.Activar(); else entity.Desactivar();
        entity.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        entity.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        _repository.Update(entity);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No fue posible cambiar el estado del centro de costo.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Finanzas,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Centro de costo {(activo ? "activado" : "desactivado")}: {entity.Codigo} - {entity.Nombre}",
            entity.Id,
            entidad: nameof(CentroCosto),
            valoresNuevos: new { entity.Activo });

        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return false;

        entity.MarcarEliminado(_currentUser.UsuarioId);
        entity.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        entity.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        _repository.Update(entity);
        var eliminado = await _repository.SaveChangesAsync();
        if (!eliminado) return false;

        await _auditoria.RegistrarAsync(
            ModuloSistema.Finanzas,
            AccionPermiso.EliminarLogico,
            $"Centro de costo eliminado lógicamente: {entity.Codigo} - {entity.Nombre}",
            entity.Id,
            entidad: nameof(CentroCosto),
            valoresNuevos: new { entity.Activo, entity.Eliminado, entity.FechaEliminacion, entity.EliminadoPorUsuarioId });

        return true;
    }

    private static void ValidarAsociacion(TipoCentroCosto tipo, int? sucursalId)
    {
        if (tipo == TipoCentroCosto.Sucursal && (!sucursalId.HasValue || sucursalId.Value <= 0))
            throw new BusinessRuleException("SucursalId es obligatorio y debe ser mayor que cero cuando Tipo=Sucursal.");
        if (tipo != TipoCentroCosto.Sucursal && sucursalId.HasValue)
            throw new BusinessRuleException("SucursalId debe ser nulo cuando el tipo de centro de costo no es Sucursal.");
    }

    private static string NormalizarCodigo(string? valor)
        => NormalizarRequerido(valor, "El código del centro de costo es obligatorio.").ToUpperInvariant();

    private static string NormalizarRequerido(string? valor, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new BusinessRuleException(mensaje);
        return valor.Trim();
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static CentroCostoDto ToDto(CentroCosto entity) => new()
    {
        Id = entity.Id,
        Codigo = entity.Codigo,
        Nombre = entity.Nombre,
        Descripcion = entity.Descripcion,
        Tipo = entity.Tipo,
        SucursalId = entity.SucursalId,
        SucursalCodigo = entity.Sucursal?.Codigo,
        SucursalNombre = entity.Sucursal?.Nombre,
        Activo = entity.Activo,
        Eliminado = entity.Eliminado,
        FechaEliminacion = entity.FechaEliminacion,
        EliminadoPorUsuarioId = entity.EliminadoPorUsuarioId,
        CreadoPorNombreUsuario = entity.CreadoPorNombreUsuario,
        ActualizadoPorNombreUsuario = entity.ActualizadoPorNombreUsuario,
        FechaCreacion = entity.FechaCreacion,
        FechaActualizacion = entity.FechaActualizacion
    };
}
