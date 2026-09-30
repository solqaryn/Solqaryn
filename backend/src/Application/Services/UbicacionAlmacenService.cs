using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class UbicacionAlmacenService : IUbicacionAlmacenService
{
    private const int TamanoPaginaMaximo = 100;

    private readonly IUbicacionAlmacenRepository _repository;
    private readonly IAlmacenRepository _almacenRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public UbicacionAlmacenService(
        IUbicacionAlmacenRepository repository,
        IAlmacenRepository almacenRepository,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _almacenRepository = almacenRepository;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<UbicacionAlmacenPaginaDto> BuscarAsync(UbicacionAlmacenFiltroDto filtro)
    {
        ValidarIdOpcional(filtro.AlmacenId, "AlmacenId");
        ValidarIdOpcional(filtro.UbicacionPadreId, "UbicacionPadreId");
        if (filtro.SoloRaiz && filtro.UbicacionPadreId.HasValue)
            throw new BusinessRuleException("SoloRaiz y UbicacionPadreId no pueden combinarse.");

        var tipo = ParseTipoOpcional(filtro.Tipo);
        var pagina = Math.Max(1, filtro.Pagina);
        var tamanoPagina = Math.Clamp(filtro.TamanoPagina, 1, TamanoPaginaMaximo);

        var (items, total) = await _repository.BuscarAsync(
            Limpiar(filtro.Buscar),
            filtro.AlmacenId,
            filtro.UbicacionPadreId,
            filtro.SoloRaiz,
            tipo,
            filtro.Activa,
            pagina,
            tamanoPagina);

        return new UbicacionAlmacenPaginaDto
        {
            Items = items.Select(ToDto).ToList(),
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            Total = total,
            TotalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanoPagina)
        };
    }

    public async Task<List<UbicacionAlmacenDto>> GetActivasAsync(int? almacenId = null, int? ubicacionPadreId = null)
    {
        ValidarIdOpcional(almacenId, "AlmacenId");
        ValidarIdOpcional(ubicacionPadreId, "UbicacionPadreId");
        var ubicaciones = await _repository.GetActivasAsync(almacenId, ubicacionPadreId);
        return ubicaciones.Select(ToDto).ToList();
    }

    public IReadOnlyList<TipoUbicacionAlmacenDto> GetTipos() =>
        Enum.GetValues<TipoUbicacionAlmacen>()
            .OrderBy(t => (int)t)
            .Select(t => new TipoUbicacionAlmacenDto
            {
                Codigo = t.ToString(),
                Nombre = NombreTipo(t)
            })
            .ToList();

    public async Task<UbicacionAlmacenDto?> GetByIdAsync(int id)
    {
        var ubicacion = await _repository.GetByIdAsync(id);
        return ubicacion is null ? null : ToDto(ubicacion);
    }

    public async Task<UbicacionAlmacenDto> CreateAsync(CreateUbicacionAlmacenDto dto)
    {
        var almacen = await ObtenerAlmacenOperativoAsync(dto.AlmacenId);
        var padre = await ObtenerPadreActivoMismoAlmacenAsync(dto.UbicacionPadreId, almacen.Id, null);
        var codigo = NormalizarCodigo(dto.Codigo);
        var nombre = NormalizarRequerido(dto.Nombre, "El nombre de la ubicación es obligatorio.");
        var tipo = ParseTipo(dto.Tipo);

        if (await _repository.ExisteCodigoAsync(almacen.Id, codigo))
            throw new BusinessRuleException($"Ya existe una ubicación activa con el código '{codigo}' en este almacén.");

        var ubicacion = new UbicacionAlmacen
        {
            AlmacenId = almacen.Id,
            Almacen = almacen,
            UbicacionPadreId = padre?.Id,
            UbicacionPadre = padre,
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Activa = true,
            Eliminado = false,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(ubicacion);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.UbicacionesAlmacen,
            AccionPermiso.Crear,
            $"Ubicación creada: {ubicacion.Codigo} - {ubicacion.Nombre}",
            ubicacion.Id,
            entidad: "UbicacionAlmacen");

        return ToDto(ubicacion);
    }

    public async Task<UbicacionAlmacenDto?> UpdateAsync(int id, UpdateUbicacionAlmacenDto dto)
    {
        var ubicacion = await _repository.GetByIdAsync(id);
        if (ubicacion is null) return null;

        ValidarId(dto.AlmacenId, "AlmacenId");
        ValidarIdOpcional(dto.UbicacionPadreId, "UbicacionPadreId");

        var codigo = NormalizarCodigo(dto.Codigo);
        var nombre = NormalizarRequerido(dto.Nombre, "El nombre de la ubicación es obligatorio.");
        var tipo = ParseTipo(dto.Tipo);
        var cambiaAlmacen = ubicacion.AlmacenId != dto.AlmacenId;
        var cambiaPadre = ubicacion.UbicacionPadreId != dto.UbicacionPadreId;
        var cambiaEstructura = cambiaAlmacen || cambiaPadre;

        Almacen almacenDestino;
        if (cambiaEstructura)
        {
            almacenDestino = await ObtenerAlmacenOperativoAsync(dto.AlmacenId);
            if (cambiaAlmacen && await _repository.TieneHijasNoEliminadasAsync(id))
                throw new BusinessRuleException("No se puede mover una ubicación a otro almacén mientras tenga ubicaciones hijas.");
        }
        else
        {
            almacenDestino = ubicacion.Almacen;
        }

        UbicacionAlmacen? padreDestino = ubicacion.UbicacionPadre;
        if (cambiaEstructura)
        {
            padreDestino = await ObtenerPadreActivoMismoAlmacenAsync(dto.UbicacionPadreId, almacenDestino.Id, id);
            if (await _repository.CreariaCicloAsync(id, almacenDestino.Id, dto.UbicacionPadreId))
                throw new BusinessRuleException("La ubicación padre seleccionada produciría un ciclo jerárquico.");
        }

        if (await _repository.ExisteCodigoAsync(almacenDestino.Id, codigo, id))
            throw new BusinessRuleException($"Ya existe otra ubicación activa con el código '{codigo}' en este almacén.");

        ubicacion.AlmacenId = almacenDestino.Id;
        ubicacion.Almacen = almacenDestino;
        ubicacion.UbicacionPadreId = padreDestino?.Id;
        ubicacion.UbicacionPadre = padreDestino;
        ubicacion.Codigo = codigo;
        ubicacion.Nombre = nombre;
        ubicacion.Tipo = tipo;
        // El estado se modifica exclusivamente mediante Activar/Desactivar.
        ubicacion.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        ubicacion.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        ubicacion.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(ubicacion);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.UbicacionesAlmacen,
            AccionPermiso.Editar,
            $"Ubicación actualizada: {ubicacion.Codigo} - {ubicacion.Nombre}",
            ubicacion.Id,
            entidad: "UbicacionAlmacen");

        return ToDto(ubicacion);
    }

    public async Task<UbicacionAlmacenDto?> CambiarEstadoAsync(int id, bool activa)
    {
        var ubicacion = await _repository.GetByIdAsync(id);
        if (ubicacion is null) return null;

        // PATCH idempotente: repetir estado no escribe ni duplica auditoría.
        if (ubicacion.Activa == activa)
            return ToDto(ubicacion);

        if (activa)
        {
            await ObtenerAlmacenOperativoAsync(ubicacion.AlmacenId);
            await ObtenerPadreActivoMismoAlmacenAsync(ubicacion.UbicacionPadreId, ubicacion.AlmacenId, ubicacion.Id);
        }
        else if (await _repository.TieneHijasActivasAsync(id))
        {
            throw new BusinessRuleException("No se puede desactivar una ubicación que tiene ubicaciones hijas activas.");
        }

        ubicacion.Activa = activa;
        ubicacion.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        ubicacion.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        ubicacion.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(ubicacion);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.UbicacionesAlmacen,
            activa ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Ubicación {(activa ? "activada" : "desactivada")}: {ubicacion.Codigo} - {ubicacion.Nombre}",
            ubicacion.Id,
            entidad: "UbicacionAlmacen");

        return ToDto(ubicacion);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var ubicacion = await _repository.GetByIdAsync(id);
        if (ubicacion is null) return false;

        if (await _repository.TieneHijasNoEliminadasAsync(id))
            throw new BusinessRuleException("No se puede eliminar una ubicación que tiene ubicaciones hijas no eliminadas.");

        ubicacion.Activa = false;
        ubicacion.Eliminado = true;
        ubicacion.FechaEliminacion = DateTime.UtcNow;
        ubicacion.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        ubicacion.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        ubicacion.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        ubicacion.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(ubicacion);
        var eliminado = await _repository.SaveChangesAsync();
        if (eliminado)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.UbicacionesAlmacen,
                AccionPermiso.EliminarLogico,
                $"Ubicación eliminada lógicamente: {ubicacion.Codigo} - {ubicacion.Nombre}",
                ubicacion.Id,
                entidad: "UbicacionAlmacen");
        }

        return eliminado;
    }

    private async Task<Almacen> ObtenerAlmacenOperativoAsync(int almacenId)
    {
        ValidarId(almacenId, "AlmacenId");
        var almacen = await _almacenRepository.GetByIdAsync(almacenId);
        if (almacen is null)
            throw new BusinessRuleException("El almacén indicado no existe.");
        if (!almacen.Activo || almacen.Sucursal is null || !almacen.Sucursal.Activa)
            throw new BusinessRuleException("El almacén indicado o su sucursal están inactivos y no admiten cambios estructurales de ubicaciones.");
        return almacen;
    }

    private async Task<UbicacionAlmacen?> ObtenerPadreActivoMismoAlmacenAsync(
        int? ubicacionPadreId,
        int almacenId,
        int? ubicacionActualId)
    {
        if (!ubicacionPadreId.HasValue)
            return null;

        ValidarId(ubicacionPadreId.Value, "UbicacionPadreId");
        if (ubicacionActualId.HasValue && ubicacionPadreId.Value == ubicacionActualId.Value)
            throw new BusinessRuleException("Una ubicación no puede ser su propio padre.");

        var padre = await _repository.GetByIdAsync(ubicacionPadreId.Value);
        if (padre is null)
            throw new BusinessRuleException("La ubicación padre indicada no existe.");
        if (padre.AlmacenId != almacenId)
            throw new BusinessRuleException("La ubicación padre debe pertenecer al mismo almacén.");
        if (!padre.Activa)
            throw new BusinessRuleException("La ubicación padre indicada está inactiva.");
        return padre;
    }

    private static void ValidarId(int id, string nombre)
    {
        if (id <= 0)
            throw new BusinessRuleException($"{nombre} debe ser mayor que cero.");
    }

    private static void ValidarIdOpcional(int? id, string nombre)
    {
        if (id.HasValue)
            ValidarId(id.Value, nombre);
    }

    private static TipoUbicacionAlmacen? ParseTipoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : ParseTipo(valor);

    private static TipoUbicacionAlmacen ParseTipo(string? valor)
    {
        var limpio = NormalizarRequerido(valor, "El tipo de ubicación es obligatorio.");
        if (!Enum.TryParse<TipoUbicacionAlmacen>(limpio, ignoreCase: true, out var tipo) || !Enum.IsDefined(tipo))
            throw new BusinessRuleException($"El tipo de ubicación '{limpio}' no es válido.");
        return tipo;
    }

    private static string NormalizarCodigo(string? valor) =>
        NormalizarRequerido(valor, "El código de la ubicación es obligatorio.").ToUpperInvariant();

    private static string NormalizarRequerido(string? valor, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new BusinessRuleException(mensaje);
        return valor.Trim();
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string NombreTipo(TipoUbicacionAlmacen tipo) => tipo switch
    {
        TipoUbicacionAlmacen.Pasillo => "Pasillo",
        TipoUbicacionAlmacen.Estante => "Estante",
        TipoUbicacionAlmacen.Rack => "Rack",
        TipoUbicacionAlmacen.Seccion => "Sección",
        TipoUbicacionAlmacen.Bin => "Bin",
        TipoUbicacionAlmacen.Otra => "Otra",
        _ => tipo.ToString()
    };

    private static UbicacionAlmacenDto ToDto(UbicacionAlmacen u) => new()
    {
        Id = u.Id,
        AlmacenId = u.AlmacenId,
        AlmacenCodigo = u.Almacen?.Codigo ?? string.Empty,
        AlmacenNombre = u.Almacen?.Nombre ?? string.Empty,
        UbicacionPadreId = u.UbicacionPadreId,
        UbicacionPadreCodigo = u.UbicacionPadre?.Codigo,
        UbicacionPadreNombre = u.UbicacionPadre?.Nombre,
        Codigo = u.Codigo,
        Nombre = u.Nombre,
        Tipo = u.Tipo.ToString(),
        Activa = u.Activa,
        CreadoPorNombreUsuario = u.CreadoPorNombreUsuario,
        ActualizadoPorNombreUsuario = u.ActualizadoPorNombreUsuario,
        FechaCreacion = u.FechaCreacion,
        FechaActualizacion = u.FechaActualizacion
    };
}
