using Microsoft.EntityFrameworkCore;
using Moq;
using Solqaryn.Application.DTOs;
using Solqaryn.Application.Exceptions;
using Solqaryn.Application.Interfaces;
using Solqaryn.Application.Services;
using Solqaryn.Domain.Entities;
using Solqaryn.Domain.Enums;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

/// <summary>
/// P1-009.A: evidencia dirigida del contrato vigente de roles y de su unicidad.
/// Sin acceso a datos reales, sin cambiar RBAC, migraciones o reglas de negocio.
/// </summary>
public sealed class P1009ARoleTemplateUniquenessTests
{
    [Fact]
    public void P1009A_PlantillaEmpresarialSeComparteYPrivadosConservanSuOwner()
    {
        var plantilla = new Rol
        {
            EmpresaId = null, EsSistema = true,
            Ambito = AmbitoAutorizacion.Empresa, Activo = true
        };
        var privadoA = new Rol
        {
            EmpresaId = 11, EsSistema = false,
            Ambito = AmbitoAutorizacion.Empresa, Activo = true
        };
        Assert.True(plantilla.EsAsignableAEmpresa(11));
        Assert.True(plantilla.EsAsignableAEmpresa(22));
        Assert.True(privadoA.EsAsignableAEmpresa(11));
        Assert.False(privadoA.EsAsignableAEmpresa(22));
        Assert.False(plantilla.EsAsignableAEmpresa(0));
    }

    [Theory]
    [InlineData(22, AmbitoAutorizacion.Empresa, false)]
    [InlineData(null, AmbitoAutorizacion.Plataforma, true)]
    [InlineData(null, AmbitoAutorizacion.Empresa, false)]
    public void P1009A_TenantARechazaRolDeBOGlobalOHuerfano(
        int? owner, AmbitoAutorizacion ambito, bool esSistema)
    {
        var rol = new Rol
        {
            EmpresaId = owner, Ambito = ambito,
            EsSistema = esSistema, Activo = true
        };
        Assert.False(rol.EsAsignableAEmpresa(11));
    }

    [Fact]
    public void P1009A_IndiceDeNombreNormalizadoEsUnicoEnModeloEF()
    {
        using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase("p1009a-model-" + Guid.NewGuid().ToString("N"))
                .Options);

        var entity = db.Model.FindEntityType(typeof(Rol));
        Assert.NotNull(entity);
        Assert.Contains(entity!.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(Rol.NombreNormalizado));
    }

    [Fact]
    public async Task P1009A_NombreDuplicadoNormalizadoSeRechazaAntesDePersistir()
    {
        var roles = new Mock<IRolRepository>(MockBehavior.Strict);
        roles.Setup(r => r.ExisteNombreAsync("auditoria", null))
            .ReturnsAsync(true);
        var servicio = new RolService(roles.Object,
            Mock.Of<IAuditoriaService>(), Mock.Of<ICurrentUserService>());

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            servicio.CreateAsync(new CrearRolDto
            {
                Nombre = "  AUDITORIA  "
            }, 11));

        roles.Verify(r => r.ExisteNombreAsync("auditoria", null), Times.Once);
        roles.Verify(r => r.AddAsync(It.IsAny<Rol>()), Times.Never);
        roles.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
