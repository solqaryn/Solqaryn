using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class AlmacenService : IAlmacenService
{
    private const int TamanoPaginaMaximo = 100;

    private readonly IAlmacenRepository _repository;
    private readonly ISucursalRepository _sucursalRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public AlmacenService(
        IAlmacenRepository repository,
        ISucursalRepository sucursalRepository,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _sucursalRepository = sucursalRepository;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<AlmacenPaginaDto> BuscarAsync(AlmacenFiltroDto filtro)
    {
        ValidarSucursalIdOpcional(filtro.SucursalId);
        var tipo = ParseTipoOpcional(filtro.Tipo);
        var pagina = Math.Max(1, filtro.Pagina);
        var tamanoPagina = Math.Clamp(filtro.TamanoPagina, 1, TamanoPaginaMaximo);

        var (items, total) = await _repository.BuscarAsync(
            Limpiar(filtro.Buscar),
            filtro.Activo,
            filtro.SucursalId,
            tipo,
            pagina,
            tamanoPagina);

        return new AlmacenPaginaDto
        {
            Items = items.Select(ToDto).ToList(),
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            Total = total,
            TotalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanoPagina)
        };
    }

    public async Task<List<AlmacenDto>> GetActivosAsync(int? sucursalId = null)
    {
        ValidarSucursalIdOpcional(sucursalId);
        var almacenes = await _repository.GetActivosAsync(sucursalId);
        return almacenes.Select(ToDto).ToList();
    }

    public IReadOnlyList<TipoAlmacenDto> GetTipos() =>
        Enum.GetValues<TipoAlmacen>()
            .OrderBy(t => (int)t)
            .Select(t => new TipoAlmacenDto
            {
                Codigo = t.ToString(),
                Nombre = NombreTipo(t)
            })
            .ToList();

    public async Task<AlmacenDto?> GetByIdAsync(int id)
    {
        var almacen = await _repository.GetByIdAsync(id);
        return almacen is null ? null : ToDto(almacen);
    }

    public async Task<AlmacenDto> CreateAsync(CreateAlmacenDto dto)
    {
        var sucursal = await ObtenerSucursalActivaAsync(dto.SucursalId);
        var codigo = NormalizarCodigo(dto.Codigo);
        var nombre = NormalizarRequerido(dto.Nombre, "El nombre del almacén es obligatorio.");
        var tipo = ParseTipo(dto.Tipo);

        if (await _repository.ExisteCodigoAsync(codigo))
            throw new BusinessRuleException($"Ya existe un almacén activo con el código '{codigo}'.");

        var almacen = new Almacen
        {
            SucursalId = sucursal.Id,
            Sucursal = sucursal,
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Activo = true,
            Eliminado = false,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };

        await _repository.AddAsync(almacen);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.Almacenes,
            AccionPermiso.Crear,
            $"Almacén creado: {almacen.Codigo} - {almacen.Nombre}",
            almacen.Id,
            entidad: "Almacen");

        return ToDto(almacen);
    }

    public async Task<AlmacenDto?> UpdateAsync(int id, UpdateAlmacenDto dto)
    {
        var almacen = await _repository.GetByIdAsync(id);
        if (almacen is null) return null;

        ValidarSucursalId(dto.SucursalId);
        var codigo = NormalizarCodigo(dto.Codigo);
        var nombre = NormalizarRequerido(dto.Nombre, "El nombre del almacén es obligatorio.");
        var tipo = ParseTipo(dto.Tipo);

        if (await _repository.ExisteCodigoAsync(codigo, id))
            throw new BusinessRuleException($"Ya existe otro almacén activo con el código '{codigo}'.");

        if (almacen.SucursalId != dto.SucursalId)
        {
            var nuevaSucursal = await ObtenerSucursalActivaAsync(dto.SucursalId);
            almacen.SucursalId = nuevaSucursal.Id;
            almacen.Sucursal = nuevaSucursal;
        }

        almacen.Codigo = codigo;
        almacen.Nombre = nombre;
        almacen.Tipo = tipo;
        // El estado se modifica exclusivamente mediante Activar/Desactivar.
        almacen.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        almacen.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        almacen.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(almacen);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.Almacenes,
            AccionPermiso.Editar,
            $"Almacén actualizado: {almacen.Codigo} - {almacen.Nombre}",
            almacen.Id,
            entidad: "Almacen");

        return ToDto(almacen);
    }

    public async Task<AlmacenDto?> CambiarEstadoAsync(int id, bool activo)
    {
        var almacen = await _repository.GetByIdAsync(id);
        if (almacen is null) return null;

        // PATCH de estado idempotente: repetir el mismo estado no escribe ni audita.
        if (almacen.Activo == activo)
            return ToDto(almacen);

        if (activo)
        {
            if (almacen.Sucursal is null || !almacen.Sucursal.Activa)
                throw new BusinessRuleException("No se puede activar un almacén cuya sucursal está inactiva o no existe.");
        }

        almacen.Activo = activo;
        almacen.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        almacen.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        almacen.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(almacen);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(
            ModuloSistema.Almacenes,
            activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Almacén {(activo ? "activado" : "desactivado")}: {almacen.Codigo} - {almacen.Nombre}",
            almacen.Id,
            entidad: "Almacen");

        return ToDto(almacen);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var almacen = await _repository.GetByIdAsync(id);
        if (almacen is null) return false;

        almacen.Activo = false;
        almacen.Eliminado = true;
        almacen.FechaEliminacion = DateTime.UtcNow;
        almacen.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        almacen.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        almacen.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        almacen.FechaActualizacion = DateTime.UtcNow;

        _repository.Update(almacen);
        var eliminado = await _repository.SaveChangesAsync();
        if (eliminado)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Almacenes,
                AccionPermiso.EliminarLogico,
                $"Almacén eliminado lógicamente: {almacen.Codigo} - {almacen.Nombre}",
                almacen.Id,
                entidad: "Almacen");
        }

        return eliminado;
    }

    private async Task<Sucursal> ObtenerSucursalActivaAsync(int sucursalId)
    {
        ValidarSucursalId(sucursalId);
        var sucursal = await _sucursalRepository.GetByIdAsync(sucursalId);
        if (sucursal is null)
            throw new BusinessRuleException("La sucursal indicada no existe.");
        if (!sucursal.Activa)
            throw new BusinessRuleException("La sucursal indicada está inactiva y no puede recibir almacenes operativos.");
        return sucursal;
    }

    private static void ValidarSucursalId(int sucursalId)
    {
        if (sucursalId <= 0)
            throw new BusinessRuleException("SucursalId debe ser mayor que cero.");
    }

    private static void ValidarSucursalIdOpcional(int? sucursalId)
    {
        if (sucursalId.HasValue)
            ValidarSucursalId(sucursalId.Value);
    }

    private static TipoAlmacen? ParseTipoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : ParseTipo(valor);

    private static TipoAlmacen ParseTipo(string? valor)
    {
        var limpio = NormalizarRequerido(valor, "El tipo de almacén es obligatorio.");
        if (!Enum.TryParse<TipoAlmacen>(limpio, ignoreCase: true, out var tipo) || !Enum.IsDefined(tipo))
            throw new BusinessRuleException($"El tipo de almacén '{limpio}' no es válido.");
        return tipo;
    }

    private static string NormalizarCodigo(string? valor) =>
        NormalizarRequerido(valor, "El código del almacén es obligatorio.").ToUpperInvariant();

    private static string NormalizarRequerido(string? valor, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new BusinessRuleException(mensaje);
        return valor.Trim();
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static string NombreTipo(TipoAlmacen tipo) => tipo switch
    {
        TipoAlmacen.Tienda => "Tienda",
        TipoAlmacen.Bodega => "Bodega",
        TipoAlmacen.Transito => "Tránsito",
        TipoAlmacen.Devolucion => "Devolución",
        TipoAlmacen.Cuarentena => "Cuarentena",
        _ => tipo.ToString()
    };

    private static AlmacenDto ToDto(Almacen a) => new()
    {
        Id = a.Id,
        SucursalId = a.SucursalId,
        SucursalCodigo = a.Sucursal?.Codigo ?? string.Empty,
        SucursalNombre = a.Sucursal?.Nombre ?? string.Empty,
        Codigo = a.Codigo,
        Nombre = a.Nombre,
        Tipo = a.Tipo.ToString(),
        Activo = a.Activo,
        CreadoPorNombreUsuario = a.CreadoPorNombreUsuario,
        ActualizadoPorNombreUsuario = a.ActualizadoPorNombreUsuario,
        FechaCreacion = a.FechaCreacion,
        FechaActualizacion = a.FechaActualizacion
    };
}
