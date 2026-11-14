using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Fiscal;
using Xunit;

namespace Solqaryn.Tests;

public class FacturacionFiscalDomainTests
{
    [Fact]
    public void PerfilFiscal_NormalizaCodigosYConservaScopeSinHardcodearJurisdiccion()
    {
        var perfil = new PerfilFiscalDocumento(
            empresaId: 42,
            sucursalId: 7,
            jurisdiccion: " hn-demo ",
            proveedor: " proveedor-x ",
            tipoDocumento: " factura-electronica ");

        Assert.Equal(42, perfil.EmpresaId);
        Assert.Equal(7, perfil.SucursalId);
        Assert.Equal("HN-DEMO", perfil.Jurisdiccion);
        Assert.Equal("PROVEEDOR-X", perfil.Proveedor);
        Assert.Equal("FACTURA-ELECTRONICA", perfil.TipoDocumento);
    }

    [Theory]
    [InlineData(0, null, "HN", "P", "F")]
    [InlineData(-1, null, "HN", "P", "F")]
    [InlineData(1, 0, "HN", "P", "F")]
    [InlineData(1, -1, "HN", "P", "F")]
    public void PerfilFiscal_RechazaScopeInvalido(int empresaId, int? sucursalId, string jurisdiccion, string proveedor, string tipoDocumento)
    {
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() =>
            new PerfilFiscalDocumento(empresaId, sucursalId, jurisdiccion, proveedor, tipoDocumento));
    }

    [Theory]
    [InlineData("", "P", "F")]
    [InlineData("HN", " ", "F")]
    [InlineData("HN", "P", "   ")]
    public void PerfilFiscal_RechazaCodigosObligatoriosVacios(string jurisdiccion, string proveedor, string tipoDocumento)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new PerfilFiscalDocumento(1, null, jurisdiccion, proveedor, tipoDocumento));
    }

    [Fact]
    public void SolicitudEmision_ExigeIdempotenciaYSnapshotInmutable()
    {
        var perfil = new PerfilFiscalDocumento(1, null, "JURISDICCION-A", "ADAPTADOR-1", "DOC-X");
        var solicitud = new SolicitudEmisionFiscal(perfil, 99, " idem-001 ", " sha256:abc ");

        Assert.Same(perfil, solicitud.Perfil);
        Assert.Equal(99, solicitud.FacturaId);
        Assert.Equal("idem-001", solicitud.ClaveIdempotencia);
        Assert.Equal("sha256:abc", solicitud.HashSnapshot);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SolicitudEmisionFiscal(perfil, 0, "idem", "hash"));
        Assert.ThrowsAny<ArgumentException>(() =>
            new SolicitudEmisionFiscal(perfil, 1, " ", "hash"));
        Assert.ThrowsAny<ArgumentException>(() =>
            new SolicitudEmisionFiscal(perfil, 1, "idem", " "));
    }

    [Fact]
    public void ResultadoFiscal_MantieneSemanticaTecnicaProviderNeutral()
    {
        Assert.Equal(1, (int)EstadoResultadoFiscal.Pendiente);
        Assert.Equal(2, (int)EstadoResultadoFiscal.Confirmado);
        Assert.Equal(3, (int)EstadoResultadoFiscal.Rechazado);

        var resultado = new ResultadoEmisionFiscal(
            EstadoResultadoFiscal.Pendiente,
            referenciaExterna: " ref-123 ",
            codigoProveedor: " PENDING ",
            mensaje: " en proceso ",
            esTransitorio: true);

        Assert.Equal("ref-123", resultado.ReferenciaExterna);
        Assert.Equal("PENDING", resultado.CodigoProveedor);
        Assert.Equal("en proceso", resultado.Mensaje);
        Assert.True(resultado.EsTransitorio);
    }

    [Fact]
    public void ProveedorFiscal_ExponeSoloContratoProviderNeutral()
    {
        var nombres = typeof(IProveedorDocumentoFiscal)
            .GetMethods()
            .Select(x => x.Name)
            .ToArray();

        Assert.Contains(nameof(IProveedorDocumentoFiscal.Soporta), nombres);
        Assert.Contains(nameof(IProveedorDocumentoFiscal.EmitirAsync), nombres);
        Assert.Contains(nameof(IProveedorDocumentoFiscal.ConsultarAsync), nombres);

        Assert.DoesNotContain(nombres, x => x.Contains("CAI", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, x => x.Contains("SAR", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(nombres, x => x.Contains("Honduras", StringComparison.OrdinalIgnoreCase));
    }
}
