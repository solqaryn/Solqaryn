using Solqaryn.API.Controllers;
using Solqaryn.Application.Common;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

// C09-04.M05.02: contrato REAL del controlador con servicio simulado.
// No ejecuta filtros ASP.NET, JWT firmado, MySQL ni proveedor externo.
public sealed class C0904M0502OutboxApiContractTests
{
    private static readonly RegistrarMensajeOutboxRequest Request = new(
        "crm.mensaje.preparado", "{\"canal\":\"email\"}", "Campana", "10", "corr-m0502");

    [Fact]
    public async Task TenantA_ContratoAceptadoDevuelveDtoYSoloUnaInvocacion()
    {
        await using var db = NuevaDb();
        var staged = MensajeOutbox.Crear(7, Request.TipoEvento, Request.PayloadJson,
            "key-1", Request.TipoAgregado, Request.IdAgregado, Request.CorrelationId);
        var servicio = new ServicioObservado(staged);
        var controller = Controlador(servicio, db, "7");

        var result = await controller.Registrar(7, "  key-1  ", Request, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        var body = Assert.IsType<ApiResponse<MensajeOutboxRegistroResponse>>(accepted.Value);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(staged.EventoId, body.Data.EventoId);
        Assert.Equal(7, body.Data.EmpresaId);
        Assert.Equal(staged.Estado.ToString(), body.Data.Estado);
        Assert.Equal("key-1", servicio.UltimaClave);
        Assert.Equal(1, servicio.Invocaciones);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HeaderIdempotenciaVacioRechaza400SinInvocarServicio(string? clave)
    {
        await using var db = NuevaDb();
        var servicio = new ServicioObservado(new InvalidOperationException("secreto-proveedor"));
        var result = await Controlador(servicio, db, "7")
            .Registrar(7, clave, Request, CancellationToken.None);

        VerificarProblema(result, StatusCodes.Status400BadRequest, "OUTBOX_IDEMPOTENCY_KEY_REQUIRED");
        Assert.Equal(0, servicio.Invocaciones);
    }

    [Fact]
    public async Task EmpresaRutaBConContextoTenantANuncaInvocaServicio()
    {
        await using var db = NuevaDb();
        var servicio = new ServicioObservado(new InvalidOperationException("PII_empresa_B"));
        var result = await Controlador(servicio, db, "7")
            .Registrar(8, "key-b", Request, CancellationToken.None);

        VerificarProblema(result, StatusCodes.Status403Forbidden, "OUTBOX_TENANT_CONTEXT_MISMATCH");
        Assert.Equal(0, servicio.Invocaciones);
    }

    [Fact]
    public async Task ConflictoDeClaveMapea409SinExponerDetalleInterno()
    {
        await VerificarErrorServicio(new ConflictException("provider-connection-secret"),
            StatusCodes.Status409Conflict, "OUTBOX_IDEMPOTENCY_CONFLICT");
    }

    [Fact]
    public async Task MembresiaRevocadaMapea403SinExponerDetalleInterno()
    {
        await VerificarErrorServicio(new ForbiddenAccessException("PII_usuario_empresa_B"),
            StatusCodes.Status403Forbidden, "OUTBOX_TENANT_FORBIDDEN");
    }

    [Fact]
    public async Task DtoInvalidoMapea400SinExponerDetalleInterno()
    {
        await VerificarErrorServicio(new ArgumentException("smtp-password-secret"),
            StatusCodes.Status400BadRequest, "OUTBOX_INVALID_REQUEST");
    }

    private static async Task VerificarErrorServicio(Exception exception, int status, string code)
    {
        await using var db = NuevaDb();
        var servicio = new ServicioObservado(exception);
        var result = await Controlador(servicio, db, "7")
            .Registrar(7, "key-error", Request, CancellationToken.None);

        var problem = VerificarProblema(result, status, code);
        Assert.DoesNotContain(exception.Message, problem.Detail ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, servicio.Invocaciones);
    }

    private static ProblemDetails VerificarProblema(IActionResult result, int status, string code)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(status, objectResult.StatusCode);
        Assert.Contains("application/problem+json", objectResult.ContentTypes);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(status, problem.Status);
        Assert.Equal(code, problem.Extensions["code"]);
        Assert.Equal("/api/outbox/tenants/7", problem.Instance);
        Assert.True(problem.Extensions.ContainsKey("traceId"));
        return problem;
    }

    private static MensajesOutboxController Controlador(
        IMensajeOutboxService servicio, AppDbContext db, string tenant)
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Empresa-Id"] = tenant;
        http.Request.Path = "/api/outbox/tenants/7";
        return new MensajesOutboxController(servicio, db)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
    }

    private static AppDbContext NuevaDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"c0904m0502-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private sealed class ServicioObservado : IMensajeOutboxService
    {
        private readonly MensajeOutbox? _mensaje;
        private readonly Exception? _error;
        public int Invocaciones { get; private set; }
        public string? UltimaClave { get; private set; }

        public ServicioObservado(MensajeOutbox mensaje) => _mensaje = mensaje;
        public ServicioObservado(Exception error) => _error = error;

        public Task<MensajeOutbox> RegistrarAsync(int empresaId,
            RegistrarMensajeOutboxRequest request, string claveIdempotencia,
            CancellationToken cancellationToken = default)
        {
            Invocaciones++;
            UltimaClave = claveIdempotencia;
            return _error is not null
                ? Task.FromException<MensajeOutbox>(_error)
                : Task.FromResult(_mensaje!);
        }
    }
}
