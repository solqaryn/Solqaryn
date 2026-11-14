using Xunit;

namespace Solqaryn.Tests;

public sealed class BCryptCompatibilityTests
{
    [Fact]
    public void ExistingBcryptHash_RemainsVerifiable()
    {
        const string existingHash = "$2a$10$k1wbIrmNyFAPwPVPSVa/zecw2BCEnBwVS2GbrmgzxFUOqW9dk4TCW";

        Assert.True(BCrypt.Net.BCrypt.Verify(string.Empty, existingHash));
    }

    [Fact]
    public void WrongPassword_RemainsRejected()
    {
        const string existingHash = "$2a$10$k1wbIrmNyFAPwPVPSVa/zecw2BCEnBwVS2GbrmgzxFUOqW9dk4TCW";

        Assert.False(BCrypt.Net.BCrypt.Verify("incorrecta", existingHash));
    }

    [Fact]
    public void NewHash_WithCurrentWorkFactor_VerifiesWithoutChangingPlaintext()
    {
        const string password = "Fase8-BCrypt-Compatibility-Only";
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        Assert.StartsWith("$2", hash);
        Assert.Contains("$12$", hash);
        Assert.True(BCrypt.Net.BCrypt.Verify(password, hash));
        Assert.DoesNotContain(password, hash, StringComparison.Ordinal);
    }
}
