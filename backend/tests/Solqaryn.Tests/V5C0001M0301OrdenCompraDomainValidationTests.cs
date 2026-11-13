using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class V5C0001M0301OrdenCompraDomainValidationTests
{
    [Fact]
    public void Huella_sha256_hexadecimal_valida_se_normaliza_y_replay_es_idempotente()
    {
        var orden = new OrdenCompra();
        var huellaMayuscula = new string('A', 64);
        orden.EstablecerIdempotencia(" compra:empresa7:orden1 ", huellaMayuscula);

        Assert.Equal("compra:empresa7:orden1", orden.IdempotencyKey);
        Assert.Equal(new string('a', 64), orden.IdempotencyFingerprint);

        // Reusar la huella equivalente en otra caja no cambia la identidad persistida.
        orden.EstablecerIdempotencia("compra:empresa7:orden1", new string('a', 64));
        Assert.Equal(new string('a', 64), orden.IdempotencyFingerprint);
    }

    [Theory]
    [InlineData('G')]
    [InlineData('z')]
    [InlineData('ñ')]
    [InlineData('０')]
    public void Huella_no_hexadecimal_se_rechaza_sin_mutacion(char caracter)
    {
        var orden = new OrdenCompra();
        Assert.Throws<ArgumentException>(() =>
            orden.EstablecerIdempotencia("orden1", new string(caracter, 64)));

        Assert.Null(orden.IdempotencyKey);
        Assert.Null(orden.IdempotencyFingerprint);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(63)]
    [InlineData(65)]
    public void Huella_con_longitud_incorrecta_se_rechaza(int longitud)
    {
        var orden = new OrdenCompra();
        Assert.Throws<ArgumentException>(() =>
            orden.EstablecerIdempotencia("orden1", new string('a', longitud)));
        Assert.Null(orden.IdempotencyFingerprint);
    }

    [Fact]
    public void Reutilizar_clave_con_otro_hash_o_cambiar_clave_es_fail_closed()
    {
        var orden = new OrdenCompra();
        var original = new string('a', 64);
        orden.EstablecerIdempotencia("orden1", original);

        Assert.Throws<InvalidOperationException>(() =>
            orden.EstablecerIdempotencia("orden1", new string('b', 64)));
        Assert.Throws<InvalidOperationException>(() =>
            orden.EstablecerIdempotencia("orden2", original));
        Assert.Equal("orden1", orden.IdempotencyKey);
        Assert.Equal(original, orden.IdempotencyFingerprint);
    }
}
