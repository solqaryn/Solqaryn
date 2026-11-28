using Moq;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;

namespace Solqaryn.Tests;

internal static class CheckoutFiscalTestSupport
{
    internal static ICalculoService SinImpuestos()
    {
        var repo = new Mock<IImpuestoRepository>();
        repo.Setup(x => x.GetVigentesConRelacionesAsync(It.IsAny<DateTime>(), OperacionImpuesto.Venta))
            .ReturnsAsync(new List<Impuesto>());
        return new CalculoService(Mock.Of<IDescuentoRepository>(), repo.Object, Mock.Of<ICostoEnvioRepository>());
    }
}
