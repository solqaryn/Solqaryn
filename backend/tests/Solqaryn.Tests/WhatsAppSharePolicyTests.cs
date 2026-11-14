using Solqaryn.Application.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class WhatsAppSharePolicyTests
{
    [Theory]
    [InlineData("+504 9999-9999", "50499999999")]
    [InlineData("50499999999", "50499999999")]
    [InlineData("9999-9999", "50499999999")]
    [InlineData("99999999", "50499999999")]
    [InlineData("(504) 9999 9999", "50499999999")]
    public void NormalizePhone_accepts_documented_honduras_inputs(string input, string expected)
        => Assert.Equal(expected, WhatsAppSharePolicy.NormalizePhone(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("50450499999999")]
    public void NormalizePhone_rejects_invalid_inputs(string? input)
        => Assert.Empty(WhatsAppSharePolicy.NormalizePhone(input));

    [Fact]
    public void MaskPhone_does_not_store_recipient_in_clear()
        => Assert.Equal("*******9999", WhatsAppSharePolicy.MaskPhone("99999999"));
}
