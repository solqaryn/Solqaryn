using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

/// <summary>
/// Repositorio N7.7.D. Todas las lecturas incluyen EmpresaId en el predicado;
/// ninguna API de repositorio permite consultar un correo fuera del tenant.
/// </summary>
public sealed class EmailEmpresarialRepository : IEmailEmpresarialRepository
{
    private readonly AppDbContext _context;

    public EmailEmpresarialRepository(AppDbContext context)
    {
        _context = context;
    }

    private DbSet<EmailEmpresarial> Emails => _context.Set<EmailEmpresarial>();

    public Task<EmailEmpresarial?> GetByClaveIdempotenciaAsync(
        int empresaId,
        string claveIdempotencia,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        if (string.IsNullOrWhiteSpace(claveIdempotencia))
            throw new ArgumentException("La clave de idempotencia es obligatoria.", nameof(claveIdempotencia));

        var clave = claveIdempotencia.Trim();
        return Emails.AsNoTracking().SingleOrDefaultAsync(
            email => email.EmpresaId == empresaId && email.ClaveIdempotencia == clave,
            cancellationToken);
    }

    public Task<EmailEmpresarial?> GetByMensajeIdAsync(
        int empresaId,
        Guid mensajeId,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        if (mensajeId == Guid.Empty)
            throw new ArgumentOutOfRangeException(nameof(mensajeId));

        return Emails.AsNoTracking().SingleOrDefaultAsync(
            email => email.EmpresaId == empresaId && email.MensajeId == mensajeId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<EmailEmpresarial>> ListarAsync(
        int empresaId,
        EstadoEntregaEmail? estado,
        string? correlationId,
        int pagina,
        int tamano,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        if (pagina <= 0)
            throw new ArgumentOutOfRangeException(nameof(pagina));
        if (tamano is <= 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(tamano));

        var query = ConstruirFiltro(empresaId, estado, correlationId);
        return await query
            .AsNoTracking()
            .OrderByDescending(email => email.CreadoEnUtc)
            .ThenByDescending(email => email.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ContarAsync(
        int empresaId,
        EstadoEntregaEmail? estado,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        ExigirEmpresa(empresaId);
        return ConstruirFiltro(empresaId, estado, correlationId).CountAsync(cancellationToken);
    }

    public async Task AddAsync(EmailEmpresarial email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        ExigirEmpresa(email.EmpresaId);
        await Emails.AddAsync(email, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private IQueryable<EmailEmpresarial> ConstruirFiltro(
        int empresaId,
        EstadoEntregaEmail? estado,
        string? correlationId)
    {
        var query = Emails.Where(email => email.EmpresaId == empresaId);
        if (estado is not null)
            query = query.Where(email => email.Estado == estado.Value);

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            var correlation = correlationId.Trim();
            query = query.Where(email => email.CorrelationId == correlation);
        }

        return query;
    }

    private static void ExigirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
            throw new ArgumentOutOfRangeException(nameof(empresaId));
    }
}
