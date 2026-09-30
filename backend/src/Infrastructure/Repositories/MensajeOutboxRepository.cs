using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

/// <summary>
/// Persistencia del outbox N7.1/N7.2. Mantiene el límite transaccional del contrato:
/// AddAsync agrega la intención al DbContext, pero no confirma la unidad de trabajo.
/// Los claims y sus resultados se persisten con updates condicionales para impedir
/// que un worker stale confirme el trabajo de un intento ya supersedido.
/// </summary>
public sealed class MensajeOutboxRepository : IMensajeOutboxRepository
{
    private readonly AppDbContext _context;

    public MensajeOutboxRepository(AppDbContext context)
    {
        _context = context;
    }

    private DbSet<MensajeOutbox> Mensajes => _context.Set<MensajeOutbox>();

    public async Task AddAsync(MensajeOutbox mensaje, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mensaje);
        await Mensajes.AddAsync(mensaje, cancellationToken);
    }

    public Task<MensajeOutbox?> GetByEventoIdAsync(
        int empresaId,
        Guid eventoId,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        return Mensajes.AsNoTracking().SingleOrDefaultAsync(
            mensaje => mensaje.EmpresaId == empresaId && mensaje.EventoId == eventoId,
            cancellationToken);
    }

    public Task<MensajeOutbox?> GetByClaveIdempotenciaAsync(
        int empresaId,
        string claveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        if (string.IsNullOrWhiteSpace(claveIdempotencia))
            throw new ArgumentException("La clave de idempotencia es obligatoria.", nameof(claveIdempotencia));

        var normalizada = claveIdempotencia.Trim();
        return Mensajes.AsNoTracking().SingleOrDefaultAsync(
            mensaje => mensaje.EmpresaId == empresaId && mensaje.ClaveIdempotencia == normalizada,
            cancellationToken);
    }

    public Task<bool> ExisteClaveIdempotenciaAsync(
        int empresaId,
        string claveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        if (string.IsNullOrWhiteSpace(claveIdempotencia))
            throw new ArgumentException("La clave de idempotencia es obligatoria.", nameof(claveIdempotencia));

        var normalizada = claveIdempotencia.Trim();
        return Mensajes.AsNoTracking().AnyAsync(
            mensaje => mensaje.EmpresaId == empresaId && mensaje.ClaveIdempotencia == normalizada,
            cancellationToken);
    }

    public async Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(
        int empresaId,
        DateTime ahoraUtc,
        int maximoMensajes,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        ExigirUtc(ahoraUtc, nameof(ahoraUtc));
        ExigirLote(maximoMensajes);

        var candidatos = await Mensajes
            .AsNoTracking()
            .Where(mensaje =>
                mensaje.EmpresaId == empresaId &&
                (mensaje.Estado == EstadoMensajeOutbox.Pendiente || mensaje.Estado == EstadoMensajeOutbox.Fallido) &&
                mensaje.DisponibleDesdeUtc <= ahoraUtc &&
                mensaje.ProcesandoDesdeUtc == null)
            .OrderBy(mensaje => mensaje.DisponibleDesdeUtc)
            .ThenBy(mensaje => mensaje.Id)
            .Select(mensaje => mensaje.Id)
            .Take(maximoMensajes)
            .ToListAsync(cancellationToken);

        if (candidatos.Count == 0)
            return Array.Empty<MensajeOutbox>();

        var reclamados = new List<MensajeOutbox>(candidatos.Count);
        foreach (var id in candidatos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var afectados = await Mensajes
                .Where(mensaje =>
                    mensaje.Id == id &&
                    mensaje.EmpresaId == empresaId &&
                    (mensaje.Estado == EstadoMensajeOutbox.Pendiente || mensaje.Estado == EstadoMensajeOutbox.Fallido) &&
                    mensaje.DisponibleDesdeUtc <= ahoraUtc &&
                    mensaje.ProcesandoDesdeUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(mensaje => mensaje.Estado, EstadoMensajeOutbox.Procesando)
                        .SetProperty(mensaje => mensaje.Intentos, mensaje => mensaje.Intentos + 1)
                        .SetProperty(mensaje => mensaje.ProcesandoDesdeUtc, ahoraUtc)
                        .SetProperty(mensaje => mensaje.UltimoIntentoEnUtc, ahoraUtc)
                        .SetProperty(mensaje => mensaje.UltimoError, (string?)null)
                        .SetProperty(mensaje => mensaje.FechaActualizacion, ahoraUtc),
                    cancellationToken);

            if (afectados != 1)
                continue;

            reclamados.Add(await Mensajes.AsNoTracking().SingleAsync(
                mensaje => mensaje.Id == id && mensaje.EmpresaId == empresaId,
                cancellationToken));
        }

        return reclamados;
    }

    public async Task<int> RecuperarProcesandoStaleAsync(
        int empresaId,
        DateTime staleAntesUtc,
        DateTime ahoraUtc,
        int maximoMensajes,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        ExigirUtc(staleAntesUtc, nameof(staleAntesUtc));
        ExigirUtc(ahoraUtc, nameof(ahoraUtc));
        ExigirLote(maximoMensajes);
        if (staleAntesUtc > ahoraUtc)
            throw new ArgumentOutOfRangeException(nameof(staleAntesUtc));

        var ids = await Mensajes.AsNoTracking()
            .Where(mensaje =>
                mensaje.EmpresaId == empresaId &&
                mensaje.Estado == EstadoMensajeOutbox.Procesando &&
                mensaje.ProcesandoDesdeUtc != null &&
                mensaje.ProcesandoDesdeUtc <= staleAntesUtc)
            .OrderBy(mensaje => mensaje.ProcesandoDesdeUtc)
            .ThenBy(mensaje => mensaje.Id)
            .Select(mensaje => mensaje.Id)
            .Take(maximoMensajes)
            .ToListAsync(cancellationToken);

        var recuperados = 0;
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            recuperados += await Mensajes
                .Where(mensaje =>
                    mensaje.Id == id &&
                    mensaje.EmpresaId == empresaId &&
                    mensaje.Estado == EstadoMensajeOutbox.Procesando &&
                    mensaje.ProcesandoDesdeUtc != null &&
                    mensaje.ProcesandoDesdeUtc <= staleAntesUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(mensaje => mensaje.Estado, EstadoMensajeOutbox.Fallido)
                        .SetProperty(mensaje => mensaje.DisponibleDesdeUtc, ahoraUtc)
                        .SetProperty(mensaje => mensaje.ProcesandoDesdeUtc, (DateTime?)null)
                        .SetProperty(mensaje => mensaje.UltimoError, "STALE_CLAIM_RECOVERED")
                        .SetProperty(mensaje => mensaje.FechaActualizacion, ahoraUtc),
                    cancellationToken);
        }

        return recuperados;
    }

    public async Task<bool> MarcarEntregadoAsync(
        int empresaId,
        int mensajeId,
        int intentoEsperado,
        DateTime ahoraUtc,
        CancellationToken cancellationToken = default)
    {
        ExigirIdentidadIntento(empresaId, mensajeId, intentoEsperado);
        ExigirUtc(ahoraUtc, nameof(ahoraUtc));

        var afectados = await Mensajes
            .Where(mensaje =>
                mensaje.Id == mensajeId &&
                mensaje.EmpresaId == empresaId &&
                mensaje.Estado == EstadoMensajeOutbox.Procesando &&
                mensaje.Intentos == intentoEsperado &&
                mensaje.ProcesandoDesdeUtc != null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(mensaje => mensaje.Estado, EstadoMensajeOutbox.Entregado)
                    .SetProperty(mensaje => mensaje.EntregadoEnUtc, ahoraUtc)
                    .SetProperty(mensaje => mensaje.ProcesandoDesdeUtc, (DateTime?)null)
                    .SetProperty(mensaje => mensaje.UltimoError, (string?)null)
                    .SetProperty(mensaje => mensaje.FechaActualizacion, ahoraUtc),
                cancellationToken);

        return afectados == 1;
    }

    public async Task<bool> RegistrarFalloAsync(
        int empresaId,
        int mensajeId,
        int intentoEsperado,
        string errorSeguro,
        DateTime ahoraUtc,
        DateTime disponibleDesdeUtc,
        bool deadLetter,
        CancellationToken cancellationToken = default)
    {
        ExigirIdentidadIntento(empresaId, mensajeId, intentoEsperado);
        ExigirUtc(ahoraUtc, nameof(ahoraUtc));
        ExigirUtc(disponibleDesdeUtc, nameof(disponibleDesdeUtc));
        if (!deadLetter && disponibleDesdeUtc < ahoraUtc)
            throw new ArgumentOutOfRangeException(nameof(disponibleDesdeUtc));
        if (string.IsNullOrWhiteSpace(errorSeguro) || errorSeguro.Length > MensajeOutbox.LongitudMaximaError)
            throw new ArgumentException("El código de error seguro es obligatorio y acotado.", nameof(errorSeguro));

        var estado = deadLetter ? EstadoMensajeOutbox.DeadLetter : EstadoMensajeOutbox.Fallido;
        var proximaVentana = deadLetter ? ahoraUtc : disponibleDesdeUtc;
        var error = errorSeguro.Trim();

        var afectados = await Mensajes
            .Where(mensaje =>
                mensaje.Id == mensajeId &&
                mensaje.EmpresaId == empresaId &&
                mensaje.Estado == EstadoMensajeOutbox.Procesando &&
                mensaje.Intentos == intentoEsperado &&
                mensaje.ProcesandoDesdeUtc != null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(mensaje => mensaje.Estado, estado)
                    .SetProperty(mensaje => mensaje.DisponibleDesdeUtc, proximaVentana)
                    .SetProperty(mensaje => mensaje.ProcesandoDesdeUtc, (DateTime?)null)
                    .SetProperty(mensaje => mensaje.UltimoError, error)
                    .SetProperty(mensaje => mensaje.FechaActualizacion, ahoraUtc),
                cancellationToken);

        return afectados == 1;
    }

    private static void ExigirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId));
    }

    private static void ExigirLote(int maximoMensajes)
    {
        if (maximoMensajes is <= 0 or > 200)
            throw new ArgumentOutOfRangeException(nameof(maximoMensajes));
    }

    private static void ExigirUtc(DateTime value, string parametro)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("La fecha debe expresarse en UTC.", parametro);
    }

    private static void ExigirIdentidadIntento(int empresaId, int mensajeId, int intentoEsperado)
    {
        ExigirEmpresa(empresaId);
        if (mensajeId <= 0)
            throw new ArgumentOutOfRangeException(nameof(mensajeId));
        if (intentoEsperado <= 0)
            throw new ArgumentOutOfRangeException(nameof(intentoEsperado));
    }
}
