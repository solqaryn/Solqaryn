using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class SucursalService : ISucursalService
{
    private const int TamanoPaginaMaximo = 100;

    private readonly ISucursalRepository _repository;
    private readonly IEmpresaRepository _empresaRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public SucursalService(
        ISucursalRepository repository,
        IEmpresaRepository empresaRepository,
        ICurrentUserService currentUser,
        IAuditoriaService auditoria)
    {
        _repository = repository;
        _empresaRepository = empresaRepository;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<SucursalPaginaDto> BuscarAsync(SucursalFiltroDto filtro, int empresaIdAutorizada)
    {
        var empresaId = ResolverEmpresaIdConsulta(filtro.EmpresaId, empresaIdAutorizada);
        var pagina = Math.Max(1, filtro.Pagina);
        var tamanoPagina = Math.Clamp(filtro.TamanoPagina, 1, TamanoPaginaMaximo);
        var (items, total) = await _repository.BuscarAsync(
            Limpiar(filtro.Buscar),
            filtro.Activa,
            empresaId,
            pagina,
            tamanoPagina);
        return new SucursalPaginaDto
        {
            Items = items.Select(ToDto).ToList(),
            Pagina = pagina,
            TamanoPagina = tamanoPagina,
            Total = total,
            TotalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanoPagina)
        };
    }

    public async Task<List<SucursalDto>> GetActivasAsync(
        int empresaIdAutorizada,
        int? empresaIdSolicitada = null)
    {
        var empresaId = ResolverEmpresaIdConsulta(empresaIdSolicitada, empresaIdAutorizada);
        return (await _repository.GetActivasAsync(empresaId)).Select(ToDto).ToList();
    }

    public async Task<SucursalDto?> GetByIdAsync(int id, int empresaIdAutorizada)
    {
        var sucursal = await _repository.GetByIdAsync(id);
        return sucursal is null || !PerteneceAlTenantAutorizado(sucursal, empresaIdAutorizada)
            ? null
            : ToDto(sucursal);
    }

    public async Task<SucursalDto> CreateAsync(CreateSucursalDto dto, int empresaIdAutorizada)
    {
        var empresaId = ResolverEmpresaIdRequerida(dto.EmpresaId);
        ExigirCoincidenciaTenant(empresaId, empresaIdAutorizada);
        await ValidarEmpresaPropietariaActivaAsync(empresaId);
        var codigo = NormalizarCodigo(dto.Codigo);
        var nombre = NormalizarRequerido(dto.Nombre, "El nombre de la sucursal es obligatorio.");
        var zonaHoraria = ValidarZonaHoraria(dto.ZonaHoraria);

        if (await _repository.ExisteCodigoAsync(codigo, empresaId))
            throw new BusinessRuleException($"Ya existe una sucursal activa con el código '{codigo}' para la empresa indicada.");

        var sucursal = new Sucursal
        {
            EmpresaId = empresaId,
            Codigo = codigo,
            Nombre = nombre,
            Direccion = Limpiar(dto.Direccion),
            Telefono = Limpiar(dto.Telefono),
            Correo = Limpiar(dto.Correo),
            ZonaHoraria = zonaHoraria,
            Activa = true,
            Eliminado = false,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };
        await _repository.AddAsync(sucursal);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No se pudo crear la sucursal.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Sucursales,
            AccionPermiso.Crear,
            $"Sucursal creada: {sucursal.Codigo} - {sucursal.Nombre}; EmpresaId={empresaId}",
            sucursal.Id,
            entidad: "Sucursal",
            valoresNuevos: new { sucursal.EmpresaId, sucursal.Codigo, sucursal.Nombre });
        return ToDto(sucursal);
    }

    public async Task<SucursalDto?> UpdateAsync(
        int id,
        UpdateSucursalDto dto,
        int empresaIdAutorizada)
    {
        var sucursal = await _repository.GetByIdAsync(id);
        if (sucursal is null || !PerteneceAlTenantAutorizado(sucursal, empresaIdAutorizada))
            return null;

        var empresaId = ResolverEmpresaIdRequerida(dto.EmpresaId);
        ExigirCoincidenciaTenant(empresaId, empresaIdAutorizada);
        await ValidarEmpresaPropietariaActivaAsync(empresaId);
        var codigo = NormalizarCodigo(dto.Codigo);
        var nombre = NormalizarRequerido(dto.Nombre, "El nombre de la sucursal es obligatorio.");
        var zonaHoraria = ValidarZonaHoraria(dto.ZonaHoraria);
        if (await _repository.ExisteCodigoAsync(codigo, empresaId, id))
            throw new BusinessRuleException($"Ya existe otra sucursal activa con el código '{codigo}' para la empresa indicada.");

        var empresaAnterior = sucursal.EmpresaId;
        var codigoAnterior = sucursal.Codigo;
        var nombreAnterior = sucursal.Nombre;
        sucursal.EmpresaId = empresaId;
        sucursal.Codigo = codigo;
        sucursal.Nombre = nombre;
        sucursal.Direccion = Limpiar(dto.Direccion);
        sucursal.Telefono = Limpiar(dto.Telefono);
        sucursal.Correo = Limpiar(dto.Correo);
        sucursal.ZonaHoraria = zonaHoraria;
        sucursal.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        sucursal.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        sucursal.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(sucursal);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No se pudo actualizar la sucursal.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Sucursales,
            AccionPermiso.Editar,
            $"Sucursal actualizada: {sucursal.Codigo} - {sucursal.Nombre}; EmpresaId={empresaId}; tenant autorizado verificado",
            sucursal.Id,
            entidad: "Sucursal",
            valoresAnteriores: new { EmpresaId = empresaAnterior, Codigo = codigoAnterior, Nombre = nombreAnterior },
            valoresNuevos: new { sucursal.EmpresaId, sucursal.Codigo, sucursal.Nombre });
        return ToDto(sucursal);
    }

    public async Task<SucursalDto?> CambiarEstadoAsync(
        int id,
        bool activa,
        int empresaIdAutorizada)
    {
        var sucursal = await _repository.GetByIdAsync(id);
        if (sucursal is null || !PerteneceAlTenantAutorizado(sucursal, empresaIdAutorizada))
            return null;
        if (sucursal.Activa == activa)
            return ToDto(sucursal);

        sucursal.Activa = activa;
        sucursal.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        sucursal.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        sucursal.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(sucursal);
        if (!await _repository.SaveChangesAsync())
            throw new BusinessRuleException("No se pudo cambiar el estado de la sucursal.");

        await _auditoria.RegistrarAsync(
            ModuloSistema.Sucursales,
            activa ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Sucursal {(activa ? "activada" : "desactivada")}: {sucursal.Codigo} - {sucursal.Nombre}; EmpresaId={sucursal.EmpresaId}",
            sucursal.Id,
            entidad: "Sucursal");
        return ToDto(sucursal);
    }

    public async Task<bool> DeleteAsync(int id, int empresaIdAutorizada)
    {
        var sucursal = await _repository.GetByIdAsync(id);
        if (sucursal is null || !PerteneceAlTenantAutorizado(sucursal, empresaIdAutorizada))
            return false;

        sucursal.Activa = false;
        sucursal.Eliminado = true;
        sucursal.FechaEliminacion = DateTime.UtcNow;
        sucursal.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        sucursal.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        sucursal.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        sucursal.FechaActualizacion = DateTime.UtcNow;
        _repository.Update(sucursal);
        var eliminado = await _repository.SaveChangesAsync();
        if (eliminado)
        {
            await _auditoria.RegistrarAsync(
                ModuloSistema.Sucursales,
                AccionPermiso.EliminarLogico,
                $"Sucursal eliminada lógicamente: {sucursal.Codigo} - {sucursal.Nombre}; EmpresaId={sucursal.EmpresaId}",
                sucursal.Id,
                entidad: "Sucursal");
        }

        return eliminado;
    }

    private async Task ValidarEmpresaPropietariaActivaAsync(int empresaId)
    {
        var empresa = await _empresaRepository.GetByIdAsync(empresaId, CancellationToken.None);
        if (empresa is null)
            throw new BusinessRuleException($"La empresa propietaria {empresaId} no existe.");
        if (!empresa.Activa)
            throw new BusinessRuleException($"La empresa propietaria {empresaId} está inactiva.");
    }

    private static int ResolverEmpresaIdRequerida(int? empresaId)
    {
        if (!empresaId.HasValue || empresaId.Value <= 0)
            throw new BusinessRuleException(
                "EmpresaId es obligatorio y debe ser mayor que cero para establecer el tenant propietario de la sucursal.");
        return empresaId.Value;
    }

    private static int ResolverEmpresaIdConsulta(int? empresaIdSolicitada, int empresaIdAutorizada)
    {
        var autorizada = ExigirEmpresaAutorizada(empresaIdAutorizada);
        if (!empresaIdSolicitada.HasValue)
            return autorizada;
        if (empresaIdSolicitada.Value <= 0)
            throw new BusinessRuleException("EmpresaId debe ser mayor que cero cuando se especifica.");

        ExigirCoincidenciaTenant(empresaIdSolicitada.Value, autorizada);
        return autorizada;
    }

    private static bool PerteneceAlTenantAutorizado(Sucursal sucursal, int empresaIdAutorizada)
    {
        var autorizada = ExigirEmpresaAutorizada(empresaIdAutorizada);
        return sucursal.EmpresaId.HasValue && sucursal.EmpresaId.Value == autorizada;
    }

    private static void ExigirCoincidenciaTenant(int empresaIdSolicitada, int empresaIdAutorizada)
    {
        var autorizada = ExigirEmpresaAutorizada(empresaIdAutorizada);
        if (empresaIdSolicitada != autorizada)
        {
            throw new ForbiddenAccessException(
                "La empresa solicitada no coincide con el tenant autorizado para esta operación.");
        }
    }

    private static int ExigirEmpresaAutorizada(int empresaIdAutorizada)
    {
        if (empresaIdAutorizada <= 0)
        {
            throw new ForbiddenAccessException(
                "No existe un tenant autorizado válido para esta operación.");
        }

        return empresaIdAutorizada;
    }

    private static string NormalizarCodigo(string? valor) =>
        NormalizarRequerido(valor, "El código de la sucursal es obligatorio.").ToUpperInvariant();

    private static string NormalizarRequerido(string? valor, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new BusinessRuleException(mensaje);
        return valor.Trim();
    }

    private static string ValidarZonaHoraria(string? valor)
    {
        var zona = NormalizarRequerido(valor, "La zona horaria es obligatoria.");
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(zona);
            return zona;
        }
        catch (TimeZoneNotFoundException)
        {
            throw new BusinessRuleException($"La zona horaria '{zona}' no es válida.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new BusinessRuleException($"La zona horaria '{zona}' no es válida.");
        }
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static SucursalDto ToDto(Sucursal s) => new()
    {
        Id = s.Id,
        EmpresaId = s.EmpresaId,
        Codigo = s.Codigo,
        Nombre = s.Nombre,
        Direccion = s.Direccion,
        Telefono = s.Telefono,
        Correo = s.Correo,
        ZonaHoraria = s.ZonaHoraria,
        Activa = s.Activa,
        CreadoPorNombreUsuario = s.CreadoPorNombreUsuario,
        ActualizadoPorNombreUsuario = s.ActualizadoPorNombreUsuario,
        FechaCreacion = s.FechaCreacion,
        FechaActualizacion = s.FechaActualizacion
    };
}
