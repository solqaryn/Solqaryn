using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class C0604PortalClienteDomainRulesTests
{
    private static readonly DateTime Ahora = new(2026, 11, 15, 13, 0, 0, DateTimeKind.Utc);

    private static (Cliente Cliente, TiendaCuentaCliente Cuenta, TiendaSesionCliente Sesion)
        Construir()
    {
        var cliente = new Cliente { Id = 101, EmpresaId = 10, Activo = true, Nombre = "Cliente A1" };
        var cuenta = new TiendaCuentaCliente
        {
            Id = 501, ClienteId = 101, Activa = true,
            CorreoNormalizado = "a1@example.invalid",
            PasswordHash = "hash-sintetico"
        };
        var sesion = new TiendaSesionCliente
        {
            Id = 701, CuentaClienteId = 501, TokenHash = "hash-sesion-sintetico",
            ExpiraUtc = Ahora.AddMinutes(30)
        };
        return (cliente, cuenta, sesion);
    }

    [Fact]
    public void A1_ConCuentaSesionYEmpresaPropias_PasaPrecondiciones()
    {
        var (c, a, s) = Construir();
        C0604PortalClienteDomainRules.ExigirSesionYPropiedad(10, 501, c, a, s, Ahora);
        C0604PortalClienteDomainRules.ExigirRecursoPrivado(10, 101, 10, 101);
        C0604PortalClienteDomainRules.ExigirVersionEsperada(7, 7);
    }

    [Theory]
    [InlineData(20, 501)]
    [InlineData(10, 502)]
    [InlineData(0, 501)]
    public void TenantOAcountAjena_NoAutorizaSesion(int empresa, int cuenta)
    {
        var (c, a, s) = Construir();
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirSesionYPropiedad(empresa, cuenta, c, a, s, Ahora));
    }

    [Fact]
    public void ClienteLegacySinOwner_NoSeConvierteEnTenantValido()
    {
        var (c, a, s) = Construir();
        c.EmpresaId = null;
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirSesionYPropiedad(10, 501, c, a, s, Ahora));
    }

    [Fact]
    public void CuentaLigadaAOtroCliente_NoConcedeAcceso()
    {
        var (c, a, s) = Construir();
        a.ClienteId = 102;
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirSesionYPropiedad(10, 501, c, a, s, Ahora));
    }

    [Fact]
    public void ClienteOCuentaInactiva_Deniega()
    {
        var (c, a, s) = Construir();
        c.Activo = false;
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirSesionYPropiedad(10, 501, c, a, s, Ahora));
        c.Activo = true;
        a.Activa = false;
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirSesionYPropiedad(10, 501, c, a, s, Ahora));
    }

    [Fact]
    public void SesionRevocadaOExpirada_Deniega()
    {
        var (c, a, s) = Construir();
        s.RevocadaUtc = Ahora.AddSeconds(-1);
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirSesionYPropiedad(10, 501, c, a, s, Ahora));
        s.RevocadaUtc = null;
        s.ExpiraUtc = Ahora;
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirSesionYPropiedad(10, 501, c, a, s, Ahora));
    }

    [Fact]
    public void SesionDeCuentaAjenaOSinHash_Deniega()
    {
        var (c, a, s) = Construir();
        s.CuentaClienteId = 502;
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirSesionYPropiedad(10, 501, c, a, s, Ahora));
        s.CuentaClienteId = 501;
        s.TokenHash = string.Empty;
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirSesionYPropiedad(10, 501, c, a, s, Ahora));
    }

    [Theory]
    [InlineData(20, 101)]
    [InlineData(10, 102)]
    [InlineData(10, 0)]
    public void RecursoTenantBOClienteA2_NoSeEntrega(int recursoEmpresa, int recursoCliente)
    {
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirRecursoPrivado(10, 101, recursoEmpresa, recursoCliente));
    }

    [Fact]
    public void VersionObsoletaOCero_RechazaEscrituraOptimista()
    {
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirVersionEsperada(6, 7));
        Assert.Throws<InvalidOperationException>(() =>
            C0604PortalClienteDomainRules.ExigirVersionEsperada(0, 7));
    }
}
