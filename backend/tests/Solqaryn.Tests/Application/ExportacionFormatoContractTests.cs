using Solqaryn.Application.DTOs;
using Xunit;

namespace Solqaryn.Tests.Application;

public class FormatoExportacionContratoTests
{
    [Theory]
    [InlineData("csv", FormatoExportacion.Csv, "text/csv; charset=utf-8", ".csv")]
    [InlineData(" XLSX ", FormatoExportacion.Xlsx, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    [InlineData("Pdf", FormatoExportacion.Pdf, "application/pdf", ".pdf")]
    public void Parse_MapeaFormatoMimeYExtensionDeFormaDeterminista(
        string token,
        FormatoExportacion formatoEsperado,
        string contentTypeEsperado,
        string extensionEsperada)
    {
        var contrato = FormatoExportacionContrato.Parse(token);

        Assert.Equal(formatoEsperado, contrato.Formato);
        Assert.Equal(token.Trim().ToLowerInvariant(), contrato.Token);
        Assert.Equal(contentTypeEsperado, contrato.ContentType);
        Assert.Equal(extensionEsperada, contrato.Extension);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xls")]
    [InlineData("json")]
    public void TryParse_RechazaTokensFueraDelContrato(string? token)
    {
        var aceptado = FormatoExportacionContrato.TryParse(token, out var contrato);

        Assert.False(aceptado);
        Assert.Null(contrato);
    }

    [Fact]
    public void Parse_FallaCerradoParaFormatoDesconocido()
    {
        var error = Assert.Throws<ArgumentException>(() => FormatoExportacionContrato.Parse("json"));

        Assert.Contains("csv, xlsx o pdf", error.Message, StringComparison.Ordinal);
    }
}
