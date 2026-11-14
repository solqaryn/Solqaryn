using Solqaryn.Application.DTOs;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public class NumeracionDocumentoDomainTests
{
    [Fact]
    public void SecuenciaDocumento_DefineScopeEmpresaSucursalTipoYNormalizaTipo()
    {
        var secuencia = new SecuenciaDocumento(10, 20, " factura ", "FAC-", 8, 41);

        Assert.Equal(10, secuencia.EmpresaId);
        Assert.Equal(20, secuencia.SucursalId);
        Assert.Equal("FACTURA", secuencia.TipoDocumento);
        Assert.Equal(41, secuencia.UltimoValor);
        Assert.Equal("FAC-00000041", secuencia.Formatear(41));
    }

    [Fact]
    public void SecuenciaDocumento_AdmiteScopeEmpresaSinSucursal()
    {
        var secuencia = new SecuenciaDocumento(10, null, "NOTA-CREDITO");

        Assert.Equal(10, secuencia.EmpresaId);
        Assert.Null(secuencia.SucursalId);
        Assert.Equal("NOTA-CREDITO", secuencia.TipoDocumento);
    }

    [Theory]
    [InlineData(0, null, "FACTURA")]
    [InlineData(-1, null, "FACTURA")]
    [InlineData(1, 0, "FACTURA")]
    [InlineData(1, -2, "FACTURA")]
    public void SecuenciaDocumento_RechazaScopeConIdentidadesInvalidas(int empresaId, int? sucursalId, string tipo)
    {
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => new SecuenciaDocumento(empresaId, sucursalId, tipo));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SecuenciaDocumento_RechazaTipoDocumentalVacio(string tipo)
    {
        Assert.Throws<ArgumentException>(() => new SecuenciaDocumento(1, 2, tipo));
    }

    [Fact]
    public void SecuenciaDocumento_RechazaTipoDocumentalQueExcedePersistencia()
    {
        Assert.Throws<ArgumentException>(() => new SecuenciaDocumento(1, 2, new string('X', 81)));
    }

    [Fact]
    public void SecuenciaDocumento_RechazaPrefijoQueExcedePersistencia()
    {
        var secuencia = new SecuenciaDocumento(1, 2, "FACTURA");

        Assert.Throws<ArgumentException>(() => secuencia.ActualizarFormato(new string('P', 41), 6));
    }

    [Fact]
    public void RegistrarValorReservado_ExigeSiguienteConsecutivoExacto()
    {
        var secuencia = new SecuenciaDocumento(1, 2, "VENTA", valorInicial: 7);

        secuencia.RegistrarValorReservado(8);

        Assert.Equal(8, secuencia.UltimoValor);
        Assert.Throws<InvalidOperationException>(() => secuencia.RegistrarValorReservado(8));
        Assert.Throws<InvalidOperationException>(() => secuencia.RegistrarValorReservado(10));
    }

    [Fact]
    public void ActualizarFormato_NoModificaContador()
    {
        var secuencia = new SecuenciaDocumento(1, 2, "VENTA", "V-", 6, 25);

        secuencia.ActualizarFormato("VEN-", 9);

        Assert.Equal(25, secuencia.UltimoValor);
        Assert.Equal("VEN-000000025", secuencia.Formatear(25));
    }

    [Fact]
    public void ContratosNumeracion_ExponenScopeSinCalculoCliente()
    {
        var scope = new ScopeNumeracionDocumentoDto
        {
            EmpresaId = 3,
            SucursalId = 4,
            TipoDocumento = "FACTURA"
        };

        var config = new ConfigurarSecuenciaDocumentoDto
        {
            EmpresaId = 3,
            SucursalId = 4,
            TipoDocumento = "FACTURA",
            Prefijo = "FAC-",
            LongitudNumero = 8
        };

        Assert.Equal(3, scope.EmpresaId);
        Assert.Equal(4, scope.SucursalId);
        Assert.Equal("FACTURA", scope.TipoDocumento);
        Assert.Null(typeof(ScopeNumeracionDocumentoDto).GetProperty("SiguienteValor"));
        Assert.Null(typeof(ConfigurarSecuenciaDocumentoDto).GetProperty("SiguienteValor"));
        Assert.Null(typeof(ConfigurarSecuenciaDocumentoDto).GetProperty("NumeroRenderizado"));
        Assert.Equal("FAC-", config.Prefijo);
    }
}
