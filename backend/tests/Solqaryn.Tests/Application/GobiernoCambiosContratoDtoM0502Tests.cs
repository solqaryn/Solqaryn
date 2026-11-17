using System.Text.Json;
using Solqaryn.Application.Governance;
using Solqaryn.Domain.Governance;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C00-04.M05.02: contrato serializable de la preparacion inerte.
/// No hay endpoint HTTP ni autorizacion de escritura en este componente.
/// </summary>
public sealed class GobiernoCambiosContratoDtoM0502Tests
{
    private const string Sha = "fbd8fbb9ffbe4c500ebc831d50421c887cd8fe63";
    private readonly GobiernoCambiosPreparacionService _service = new();

    private static SolicitudPreparacionCambio Solicitud(
        TipoCambioGobierno tipo = TipoCambioGobierno.CodigoDev) =>
        new("C00-04.M05.02", "D01", "Preparacion segura en DEV", Sha,
            "governance:contract:0001", 4, tipo, true);

    [Fact]
    public void DtoSerializado_NoFiltraMotivoNiClavePrivadaNiAutorizaEjecucion()
    {
        const string motivoSecreto = "MOTIVO_INTERNO_NO_EXPOSICION_001";
        const string claveSecreta = "secreto:privado:0001";
        var resultado = _service.Preparar(Solicitud() with
        {
            Motivo = motivoSecreto,
            IdempotencyKey = claveSecreta
        });

        var serializado = JsonSerializer.Serialize(resultado);
        using var documento = JsonDocument.Parse(serializado);
        var dto = documento.RootElement;

        Assert.Equal("C00-04.M05.02", dto.GetProperty("MicrotareaId").GetString());
        Assert.Equal("D01", dto.GetProperty("DecisionId").GetString());
        Assert.Equal(Sha, dto.GetProperty("EvidenciaSha").GetString());
        Assert.Equal("PENDIENTE_AUTORIZACION", dto.GetProperty("Estado").GetString());
        Assert.False(dto.GetProperty("AutorizaEjecucion").GetBoolean());
        Assert.False(dto.GetProperty("AutorizaPersistencia").GetBoolean());
        Assert.False(dto.GetProperty("AutorizaConcesionPermisos").GetBoolean());
        Assert.False(dto.TryGetProperty("Motivo", out _));
        Assert.False(dto.TryGetProperty("IdempotencyKey", out _));
        Assert.DoesNotContain(motivoSecreto, serializado);
        Assert.DoesNotContain(claveSecreta, serializado);
    }

    [Theory]
    [InlineData(TipoCambioGobierno.SeguridadYPermisos)]
    [InlineData(TipoCambioGobierno.PersistenciaYMigracion)]
    [InlineData(TipoCambioGobierno.InfraestructuraYProduccion)]
    public void DtoSensible_DevuelveNoGoSinPrivilegios(TipoCambioGobierno tipo)
    {
        var respuesta = _service.Preparar(Solicitud(tipo));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(respuesta));
        var dto = json.RootElement;

        Assert.Equal("NO_GO", dto.GetProperty("Estado").GetString());
        Assert.False(dto.GetProperty("AutorizaEjecucion").GetBoolean());
        Assert.False(dto.GetProperty("AutorizaPersistencia").GetBoolean());
        Assert.False(dto.GetProperty("AutorizaConcesionPermisos").GetBoolean());
    }

    [Theory]
    [InlineData("C00-04.M05.02\n")]
    [InlineData("C00-04.M05")]
    [InlineData("C00-04.M05.02;DROP")]
    public void IdentificadorHostil_EsRechazadoSinFiltrarMotivo(string id)
    {
        const string interno = "SENTINEL_INTERNO_MOTIVO_123";
        var error = Assert.Throws<ArgumentException>(() =>
            _service.Preparar(Solicitud() with { MicrotareaId = id, Motivo = interno }));
        Assert.DoesNotContain(interno, error.Message);
    }

    [Fact]
    public void MotivoDe600Caracteres_Admitido_Pero601YControlRechazados()
    {
        var permitido = _service.Preparar(Solicitud() with { Motivo = new string('x', 600) });
        Assert.False(permitido.AutorizaEjecucion);

        Assert.Throws<ArgumentException>(() =>
            _service.Preparar(Solicitud() with { Motivo = new string('x', 601) }));
        Assert.Throws<ArgumentException>(() =>
            _service.Preparar(Solicitud() with { Motivo = "texto\r\ninyectado" }));
    }

    [Fact]
    public void IdBienFormadoFueraDeCatalogo_NoSeInterpretaComoAutorizacion()
    {
        var resultado = _service.Preparar(Solicitud() with
        {
            MicrotareaId = "C99-99.M99.99",
            IdempotencyKey = "governance:outside:001"
        });
        Assert.Equal("PENDIENTE_AUTORIZACION", resultado.Estado);
        Assert.False(resultado.AutorizaEjecucion);
        Assert.False(resultado.AutorizaPersistencia);
        Assert.False(resultado.AutorizaConcesionPermisos);
    }

    [Fact]
    public void SolicitudNulaYShaTruncado_FallanAntesDeProducirDto()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Preparar(null!));
        Assert.Throws<ArgumentException>(() =>
            _service.Preparar(Solicitud() with { EvidenciaSha = "1234" }));
    }
}
