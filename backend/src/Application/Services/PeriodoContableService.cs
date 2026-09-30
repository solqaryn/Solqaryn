using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs.Contabilidad;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

public sealed class PeriodoContableService : IPeriodoContableService
{
    private readonly IPeriodoContableRepository _repository;
    private readonly IAuditoriaService _auditoria;

    public PeriodoContableService(IPeriodoContableRepository repository, IAuditoriaService auditoria)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<PagedResult<PeriodoContableDto>> GetPagedAsync(PeriodoContableQueryDto filter)
    {
        var result = await _repository.GetPagedAsync(filter);
        return new PagedResult<PeriodoContableDto>
        {
            Items = result.Items.Select(Map).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }

    public async Task<PeriodoContableDto?> GetByIdAsync(int id)
    {
        var periodo = await _repository.GetByIdAsync(id);
        return periodo is null ? null : Map(periodo);
    }

    public async Task<PeriodoContableDto> CreateAsync(CrearPeriodoContableDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var inicio = NormalizeUtc(dto.FechaInicio, nameof(dto.FechaInicio));
        var fin = NormalizeUtc(dto.FechaFin, nameof(dto.FechaFin));

        if (await _repository.HasOverlappingPeriodAsync(inicio, fin))
            throw new ConflictException("El período contable se superpone con un período existente.");

        var periodo = new PeriodoContable(inicio, fin);
        await _repository.AddAsync(periodo);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Configuracion, AccionPermiso.Crear,
            "Crear período contable", periodo.Id, "PeriodoContable",
            valoresNuevos: new { periodo.FechaInicio, periodo.FechaFin, periodo.Estado });
        return Map(periodo);
    }

    public async Task CerrarAsync(int id)
    {
        var periodo = await _repository.GetByIdAsync(id, tracking: true)
            ?? throw new ResourceNotFoundException($"No se encontró el período contable con ID {id}.");
        var anterior = new { periodo.Estado, periodo.CerradoEnUtc };

        try
        {
            periodo.Cerrar(DateTime.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            throw new ConflictException(ex.Message);
        }

        _repository.Update(periodo);
        await _repository.SaveChangesAsync();
        await _auditoria.RegistrarAsync(ModuloSistema.Configuracion, AccionPermiso.Cerrar,
            "Cerrar período contable", periodo.Id, "PeriodoContable",
            valoresAnteriores: anterior,
            valoresNuevos: new { periodo.Estado, periodo.CerradoEnUtc });
    }

    public async Task ValidarOperacionAsync(DateTime fechaOperacion, bool autorizadoCambioRetroactivo = false)
    {
        var fecha = NormalizeUtc(fechaOperacion, nameof(fechaOperacion));
        var periodo = await _repository.GetByDateAsync(fecha)
            ?? throw new InvalidOperationException("No existe un período contable configurado para la fecha de la operación.");
        periodo.ValidarCambio(fecha, autorizadoCambioRetroactivo);
    }

    private static DateTime NormalizeUtc(DateTime value, string paramName) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => throw new ArgumentException("La fecha debe incluir zona horaria explícita.", paramName)
    };

    private static PeriodoContableDto Map(PeriodoContable p) => new()
    {
        Id = p.Id,
        FechaInicio = p.FechaInicio,
        FechaFin = p.FechaFin,
        Estado = p.Estado,
        CerradoEnUtc = p.CerradoEnUtc
    };
}
