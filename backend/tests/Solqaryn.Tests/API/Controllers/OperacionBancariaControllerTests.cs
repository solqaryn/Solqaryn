using System.Security.Claims;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.DTOs.Bancos;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

public sealed class OperacionBancariaControllerTests
{
    private readonly Mock<IOperacionBancariaService> _serviceMock;
    private readonly OperacionBancariaController _controller;

    public OperacionBancariaControllerTests()
    {
        _serviceMock = new Mock<IOperacionBancariaService>();
        _controller = new OperacionBancariaController(_serviceMock.Object);
    }

    [Fact]
    public void Controller_ExigeAutenticacionYRutaCanonica()
    {
        var type = typeof(OperacionBancariaController);
        Assert.Single(type.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true));
        Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Cast<RouteAttribute>());
        Assert.Equal("operaciones-bancarias", route.Template);
    }

    [Theory]
    [InlineData(nameof(OperacionBancariaController.RegistrarDeposito), "deposito")]
    [InlineData(nameof(OperacionBancariaController.RegistrarRetiro), "retiro")]
    [InlineData(nameof(OperacionBancariaController.RegistrarTransferencia), "transferencia")]
    [InlineData(nameof(OperacionBancariaController.RegistrarComision), "comision")]
    [InlineData(nameof(OperacionBancariaController.RegistrarInteres), "interes")]
    [InlineData(nameof(OperacionBancariaController.RegistrarConciliacion), "conciliacion")]
    public void Acciones_TienenRutaPostYPermisoCrear(string methodName, string expectedTemplate)
    {
        var methodInfo = typeof(OperacionBancariaController).GetMethod(methodName);
        Assert.NotNull(methodInfo);

        var http = Assert.Single(methodInfo!.GetCustomAttributes(typeof(HttpPostAttribute), inherit: false).Cast<HttpPostAttribute>());
        Assert.Equal(expectedTemplate, http.Template);
        Assert.Single(methodInfo.GetCustomAttributes(typeof(RequierePermisoAttribute), inherit: false));
    }

    [Theory]
    [InlineData(nameof(OperacionBancariaController.RegistrarDeposito), typeof(DepositoBancarioDto))]
    [InlineData(nameof(OperacionBancariaController.RegistrarRetiro), typeof(RetiroBancarioDto))]
    [InlineData(nameof(OperacionBancariaController.RegistrarTransferencia), typeof(TransferenciaBancariaDto))]
    [InlineData(nameof(OperacionBancariaController.RegistrarComision), typeof(ComisionBancariaDto))]
    [InlineData(nameof(OperacionBancariaController.RegistrarInteres), typeof(InteresBancarioDto))]
    [InlineData(nameof(OperacionBancariaController.RegistrarConciliacion), typeof(ConciliacionBancariaDto))]
    public async Task TodasLasAcciones_SinClaim_OIdentidadInvalida_FallaCerrado(string methodName, Type dtoType)
    {
        var methodInfo = typeof(OperacionBancariaController).GetMethod(methodName);
        Assert.NotNull(methodInfo);
        var dto = Activator.CreateInstance(dtoType);

        var scenarios = new ClaimsPrincipal[]
        {
            new ClaimsPrincipal(new ClaimsIdentity()),
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "invalid") }, "test")),
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "0") }, "test")),
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "-1") }, "test"))
        };

        foreach (var principal in scenarios)
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var result = await (Task<IActionResult>)methodInfo!.Invoke(_controller, new[] { dto })!;

            Assert.IsType<UnauthorizedResult>(result);
            _serviceMock.VerifyNoOtherCalls();
        }
    }

    [Theory]
    [InlineData(nameof(OperacionBancariaController.RegistrarDeposito), typeof(DepositoBancarioDto), typeof(ArgumentException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarRetiro), typeof(RetiroBancarioDto), typeof(ArgumentException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarTransferencia), typeof(TransferenciaBancariaDto), typeof(ArgumentException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarComision), typeof(ComisionBancariaDto), typeof(ArgumentException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarInteres), typeof(InteresBancarioDto), typeof(ArgumentException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarConciliacion), typeof(ConciliacionBancariaDto), typeof(ArgumentException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarDeposito), typeof(DepositoBancarioDto), typeof(FluentValidation.ValidationException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarRetiro), typeof(RetiroBancarioDto), typeof(FluentValidation.ValidationException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarTransferencia), typeof(TransferenciaBancariaDto), typeof(FluentValidation.ValidationException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarComision), typeof(ComisionBancariaDto), typeof(FluentValidation.ValidationException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarInteres), typeof(InteresBancarioDto), typeof(FluentValidation.ValidationException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarConciliacion), typeof(ConciliacionBancariaDto), typeof(FluentValidation.ValidationException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarDeposito), typeof(DepositoBancarioDto), typeof(Solqaryn.Application.Exceptions.BusinessRuleException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarRetiro), typeof(RetiroBancarioDto), typeof(Solqaryn.Application.Exceptions.BusinessRuleException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarTransferencia), typeof(TransferenciaBancariaDto), typeof(Solqaryn.Application.Exceptions.BusinessRuleException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarComision), typeof(ComisionBancariaDto), typeof(Solqaryn.Application.Exceptions.BusinessRuleException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarInteres), typeof(InteresBancarioDto), typeof(Solqaryn.Application.Exceptions.BusinessRuleException))]
    [InlineData(nameof(OperacionBancariaController.RegistrarConciliacion), typeof(ConciliacionBancariaDto), typeof(Solqaryn.Application.Exceptions.BusinessRuleException))]
    public async Task TodasLasAcciones_CuandoLanzaExcepcionDeValidacion_FallaCerrado(string methodName, Type dtoType, Type exceptionType)
    {
        var methodInfo = typeof(OperacionBancariaController).GetMethod(methodName);
        Assert.NotNull(methodInfo);
        var dto = Activator.CreateInstance(dtoType);
        const int usuarioId = 1;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "test"))
            }
        };

        var exceptionInstance = exceptionType == typeof(FluentValidation.ValidationException)
            ? new FluentValidation.ValidationException("Simulated validation")
            : (Exception)Activator.CreateInstance(exceptionType, "Simulated exception")!;

        if (dto is DepositoBancarioDto deposito)
            _serviceMock.Setup(s => s.RegistrarDepositoAsync(deposito, usuarioId)).ThrowsAsync(exceptionInstance);
        else if (dto is RetiroBancarioDto retiro)
            _serviceMock.Setup(s => s.RegistrarRetiroAsync(retiro, usuarioId)).ThrowsAsync(exceptionInstance);
        else if (dto is TransferenciaBancariaDto transferencia)
            _serviceMock.Setup(s => s.RegistrarTransferenciaAsync(transferencia, usuarioId)).ThrowsAsync(exceptionInstance);
        else if (dto is ComisionBancariaDto comision)
            _serviceMock.Setup(s => s.RegistrarComisionAsync(comision, usuarioId)).ThrowsAsync(exceptionInstance);
        else if (dto is InteresBancarioDto interes)
            _serviceMock.Setup(s => s.RegistrarInteresAsync(interes, usuarioId)).ThrowsAsync(exceptionInstance);
        else if (dto is ConciliacionBancariaDto conciliacion)
            _serviceMock.Setup(s => s.RegistrarConciliacionAsync(conciliacion, usuarioId)).ThrowsAsync(exceptionInstance);

        var middleware = new Solqaryn.API.Middleware.ExceptionHandlingMiddleware(
            _ => (Task)methodInfo!.Invoke(_controller, new[] { dto })!,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<Solqaryn.API.Middleware.ExceptionHandlingMiddleware>>());

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new System.IO.MemoryStream();
        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task RegistrarDeposito_SinClaimUsuario_FallaCerradoYNoInvocaServicio()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var result = await _controller.RegistrarDeposito(new DepositoBancarioDto());

        Assert.IsType<UnauthorizedResult>(result);
        _serviceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegistrarDeposito_ConClaimValido_PropagaUsuarioAlServicio()
    {
        const int usuarioId = 37;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString())
                }, "test"))
            }
        };
        var dto = new DepositoBancarioDto();

        var result = await _controller.RegistrarDeposito(dto);

        Assert.IsType<OkResult>(result);
        _serviceMock.Verify(s => s.RegistrarDepositoAsync(dto, usuarioId), Times.Once);
    }

    [Fact]
    public async Task RegistrarRetiro_ConClaimValido_PropagaUsuarioAlServicio()
    {
        const int usuarioId = 37;
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "test")) } };
        var dto = new RetiroBancarioDto();

        var result = await _controller.RegistrarRetiro(dto);

        Assert.IsType<OkResult>(result);
        _serviceMock.Verify(s => s.RegistrarRetiroAsync(dto, usuarioId), Times.Once);
    }

    [Fact]
    public async Task RegistrarTransferencia_ConClaimValido_PropagaUsuarioAlServicio()
    {
        const int usuarioId = 37;
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "test")) } };
        var dto = new TransferenciaBancariaDto();

        var result = await _controller.RegistrarTransferencia(dto);

        Assert.IsType<OkResult>(result);
        _serviceMock.Verify(s => s.RegistrarTransferenciaAsync(dto, usuarioId), Times.Once);
    }

    [Fact]
    public async Task RegistrarComision_ConClaimValido_PropagaUsuarioAlServicio()
    {
        const int usuarioId = 37;
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "test")) } };
        var dto = new ComisionBancariaDto();

        var result = await _controller.RegistrarComision(dto);

        Assert.IsType<OkResult>(result);
        _serviceMock.Verify(s => s.RegistrarComisionAsync(dto, usuarioId), Times.Once);
    }

    [Fact]
    public async Task RegistrarInteres_ConClaimValido_PropagaUsuarioAlServicio()
    {
        const int usuarioId = 37;
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "test")) } };
        var dto = new InteresBancarioDto();

        var result = await _controller.RegistrarInteres(dto);

        Assert.IsType<OkResult>(result);
        _serviceMock.Verify(s => s.RegistrarInteresAsync(dto, usuarioId), Times.Once);
    }

    [Fact]
    public async Task RegistrarConciliacion_ConClaimValido_PropagaUsuarioAlServicio()
    {
        const int usuarioId = 37;
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "test")) } };
        var dto = new ConciliacionBancariaDto();

        var result = await _controller.RegistrarConciliacion(dto);

        Assert.IsType<OkResult>(result);
        _serviceMock.Verify(s => s.RegistrarConciliacionAsync(dto, usuarioId), Times.Once);
    }

    [Theory]
    [InlineData(nameof(OperacionBancariaController.RegistrarDeposito), typeof(DepositoBancarioDto))]
    [InlineData(nameof(OperacionBancariaController.RegistrarRetiro), typeof(RetiroBancarioDto))]
    [InlineData(nameof(OperacionBancariaController.RegistrarTransferencia), typeof(TransferenciaBancariaDto))]
    [InlineData(nameof(OperacionBancariaController.RegistrarComision), typeof(ComisionBancariaDto))]
    [InlineData(nameof(OperacionBancariaController.RegistrarInteres), typeof(InteresBancarioDto))]
    [InlineData(nameof(OperacionBancariaController.RegistrarConciliacion), typeof(ConciliacionBancariaDto))]
    public async Task TodasLasAcciones_CuandoLanzaConflictException_RetornaConflict(string methodName, Type dtoType)
    {
        var methodInfo = typeof(OperacionBancariaController).GetMethod(methodName);
        Assert.NotNull(methodInfo);
        var dto = Activator.CreateInstance(dtoType);
        const int usuarioId = 1;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) }, "test"))
            }
        };

        var exceptionInstance = new Solqaryn.Application.Exceptions.ConflictException("Simulated conflict");

        if (dto is DepositoBancarioDto deposito)
            _serviceMock.Setup(s => s.RegistrarDepositoAsync(deposito, usuarioId)).ThrowsAsync(exceptionInstance);
        else if (dto is RetiroBancarioDto retiro)
            _serviceMock.Setup(s => s.RegistrarRetiroAsync(retiro, usuarioId)).ThrowsAsync(exceptionInstance);
        else if (dto is TransferenciaBancariaDto transferencia)
            _serviceMock.Setup(s => s.RegistrarTransferenciaAsync(transferencia, usuarioId)).ThrowsAsync(exceptionInstance);
        else if (dto is ComisionBancariaDto comision)
            _serviceMock.Setup(s => s.RegistrarComisionAsync(comision, usuarioId)).ThrowsAsync(exceptionInstance);
        else if (dto is InteresBancarioDto interes)
            _serviceMock.Setup(s => s.RegistrarInteresAsync(interes, usuarioId)).ThrowsAsync(exceptionInstance);
        else if (dto is ConciliacionBancariaDto conciliacion)
            _serviceMock.Setup(s => s.RegistrarConciliacionAsync(conciliacion, usuarioId)).ThrowsAsync(exceptionInstance);

        var middleware = new Solqaryn.API.Middleware.ExceptionHandlingMiddleware(
            _ => (Task)methodInfo!.Invoke(_controller, new[] { dto })!,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<Solqaryn.API.Middleware.ExceptionHandlingMiddleware>>());

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new System.IO.MemoryStream();
        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
    }
}
