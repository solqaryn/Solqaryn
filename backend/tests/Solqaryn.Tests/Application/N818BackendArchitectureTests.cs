using Solqaryn.Application.Bancos;
using Solqaryn.Application.Interfaces;
using Xunit;

namespace Solqaryn.Tests.Application;

public sealed class N818BackendArchitectureTests
{
    [Fact]
    public void Banking_use_case_is_owned_by_domain_slice_and_preserves_interface_contract()
    {
        Assert.Equal("Solqaryn.Application.Bancos", typeof(OperacionBancariaService).Namespace);
        Assert.Contains(typeof(IOperacionBancariaService), typeof(OperacionBancariaService).GetInterfaces());
        Assert.Equal("Solqaryn.Application.Bancos", typeof(BancosIdempotencyKey).Namespace);
        Assert.Equal(100, BancosIdempotencyKey.MaxLength);
    }

    [Fact]
    public void Banking_idempotency_key_fails_closed_on_unsafe_input()
    {
        Assert.Throws<ArgumentException>(() => BancosIdempotencyKey.Create("bad key"));
        Assert.Throws<ArgumentException>(() => BancosIdempotencyKey.Create(" "));
        Assert.Equal("safe-key_1", BancosIdempotencyKey.Create("safe-key_1").Value);
    }
}
