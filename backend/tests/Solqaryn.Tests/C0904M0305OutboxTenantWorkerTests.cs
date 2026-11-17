using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Xunit;

namespace Solqaryn.Tests;

public sealed class C0904M0305OutboxTenantWorkerTests
{
    private static readonly DateTime AhoraUtc = new(2026, 11, 17, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SinAutorizacionTenant_NoHayClaimNiDespacho()
    {
        var r = new Repositorio(Crear(7)); var g = new Gate { TenantPermitido = false };
        var d = new Despachador();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => CrearWorker(r,g,d).ProcesarAsync(7, AhoraUtc));
        Assert.Equal(0,r.Claims); Assert.Equal(0,r.Stale); Assert.Equal(0,d.Envios);
        Assert.Equal(0,g.Mensajes);
    }

    [Fact]
    public async Task Autorizado_ConfirmaUnaSolaEntrega()
    {
        var r = new Repositorio(Crear(7)); var g = new Gate(); var d = new Despachador();
        var resultado = await CrearWorker(r,g,d).ProcesarAsync(7, AhoraUtc);
        Assert.Equal(1,resultado.Entregados); Assert.Equal(1,resultado.Reclamados);
        Assert.Equal(1,d.Envios); Assert.Equal(1,r.Confirmaciones); Assert.Equal(7,g.UltimaEmpresa);
    }

    [Fact]
    public async Task ConsentimientoRevocado_NoInvocaProveedorYErrorEsOpaco()
    {
        var r = new Repositorio(Crear(7)); var g = new Gate { MensajePermitido = false };
        var d = new Despachador();
        var resultado = await CrearWorker(r,g,d).ProcesarAsync(7, AhoraUtc);
        Assert.Equal(0,d.Envios); Assert.Equal(0,r.Confirmaciones);
        Assert.Equal(1,r.Fallos); Assert.Equal("DELIVERY_FAILED",r.ErrorSeguro);
        Assert.Equal(1,resultado.Reprogramados);
    }

    [Fact]
    public async Task LoteTenantB_ImpideTodosLosEnviosInclusoDeA()
    {
        var r = new Repositorio(Crear(7),Crear(8)); var g = new Gate(); var d = new Despachador();
        await Assert.ThrowsAsync<InvalidOperationException>(() => CrearWorker(r,g,d).ProcesarAsync(7,AhoraUtc));
        Assert.Equal(0,d.Envios); Assert.Equal(0,r.Confirmaciones); Assert.Equal(0,g.Mensajes);
    }

    [Fact]
    public async Task CancelacionYEntradasInvalidas_NoHacenClaim()
    {
        var r = new Repositorio(Crear(7)); var g = new Gate(); var d = new Despachador();
        var worker=CrearWorker(r,g,d); using var cts=new CancellationTokenSource(); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>worker.ProcesarAsync(7,AhoraUtc,cts.Token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(()=>worker.ProcesarAsync(0,AhoraUtc));
        await Assert.ThrowsAsync<ArgumentException>(()=>worker.ProcesarAsync(
            7,DateTime.SpecifyKind(AhoraUtc,DateTimeKind.Unspecified)));
        Assert.Equal(0,r.Claims); Assert.Equal(0,r.Stale); Assert.Equal(0,d.Envios);
    }


    [Fact]
    public async Task M0501_ClaveIdempotenciaRepetida_NoDespachaNiConfirma()
    {
        // Dos eventos distintos del mismo tenant con clave repetida:
        // el lote completo se rechaza antes del primer efecto externo.
        var r = new Repositorio(Crear(7, "clave-repetida"), Crear(7, "clave-repetida"));
        var g = new Gate(); var d = new Despachador();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CrearWorker(r, g, d).ProcesarAsync(7, AhoraUtc));

        Assert.Equal(1, r.Claims);
        Assert.Equal(0, d.Envios);
        Assert.Equal(0, r.Confirmaciones);
        Assert.Equal(0, r.Fallos);
        Assert.Equal(0, g.Mensajes);
    }

    [Fact]
    public async Task M0501_RevocacionEntreDosMensajes_ImpideSegundoEnvioYFallaOpaco()
    {
        // Autorizar solo el primer mensaje simula consentimiento revocado
        // despues de la primera entrega, antes de ejecutar el segundo efecto.
        var r = new Repositorio(Crear(7, "clave-primera"), Crear(7, "clave-segunda"));
        var g = new Gate { PermitirHasta = 1 }; var d = new Despachador();

        var resultado = await CrearWorker(r, g, d).ProcesarAsync(7, AhoraUtc);

        Assert.Equal(2, resultado.Reclamados);
        Assert.Equal(1, resultado.Entregados);
        Assert.Equal(1, resultado.Reprogramados);
        Assert.Equal(0, resultado.DeadLetter);
        Assert.Equal(2, g.Mensajes);
        Assert.Equal(1, d.Envios);
        Assert.Equal(1, r.Confirmaciones);
        Assert.Equal(1, r.Fallos);
        Assert.Equal("DELIVERY_FAILED", r.ErrorSeguro);
    }

    private static OutboxTenantDeliveryWorker CrearWorker(Repositorio r,Gate g,Despachador d) =>
        new(new OutboxRetryProcessor(r,new OutboxRetryProcessorOptions(JitterRatio:0),()=>0.5),g,d);

    private static MensajeOutbox Crear(int empresaId, string? clave = null)
    {
        var m=MensajeOutbox.Crear(empresaId,"correo.notificacion","{\"test\":true}",
            (clave ?? $"idempotency-{empresaId}"),creadoEnUtc:AhoraUtc.AddMinutes(-1));
        m.MarcarProcesando(AhoraUtc);
        return m;
    }

    private sealed class Gate : IOutboxTenantDeliveryGate
    {
        public bool TenantPermitido {get;init;}=true;
        public bool MensajePermitido {get;init;}=true;
        public int PermitirHasta {get;init;}=int.MaxValue;
        public int Mensajes {get;private set;}
        public int UltimaEmpresa {get;private set;}
        public Task<bool> AutorizarTenantAsync(int empresaId,CancellationToken cancellationToken=default)
        {UltimaEmpresa=empresaId;return Task.FromResult(TenantPermitido);}
        public Task<bool> AutorizarMensajeAsync(int empresaId,Guid eventoId,string tipoEvento,
            CancellationToken cancellationToken=default)
        {Mensajes++;UltimaEmpresa=empresaId;return Task.FromResult(MensajePermitido && Mensajes <= PermitirHasta);}
    }
    private sealed class Despachador : IOutboxEffectDispatcher
    {
        public int Envios {get;private set;}
        public Task DispatchAsync(MensajeOutbox mensaje,CancellationToken cancellationToken=default)
        {Envios++;return Task.CompletedTask;}
    }
    private sealed class Repositorio : IMensajeOutboxRepository
    {
        private readonly IReadOnlyList<MensajeOutbox> _lista;
        public Repositorio(params MensajeOutbox[] lista)=>_lista=lista;
        public int Claims {get;private set;}
        public int Stale {get;private set;}
        public int Confirmaciones {get;private set;}
        public int Fallos {get;private set;}
        public string? ErrorSeguro {get;private set;}
        public Task AddAsync(MensajeOutbox m,CancellationToken ct=default)=>Task.CompletedTask;
        public Task<MensajeOutbox?> GetByEventoIdAsync(int e,Guid id,CancellationToken ct=default)=>
            Task.FromResult<MensajeOutbox?>(null);
        public Task<bool> ExisteClaveIdempotenciaAsync(int e,string clave,CancellationToken ct=default)=>
            Task.FromResult(false);
        public Task<int> RecuperarProcesandoStaleAsync(int e,DateTime stale,DateTime ahora,
            int n,CancellationToken ct=default){Stale++;return Task.FromResult(0);}
        public Task<IReadOnlyList<MensajeOutbox>> ClaimDisponiblesAsync(int e,DateTime ahora,
            int n,CancellationToken ct=default){Claims++;return Task.FromResult(_lista);}
        public Task<bool> MarcarEntregadoAsync(int e,int id,int intento,DateTime ahora,
            CancellationToken ct=default){Confirmaciones++;return Task.FromResult(true);}
        public Task<bool> RegistrarFalloAsync(int e,int id,int intento,string error,DateTime ahora,
            DateTime disponible,bool dead,CancellationToken ct=default)
        {Fallos++;ErrorSeguro=error;return Task.FromResult(true);}
    }
}
