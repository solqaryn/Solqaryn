using System;
using Solqaryn.Application.Security;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-002.A: comprobaciones criptograficas y plazo maximo aislados.
/// No acredita correo verificado, binding tenant, persistencia, consumo
/// atomico, SMTP, rate limiting ni endpoints de recuperacion.
/// </summary>
public sealed class I8002ARecoveryTokenCryptographyTests
{
    [Fact]
    public void Emision_aleatoria_256bits_con_huella_no_equivalente_al_token()
    {
        var a = RecoveryTokenCryptography.GenerateOpaqueToken();
        var b = RecoveryTokenCryptography.GenerateOpaqueToken();
        var digest = RecoveryTokenCryptography.ComputeDigest(a);
        var emitido = DateTimeOffset.UtcNow;

        Assert.Equal(64, a.Length);
        Assert.Equal(64, digest.Length);
        Assert.NotEqual(a, digest);
        Assert.NotEqual(a, b);
        Assert.True(RecoveryTokenCryptography.MatchesUnconsumedToken(
            a, digest, emitido, emitido.AddMinutes(5), emitido, false));
    }

    [Fact]
    public void El_token_invalido_o_consumido_o_vencido_no_es_aceptado()
    {
        var token = RecoveryTokenCryptography.GenerateOpaqueToken();
        var otro = RecoveryTokenCryptography.GenerateOpaqueToken();
        var digest = RecoveryTokenCryptography.ComputeDigest(token);
        var ahora = new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero);
        var emitido = ahora;
        var expira = emitido.AddMinutes(10);

        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            otro, digest, emitido, expira, ahora, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, emitido, expira, ahora, true));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, emitido, expira, expira, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, emitido, expira, expira.AddSeconds(1), false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            "no-es-un-token", digest, emitido, expira, ahora, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, new string('X', 64), emitido, expira, ahora, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            null, digest, emitido, expira, ahora, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, null, emitido, expira, ahora, false));
    }

    [Fact]
    public void VentanaExcedeDiezMinutos_OFechaDeEmisionFutura_SeRechaza()
    {
        var emitido = new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero);
        var token = RecoveryTokenCryptography.GenerateOpaqueToken();
        var digest = RecoveryTokenCryptography.ComputeDigest(token);

        Assert.True(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, emitido, emitido.AddMinutes(10), emitido, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, emitido, emitido.AddMinutes(10).AddTicks(1),
            emitido, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, emitido.AddMinutes(1), emitido.AddMinutes(10),
            emitido, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, emitido, emitido, emitido, false));
        Assert.False(RecoveryTokenCryptography.MatchesUnconsumedToken(
            token, digest, emitido, emitido.AddMinutes(-1), emitido, false));
    }

    [Fact]
    public void Huella_rechaza_entradas_sin_256bits_hexadecimales()
    {
        Assert.Throws<ArgumentException>(() => RecoveryTokenCryptography.ComputeDigest("abc"));
        Assert.Throws<ArgumentException>(() => RecoveryTokenCryptography.ComputeDigest(new string('Z', 64)));
        Assert.Equal(TimeSpan.FromMinutes(10), RecoveryTokenCryptography.MaximumLifetime);
    }
}
