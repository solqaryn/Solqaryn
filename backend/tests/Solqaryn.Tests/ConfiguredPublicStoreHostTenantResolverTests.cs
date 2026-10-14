using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Solqaryn.Infrastructure.Services;
using Xunit;

namespace Solqaryn.Tests;

public sealed class ConfiguredPublicStoreHostTenantResolverTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(
            values.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();

    [Fact]
    public void SinActivacionExplicita_NoResuelveNingunHost()
    {
        var resolver = new ConfiguredPublicStoreHostTenantResolver(
            Config(("PublicStore:HostBindings:0:Host", "tienda-a.example.test"),
                   ("PublicStore:HostBindings:0:EmpresaId", "11")));
        Assert.False(resolver.IsEnabled);
        Assert.Null(resolver.ResolveEmpresaId(new HostString("tienda-a.example.test")));
    }

    [Fact]
    public void HostsExactos_AyB_TienenIdentidadesSeparadasYNoAceptanSimilares()
    {
        var resolver = new ConfiguredPublicStoreHostTenantResolver(Config(
            ("PublicStore:EnableMultiEmpresaBinding", "true"),
            ("PublicStore:HostBindings:0:Host", "tienda-a.example.test"),
            ("PublicStore:HostBindings:0:EmpresaId", "11"),
            ("PublicStore:HostBindings:1:Host", "tienda-b.example.test"),
            ("PublicStore:HostBindings:1:EmpresaId", "22")));
        Assert.True(resolver.IsEnabled);
        Assert.Equal(11, resolver.ResolveEmpresaId(new HostString("TIENDA-A.EXAMPLE.TEST")));
        Assert.Equal(22, resolver.ResolveEmpresaId(new HostString("tienda-b.example.test")));
        Assert.Null(resolver.ResolveEmpresaId(new HostString("tienda-a.example.test.evil.test")));
        Assert.Null(resolver.ResolveEmpresaId(new HostString("no-validada.example.test")));
        Assert.Null(resolver.ResolveEmpresaId(new HostString("127.0.0.1")));
    }

    [Theory]
    [InlineData("*.example.test")]
    [InlineData("https://tienda-a.example.test")]
    [InlineData("tienda_a.example.test")]
    [InlineData("127.0.0.1")]
    [InlineData("tienda..example.test")]
    [InlineData(".example.test")]
    public void ConfiguracionInsegura_NoIniciaPublicacion(string host)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new ConfiguredPublicStoreHostTenantResolver(Config(
                ("PublicStore:EnableMultiEmpresaBinding", "true"),
                ("PublicStore:HostBindings:0:Host", host),
                ("PublicStore:HostBindings:0:EmpresaId", "11"))));
    }

    [Fact]
    public void HostDuplicado_NoPermiteMapeoAmbiguo()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new ConfiguredPublicStoreHostTenantResolver(Config(
                ("PublicStore:EnableMultiEmpresaBinding", "true"),
                ("PublicStore:HostBindings:0:Host", "tienda-a.example.test"),
                ("PublicStore:HostBindings:0:EmpresaId", "11"),
                ("PublicStore:HostBindings:1:Host", "TIENDA-A.EXAMPLE.TEST"),
                ("PublicStore:HostBindings:1:EmpresaId", "22"))));
    }

    [Fact]
    public void PublicacionSinVinculaciones_FallaCerrada()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new ConfiguredPublicStoreHostTenantResolver(Config(
                ("PublicStore:EnableMultiEmpresaBinding", "true"))));
    }
}
