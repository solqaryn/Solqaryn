using System.Text.Json;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Solqaryn.Application.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly IAuditoriaRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditoriaService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public AuditoriaService(IAuditoriaRepository repository, ICurrentUserService currentUser, IHttpContextAccessor httpContextAccessor, ILogger<AuditoriaService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task RegistrarAsync(ModuloSistema modulo, AccionPermiso accion, string descripcion, int? referenciaId = null, string? entidad = null, object? valoresAnteriores = null, object? valoresNuevos = null, string? motivo = null, string resultado = "Exito", string? error = null)
    {
        try
        {
            await RegistrarCoreAsync(modulo, accion, descripcion, referenciaId, entidad, valoresAnteriores, valoresNuevos, motivo, resultado, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo registrar entrada de auditoría: {Descripcion}", descripcion);
        }
    }

    public Task RegistrarEstrictoAsync(ModuloSistema modulo, AccionPermiso accion, string descripcion, int? referenciaId = null, string? entidad = null, object? valoresAnteriores = null, object? valoresNuevos = null, string? motivo = null, string resultado = "Exito", string? error = null)
        => RegistrarCoreAsync(modulo, accion, descripcion, referenciaId, entidad, valoresAnteriores, valoresNuevos, motivo, resultado, error);

    private async Task RegistrarCoreAsync(ModuloSistema modulo, AccionPermiso accion, string descripcion, int? referenciaId, string? entidad, object? valoresAnteriores, object? valoresNuevos, string? motivo, string resultado, string? error)
    {
        if (accion == AccionPermiso.Eliminar && (modulo == ModuloSistema.Compras || modulo == ModuloSistema.Ventas))
            accion = AccionPermiso.EliminarLogico;

        var http = _httpContextAccessor.HttpContext;

        // CorrelationIdMiddleware valida/normaliza X-Correlation-ID y establece
        // TraceIdentifier antes de autenticación/autorización y de los servicios.
        // La auditoría consume únicamente ese valor saneado; nunca vuelve a
        // confiar en el header bruto controlado por el cliente.
        var correlationId = http?.TraceIdentifier;

        await _repository.AddAsync(new RegistroAuditoria
        {
            UsuarioId = _currentUser.UsuarioId,
            NombreUsuario = _currentUser.NombreUsuario ?? "Sistema",
            Modulo = modulo,
            Accion = accion,
            Entidad = entidad,
            ReferenciaId = referenciaId,
            Descripcion = descripcion,
            ValoresAnteriores = valoresAnteriores is null ? null : JsonSerializer.Serialize(valoresAnteriores, JsonOptions),
            ValoresNuevos = valoresNuevos is null ? null : JsonSerializer.Serialize(valoresNuevos, JsonOptions),
            Motivo = motivo,
            Ip = http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http?.Request.Headers["User-Agent"].FirstOrDefault(),
            CorrelationId = correlationId,
            Resultado = resultado,
            Error = error
        });
        await _repository.SaveChangesAsync();
    }

    public async Task<PagedResult<RegistroAuditoriaDto>> GetFilteredAsync(AuditoriaFiltroDto filtro)
    {
        var (items, total) = await _repository.GetFilteredAsync(filtro);
        return new PagedResult<RegistroAuditoriaDto>
        {
            Items = items.Select(r => new RegistroAuditoriaDto
            {
                Id = r.Id, Fecha = r.Fecha, UsuarioId = r.UsuarioId, NombreUsuario = r.NombreUsuario,
                Modulo = r.Modulo.ToString(), Accion = r.Accion.ToString(), Entidad = r.Entidad,
                ReferenciaId = r.ReferenciaId, Descripcion = r.Descripcion,
                ValoresAnteriores = r.ValoresAnteriores, ValoresNuevos = r.ValoresNuevos,
                Motivo = r.Motivo, Ip = r.Ip, UserAgent = r.UserAgent, CorrelationId = r.CorrelationId,
                Resultado = r.Resultado, Error = r.Error
            }).ToList(),
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            TotalCount = total
        };
    }
}
