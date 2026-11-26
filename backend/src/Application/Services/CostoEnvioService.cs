using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>Single administrative authority for persisted shipping tariffs.</summary>
public sealed class CostoEnvioService : ICostoEnvioService
{
    private readonly ICostoEnvioRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;
    public CostoEnvioService(ICostoEnvioRepository repository, ICurrentUserService currentUser, IAuditoriaService auditoria)
    {
        _repository = repository;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    public async Task<List<CostoEnvioDto>> GetAllAsync() => (await _repository.GetAllAsync()).Select(ToDto).ToList();
    public Task<bool> ExisteIncluidoEliminadoAsync(int id) => _repository.ExisteIncluidoEliminadoAsync(id);
    public async Task<CostoEnvioDto?> GetByIdAsync(int id) => (await _repository.GetByIdAsync(id)) is { } x ? ToDto(x) : null;
    public async Task<CostoEnvioDto?> GetPredeterminadoVigenteAsync() =>
        (await _repository.GetPredeterminadoVigenteAsync(DateTime.UtcNow)) is { } x ? ToDto(x) : null;

    public async Task<CostoEnvioDto?> ResolverAsync(ResolverCostoEnvioDto dto)
    {
        var fecha = dto.FechaUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        var candidatos = (await _repository.GetAllAsync()).Where(x => x.EstaVigente(fecha));
        var item = candidatos.Where(x => Coincide(x.Departamento, dto.Departamento) && Coincide(x.Ciudad, dto.Ciudad)
                    && Coincide(x.Zona, dto.Zona) && Coincide(x.Modalidad, dto.Modalidad))
            .OrderByDescending(Especificidad).ThenBy(x => x.Prioridad).ThenBy(x => x.Id).FirstOrDefault()
            ?? candidatos.Where(x => x.EsPredeterminado && Especificidad(x) == 0)
                .OrderBy(x => x.Prioridad).ThenBy(x => x.Id).FirstOrDefault();
        return item is null ? null : ToDto(item);
    }

    public Task<CostoEnvioDto> CreateAsync(GuardarCostoEnvioDto dto) => _repository.EnTransaccionAsync(async () =>
    {
        Validar(dto);
        if (await _repository.ExisteNombreAsync(dto.Nombre.Trim().ToUpperInvariant()))
            throw new ConflictException("Ya existe un costo de envío con ese nombre.");
        if (dto.EsPredeterminado) await DesmarcarAsync();
        var item = new CostoEnvio { CreadoPorUsuarioId = _currentUser.UsuarioId, CreadoPorNombreUsuario = _currentUser.NombreUsuario };
        Aplicar(item, dto);
        await _repository.AddAsync(item);
        await _repository.SaveChangesAsync();
        var salida = ToDto(item);
        await Auditar(AccionPermiso.Crear, item, null, salida);
        return salida;
    });

    public Task<CostoEnvioDto?> UpdateAsync(int id, GuardarCostoEnvioDto dto) => _repository.EnTransaccionAsync<CostoEnvioDto?>(async () =>
    {
        Validar(dto);
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return null;
        ProtegerPredeterminado(item, dto.EsPredeterminado && dto.Activo);
        if (await _repository.ExisteNombreAsync(dto.Nombre.Trim().ToUpperInvariant(), id))
            throw new ConflictException("Ya existe un costo de envío con ese nombre.");
        var anterior = ToDto(item);
        if (dto.EsPredeterminado) await DesmarcarAsync(id);
        Aplicar(item, dto);
        MarcarActualizacion(item);
        await _repository.SaveChangesAsync();
        var salida = ToDto(item);
        await Auditar(AccionPermiso.Editar, item, anterior, salida);
        return salida;
    });

    public Task<bool> CambiarEstadoAsync(int id, bool activo) => _repository.EnTransaccionAsync(async () =>
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return false;
        ProtegerPredeterminado(item, activo);
        if (item.Activo == activo) return true;
        var anterior = ToDto(item);
        item.Activo = activo;
        MarcarActualizacion(item);
        await _repository.SaveChangesAsync();
        await Auditar(activo ? AccionPermiso.Activar : AccionPermiso.Desactivar, item, anterior, ToDto(item));
        return true;
    });

    public Task<bool> EliminarAsync(int id) => _repository.EnTransaccionAsync(async () =>
    {
        var item = await _repository.GetByIdAsync(id);
        if (item is null) return false;
        ProtegerPredeterminado(item, false);
        var anterior = ToDto(item);
        item.Eliminado = true;
        item.Activo = false;
        item.EsPredeterminado = false;
        item.FechaEliminacion = DateTime.UtcNow;
        item.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        MarcarActualizacion(item);
        await _repository.SaveChangesAsync();
        await Auditar(AccionPermiso.EliminarLogico, item, anterior, ToDto(item));
        return true;
    });

    private async Task DesmarcarAsync(int? excluirId = null)
    {
        foreach (var item in (await _repository.GetAllAsync()).Where(x => x.EsPredeterminado && x.Id != excluirId))
        {
            var anterior = ToDto(item);
            item.EsPredeterminado = false;
            MarcarActualizacion(item);
            await _repository.SaveChangesAsync();
            await Auditar(AccionPermiso.Editar, item, anterior, ToDto(item));
        }
    }
    private Task Auditar(AccionPermiso accion, CostoEnvio item, CostoEnvioDto? anterior, CostoEnvioDto salida) =>
        _auditoria.RegistrarEstrictoAsync(ModuloSistema.Facturacion, accion, $"Costo de envío {accion}: {item.Nombre}.",
            item.Id, entidad: "CostoEnvio", valoresAnteriores: anterior, valoresNuevos: salida);

    private void MarcarActualizacion(CostoEnvio item)
    {
        item.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        item.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        item.FechaActualizacion = DateTime.UtcNow;
    }
    private static void ProtegerPredeterminado(CostoEnvio item, bool conservar)
    {
        if (item.Activo && item.EsPredeterminado && !conservar)
            throw new ConflictException("Asigne primero otro costo de envío predeterminado activo antes de retirar el actual.");
    }
    private static void Validar(GuardarCostoEnvioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || dto.Nombre.Trim().Length > 120)
            throw new BusinessRuleException("El nombre es obligatorio y admite hasta 120 caracteres.");
        if (dto.Monto < 0 || dto.Monto > 9999999999999999.99m || decimal.Round(dto.Monto, 2) != dto.Monto || dto.Prioridad < 0)
            throw new BusinessRuleException("Indique un monto no negativo con hasta dos decimales y prioridad válida.");
        if (dto.EsPredeterminado && !dto.Activo)
            throw new BusinessRuleException("Un costo de envío predeterminado debe estar activo.");
        if (dto.EsPredeterminado && new[] { dto.Departamento, dto.Ciudad, dto.Zona, dto.Modalidad }.Any(s => !string.IsNullOrWhiteSpace(s)))
            throw new BusinessRuleException("La tarifa predeterminada debe tener cobertura general; configure las excepciones como tarifas específicas.");
        if (dto.VigenteDesde.HasValue && dto.VigenteHasta.HasValue && dto.VigenteHasta < dto.VigenteDesde)
            throw new BusinessRuleException("La fecha final no puede ser anterior a la inicial.");
        if (dto.Descripcion?.Trim().Length > 500 || dto.Departamento?.Trim().Length > 120 || dto.Ciudad?.Trim().Length > 120 ||
            dto.Zona?.Trim().Length > 150 || dto.Modalidad?.Trim().Length > 80)
            throw new BusinessRuleException("Uno de los campos supera la longitud permitida.");
    }
    private static string? Normalizar(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static bool Coincide(string? regla, string? entrada) => string.IsNullOrWhiteSpace(regla) ||
        string.Equals(regla.Trim(), entrada?.Trim(), StringComparison.OrdinalIgnoreCase);
    private static int Especificidad(CostoEnvio x) =>
        new[] { x.Departamento, x.Ciudad, x.Zona, x.Modalidad }.Count(s => !string.IsNullOrWhiteSpace(s));
    private static void Aplicar(CostoEnvio x, GuardarCostoEnvioDto dto)
    {
        x.Nombre = dto.Nombre.Trim();
        x.Descripcion = Normalizar(dto.Descripcion);
        x.Departamento = Normalizar(dto.Departamento);
        x.Ciudad = Normalizar(dto.Ciudad);
        x.Zona = Normalizar(dto.Zona);
        x.Modalidad = Normalizar(dto.Modalidad);
        x.Monto = dto.Monto;
        x.VigenteDesde = dto.VigenteDesde?.ToUniversalTime();
        x.VigenteHasta = dto.VigenteHasta?.ToUniversalTime();
        x.Prioridad = dto.Prioridad;
        x.EsPredeterminado = dto.EsPredeterminado;
        x.Activo = dto.Activo;
    }
    private static CostoEnvioDto ToDto(CostoEnvio x) => new()
    {
        Id = x.Id, Nombre = x.Nombre, Descripcion = x.Descripcion, Departamento = x.Departamento,
        Ciudad = x.Ciudad, Zona = x.Zona, Modalidad = x.Modalidad, Monto = x.Monto,
        VigenteDesde = x.VigenteDesde, VigenteHasta = x.VigenteHasta, Prioridad = x.Prioridad,
        EsPredeterminado = x.EsPredeterminado, Activo = x.Activo, EstaVigente = x.EstaVigente(DateTime.UtcNow),
        FechaCreacion = x.FechaCreacion, FechaActualizacion = x.FechaActualizacion
    };
}
