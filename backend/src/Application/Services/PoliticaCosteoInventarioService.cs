using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Application.Services;

/// <summary>
/// Administra la única política de costeo vigente del ámbito empresarial activo.
/// Cambiar el método cierra la versión actual y abre una nueva; nunca reescribe
/// políticas históricas ni recalcula costos ya materializados.
/// </summary>
public sealed class PoliticaCosteoInventarioService : IPoliticaCosteoInventarioService
{
    private readonly IPoliticaCosteoInventarioRepository _repository;
    private readonly IEmpresaConfiguracionRepository _empresas;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditoriaService _auditoria;

    public PoliticaCosteoInventarioService(
        IPoliticaCosteoInventarioRepository repository,
        IEmpresaConfiguracionRepository empresas,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        IAuditoriaService auditoria)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _empresas = empresas ?? throw new ArgumentNullException(nameof(empresas));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _auditoria = auditoria ?? throw new ArgumentNullException(nameof(auditoria));
    }

    public async Task<PoliticaCosteoInventarioDto> GetVigenteAsync()
    {
        var empresa = await ObtenerEmpresaActivaAsync();
        var politica = await _repository.GetVigenteAsync(empresa.Id)
            ?? throw new BusinessRuleException("No existe una política de costeo vigente para la empresa activa.");
        return Map(politica);
    }

    public async Task<PagedResult<PoliticaCosteoInventarioDto>> GetHistorialAsync(PoliticaCosteoInventarioQueryDto query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ValidarUtcOpcional(query.DesdeUtc, nameof(query.DesdeUtc));
        ValidarUtcOpcional(query.HastaUtc, nameof(query.HastaUtc));
        if (query.DesdeUtc.HasValue && query.HastaUtc.HasValue && query.DesdeUtc > query.HastaUtc)
            throw new BusinessRuleException("El rango de vigencia es inválido: DesdeUtc no puede ser posterior a HastaUtc.");
        if (query.Metodo.HasValue && !Enum.IsDefined(typeof(MetodoCosteoInventario), query.Metodo.Value))
            throw new BusinessRuleException("El método de costeo indicado no es válido.");

        var empresa = await ObtenerEmpresaActivaAsync();
        var (items, total) = await _repository.GetHistorialAsync(empresa.Id, query);
        return new PagedResult<PoliticaCosteoInventarioDto>
        {
            Items = items.Select(Map).ToList(),
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 100),
            TotalCount = total
        };
    }

    public Task<IReadOnlyList<MetodoCosteoInventarioDto>> GetMetodosAsync()
    {
        IReadOnlyList<MetodoCosteoInventarioDto> metodos = Enum
            .GetValues<MetodoCosteoInventario>()
            .Select(x => new MetodoCosteoInventarioDto { Id = x, Nombre = NombreMetodo(x) })
            .ToList();
        return Task.FromResult(metodos);
    }

    public async Task<PoliticaCosteoInventarioDto> CambiarAsync(CambiarPoliticaCosteoInventarioDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (!Enum.IsDefined(typeof(MetodoCosteoInventario), dto.Metodo))
            throw new BusinessRuleException("El método de costeo indicado no es válido.");

        var motivo = dto.Motivo?.Trim() ?? string.Empty;
        if (motivo.Length < 3 || motivo.Length > 500)
            throw new BusinessRuleException("El motivo debe contener entre 3 y 500 caracteres.");

        PoliticaCosteoInventario? resultado = null;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var empresa = await ObtenerEmpresaActivaAsync();
            var vigente = await _repository.GetVigenteAsync(empresa.Id, tracking: true);

            // Idempotencia semántica: solicitar de nuevo el método ya vigente no crea
            // una versión histórica artificial ni genera auditoría duplicada.
            if (vigente is not null && vigente.Metodo == dto.Metodo)
            {
                resultado = vigente;
                return;
            }

            var anterior = vigente is null
                ? null
                : new
                {
                    vigente.Id,
                    vigente.Metodo,
                    vigente.VigenteDesdeUtc,
                    vigente.VigenteHastaUtc,
                    vigente.EstaVigente
                };

            var ahora = DateTime.UtcNow;
            if (vigente is not null)
            {
                if (ahora <= vigente.VigenteDesdeUtc)
                    ahora = vigente.VigenteDesdeUtc.AddMilliseconds(1);
                vigente.Cerrar(ahora);
                vigente.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
                vigente.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            }

            var nueva = PoliticaCosteoInventario.Crear(empresa.Id, dto.Metodo, ahora, motivo);
            nueva.CreadoPorUsuarioId = _currentUser.UsuarioId;
            nueva.CreadoPorNombreUsuario = _currentUser.NombreUsuario;
            nueva.ActualizadoPorUsuarioId = _currentUser.UsuarioId;
            nueva.ActualizadoPorNombreUsuario = _currentUser.NombreUsuario;
            await _repository.AddAsync(nueva);
            await _repository.SaveChangesAsync();
            resultado = nueva;

            await _auditoria.RegistrarEstrictoAsync(
                ModuloSistema.MovimientosInventario,
                AccionPermiso.Editar,
                "Política de costeo de inventario actualizada.",
                referenciaId: nueva.Id,
                entidad: "PoliticaCosteoInventario",
                valoresAnteriores: anterior,
                valoresNuevos: new
                {
                    nueva.Id,
                    nueva.Metodo,
                    nueva.VigenteDesdeUtc,
                    nueva.EstaVigente
                },
                motivo: motivo);
        });

        return Map(resultado ?? throw new BusinessRuleException("No fue posible materializar la política de costeo."));
    }

    private async Task<EmpresaConfiguracion> ObtenerEmpresaActivaAsync() =>
        await _empresas.GetActivaAsync()
        ?? throw new BusinessRuleException("No existe una configuración empresarial activa.");

    private static void ValidarUtcOpcional(DateTime? valor, string campo)
    {
        if (valor.HasValue && valor.Value.Kind != DateTimeKind.Utc)
            throw new BusinessRuleException($"{campo} debe expresarse en UTC.");
    }

    private static PoliticaCosteoInventarioDto Map(PoliticaCosteoInventario politica) => new()
    {
        Id = politica.Id,
        EmpresaConfiguracionId = politica.EmpresaConfiguracionId,
        Metodo = politica.Metodo,
        MetodoNombre = NombreMetodo(politica.Metodo),
        VigenteDesdeUtc = politica.VigenteDesdeUtc,
        VigenteHastaUtc = politica.VigenteHastaUtc,
        EstaVigente = politica.EstaVigente,
        Motivo = politica.Motivo,
        FechaCreacion = politica.FechaCreacion,
        FechaActualizacion = politica.FechaActualizacion
    };

    private static string NombreMetodo(MetodoCosteoInventario metodo) => metodo switch
    {
        MetodoCosteoInventario.PromedioPonderado => "Promedio ponderado",
        MetodoCosteoInventario.FIFO => "FIFO",
        MetodoCosteoInventario.Estandar => "Estándar",
        _ => metodo.ToString()
    };
}
