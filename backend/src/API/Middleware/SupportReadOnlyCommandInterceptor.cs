using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.API.Middleware;

/// <summary>
/// Second barrier for support requests: any database command other than a plain
/// SELECT is rejected after the permission filter authorizes the support scope.
/// </summary>
public sealed class SupportReadOnlyCommandInterceptor : DbCommandInterceptor
{
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        EnsureReadOnly(command, eventData);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        EnsureReadOnly(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        EnsureReadOnly(command, eventData);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        EnsureReadOnly(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<System.Data.Common.DbDataReader> result)
    {
        EnsureReadOnly(command, eventData);
        return result;
    }

    public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData,
        InterceptionResult<System.Data.Common.DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        EnsureReadOnly(command, eventData);
        return ValueTask.FromResult(result);
    }

    private static void EnsureReadOnly(DbCommand command, CommandEventData eventData)
    {
        if (eventData.Context is not AppDbContext db || !db.IsPlatformSupportReadRequest)
            return;

        var sql = command.CommandText.TrimStart();
        if (!sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains(';') ||
            sql.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains("FOR SHARE", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains("LOCK IN SHARE MODE", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains("INTO OUTFILE", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains("INTO DUMPFILE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Las sesiones temporales de soporte solo permiten consultas de lectura.");
        }
    }
}