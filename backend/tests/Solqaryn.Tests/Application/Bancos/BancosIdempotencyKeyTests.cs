using Solqaryn.Application.Bancos;
using Xunit;
using System;

namespace Solqaryn.Tests.Application.Bancos;

public class BancosIdempotencyKeyTests
{
    [Fact]
    public void Create_WithValidKey_ReturnsNormalizedKey()
    {
        var key = BancosIdempotencyKey.Create("  valid-key_123  ");
        Assert.Equal("valid-key_123", key.Value);
        Assert.Equal("valid-key_123", key.ToString());
    }

    [Fact]
    public void Create_WithPadding_NormalizesKeyLength()
    {
        var baseKey = new string('a', BancosIdempotencyKey.MaxLength);
        var paddedKey = $"   {baseKey}   ";
        var key = BancosIdempotencyKey.Create(paddedKey);
        Assert.Equal(baseKey, key.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void Create_WithNullOrWhiteSpace_ThrowsArgumentException(string? invalidKey)
    {
        var ex = Assert.Throws<ArgumentException>(() => BancosIdempotencyKey.Create(invalidKey));
        Assert.Contains("cannot be null, empty, or whitespace", ex.Message);
    }

    [Fact]
    public void Create_WithExactMaxLength_ReturnsKey()
    {
        var exactLengthKey = new string('a', BancosIdempotencyKey.MaxLength);
        var key = BancosIdempotencyKey.Create(exactLengthKey);
        Assert.Equal(exactLengthKey, key.Value);
    }

    [Fact]
    public void Create_WithOversizedKey_ThrowsArgumentException()
    {
        var oversizedKey = new string('a', BancosIdempotencyKey.MaxLength + 1);
        var ex = Assert.Throws<ArgumentException>(() => BancosIdempotencyKey.Create(oversizedKey));
        Assert.Contains($"exceeds maximum length of {BancosIdempotencyKey.MaxLength}", ex.Message);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("A")]
    [InlineData("0")]
    [InlineData("-")]
    [InlineData("_")]
    [InlineData("aA09-_")]
    [InlineData("valid-key-with-dashes")]
    [InlineData("valid_key_with_underscores")]
    public void Create_WithAllowedCharacters_ReturnsKey(string validKey)
    {
        var key = BancosIdempotencyKey.Create(validKey);
        Assert.Equal(validKey, key.Value);
    }

    [Theory]
    [InlineData("invalid key")]
    [InlineData("invalid!key")]
    [InlineData("invalid@key")]
    [InlineData("invalid#key")]
    [InlineData("invalid$key")]
    [InlineData("invalid%key")]
    [InlineData("invalid^key")]
    [InlineData("invalid&key")]
    [InlineData("invalid*key")]
    [InlineData("invalid(key")]
    [InlineData("invalid)key")]
    [InlineData("invalid+key")]
    [InlineData("invalid=key")]
    [InlineData("invalid{key")]
    [InlineData("invalid}key")]
    [InlineData("invalid[key")]
    [InlineData("invalid]key")]
    [InlineData("invalid|key")]
    [InlineData("invalid\\key")]
    [InlineData("invalid:key")]
    [InlineData("invalid;key")]
    [InlineData("invalid\"key")]
    [InlineData("invalid'key")]
    [InlineData("invalid<key")]
    [InlineData("invalid>key")]
    [InlineData("invalid,key")]
    [InlineData("invalid.key")]
    [InlineData("invalid?key")]
    [InlineData("invalid/key")]
    [InlineData("invalid\nkey")]
    [InlineData("invalid\rkey")]
    [InlineData("invalid\tkey")]
    [InlineData("invalid\0key")]
    public void Create_WithUnsafeCharacters_ThrowsArgumentException(string unsafeKey)
    {
        var ex = Assert.Throws<ArgumentException>(() => BancosIdempotencyKey.Create(unsafeKey));
        Assert.Contains("contains unsafe character", ex.Message);
    }

    [Fact]
    public void ValueEquality_UsesNormalizedValue()
    {
        var key1 = BancosIdempotencyKey.Create("test-key-1");
        var key2 = BancosIdempotencyKey.Create("test-key-1");
        Assert.Equal(key1, key2);
        Assert.NotSame(key1, key2);
    }

    [Fact]
    public void Inequality_DifferentValues()
    {
        Assert.NotEqual(BancosIdempotencyKey.Create("test-key-1"), BancosIdempotencyKey.Create("test-key-2"));
    }

    [Fact]
    public void Equality_IsCaseSensitive()
    {
        var keyLower = BancosIdempotencyKey.Create("test-key-1");
        var keyUpper = BancosIdempotencyKey.Create("TEST-KEY-1");
        Assert.NotEqual(keyLower, keyUpper);
        Assert.False(keyLower == keyUpper);
    }

    [Fact]
    public void HashCodeEquality_UsesNormalizedValue()
    {
        var key1 = BancosIdempotencyKey.Create("  test-key-1  ");
        var key2 = BancosIdempotencyKey.Create("test-key-1");
        Assert.Equal(key1.GetHashCode(), key2.GetHashCode());
    }

    [Fact]
    public void EqualityOperator_UsesNormalizedValue()
    {
        var key1 = BancosIdempotencyKey.Create("  test-key-1  ");
        var key2 = BancosIdempotencyKey.Create("test-key-1");
        Assert.True(key1 == key2);
        Assert.False(key1 != key2);
    }

    [Fact]
    public void EqualityOperator_WithNull_HandlesProperly()
    {
        var key1 = BancosIdempotencyKey.Create("test-key-1");
        BancosIdempotencyKey? nullKey1 = null;
        BancosIdempotencyKey? nullKey2 = null;
        Assert.False(key1 == nullKey1);
        Assert.True(key1 != nullKey1);
        Assert.False(nullKey1 == key1);
        Assert.True(nullKey1 != key1);
        Assert.True(nullKey1 == nullKey2);
        Assert.False(nullKey1 != nullKey2);
    }
}
