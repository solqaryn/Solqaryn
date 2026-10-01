using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.API.Controllers;

[ApiController]
[Authorize]
[Route("costos-envio")]
public class CostosEnvioController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditoriaService _auditoria;

    public CostosEnvioController(AppDbContext db, ICurrentUserService currentUser, IAuditoriaService auditoria)
    {
        _db = db;
        _currentUser = currentUser;
        _auditoria = auditoria;
    }

    [HttpGet]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetAll()
    {
        var items = await _db.CostosEnvio.AsNoTracking()
            .Where(x => !x.Eliminado)
            .OrderBy(x => x.Prioridad).ThenBy(x => x.Nombre)
            .Select(x => ToDto(x)).ToListAsync();
        return Ok(ApiResponse<List<CostoEnvioDto>>.Ok(items));
    }

    [HttpGet("predeterminado")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetPredeterminado()
    {
        var ahora = DateTime.UtcNow;
        var item = await _db.CostosEnvio.AsNoTracking()
            .Where(x => !x.Eliminado && x.Activo && x.EsPredeterminado)
            .Where(x => !x.VigenteDesde.HasValue || x.VigenteDesde <= ahora)
            .Where(x => !x.VigenteHasta.HasValue || x.VigenteHasta >= ahora)
            .OrderBy(x => x.Prioridad).FirstOrDefaultAsync();
        return item is null
            ? NotFound(ApiResponse<object>.Fail("No existe un costo de envío predeterminado vigente."))
            : Ok(ApiResponse<CostoEnvioDto>.Ok(ToDto(item)));
    }

    [HttpPost("resolver")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> Resolver([FromBody] ResolverCostoEnvioDto dto)
    {
        var fecha = dto.FechaUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        var departamento = NormalizarClave(dto.Departamento);
        var ciudad = NormalizarClave(dto.Ciudad);
        var zona = NormalizarClave(dto.Zona);
        var modalidad = NormalizarClave(dto.Modalidad);

        var candidatos = await _db.CostosEnvio.AsNoTracking()
            .Where(x => !x.Eliminado && x.Activo)
            .Where(x => !x.VigenteDesde.HasValue || x.VigenteDesde <= fecha)
            .Where(x => !x.VigenteHasta.HasValue || x.VigenteHasta >= fecha)
            .ToListAsync();

        var coincidencia = candidatos
            .Where(x => Coincide(x.Departamento, departamento)
                     && Coincide(x.Ciudad, ciudad)
                     && Coincide(x.Zona, zona)
                     && Coincide(x.Modalidad, modalidad))
            .OrderByDescending(Especificidad)
            .ThenBy(x => x.Prioridad)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        coincidencia ??= candidatos
            .Where(x => x.EsPredeterminado)
            .OrderBy(x => x.Prioridad)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        return coincidencia is null
            ? NotFound(ApiResponse<object>.Fail("No existe un costo de envío vigente aplicable ni un predeterminado vigente."))
            : Ok(ApiResponse<CostoEnvioDto>.Ok(ToDto(coincidencia)));
    }

    [HttpGet("{id:int}/historial")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> Historial(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!await _db.CostosEnvio.IgnoreQueryFilters().AnyAsync(x => x.Id == id))
            return NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));

        var resultado = await _auditoria.GetFilteredAsync(new AuditoriaFiltroDto
        {
            Entidad = "CostoEnvio",
            ReferenciaId = id,
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 100)
        });
        return Ok(ApiResponse<PagedResult<RegistroAuditoriaDto>>.Ok(resultado));
    }

    [HttpGet("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Ver)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _db.CostosEnvio.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.Eliminado);
        return item is null
            ? NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."))
            : Ok(ApiResponse<CostoEnvioDto>.Ok(ToDto(item)));
    }

    [HttpPost]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> Create([FromBody] GuardarCostoEnvioDto dto)
    {
        var error = Validar(dto);
        if (error is not null) return BadRequest(ApiResponse<object>.Fail(error));
        var nombre = dto.Nombre.Trim();
        if (await _db.CostosEnvio.AnyAsync(x => !x.Eliminado && x.Nombre.ToUpper() == nombre.ToUpper()))
            return Conflict(ApiResponse<object>.Fail("Ya existe un costo de envío con ese nombre."));

        await using var transaction = await _db.Database.BeginTransactionAsync();
        if (dto.EsPredeterminado) await DesmarcarPredeterminadosYPersistirAsync();

        var item = new CostoEnvio
        {
            Nombre = nombre,
            Descripcion = Normalizar(dto.Descripcion, 500),
            Departamento = Normalizar(dto.Departamento, 120),
            Ciudad = Normalizar(dto.Ciudad, 120),
            Zona = Normalizar(dto.Zona, 150),
            Modalidad = Normalizar(dto.Modalidad, 80),
            Monto = Math.Round(dto.Monto, 2, MidpointRounding.AwayFromZero),
            VigenteDesde = dto.VigenteDesde?.ToUniversalTime(),
            VigenteHasta = dto.VigenteHasta?.ToUniversalTime(),
            Prioridad = dto.Prioridad,
            EsPredeterminado = dto.EsPredeterminado,
            Activo = dto.Activo,
            CreadoPorUsuarioId = _currentUser.UsuarioId,
            CreadoPorNombreUsuario = _currentUser.NombreUsuario
        };
        _db.CostosEnvio.Add(item);
        await _db.SaveChangesAsync();
        var salida = ToDto(item);
        await _auditoria.RegistrarAsync(ModuloSistema.Facturacion, AccionPermiso.Crear,
            $"Costo de envío creado: {item.Nombre}.", item.Id, entidad: "CostoEnvio", valoresNuevos: salida);
        await transaction.CommitAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, ApiResponse<CostoEnvioDto>.Ok(salida));
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> Update(int id, [FromBody] GuardarCostoEnvioDto dto)
    {
        var error = Validar(dto);
        if (error is not null) return BadRequest(ApiResponse<object>.Fail(error));
        var item = await _db.CostosEnvio.FirstOrDefaultAsync(x => x.Id == id && !x.Eliminado);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));
        var anterior = ToDto(item);
        if (item.EsPredeterminado && item.Activo && (!dto.EsPredeterminado || !dto.Activo))
            return Conflict(ApiResponse<object>.Fail("Asigne primero otro costo de envío predeterminado activo antes de retirar el actual."));

        var nombre = dto.Nombre.Trim();
        if (await _db.CostosEnvio.AnyAsync(x => !x.Eliminado && x.Id != id && x.Nombre.ToUpper() == nombre.ToUpper()))
            return Conflict(ApiResponse<object>.Fail("Ya existe un costo de envío con ese nombre."));

        await using var transaction = await _db.Database.BeginTransactionAsync();
        if (dto.EsPredeterminado) await DesmarcarPredeterminadosYPersistirAsync(id);

        item.Nombre = nombre;
        item.Descripcion = Normalizar(dto.Descripcion, 500);
        item.Departamento = Normalizar(dto.Departamento, 120);
        item.Ciudad = Normalizar(dto.Ciudad, 120);
        item.Zona = Normalizar(dto.Zona, 150);
        item.Modalidad = Normalizar(dto.Modalidad, 80);
        item.Monto = Math.Round(dto.Monto, 2, MidpointRounding.AwayFromZero);
        item.VigenteDesde = dto.VigenteDesde?.ToUniversalTime();
        item.VigenteHasta = dto.VigenteHasta?.ToUniversalTime();
        item.Prioridad = dto.Prioridad;
        item.EsPredeterminado = dto.EsPredeterminado;
        item.Activo = dto.Activo;
        item.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
        item.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
        item.FechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        var salida = ToDto(item);
        await _auditoria.RegistrarAsync(ModuloSistema.Facturacion, AccionPermiso.Editar,
            $"Costo de envío actualizado: {item.Nombre}.", item.Id, entidad: "CostoEnvio", valoresAnteriores: anterior, valoresNuevos: salida);
        await transaction.CommitAsync();
        return Ok(ApiResponse<CostoEnvioDto>.Ok(salida));
    }

    [HttpPatch("{id:int}/estado")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoCostoEnvioDto dto)
    {
        var item = await _db.CostosEnvio.FirstOrDefaultAsync(x => x.Id == id && !x.Eliminado);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));
        var anterior = ToDto(item);
        if (!dto.Activo && item.EsPredeterminado && item.Activo)
            return Conflict(ApiResponse<object>.Fail("Asigne primero otro costo de envío predeterminado activo antes de desactivar el actual."));

        item.Activo = dto.Activo;
        item.FechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Facturacion,
            dto.Activo ? AccionPermiso.Activar : AccionPermiso.Desactivar,
            $"Costo de envío {(dto.Activo ? "activado" : "desactivado")}.", id, entidad: "CostoEnvio", valoresAnteriores: anterior, valoresNuevos: ToDto(item));
        return Ok(ApiResponse<object>.Ok(new { id, dto.Activo }));
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(ModuloSistema.Facturacion, AccionPermiso.Administrar)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.CostosEnvio.FirstOrDefaultAsync(x => x.Id == id && !x.Eliminado);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Costo de envío no encontrado."));
        var anterior = ToDto(item);
        if (item.EsPredeterminado && item.Activo)
            return Conflict(ApiResponse<object>.Fail("Asigne primero otro costo de envío predeterminado activo antes de eliminar el actual."));

        item.Eliminado = true;
        item.Activo = false;
        item.EsPredeterminado = false;
        item.FechaEliminacion = DateTime.UtcNow;
        item.EliminadoPorUsuarioId = _currentUser.UsuarioId;
        item.FechaActualizacion = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Facturacion, AccionPermiso.EliminarLogico,
            "Costo de envío eliminado lógicamente.", id, entidad: "CostoEnvio", valoresAnteriores: anterior, valoresNuevos: ToDto(item));
        return Ok(ApiResponse<object>.Ok(new { id }));
    }

    private async Task DesmarcarPredeterminadosYPersistirAsync(int? excluirId = null)
    {
        var items = await _db.CostosEnvio
            .Where(x => !x.Eliminado && x.EsPredeterminado && (!excluirId.HasValue || x.Id != excluirId.Value))
            .ToListAsync();
        if (items.Count == 0) return;

        var ahora = DateTime.UtcNow;
        foreach (var item in items)
        {
            item.EsPredeterminado = false;
            item.FechaActualizacion = ahora;
        }

        await _db.SaveChangesAsync();
    }

    private static string? Validar(GuardarCostoEnvioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return "El nombre es obligatorio.";
        if (dto.Nombre.Trim().Length > 120) return "El nombre no puede superar 120 caracteres.";
        if (dto.Monto < 0) return "El monto no puede ser negativo.";
        if (dto.Prioridad < 0) return "La prioridad no puede ser negativa.";
        if (dto.EsPredeterminado && !dto.Activo) return "Un costo de envío predeterminado debe estar activo.";
        if (dto.VigenteDesde.HasValue && dto.VigenteHasta.HasValue && dto.VigenteHasta < dto.VigenteDesde)
            return "La fecha final de vigencia no puede ser anterior a la fecha inicial.";
        return null;
    }

    private static string? Normalizar(string? valor, int maximo)
    {
        var limpio = string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        return limpio is not null && limpio.Length > maximo ? limpio[..maximo] : limpio;
    }

    private static string? NormalizarClave(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim().ToUpperInvariant();

    private static bool Coincide(string? regla, string? solicitado)
    {
        if (string.IsNullOrWhiteSpace(regla)) return true;
        if (string.IsNullOrWhiteSpace(solicitado)) return false;
        return string.Equals(regla.Trim(), solicitado, StringComparison.OrdinalIgnoreCase);
    }

    private static int Especificidad(CostoEnvio x) =>
        (string.IsNullOrWhiteSpace(x.Departamento) ? 0 : 1) +
        (string.IsNullOrWhiteSpace(x.Ciudad) ? 0 : 1) +
        (string.IsNullOrWhiteSpace(x.Zona) ? 0 : 1) +
        (string.IsNullOrWhiteSpace(x.Modalidad) ? 0 : 1);

    private static CostoEnvioDto ToDto(CostoEnvio x) => new()
    {
        Id = x.Id,
        Nombre = x.Nombre,
        Descripcion = x.Descripcion,
        Departamento = x.Departamento,
        Ciudad = x.Ciudad,
        Zona = x.Zona,
        Modalidad = x.Modalidad,
        Monto = x.Monto,
        VigenteDesde = x.VigenteDesde,
        VigenteHasta = x.VigenteHasta,
        Prioridad = x.Prioridad,
        EsPredeterminado = x.EsPredeterminado,
        Activo = x.Activo,
        EstaVigente = x.EstaVigente(DateTime.UtcNow),
        FechaCreacion = x.FechaCreacion,
        FechaActualizacion = x.FechaActualizacion
    };
}

public class CambiarEstadoCostoEnvioDto
{
    public bool Activo { get; set; }
}
