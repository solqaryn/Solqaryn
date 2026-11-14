using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N19TrazabilidadCorrelationAuditTests
{
    [Fact]
    public async Task Auditoria_estricta_usa_TraceIdentifier_y_no_header_correlation_bruto()
    {
        var rawClientCorrelation = "cliente-no-confiable-123";
        var traceSaneado = "srv-01JXYZSAFE";
        var http = new DefaultHttpContext { TraceIdentifier = traceSaneado };
        http.Request.Headers["X-Correlation-ID"] = rawClientCorrelation;

        RegistroAuditoria? capturado = null;
        var repo = new Mock<IAuditoriaRepository>();
        repo.Setup(x => x.AddAsync(It.IsAny<RegistroAuditoria>()))
            .Callback<RegistroAuditoria>(r => capturado = r)
            .Returns(Task.CompletedTask);
        repo.Setup(x => x.SaveChangesAsync()).ReturnsAsync(true);

        var current = new Mock<ICurrentUserService>();
        current.SetupGet(x => x.UsuarioId).Returns(99);
        current.SetupGet(x => x.NombreUsuario).Returns("n19-correlation-test");
        var accessor = new HttpContextAccessor { HttpContext = http };
        var logger = new Mock<ILogger<AuditoriaService>>();
        var service = new AuditoriaService(repo.Object, current.Object, accessor, logger.Object);

        await service.RegistrarEstrictoAsync(
            ModuloSistema.MovimientosInventario,
            AccionPermiso.Crear,
            "Lote de inventario registrado.",
            referenciaId: 7,
            entidad: "LoteInventario");

        Assert.NotNull(capturado);
        Assert.Equal(traceSaneado, capturado!.CorrelationId);
        Assert.NotEqual(rawClientCorrelation, capturado.CorrelationId);
        repo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
