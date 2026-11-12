using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Solqaryn.API.Controllers;
using Solqaryn.API.Filters;
using Solqaryn.Application.Common;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// I8-011.E: regresion limitada de respuestas de usuarios con rol global.
/// No afirma autorizacion HTTP real, JWT, tokens persistidos ni MySQL.
/// Los secretos son marcadores sinteticos y nunca credenciales de DEV.
/// </summary>
public sealed class I8011ESuperadminDatosUsuariosSinSecretosTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task I8011E_ConsultaDeUsuariosNoExponeHashNiContratoDeTokens(
        bool esAdministrador)
    {
        const string hashSintetico = "I8011E-HASH-TOKEN-PRIVADO-FICTICIO";
        var rol = new Rol
        {
            Id = 501,
            Nombre = "Rol de plataforma de prueba",
            NombreNormalizado = "I8011E_PLATAFORMA",
            Ambito = AmbitoAutorizacion.Plataforma,
            EmpresaId = null,
            EsSistema = true,
            EsAdministrador = esAdministrador,
            Activo = true
        };
        var usuario = new Usuario
        {
            Id = 701,
            NombreUsuario = "i8011e-global",
            NombreCompleto = "Usuario global sintetico",
            PasswordHash = hashSintetico,
            RolId = rol.Id,
            RolEntidad = rol,
            Activo = true
        };

        var usuarios = new Mock<IUsuarioRepository>(MockBehavior.Strict);
        usuarios.Setup(x => x.GetByIdAsync(usuario.Id)).ReturnsAsync(usuario);
        usuarios.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Usuario> { usuario });
        usuarios.Setup(x => x.GetPagedAsync(It.IsAny<PagedRequest>()))
            .ReturnsAsync(new PagedResult<Usuario>
            {
                Items = new List<Usuario> { usuario },
                Page = 1,
                PageSize = 10,
                TotalCount = 1
            });

        var servicio = new UsuarioService(
            usuarios.Object,
            new Mock<IRolRepository>(MockBehavior.Strict).Object,
            new Mock<IEmpresaRepository>(MockBehavior.Strict).Object,
            new Mock<IAuditoriaService>(MockBehavior.Strict).Object,
            new Mock<ICurrentUserService>(MockBehavior.Strict).Object);

        var detalle = await servicio.GetByIdAsync(usuario.Id);
        var listado = await servicio.GetAllAsync();
        var pagina = await servicio.GetPagedAsync(new PagedRequest { Page = 1, PageSize = 10 });

        Assert.NotNull(detalle);
        Assert.Equal(usuario.Id, detalle.Id);
        Assert.Single(listado);
        Assert.Single(pagina.Items);
        Assert.Equal(AmbitoAutorizacion.Plataforma, rol.Ambito);
        Assert.Equal(esAdministrador, rol.EsAdministrador);

        // Se ejecutan las tres proyecciones del servicio, no solo una
        // inspeccion de propiedades sin datos adversariales.
        var respuesta = JsonSerializer.Serialize(new
        {
            Detalle = detalle,
            Lista = listado,
            Pagina = pagina
        });
        Assert.DoesNotContain(hashSintetico, respuesta, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", respuesta, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RefreshToken", respuesta, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ResetToken", respuesta, StringComparison.OrdinalIgnoreCase);

        foreach (var tipo in new[] { typeof(UsuarioDto), typeof(UsuarioDetalleDto) })
        {
            foreach (var propiedad in tipo.GetProperties())
            {
                Assert.False(
                    propiedad.Name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                    propiedad.Name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                    propiedad.Name.Contains("Secreto", StringComparison.OrdinalIgnoreCase),
                    $"DTO {tipo.Name} expone campo sensible {propiedad.Name}");
            }
        }

        // Metadatos de entrada, no sustituyen la ejecucion del middleware.
        Assert.NotNull(typeof(UsuariosController).GetCustomAttribute<AuthorizeAttribute>());
        foreach (var accion in new[]
                 {
                     nameof(UsuariosController.GetAll),
                     nameof(UsuariosController.GetPaged),
                     nameof(UsuariosController.GetById)
                 })
        {
            var metodo = typeof(UsuariosController).GetMethod(accion);
            Assert.NotNull(metodo);
            Assert.NotNull(metodo.GetCustomAttribute<RequierePermisoAttribute>());
        }

        usuarios.Verify(x => x.GetByIdAsync(usuario.Id), Times.Once);
        usuarios.Verify(x => x.GetAllAsync(), Times.Once);
        usuarios.Verify(x => x.GetPagedAsync(It.IsAny<PagedRequest>()), Times.Once);
    }
}
