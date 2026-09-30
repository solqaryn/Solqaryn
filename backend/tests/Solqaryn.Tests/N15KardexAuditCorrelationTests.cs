using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N15KardexAuditCorrelationTests
{
    [Fact]
    public async Task Auditoria_UsaTraceIdentifierSaneado_Y_No_HeaderBrutoDelCliente()
    {
        const string correlationSaneado = "req:kardex:abc-123";
        const string headerNoConfiable = "../../correlation inyectado";
        var context = new DefaultHttpContext
        {
            TraceIdentifier = correlationSaneado
        };
        context.Request.Headers["X-Correlation-ID"] = headerNoConfiable;

        RegistroAuditoria? capturado = null;
        var repository = new Mock<IAuditoriaRepository>();
        repository.Setup(r => r.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Callback<RegistroAuditoria>(r => capturado = r)
            .Returns(Task.CompletedTask);
        repository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(true);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(x => x.UsuarioId).Returns(7);
        currentUser.SetupGet(x => x.NombreUsuario).Returns("operador");

        var accessor = new HttpContextAccessor { HttpContext = context };
        var service = new AuditoriaService(
            repository.Object,
            currentUser.Object,
            accessor,
            Mock.Of<ILogger<AuditoriaService>>());

        await service.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Ver,
            "Consulta Kardex paginada",
            entidad: "MovimientoInventario");

        Assert.NotNull(capturado);
        Assert.Equal(correlationSaneado, capturado!.CorrelationId);
        Assert.DoesNotContain("..", capturado.CorrelationId);
        Assert.NotEqual(headerNoConfiable, capturado.CorrelationId);
        repository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
