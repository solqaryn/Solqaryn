using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Solqaryn.Tests;

public sealed class N49FPeriodoContableSecurityRegressionTests
{
    [Fact]
    public void PeriodosContablesController_RequiresAuthorizationAndRbacMetadata()
    {
        var controllerType = typeof(PeriodosContablesController);

        var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttribute);

        var apiControllerAttribute = controllerType.GetCustomAttribute<ApiControllerAttribute>();
        Assert.NotNull(apiControllerAttribute);

        var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(routeAttribute);
        Assert.Equal("api/periodos-contables", routeAttribute.Template);

        var getAllMethod = controllerType.GetMethod(nameof(PeriodosContablesController.GetAll));
        Assert.NotNull(getAllMethod);
        var getAllRbac = getAllMethod.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(getAllRbac);

        var getByIdMethod = controllerType.GetMethod(nameof(PeriodosContablesController.GetById));
        Assert.NotNull(getByIdMethod);
        var getByIdRbac = getByIdMethod.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(getByIdRbac);

        var createMethod = controllerType.GetMethod(nameof(PeriodosContablesController.Create));
        Assert.NotNull(createMethod);
        var createRbac = createMethod.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(createRbac);

        var cerrarMethod = controllerType.GetMethod(nameof(PeriodosContablesController.Cerrar));
        Assert.NotNull(cerrarMethod);
        var cerrarRbac = cerrarMethod.GetCustomAttribute<RequierePermisoAttribute>();
        Assert.NotNull(cerrarRbac);
    }

    [Fact]
    public async Task CerrarAsync_MissingPeriodo_ThrowsResourceNotFoundException()
    {
        var repoMock = new Mock<IPeriodoContableRepository>();
        repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>(), true, default)).ReturnsAsync((PeriodoContable?)null);
        var auditoriaMock = new Mock<IAuditoriaService>();

        var service = new PeriodoContableService(repoMock.Object, auditoriaMock.Object);

        var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.CerrarAsync(999));
        Assert.Equal("No se encontró el período contable con ID 999.", ex.Message);
    }

    [Fact]
    public async Task CerrarAsync_AlreadyClosed_ThrowsConflictException()
    {
        var periodo = new PeriodoContable(DateTime.UtcNow, DateTime.UtcNow.AddDays(30));
        periodo.Cerrar(DateTime.UtcNow);

        var repoMock = new Mock<IPeriodoContableRepository>();
        repoMock.Setup(r => r.GetByIdAsync(1, true, default)).ReturnsAsync(periodo);
        var auditoriaMock = new Mock<IAuditoriaService>();

        var service = new PeriodoContableService(repoMock.Object, auditoriaMock.Object);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => service.CerrarAsync(1));
        Assert.Equal("El período contable ya está cerrado.", ex.Message);
    }

    [Fact]
    public async Task ValidarOperacionAsync_RetroactiveChangeWithoutAuthorization_ThrowsInvalidOperationException()
    {
        var repoMock = new Mock<IPeriodoContableRepository>();
        var auditoriaMock = new Mock<IAuditoriaService>();

        var inicio = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fin = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);
        var periodo = new PeriodoContable(inicio, fin);
        periodo.Cerrar(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        var fechaOperacion = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        repoMock.Setup(r => r.GetByDateAsync(fechaOperacion, false, default)).ReturnsAsync(periodo);

        var service = new PeriodoContableService(repoMock.Object, auditoriaMock.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ValidarOperacionAsync(fechaOperacion, false));
        Assert.Equal("El período contable está cerrado; el cambio retroactivo requiere autorización explícita.", ex.Message);
    }
}
