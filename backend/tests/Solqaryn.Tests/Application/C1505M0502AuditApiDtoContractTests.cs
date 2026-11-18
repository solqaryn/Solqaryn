using System.Reflection;
using Moq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.Application;

/// <summary>
/// C15-05.M05.02: executable controller/DTO contract; direct invocation only.
/// It cannot replace hosted HTTP/JWT/permission-middleware verification.
/// </summary>
public sealed class C1505M0502AuditApiDtoContractTests
{
    private static Mock<ICurrentUserService> User(bool admin)
    {
        var user = new Mock<ICurrentUserService>(MockBehavior.Strict);
        user.SetupGet(x => x.EsAdministrador).Returns(admin);
        return user;
    }

    private static Mock<IAuditoriaService> Reader(PagedResult<RegistroAuditoriaDto> page)
    {
        var service = new Mock<IAuditoriaService>(MockBehavior.Strict);
        service.Setup(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()))
            .ReturnsAsync(page);
        return service;
    }

    [Fact]
    public void ControllerDeclaresRouteAuthenticationPermissionAndGet()
    {
        var type = typeof(AuditoriaController);
        Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>());
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("auditoria", type.GetCustomAttribute<RouteAttribute>()?.Template);
        var permission = type.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(permission);
        Assert.Equal(ModuloSistema.Auditoria, permission.Modulo);
        Assert.Equal(AccionPermiso.Ver, permission.Accion);
        Assert.NotNull(type.GetMethod(nameof(AuditoriaController.GetFiltered))?
            .GetCustomAttribute<HttpGetAttribute>());
    }

    [Fact]
    public async Task NonAdministratorGetsForbidWithoutReadingSensitiveAudit()
    {
        var service = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var controller = new AuditoriaController(service.Object, User(false).Object);
        var result = await controller.GetFiltered(new AuditoriaFiltroDto());
        Assert.IsType<ForbidResult>(result);
        service.Verify(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()), Times.Never);
    }

    [Fact]
    public async Task MaliciousQueryCannotCauseAuditReadForNonAdministrator()
    {
        var service = new Mock<IAuditoriaService>(MockBehavior.Strict);
        var controller = new AuditoriaController(service.Object, User(false).Object);
        var result = await controller.GetFiltered(new AuditoriaFiltroDto
        {
            Texto = "tenant-B;secret=private",
            UsuarioId = int.MaxValue,
            PageSize = int.MaxValue
        });
        Assert.IsType<ForbidResult>(result);
        service.Verify(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()), Times.Never);
    }

    [Fact]
    public async Task AdministratorReceivesTypedPagedSuccessEnvelope()
    {
        var page = new PagedResult<RegistroAuditoriaDto>
        {
            Items = new List<RegistroAuditoriaDto>
            {
                new() { Id = 7, Modulo = "Auditoria", Accion = "Ver" }
            },
            Page = 2, PageSize = 1, TotalCount = 3
        };
        var service = Reader(page);
        var controller = new AuditoriaController(service.Object, User(true).Object);
        var ok = Assert.IsType<OkObjectResult>(
            await controller.GetFiltered(new AuditoriaFiltroDto { Page = 2, PageSize = 1 }));
        var result = Assert.IsType<ApiResponse<PagedResult<RegistroAuditoriaDto>>>(ok.Value);
        Assert.True(result.Success);
        Assert.Same(page, result.Data);
        Assert.Equal(3, result.Data!.TotalPages);
        Assert.Equal(7, Assert.Single(result.Data.Items).Id);
        service.Verify(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()), Times.Once);
    }

    [Fact]
    public async Task EmptyAuditPageStaysEmptyAndTyped()
    {
        var page = new PagedResult<RegistroAuditoriaDto>
        {
            Page = 1, PageSize = 50, TotalCount = 0
        };
        var controller = new AuditoriaController(Reader(page).Object, User(true).Object);
        var ok = Assert.IsType<OkObjectResult>(
            await controller.GetFiltered(new AuditoriaFiltroDto()));
        var response = Assert.IsType<ApiResponse<PagedResult<RegistroAuditoriaDto>>>(ok.Value);
        Assert.True(response.Success);
        Assert.Empty(response.Data!.Items);
        Assert.Equal(0, response.Data.TotalPages);
    }

    [Fact]
    public async Task FiltersAreForwardedByReferenceWithoutTenantRewriting()
    {
        var page = new PagedResult<RegistroAuditoriaDto> { Page = 3, PageSize = 7 };
        var service = Reader(page);
        var filter = new AuditoriaFiltroDto
        {
            Page = 3, PageSize = 7, Modulo = "Inventario",
            Resultado = "Error", Texto = "corr-77", UsuarioId = 12
        };
        var controller = new AuditoriaController(service.Object, User(true).Object);
        Assert.IsType<OkObjectResult>(await controller.GetFiltered(filter));
        service.Verify(x => x.GetFilteredAsync(
            It.Is<AuditoriaFiltroDto>(v => ReferenceEquals(v, filter)
                && v.Page == 3 && v.PageSize == 7
                && v.Resultado == "Error" && v.Texto == "corr-77")), Times.Once);
    }

    [Fact]
    public void DtoDefinesDocumentedPaginationDefaultsAndOptionalFilters()
    {
        var dto = new AuditoriaFiltroDto();
        Assert.Equal(1, dto.Page);
        Assert.Equal(50, dto.PageSize);
        Assert.Null(dto.UsuarioId);
        Assert.Null(dto.Desde);
        Assert.Null(dto.Hasta);
        Assert.Null(dto.Texto);
    }

    [Fact]
    public async Task ExtremePaginationIsForwardedAndNotMistakenForValidation()
    {
        var page = new PagedResult<RegistroAuditoriaDto>
        {
            Page = int.MaxValue, PageSize = 1, TotalCount = 0
        };
        var filter = new AuditoriaFiltroDto { Page = int.MaxValue, PageSize = 1 };
        var service = Reader(page);
        var controller = new AuditoriaController(service.Object, User(true).Object);
        Assert.IsType<OkObjectResult>(await controller.GetFiltered(filter));
        service.Verify(x => x.GetFilteredAsync(
            It.Is<AuditoriaFiltroDto>(f => f.Page == int.MaxValue && f.PageSize == 1)),
            Times.Once);
    }

    [Fact]
    public async Task ServiceFailureCannotBeCountedAsSuccessfulDtoEnvelope()
    {
        var service = new Mock<IAuditoriaService>(MockBehavior.Strict);
        service.Setup(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()))
            .ThrowsAsync(new InvalidOperationException("INTERNAL_TEST_SENTINEL"));
        var controller = new AuditoriaController(service.Object, User(true).Object);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => controller.GetFiltered(new AuditoriaFiltroDto()));
        service.Verify(x => x.GetFilteredAsync(It.IsAny<AuditoriaFiltroDto>()), Times.Once);
    }
}
