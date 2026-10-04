using System;

namespace Solqaryn.Infrastructure.Services;

internal readonly record struct MySqlProviderError(int Number, string Message, Exception Source);

internal static class MySqlProviderErrorClassifier
{
    private const string MySqlConnectorExceptionType = "MySqlConnector.MySqlException";
    private const string OracleMySqlExceptionType = "MySql.Data.MySqlClient.MySqlException";

    public static bool TryFind(Exception exception, out MySqlProviderError error)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (TryClassify(current, out error))
                return true;
        }

        error = default;
        return false;
    }

    public static bool TryClassify(Exception exception, out MySqlProviderError error)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var type = exception.GetType();
        if (type.FullName is not (MySqlConnectorExceptionType or OracleMySqlExceptionType))
        {
            error = default;
            return false;
        }

        var numberProperty = type.GetProperty("Number");
        if (numberProperty?.PropertyType != typeof(int))
        {
            error = default;
            return false;
        }

        try
        {
            if (numberProperty.GetValue(exception) is int number)
            {
                error = new MySqlProviderError(number, exception.Message, exception);
                return true;
            }
        }
        catch (Exception reflectionError) when (
            reflectionError is System.Reflection.TargetInvocationException or
            MethodAccessException)
        {
            // Fail closed: a provider exception whose public Number contract
            // cannot be read must never be treated as a retryable MySQL error.
        }

        error = default;
        return false;
    }
}
