using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Repositories;

public class CompraDocumentoRepository : ICompraDocumentoRepository
{
    private readonly AppDbContext _context;

    public CompraDocumentoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<CompraDocumento>> GetByCompraIdAsync(int compraId) =>
        await _context.CompraDocumentos
            .Where(d => d.CompraId == compraId)
            .OrderByDescending(d => d.FechaCreacion)
            .ToListAsync();

    public async Task<CompraDocumento?> GetByIdAsync(int compraId, int documentoId) =>
        await _context.CompraDocumentos
            .FirstOrDefaultAsync(d => d.Id == documentoId && d.CompraId == compraId);

    public async Task<int> CountByCompraIdAsync(int compraId) =>
        await _context.CompraDocumentos.CountAsync(d => d.CompraId == compraId);

    public async Task AddAsync(CompraDocumento documento) =>
        await _context.CompraDocumentos.AddAsync(documento);

    public void Update(CompraDocumento documento) =>
        _context.CompraDocumentos.Update(documento);

    public async Task<bool> SaveChangesAsync() =>
        await _context.SaveChangesAsync() > 0;
}
