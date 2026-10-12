using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Exceptions;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Solqaryn.Infrastructure.Services;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private const int MaxRetryCount = 3;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Las operaciones internas de un agregado deben participar en la
        // transacción ya abierta por el orquestador exterior. El propietario de
        // esa transacción conserva la responsabilidad de commit, rollback y
        // reintento completo ante errores transitorios 1205/1213.
        if (_context.Database.CurrentTransaction is not null)
        {
            await operation();
            return;
        }

        int attempt = 0;
        while (true)
        {
            attempt++;
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await transaction.CommitAsync();
                break;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _context.ChangeTracker.Clear();

                if (attempt < MaxRetryCount && IsTransientError(ex))
                {
                    await Task.Delay(100 * attempt);
                    continue;
                }

                var translated = TranslateException(ex);
                if (translated != null)
                {
                    throw translated;
                }
                throw;
            }
        }
    }

    private static bool IsTransientError(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (MySqlProviderErrorClassifier.TryClassify(current, out var error) &&
                (error.Number == 1205 || error.Number == 1213))
            {
                return true;
            }
        }
        return false;
    }

    private Exception? TranslateException(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (MySqlProviderErrorClassifier.TryClassify(current, out var error) &&
                error.Number == 1062 &&
                error.Message.Contains("IX_TipoClientes_EsPredeterminadoUnico", StringComparison.Ordinal))
            {
                return new UniqueConstraintViolationException(
                    "TipoClientePredeterminadoUnico",
                    "Conflicto de concurrencia: Ya existe otro tipo de cliente marcado como predeterminado único. Inténtalo de nuevo.",
                    exception);
            }
        }
        return null;
    }
}
