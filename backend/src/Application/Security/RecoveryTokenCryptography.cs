using System;
using System.Security.Cryptography;

namespace Solqaryn.Application.Security;

/// <summary>
/// Material criptografico para una futura recuperacion de credenciales.
/// No es un flujo de recuperacion: la emision requiere una direccion verificada
/// asociada al usuario Y empresa, y el consumo exige una transaccion atomica
/// contra un registro persistido. Nunca guardar el token en claro.
/// </summary>
public static class RecoveryTokenCryptography
{
    private const int TokenBytes = 32;
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromMinutes(10);

    /// <summary>Genera 256 bits con un CSPRNG. El valor se entrega solo al destinatario verificado.</summary>
    public static string GenerateOpaqueToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytes));

    /// <summary>El repositorio debe persistir exclusivamente esta huella, nunca el token.</summary>
    public static string ComputeDigest(string token)
    {
        if (!TryDecode32Bytes(token, out var bytes))
            throw new ArgumentException("Token de recuperacion invalido.", nameof(token));

        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    /// <summary>
    /// Comprueba criptografia y caducidad, NO realiza consumo. El llamador debe
    /// verificar pertenencia usuario/tenant/correo acreditado, guardar IssuedAtUtc y marcar ConsumedAt
    /// mediante UPDATE atomico condicionado a ConsumedAt IS NULL antes de cambiar clave.
    /// </summary>
    public static bool MatchesUnconsumedToken(
        string? candidate,
        string? savedDigest,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc,
        bool alreadyConsumed)
    {
        // El registro persistido debe conservar issuedAtUtc. Impedir que un
        // plazo arbitrariamente largo extienda la vida del token en silencio.
        if (alreadyConsumed ||
            expiresAtUtc <= issuedAtUtc ||
            expiresAtUtc - issuedAtUtc > MaximumLifetime ||
            nowUtc < issuedAtUtc ||
            nowUtc >= expiresAtUtc)
            return false;

        if (!TryDecode32Bytes(candidate, out var candidateBytes) ||
            !TryDecode32Bytes(savedDigest, out var expectedDigest))
            return false;

        var actualDigest = SHA256.HashData(candidateBytes);
        return CryptographicOperations.FixedTimeEquals(actualDigest, expectedDigest);
    }

    private static bool TryDecode32Bytes(string? hex, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (hex is null || hex.Length != TokenBytes * 2)
            return false;
        try
        {
            bytes = Convert.FromHexString(hex);
            return bytes.Length == TokenBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
