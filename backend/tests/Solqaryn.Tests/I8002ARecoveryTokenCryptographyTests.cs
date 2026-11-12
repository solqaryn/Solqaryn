using System;
using Solqaryn.Application.Security;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-002.A: prueba solo material criptografico aislado. No acredita correo
/// verificado, binding de tenant, persistencia, consumo atomico, SMTP ni endpoint.
/// </summary>
public sealed class I8002ARecoveryTokenCryptographyTests
{
    [Fact]
    public void Emision_aleatoria_256bits_con_huella_no_equivalente_al_token()
    {
        var a = RecoveryTokenCryptography.GenerateOpaqueToken();
        var b = RecoveryTokenCryptography.GenerateOpaqueToken();
        var digest = RecoveryTokenCryptography.ComputeDigest(a);

        Assert.Equal(64, a.Length);
        Assert.Equal(64, digest.Length);
        Assert.NotEqual(a, digest);
        Assert.NotEqual(a, b);
        Assert.True(RecoveryTokenCryptography.MatchesUnconsumedToken(
            a, digest, DateTimeOffset.UtcNow.AddMinutes(5), DateTimeOffset.UtcNow, false));
    }

    [Fact]
    public void El_token_invalido_o_consumido_o_vencido_no_es_aceptado()
    {
        var token = RecoveryTokenCryptography.GenerateOpaqueToken();
        var otro = RecoveryTokenCryptography.GenerateOpaqueToken();
        var digest = RecoveryTokenCryptography.ComputeDigest(token);
        var ahora = new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero);
        var expira = ahora.AddMinutes(10);

        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            otro, digest, expira, ahora, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, expira, ahora, true));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, expira, expira, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, expira, expira.AddSeconds(1), false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            "no-es-un-token", digest, expira, ahora, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, new string('X', 64), expira, ahora, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            null, digest, expira, ahora, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, null, expira, ahora, false));
    }

    [Fact]
    public void Huella_rechaza_entradas_sin_256bits_hexadecimales()
    {
        Assert.Throws<ArgumentException>(() => RecoveryTokenCryptography.ComputeDigest("abc"));
        Assert.Throws<ArgumentException>(() => RecoveryTokenCryptography.ComputeDigest(new string('Z', 64)));
        Assert.Equal(TimeSpan.FromMinutes(10), RecoveryTokenCryptography.MaximumLifetime);
    }
}
