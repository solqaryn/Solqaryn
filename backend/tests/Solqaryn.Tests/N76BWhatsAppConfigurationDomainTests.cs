using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N76BWhatsAppConfigurationDomainTests
{
    [Fact]
    public void Configuracion_EsTenantScopedYNoExponeSecretosEnClaro()
    {
        var configuracion = new ConfiguracionWhatsAppEmpresa(
            empresaId: 73,
            numeroTelefonoE164: "+50499990000",
            tokenSecretoReferencia: "vault://tenant-73/whatsapp/access-token",
            webhookSecretoReferencia: "vault://tenant-73/whatsapp/webhook-secret");

        Assert.Equal(73, configuracion.EmpresaId);
        Assert.Equal("+50499990000", configuracion.NumeroTelefonoE164);
        Assert.Equal("vault://tenant-73/whatsapp/access-token", configuracion.TokenSecretoReferencia);
        Assert.Equal("vault://tenant-73/whatsapp/webhook-secret", configuracion.WebhookSecretoReferencia);
        Assert.True(configuracion.Activa);
        Assert.Equal(1, configuracion.Version);

        Assert.Null(typeof(ConfiguracionWhatsAppEmpresa).GetProperty("Token"));
        Assert.Null(typeof(ConfiguracionWhatsAppEmpresa).GetProperty("AccessToken"));
        Assert.Null(typeof(ConfiguracionWhatsAppEmpresa).GetProperty("WebhookSecret"));
        Assert.Null(typeof(ConfiguracionWhatsAppEmpresa).GetProperty("WebhookSecreto"));
    }

    [Fact]
    public void Configuracion_RechazaTenantTelefonoYReferenciasInvalidas()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfiguracionWhatsAppEmpresa(
            0, "+50499990000", "vault://token", "vault://webhook"));

        Assert.Throws<ArgumentException>(() => new ConfiguracionWhatsAppEmpresa(
            73, "99990000", "vault://token", "vault://webhook"));

        Assert.Throws<ArgumentException>(() => new ConfiguracionWhatsAppEmpresa(
            73, "+50499990000", "token-en-claro", "vault://webhook"));

        Assert.Throws<ArgumentException>(() => new ConfiguracionWhatsAppEmpresa(
            73, "+50499990000", "vault://token", "secreto-en-claro"));
    }

    [Fact]
    public void Reconfigurar_NormalizaYAvanzaVersionSinCambiarTenant()
    {
        var configuracion = new ConfiguracionWhatsAppEmpresa(
            73,
            "+50499990000",
            "vault://tenant-73/whatsapp/access-token-v1",
            "vault://tenant-73/whatsapp/webhook-secret-v1");

        configuracion.Reconfigurar(
            " +50499991111 ",
            " vault://tenant-73/whatsapp/access-token-v2 ",
            " vault://tenant-73/whatsapp/webhook-secret-v2 ");

        Assert.Equal(73, configuracion.EmpresaId);
        Assert.Equal("+50499991111", configuracion.NumeroTelefonoE164);
        Assert.Equal("vault://tenant-73/whatsapp/access-token-v2", configuracion.TokenSecretoReferencia);
        Assert.Equal("vault://tenant-73/whatsapp/webhook-secret-v2", configuracion.WebhookSecretoReferencia);
        Assert.Equal(2, configuracion.Version);
    }
}
