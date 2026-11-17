using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Services;

/// <summary>
/// Caso de uso N7.1.D para registrar una intención outbox tenant-bound.
/// La persistencia definitiva pertenece al límite transaccional del caller;
/// este servicio valida autoridad server-side e idempotencia antes de stagear.
/// </summary>
public sealed class MensajeOutboxService : IMensajeOutboxService
{
    private readonly IMensajeOutboxRepository _repository;
    private readonly IUsuarioScopeService _usuarioScopeService;

    public MensajeOutboxService(
        IMensajeOutboxRepository repository,
        IUsuarioScopeService usuarioScopeService)
    {
        _repository = repository;
        _usuarioScopeService = usuarioScopeService;
    }

    public async Task<MensajeOutbox> RegistrarAsync(
        int empresaId,
        RegistrarMensajeOutboxRequest request,
        string claveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId), "La empresa/tenant es obligatoria.");

        if (string.IsNullOrWhiteSpace(claveIdempotencia))
            throw new ArgumentException("Idempotency-Key es obligatoria.", nameof(claveIdempotencia));

        var scope = await _usuarioScopeService.ObtenerActualAsync(empresaId, cancellationToken);
        // No otorgar autoridad a un contexto tenant malformado: identidad y rol
        // deben provenir de una membresía empresarial válida del servidor.
        // EsAdministrador NO es un bypass de la frontera del tenant.
        if (scope is null ||
            scope.EmpresaId != empresaId ||
            scope.UsuarioId <= 0 ||
            scope.RolId <= 0 ||
            string.IsNullOrWhiteSpace(scope.RolNombre))
        {
            throw new ForbiddenAccessException("No existe una membresía activa para la empresa solicitada.");
        }

        // La clave original se valida antes de Trim. Una secuencia CR/LF
        // terminal no puede transformarse silenciosamente en otra clave válida.
        foreach (var caracter in claveIdempotencia)
        {
            if (char.IsControl(caracter))
                throw new ArgumentException(
                    "Idempotency-Key contiene caracteres de control.",
                    nameof(claveIdempotencia));
        }

        var claveNormalizada = claveIdempotencia.Trim();
        var candidato = MensajeOutbox.Crear(
            empresaId,
            request.TipoEvento,
            request.PayloadJson,
            claveNormalizada,
            request.TipoAgregado,
            request.IdAgregado,
            request.CorrelationId);

        var existente = await _repository.GetByClaveIdempotenciaAsync(
            empresaId,
            claveNormalizada,
            cancellationToken);

        if (existente is not null)
        {
            if (EsMismaSolicitud(existente, candidato))
                return existente;

            throw new ConflictException(
                "La clave de idempotencia ya fue registrada con una solicitud diferente para esta empresa.");
        }

        // Fallback fail-closed para repositorios legacy/test que todavía no
        // implementen lookup por clave, además de mantener la precondición
        // histórica antes de stagear una nueva intención.
        if (await _repository.ExisteClaveIdempotenciaAsync(empresaId, claveNormalizada, cancellationToken))
            throw new ConflictException("La clave de idempotencia ya fue registrada para esta empresa.");

        await _repository.AddAsync(candidato, cancellationToken);
        return candidato;
    }

    private static bool EsMismaSolicitud(MensajeOutbox existente, MensajeOutbox candidato)
    {
        return existente.EmpresaId == candidato.EmpresaId &&
               string.Equals(existente.ClaveIdempotencia, candidato.ClaveIdempotencia, StringComparison.Ordinal) &&
               string.Equals(existente.TipoEvento, candidato.TipoEvento, StringComparison.Ordinal) &&
               string.Equals(existente.PayloadJson, candidato.PayloadJson, StringComparison.Ordinal) &&
               string.Equals(existente.TipoAgregado, candidato.TipoAgregado, StringComparison.Ordinal) &&
               string.Equals(existente.IdAgregado, candidato.IdAgregado, StringComparison.Ordinal) &&
               string.Equals(existente.CorrelationId, candidato.CorrelationId, StringComparison.Ordinal);
    }
}
