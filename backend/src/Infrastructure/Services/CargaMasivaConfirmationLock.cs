using System.Data;
using System.Data.Common;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Solqaryn.Infrastructure.Services;

public static class CargaMasivaConfirmationLock
{
    public static async Task<IAsyncDisposable?> TryAcquireAsync(
        AppDbContext db,
        int cargaId,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT GET_LOCK(@lockName, 0);";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@lockName";
            parameter.Value = $"solqaryn:carga:{cargaId}";
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var acquired = result is not null && result != DBNull.Value && Convert.ToInt32(result) == 1;
            if (!acquired)
            {
                if (openedHere) await connection.CloseAsync();
                return null;
            }

            return new Lease(connection, $"solqaryn:carga:{cargaId}", openedHere);
        }
        catch
        {
            if (openedHere && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
            throw;
        }
    }

    private sealed class Lease : IAsyncDisposable
    {
        private readonly DbConnection _connection;
        private readonly string _name;
        private readonly bool _closeConnection;
        private bool _disposed;

        public Lease(DbConnection connection, string name, bool closeConnection)
        {
            _connection = connection;
            _name = name;
            _closeConnection = closeConnection;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await using var command = _connection.CreateCommand();
                    command.CommandText = "SELECT RELEASE_LOCK(@lockName);";
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@lockName";
                    parameter.Value = _name;
                    command.Parameters.Add(parameter);
                    await command.ExecuteScalarAsync();
                }
            }
            finally
            {
                if (_closeConnection && _connection.State == ConnectionState.Open)
                    await _connection.CloseAsync();
            }
        }
    }
}
