using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public class TipoClienteService : ITipoClienteService
{
    private readonly ITipoClienteRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    private readonly IUnitOfWork _unitOfWork;

    public TipoClienteService(
        ITipoClienteRepository repository,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _auditoria = auditoria;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<TipoClienteDto>> GetAllAsync()
    {
        var tipos = await _repository.GetAllAsync();
        return tipos.Select(ToDto).ToList();
    }

    public async Task<List<TipoClienteDto>> GetActivosAsync()
    {
        var tipos = await _repository.GetActivosAsync();
        return tipos.Select(ToDto).ToList();
    }

    public async Task<TipoClienteDto?> GetByIdAsync(int id)
    {
        var tipo = await _repository.GetByIdAsync(id);
        return tipo is null ? null : ToDto(tipo);
    }

    public async Task<TipoClienteDto> CreateAsync(CreateTipoClienteDto dto)
    {
        if (dto.EsPredeterminado && !dto.Activo)
        {
            throw new BusinessRuleException("El tipo de cliente predeterminado debe estar activo.");
        }

        var nombre = dto.Nombre.Trim();
        var normalizado = nombre.ToUpper();

        if (await _repository.ExisteNombreNormalizadoAsync(normalizado))
            throw new BusinessRuleException($"Ya existe un tipo de cliente con el nombre '{nombre}'.");

        var baseCodigo = string.Concat(normalizado.Where(c => char.IsLetterOrDigit(c) || c == ' ')).Replace(" ", "_").Trim();
        if (string.IsNullOrEmpty(baseCodigo)) baseCodigo = "CUSTOM";
        var codigo = baseCodigo;
        int counter = 1;
        while (await _repository.ExisteCodigoAsync(codigo))
        {
            codigo = $"{baseCodigo}_{counter++}";
        }

        var tipo = new TipoCliente
        {
            Codigo = codigo,
            EsSistema = false,
            Nombre = nombre,
            NombreNormalizado = normalizado,
            Descripcion = dto.Descripcion,
            ColorHex = dto.ColorHex,
            Activo = dto.Activo,
            Orden = dto.Orden,
            EsPredeterminado = dto.EsPredeterminado,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (tipo.EsPredeterminado)
                {
                    await DesmarcarPredeterminadosExistentesAsync();
                }

                await _repository.AddAsync(tipo);
                await _repository.SaveChangesAsync();
                await _auditoria.RegistrarAsync(ModuloSistema.TiposClientes, AccionPermiso.Crear, $"Tipo de cliente creado: {tipo.Nombre} ({tipo.Codigo})", tipo.Id);
            });
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == "TipoClientePredeterminadoUnico")
        {
            throw new BusinessRuleException(ex.Message);
        }

        return ToDto(tipo);
    }

    public async Task<TipoClienteDto?> UpdateAsync(int id, UpdateTipoClienteDto dto)
    {
        if (dto.EsPredeterminado && !dto.Activo)
        {
            throw new BusinessRuleException("El tipo de cliente predeterminado debe estar activo.");
        }

        var tipo = await _repository.GetByIdAsync(id);
        if (tipo is null) return null;

        var nombre = dto.Nombre.Trim();
        var normalizado = nombre.ToUpper();

        if (await _repository.ExisteNombreNormalizadoAsync(normalizado, id))
            throw new BusinessRuleException($"Ya existe un tipo de cliente con el nombre '{nombre}'.");

        // Reglas para tipos de sistema
        if (tipo.EsSistema)
        {
            // SIN_CLASIFICAR no se puede desactivar ni desmarcar como predeterminado directamente
            if (tipo.Codigo == "SIN_CLASIFICAR")
            {
                if (!dto.Activo)
                    throw new BusinessRuleException("El tipo de cliente 'Sin clasificar' no puede ser desactivado.");
                if (!dto.EsPredeterminado)
                    throw new BusinessRuleException("El tipo de cliente 'Sin clasificar' no puede dejar de ser predeterminado a menos que asigne otro tipo como predeterminado.");
            }
        }

        // Si se intenta desactivar el tipo predeterminado actual
        if (tipo.EsPredeterminado && !dto.Activo)
            throw new BusinessRuleException("No se puede desactivar el tipo de cliente que está configurado como predeterminado.");

        // Si se intenta quitar el predeterminado sin asignar otro
        if (tipo.EsPredeterminado && !dto.EsPredeterminado)
            throw new BusinessRuleException("Debe configurar otro tipo de cliente como predeterminado antes de desmarcar este.");

        tipo.Nombre = nombre;
        tipo.NombreNormalizado = normalizado;
        tipo.Descripcion = dto.Descripcion;
        tipo.ColorHex = dto.ColorHex;
        tipo.Activo = dto.Activo;
        tipo.Orden = dto.Orden;
        
        var cambioPredeterminado = !tipo.EsPredeterminado && dto.EsPredeterminado;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (cambioPredeterminado)
                {
                    await DesmarcarPredeterminadosExistentesAsync();
                    tipo.EsPredeterminado = true;
                }

                tipo.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                tipo.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
                tipo.FechaActualizacion = DateTime.UtcNow;

                _repository.Update(tipo);
                await _repository.SaveChangesAsync();
                await _auditoria.RegistrarAsync(ModuloSistema.TiposClientes, AccionPermiso.Editar, $"Tipo de cliente actualizado: {tipo.Nombre} ({tipo.Codigo})", tipo.Id);
            });
        }
        catch (UniqueConstraintViolationException ex) when (ex.ConstraintName == "TipoClientePredeterminadoUnico")
        {
            throw new BusinessRuleException(ex.Message);
        }

        return ToDto(tipo);
    }

    public async Task<TipoClienteDto?> CambiarEstadoAsync(int id, bool activo)
    {
        var tipo = await _repository.GetByIdAsync(id);
        if (tipo is null) return null;
        if (tipo.Activo == activo) return ToDto(tipo);

        if (tipo.Codigo == "SIN_CLASIFICAR" && !activo)
            throw new BusinessRuleException("El tipo de cliente 'Sin clasificar' no puede ser desactivado.");

        if (tipo.EsPredeterminado && !activo)
            throw new BusinessRuleException("No se puede desactivar el tipo de cliente que está configurado como predeterminado.");

        tipo.Activo = activo;
        tipo.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        tipo.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        tipo.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(tipo);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.TiposClientes,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Tipo de cliente {(activo ? "activado" : "desactivado")}: {tipo.Nombre} ({tipo.Codigo})",
            tipo.Id);

        return ToDto(tipo);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var tipo = await _repository.GetByIdAsync(id);
        if (tipo is null) return false;

        if (tipo.EsSistema)
            throw new BusinessRuleException("Los tipos de cliente del sistema no pueden ser eliminados.");

        if (tipo.EsPredeterminado)
            throw new BusinessRuleException("No se puede eliminar el tipo de cliente configurado como predeterminado.");

        if (await _repository.TieneClientesAsignadosAsync(id))
            throw new BusinessRuleException("No se puede eliminar el tipo de cliente porque tiene clientes asignados.");

        tipo.Eliminado = true;
        tipo.Activo = false;
        tipo.FechaEliminacion = DateTime.UtcNow;
        tipo.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        tipo.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        tipo.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        tipo.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(tipo);
        var guardado = await _repository.SaveChangesAsync();
        if (guardado)
        {
            await _auditoria.RegistrarAsync(ModuloSistema.TiposClientes, AccionPermiso.EliminarLogico, $"Tipo de cliente eliminado lógicamente: {tipo.Nombre} ({tipo.Codigo})", id);
        }
        return guardado;
    }

    private async Task DesmarcarPredeterminadosExistentesAsync()
    {
        var activos = await _repository.GetAllAsync();
        foreach (var t in activos.Where(x => x.EsPredeterminado))
        {
            t.EsPredeterminado = false;
            t.FechaActualizacion = DateTime.UtcNow;
            _repository.Update(t);
        }
    }

    private static TipoClienteDto ToDto(TipoCliente tc) => new()
    {
        Id = tc.Id,
        Codigo = tc.Codigo,
        EsSistema = tc.EsSistema,
        Nombre = tc.Nombre,
        NombreNormalizado = tc.NombreNormalizado,
        Descripcion = tc.Descripcion,
        ColorHex = tc.ColorHex,
        Activo = tc.Activo,
        Orden = tc.Orden,
        EsPredeterminado = tc.EsPredeterminado,
        TotalClientesAsignados = tc.Clientes?.Count ?? 0
    };
}
