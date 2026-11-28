using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QaCheckoutPagoOnlineTenantTests
{
    [Fact]
    public async Task PagosOnline_NoAceptaEmpresaDistintaALaAutorizadaPorRBAC()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"qa-payment-{Guid.NewGuid():N}").Options;
        var http = new DefaultHttpContext();
        http.Items["SOLQARYN_AUTHORIZED_EMPRESA_ID"] = 7;
        http.Request.Headers["X-Empresa-Id"] = "8";
        await using var db = new AppDbContext(options, httpContextAccessor: new HttpContextAccessor { HttpContext = http });
        var facturas = new Mock<IFacturaRepository>(MockBehavior.Strict);
        var usuario = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var scope = new Mock<IUsuarioScopeService>(MockBehavior.Strict);
        var controller = new PagosOnlineController(db, facturas.Object, Array.Empty<IPagoOnlineProvider>(), usuario.Object, scope.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.Iniciar(
            new IniciarPagoOnlineDto(8, 10, "provider-test", 10m, "HNL"), CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.Obtener(10, 8, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => controller.Listar(8));
        scope.VerifyNoOtherCalls();
        facturas.VerifyNoOtherCalls();
    }
}
