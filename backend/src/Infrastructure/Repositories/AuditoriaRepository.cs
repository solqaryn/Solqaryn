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
        // Never trust the raw tenant header, public storefront context, or a
        // global role claim. Authorization must precede both count and paging.
        var empresaId = _context.AuthorizedEmpresaId;
        if (empresaId is not > 0)
            throw new ForbiddenAccessException("La auditoría requiere una empresa autorizada.");
        var query = _context.RegistrosAuditoria.AsNoTracking()
            .Where(r => r.EmpresaId == empresaId &&
                r.AmbitoAutoridad == AmbitoAutorizacion.Empresa);

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
