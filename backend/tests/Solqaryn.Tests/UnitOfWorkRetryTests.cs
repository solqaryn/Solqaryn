using System;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Solqaryn.Application.Exceptions;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Xunit;

namespace Solqaryn.Tests;

public class FakeNonMySqlExceptionWithNumberProperty : Exception
{
    public int Number => 1213;
}

public class UnitOfWorkRetryTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static MySqlException CreateRealMySqlException(int number, string message)
    {
        var ex = (MySqlException)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(MySqlException));
        var numberField = typeof(MySqlException).GetField("_number", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? typeof(MySqlException).GetField("<Number>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        numberField?.SetValue(ex, number);

        var messageField = typeof(Exception).GetField("_message", BindingFlags.NonPublic | BindingFlags.Instance);
        messageField?.SetValue(ex, message);

        return ex;
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_EjecutaOperacionExitosamente()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        var executed = false;
        await uow.ExecuteInTransactionAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ErrorNoTransitorio_LanzaExcepcionDirectamente()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteInTransactionAsync(() => throw new InvalidOperationException("Non-transient failure")));

        Assert.Equal("Non-transient failure", ex.Message);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_PreservaExcepcionOriginalNoTraducida()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        var originalEx = new InvalidOperationException("Test original trace preservation");

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.ExecuteInTransactionAsync(() => throw originalEx));

        Assert.Same(originalEx, thrown);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ReintentaMySqlException1205_YSucedeEnSegundoIntento()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        int attempts = 0;
        await uow.ExecuteInTransactionAsync(() =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw CreateRealMySqlException(1205, "Lock wait timeout exceeded");
            }
            return Task.CompletedTask;
        });

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ReintentaMySqlException1213_YSucedeEnSegundoIntento()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        int attempts = 0;
        await uow.ExecuteInTransactionAsync(() =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw CreateRealMySqlException(1213, "Deadlock found");
            }
            return Task.CompletedTask;
        });

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_SuperaMaximoTresIntentosYPropagaMySqlException()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        int attempts = 0;
        await Assert.ThrowsAsync<MySqlException>(() =>
            uow.ExecuteInTransactionAsync(() =>
            {
                attempts++;
                throw CreateRealMySqlException(1213, "Persistent deadlock");
            }));

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_NoReintentaViolacionDeUnicidad1062_YTraduceExcepcion()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        int attempts = 0;
        var mysqlEx = CreateRealMySqlException(1062, "Duplicate entry for key IX_TipoClientes_EsPredeterminadoUnico");
        var dbEx = new DbUpdateException("DbUpdateException", mysqlEx);

        var thrown = await Assert.ThrowsAsync<UniqueConstraintViolationException>(() =>
            uow.ExecuteInTransactionAsync(() =>
            {
                attempts++;
                throw dbEx;
            }));

        Assert.Equal(1, attempts);
        Assert.Equal("TipoClientePredeterminadoUnico", thrown.ConstraintName);
        Assert.Equal(
            "Conflicto de concurrencia: Ya existe otro tipo de cliente marcado como predeterminado único. Inténtalo de nuevo.",
            thrown.Message);
        Assert.Same(dbEx, thrown.InnerException);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_NoReintentaExcepcionNoMySqlAunqueTengaPropiedadNumber()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        int attempts = 0;
        await Assert.ThrowsAsync<FakeNonMySqlExceptionWithNumberProperty>(() =>
            uow.ExecuteInTransactionAsync(() =>
            {
                attempts++;
                throw new FakeNonMySqlExceptionWithNumberProperty();
            }));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_NoTraduceError1062DeOtroIndice()
    {
        await using var context = CreateInMemoryContext();
        var uow = new UnitOfWork(context);

        int attempts = 0;
        var mysqlEx = CreateRealMySqlException(1062, "Duplicate entry for key IX_Usuarios_NombreUsuario");
        var dbEx = new DbUpdateException("DbUpdateException", mysqlEx);

        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() =>
            uow.ExecuteInTransactionAsync(() =>
            {
                attempts++;
                throw dbEx;
            }));

        Assert.Equal(1, attempts);
        Assert.Same(dbEx, thrown);
    }
}
