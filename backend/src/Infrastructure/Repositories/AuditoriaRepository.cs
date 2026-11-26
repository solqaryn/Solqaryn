using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class AuditoriaRepository : IAuditoriaRepository
{
    private readonly AppDbContext _context;

    public AuditoriaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RegistroAuditoria registro) =>
        await _context.RegistrosAuditoria.AddAsync(registro);

    public async Task<(List<RegistroAuditoria> Items, int TotalCount)> GetFilteredAsync(AuditoriaFiltroDto filtro)
    {
        // The existing EF query filter is defence in depth, not the authority
        // for sensitive cross-tenant audit reads. Refuse unbound background,
        // public store/invoice and incorrectly configured DI contexts.
        var verifiedTenant = _context.AuthorizedCommercialEmpresaId;
        if (verifiedTenant is not > 0 && _context.IsPlatformSupportReadRequest)
            verifiedTenant = _context.AuthorizedReadEmpresaId;
        if (verifiedTenant is not > 0)
            throw new ForbiddenAccessException(
                "La lectura de auditoria requiere empresa autenticada y autorizada.");

        // Explicit scoped predicate survives a later IgnoreQueryFilters and
        // constrains BOTH the page and the aggregate count to one company.
        var query = _context.RegistrosAuditoria.AsNoTracking()
            // API12 receipts need dedicated resource/branch authorization, not legacy Auditoria.Ver.
            .Where(r => r.EmpresaId == verifiedTenant.Value &&
                r.Accion != AccionPermiso.ReadAuditCorrelation);

        if (filtro.UsuarioId.HasValue)
            query = query.Where(r => r.UsuarioId == filtro.UsuarioId.Value);

        if (!string.IsNullOrWhiteSpace(filtro.Modulo))
        {
            if (Enum.TryParse<ModuloSistema>(filtro.Modulo.Trim(), true, out var modulo))
                query = query.Where(r => r.Modulo == modulo);
            else
                query = query.Where(_ => false);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Accion))
        {
            if (Enum.TryParse<AccionPermiso>(filtro.Accion.Trim(), true, out var accion))
                query = query.Where(r => r.Accion == accion);
            else
                query = query.Where(_ => false);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Entidad)) query = query.Where(r => r.Entidad == filtro.Entidad);
        if (filtro.ReferenciaId.HasValue) query = query.Where(r => r.ReferenciaId == filtro.ReferenciaId.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Resultado)) query = query.Where(r => r.Resultado == filtro.Resultado);
        if (filtro.Desde.HasValue) query = query.Where(r => r.Fecha >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue) query = query.Where(r => r.Fecha <= filtro.Hasta.Value);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            query = query.Where(r =>
                r.Descripcion.Contains(texto) ||
                r.NombreUsuario.Contains(texto) ||
                (r.Motivo != null && r.Motivo.Contains(texto)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.Fecha)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
