using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Security;
using Xunit;

namespace Solqaryn.Tests.Domain;

public sealed class C1004CacheJobsDomainRulesTests
{
    private const string ShaA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ShaB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly DateTimeOffset Ahora = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static ContextoTenantActual Contexto(int usuario = 10, int empresa = 100)
    {
        var membresia = new UsuarioEmpresa(usuario, empresa, 5);
        return ContextoTenantActual.DesdeMembresia(membresia, usuario, empresa);
    }

    private static void Solicitud(ContextoTenantActual contexto, int actor = 10, int owner = 100,
        string operacion = "export.request", string clave = "idem-12345678",
        string sha = ShaA, int intento = 0, int maxIntentos = 3,
        int minutosPlazo = 30)
    {
        C1004CacheJobsDomainRules.ValidarSolicitudTrabajo(contexto, actor, owner,
            operacion, clave, sha, 1, intento, maxIntentos,
            Ahora.AddMinutes(minutosPlazo), Ahora);
    }

    [Fact]
    public void SolicitudValida_AutoridadTenantSoloDesdeMembresiaVerificada()
    {
        Solicitud(Contexto());
    }

    [Theory]
    [InlineData(10, 200)]
    [InlineData(11, 100)]
    public void Solicitud_DeniegaTenantAjenoOActorDistinto(int actor, int owner)
    {
        Assert.Throws<InvalidOperationException>(() => Solicitud(Contexto(), actor, owner));
    }

    [Fact]
    public void Solicitud_DeniegaOperacionNoSegura()
    {
        Assert.Throws<ArgumentException>(() => Solicitud(Contexto(), operacion: "../exec"));
    }

    [Fact]
    public void Solicitud_DeniegaClaveHostil()
    {
        Assert.Throws<ArgumentException>(() => Solicitud(Contexto(), clave: "x=1;DROP"));
    }

    [Fact]
    public void Solicitud_DeniegaHashNoHexadecimal()
    {
        Assert.Throws<ArgumentException>(() => Solicitud(Contexto(), sha: new string('z', 64)));
    }

    [Fact]
    public void Solicitud_DeniegaIntentosYDeadlineFueraDePolitica()
    {
        Assert.Throws<InvalidOperationException>(() => Solicitud(Contexto(), intento: 3, maxIntentos: 3));
        Assert.Throws<InvalidOperationException>(() => Solicitud(Contexto(), maxIntentos: 13));
        Assert.Throws<InvalidOperationException>(() => Solicitud(Contexto(), minutosPlazo: 0));
        Assert.Throws<InvalidOperationException>(() => Solicitud(Contexto(), minutosPlazo: 1441));
    }

    [Fact]
    public void Idempotencia_PermiteMismoPayloadYOperacion()
    {
        Assert.True(C1004CacheJobsDomainRules.EsReintentoIdempotente(
            "export.request", ShaA, "export.request", ShaA.ToUpperInvariant()));
    }

    [Theory]
    [InlineData("export.request", "job.retry")]
    [InlineData("export.request", "cache.invalidate")]
    public void Idempotencia_DeniegaOperacionDistinta(string existente, string entrante)
    {
        Assert.Throws<InvalidOperationException>(() =>
            C1004CacheJobsDomainRules.EsReintentoIdempotente(existente, ShaA, entrante, ShaA));
    }

    [Fact]
    public void Idempotencia_DeniegaPayloadAlterado()
    {
        Assert.Throws<InvalidOperationException>(() =>
            C1004CacheJobsDomainRules.EsReintentoIdempotente("export.request", ShaA, "export.request", ShaB));
    }

    [Fact]
    public void Lease_VerificaVersionTokenYVigencia()
    {
        C1004CacheJobsDomainRules.ExigirLeaseVigente(5, 5, "lease-1", "lease-1", Ahora.AddMinutes(5), Ahora);
    }

    [Fact]
    public void Lease_DeniegaVersionAnteriorTokenAjenoYExpiracion()
    {
        Assert.Throws<InvalidOperationException>(() =>
            C1004CacheJobsDomainRules.ExigirLeaseVigente(4, 5, "lease-1", "lease-1", Ahora.AddMinutes(5), Ahora));
        Assert.Throws<InvalidOperationException>(() =>
            C1004CacheJobsDomainRules.ExigirLeaseVigente(5, 5, "lease-2", "lease-1", Ahora.AddMinutes(5), Ahora));
        Assert.Throws<InvalidOperationException>(() =>
            C1004CacheJobsDomainRules.ExigirLeaseVigente(5, 5, "lease-1", "lease-1", Ahora, Ahora));
    }

    [Fact]
    public void ArtefactoConfirmado_PermiteIntegridadPropiaVigente()
    {
        C1004CacheJobsDomainRules.ExigirArtefactoIntegro(
            Contexto(), 10, 100, true, 64, 64, ShaA, ShaA, Ahora.AddMinutes(5), Ahora);
    }

    [Fact]
    public void Artefacto_DeniegaParcialHashDistintoOExpirado()
    {
        Assert.Throws<InvalidOperationException>(() => C1004CacheJobsDomainRules.ExigirArtefactoIntegro(
            Contexto(), 10, 100, false, 64, 64, ShaA, ShaA, Ahora.AddMinutes(5), Ahora));
        Assert.Throws<InvalidOperationException>(() => C1004CacheJobsDomainRules.ExigirArtefactoIntegro(
            Contexto(), 10, 100, true, 64, 63, ShaA, ShaA, Ahora.AddMinutes(5), Ahora));
        Assert.Throws<InvalidOperationException>(() => C1004CacheJobsDomainRules.ExigirArtefactoIntegro(
            Contexto(), 10, 100, true, 64, 64, ShaA, ShaB, Ahora.AddMinutes(5), Ahora));
        Assert.Throws<InvalidOperationException>(() => C1004CacheJobsDomainRules.ExigirArtefactoIntegro(
            Contexto(), 10, 100, true, 64, 64, ShaA, ShaA, Ahora, Ahora));
    }

    [Fact]
    public void Artefacto_DeniegaActorDeOtraEmpresa()
    {
        Assert.Throws<InvalidOperationException>(() => C1004CacheJobsDomainRules.ExigirArtefactoIntegro(
            Contexto(), 10, 200, true, 64, 64, ShaA, ShaA, Ahora.AddMinutes(5), Ahora));
    }
}
