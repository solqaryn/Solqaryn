using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Fiscal;
using Solqaryn.Infrastructure.Persistence;
using Solqaryn.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public sealed class DocumentoFiscalEmisionServiceTests
{
    [Fact]
    public async Task EmitirAsync_ClaimDurableEvitaDuplicarProveedorYNoPersisteClavePlana()
    {
        await using var db = CrearContexto();
        var proveedor = new ProveedorFiscalFake("PROV-A", "HN", _ =>
            new ResultadoEmisionFiscal(
                EstadoResultadoFiscal.Confirmado,
                referenciaExterna: "REF-001",
                codigoProveedor: "OK"));
        var service = new DocumentoFiscalEmisionService(db, new[] { proveedor });
        var solicitud = CrearSolicitud("HN", "PROV-A", "FACTURA", "idem-secreto-001", "snapshot-a");

        var primero = await service.EmitirAsync(solicitud);
        var segundo = await service.EmitirAsync(solicitud);

        Assert.Equal(EstadoResultadoFiscal.Confirmado, primero.Estado);
        Assert.False(primero.Idempotente);
        Assert.True(segundo.Idempotente);
        Assert.Equal(1, proveedor.Emisiones);

        var registro = await db.Set<DocumentoFiscalEmision>().SingleAsync();
        Assert.Equal(64, registro.ClaveIdempotenciaHash.Length);
        Assert.DoesNotContain("idem-secreto-001", registro.ClaveIdempotenciaHash, StringComparison.Ordinal);
        Assert.Equal("snapshot-a", registro.HashSnapshot);
        Assert.Equal("REF-001", registro.ReferenciaExterna);
    }

    [Fact]
    public async Task EmitirAsync_MismaClaveConSnapshotDiferenteFallaCerrado()
    {
        await using var db = CrearContexto();
        var proveedor = new ProveedorFiscalFake("PROV-A", "HN", _ =>
            new ResultadoEmisionFiscal(EstadoResultadoFiscal.Confirmado));
        var service = new DocumentoFiscalEmisionService(db, new[] { proveedor });

        await service.EmitirAsync(CrearSolicitud("HN", "PROV-A", "FACTURA", "idem-1", "snapshot-a"));

        await Assert.ThrowsAsync<DocumentoFiscalIdempotenciaException>(() =>
            service.EmitirAsync(CrearSolicitud("HN", "PROV-A", "FACTURA", "idem-1", "snapshot-b")));

        Assert.Equal(1, proveedor.Emisiones);
    }

    [Fact]
    public async Task EmitirAsync_SeleccionaAdaptadorPorProveedorYJurisdiccionSinFormatoUniversal()
    {
        await using var db = CrearContexto();
        var hn = new ProveedorFiscalFake("PROV-HN", "HN", _ =>
            new ResultadoEmisionFiscal(EstadoResultadoFiscal.Confirmado, "HN-1"));
        var mx = new ProveedorFiscalFake("PROV-MX", "MX", _ =>
            new ResultadoEmisionFiscal(EstadoResultadoFiscal.Confirmado, "MX-1"));
        var service = new DocumentoFiscalEmisionService(db, new IProveedorDocumentoFiscal[] { hn, mx });

        var resultadoHn = await service.EmitirAsync(
            CrearSolicitud("HN", "PROV-HN", "DOCUMENTO-A", "idem-hn", "snapshot-hn", facturaId: 11));
        var resultadoMx = await service.EmitirAsync(
            CrearSolicitud("MX", "PROV-MX", "DOCUMENTO-B", "idem-mx", "snapshot-mx", facturaId: 12));

        Assert.Equal("HN-1", resultadoHn.ReferenciaExterna);
        Assert.Equal("MX-1", resultadoMx.ReferenciaExterna);
        Assert.Equal(1, hn.Emisiones);
        Assert.Equal(1, mx.Emisiones);
    }

    [Fact]
    public async Task EmitirAsync_ErrorTransitorioPermiteReintentoSeguroConMismaClave()
    {
        await using var db = CrearContexto();
        var primerIntento = true;
        var proveedor = new ProveedorFiscalFake("PROV-A", "HN", _ =>
        {
            if (primerIntento)
            {
                primerIntento = false;
                throw new HttpRequestException("provider temporalmente indisponible");
            }

            return new ResultadoEmisionFiscal(
                EstadoResultadoFiscal.Confirmado,
                referenciaExterna: "REF-RECUPERADA");
        });
        var service = new DocumentoFiscalEmisionService(db, new[] { proveedor });
        var solicitud = CrearSolicitud("HN", "PROV-A", "FACTURA", "idem-retry", "snapshot-retry");

        var error = await Assert.ThrowsAsync<DocumentoFiscalProveedorNoDisponibleException>(
            () => service.EmitirAsync(solicitud));
        Assert.NotNull(error.RegistroId);

        var recuperado = await service.EmitirAsync(solicitud);

        Assert.Equal(EstadoResultadoFiscal.Confirmado, recuperado.Estado);
        Assert.True(recuperado.Idempotente);
        Assert.True(recuperado.ReintentoAceptado);
        Assert.Equal(2, proveedor.Emisiones);
    }

    [Fact]
    public async Task EmitirAsync_SinAdaptadorCompatibleFallaControladoSinCrearClaim()
    {
        await using var db = CrearContexto();
        var service = new DocumentoFiscalEmisionService(db, Array.Empty<IProveedorDocumentoFiscal>());

        await Assert.ThrowsAsync<DocumentoFiscalProveedorNoDisponibleException>(() =>
            service.EmitirAsync(CrearSolicitud("HN", "NO-CONFIG", "FACTURA", "idem-x", "snapshot-x")));

        Assert.Empty(await db.Set<DocumentoFiscalEmision>().ToListAsync());
    }

    private static SolicitudEmisionFiscal CrearSolicitud(
        string jurisdiccion,
        string proveedor,
        string tipoDocumento,
        string idempotencia,
        string snapshot,
        int facturaId = 10)
    {
        return new SolicitudEmisionFiscal(
            new PerfilFiscalDocumento(
                empresaId: 7,
                sucursalId: 3,
                jurisdiccion,
                proveedor,
                tipoDocumento),
            facturaId,
            idempotencia,
            snapshot);
    }

    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"fiscal-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private sealed class ProveedorFiscalFake : IProveedorDocumentoFiscal
    {
        private readonly string _jurisdiccion;
        private readonly Func<SolicitudEmisionFiscal, ResultadoEmisionFiscal> _emitir;

        public ProveedorFiscalFake(
            string codigo,
            string jurisdiccion,
            Func<SolicitudEmisionFiscal, ResultadoEmisionFiscal> emitir)
        {
            Codigo = codigo;
            _jurisdiccion = jurisdiccion;
            _emitir = emitir;
        }

        public string Codigo { get; }
        public int Emisiones { get; private set; }

        public bool Soporta(PerfilFiscalDocumento perfil) =>
            string.Equals(perfil.Jurisdiccion, _jurisdiccion, StringComparison.OrdinalIgnoreCase);

        public Task<ResultadoEmisionFiscal> EmitirAsync(
            SolicitudEmisionFiscal solicitud,
            CancellationToken cancellationToken = default)
        {
            Emisiones++;
            return Task.FromResult(_emitir(solicitud));
        }

        public Task<ResultadoEmisionFiscal> ConsultarAsync(
            PerfilFiscalDocumento perfil,
            string referenciaExterna,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ResultadoEmisionFiscal(
                EstadoResultadoFiscal.Confirmado,
                referenciaExterna));
    }
}
