using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Security;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests.API.Controllers;

/// <summary>
/// I8-007.E: contrato negativo de edición de perfil comercial.
/// La entrada no puede asignar Activa, EmpresaId, roles ni flags globales.
/// Prueba unitaria y de serialización; no reemplaza JWT/HTTP o MySQL reales.
/// </summary>
public sealed class I8007EEdicionEmpresaCamposSegurosTests
{
    private const string PayloadMalicioso = """
        {
          "nombre": "Empresa A segura",
          "empresaId": 22,
          "activa": false,
          "esAdministrador": true,
          "rolGlobal": "Plataforma",
          "ambito": "Plataforma",
          "esSistema": true
        }
        """;

    private static UpdateEmpresaDto DtoDesdeJson()
        => JsonSerializer.Deserialize<UpdateEmpresaDto>(
            PayloadMalicioso, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    [Fact]
    public void I8007E_CamposPrivilegiadosNoSeEnlazanEnDtoComercial()
    {
        var dto = DtoDesdeJson();
        Assert.Equal("Empresa A segura", dto.Nombre);

        var propiedades = typeof(UpdateEmpresaDto).GetProperties()
            .Select(propiedad => propiedad.Name).ToArray();
        Assert.Single(propiedades);
        Assert.Equal(nameof(UpdateEmpresaDto.Nombre), propiedades[0]);

        using var salida = JsonDocument.Parse(JsonSerializer.Serialize(
            dto, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var propiedad = Assert.Single(salida.RootElement.EnumerateObject());
        Assert.Equal("nombre", propiedad.Name);
        Assert.Equal("Empresa A segura", propiedad.Value.GetString());
    }

    [Fact]
    public async Task I8007E_EdicionDeNombreConPayloadAdversarioPreservaEstado()
    {
        var empresa = new Empresa("Empresa A original");
        Assert.True(empresa.Activa);

        var repositorio = new Mock<IEmpresaRepository>(MockBehavior.Strict);
        repositorio.Setup(x => x.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(empresa);
        repositorio.Setup(x => x.Update(empresa));
        repositorio.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var usuario = new Mock<ICurrentUserService>();
        usuario.SetupGet(x => x.UsuarioId).Returns(17);
        usuario.SetupGet(x => x.NombreUsuario).Returns("prueba-i8007e");

        var auditoria = new Mock<IAuditoriaService>();
        auditoria.Setup(x => x.RegistrarAsync(
                It.IsAny<ModuloSistema>(),
                It.IsAny<AccionPermiso>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<object?>(),
                It.IsAny<object?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var servicio = new EmpresaService(
            repositorio.Object, usuario.Object, auditoria.Object);
        var resultado = await servicio.UpdateAsync(11, DtoDesdeJson());

        Assert.NotNull(resultado);
        Assert.Equal("Empresa A segura", empresa.Nombre);
        Assert.Equal("Empresa A segura", resultado!.Nombre);
        Assert.True(empresa.Activa);
        Assert.True(resultado.Activa);
        repositorio.Verify(x => x.GetByIdAsync(11, It.IsAny<CancellationToken>()), Times.Once);
        repositorio.Verify(x => x.Update(empresa), Times.Once);
        repositorio.Verify(x => x.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        repositorio.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task I8007E_EmpresaBNoSeEditaAunqueElPayloadElevePrivilegios()
    {
        var servicio = new Mock<IEmpresaService>(MockBehavior.Strict);
        var http = new DefaultHttpContext();
        http.Items[VerifiedCommercialTenant.AuthorizedEmpresaIdItemKey] = 11;
        http.Request.Headers["X-Empresa-Id"] = "22";
        var controlador = new EmpresasController(servicio.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            controlador.Update(22, DtoDesdeJson(), CancellationToken.None));
        servicio.VerifyNoOtherCalls();
    }
}
