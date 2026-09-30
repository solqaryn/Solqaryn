using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public class EmailEmpresarialPersistenceModelTests
{
    private static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"email-model-{Guid.NewGuid():N}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public void ModeloEmail_RegistraTablaTenantYForeignKeyRestrict()
    {
        using var db = CrearContexto();
        var entity = db.Model.FindEntityType(typeof(EmailEmpresarial));

        Assert.NotNull(entity);
        Assert.Equal("EmailsEmpresariales", entity!.GetTableName());

        var tenantFk = Assert.Single(
            entity.GetForeignKeys(),
            fk => fk.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(EmailEmpresarial.EmpresaId) }));

        Assert.Equal(DeleteBehavior.Restrict, tenantFk.DeleteBehavior);
    }

    [Fact]
    public void ModeloEmail_ProtegeIdempotenciaYCorrelationDelProveedorPorTenant()
    {
        using var db = CrearContexto();
        var entity = Assert.IsAssignableFrom<IEntityType>(db.Model.FindEntityType(typeof(EmailEmpresarial)));
        var indexes = entity.GetIndexes().ToDictionary(x => x.GetDatabaseName()!, StringComparer.Ordinal);

        Assert.True(indexes["UX_EmailsEmpresariales_Empresa_MensajeId"].IsUnique);
        Assert.Equal(
            new[] { nameof(EmailEmpresarial.EmpresaId), nameof(EmailEmpresarial.MensajeId) },
            indexes["UX_EmailsEmpresariales_Empresa_MensajeId"].Properties.Select(p => p.Name));

        Assert.True(indexes["UX_EmailsEmpresariales_Empresa_Idempotencia"].IsUnique);
        Assert.Equal(
            new[] { nameof(EmailEmpresarial.EmpresaId), nameof(EmailEmpresarial.ClaveIdempotencia) },
            indexes["UX_EmailsEmpresariales_Empresa_Idempotencia"].Properties.Select(p => p.Name));

        Assert.True(indexes["UX_EmailsEmpresariales_Empresa_ProviderMessageId"].IsUnique);
        Assert.Equal(
            new[] { nameof(EmailEmpresarial.EmpresaId), nameof(EmailEmpresarial.ProviderMessageId) },
            indexes["UX_EmailsEmpresariales_Empresa_ProviderMessageId"].Properties.Select(p => p.Name));
    }

    [Fact]
    public void ModeloEmail_ExponeIndicesDeClaimYRastreoSinSecretos()
    {
        using var db = CrearContexto();
        var entity = Assert.IsAssignableFrom<IEntityType>(db.Model.FindEntityType(typeof(EmailEmpresarial)));
        var indexes = entity.GetIndexes().ToDictionary(x => x.GetDatabaseName()!, StringComparer.Ordinal);

        Assert.Equal(
            new[] { nameof(EmailEmpresarial.Estado), nameof(EmailEmpresarial.DisponibleDesdeUtc), nameof(EmailEmpresarial.Id) },
            indexes["IX_EmailsEmpresariales_Claim"].Properties.Select(p => p.Name));
        Assert.Equal(
            new[] { nameof(EmailEmpresarial.EmpresaId), nameof(EmailEmpresarial.Estado), nameof(EmailEmpresarial.DisponibleDesdeUtc) },
            indexes["IX_EmailsEmpresariales_Empresa_Estado_Disponible"].Properties.Select(p => p.Name));
        Assert.Equal(
            new[] { nameof(EmailEmpresarial.EmpresaId), nameof(EmailEmpresarial.CorrelationId) },
            indexes["IX_EmailsEmpresariales_Empresa_Correlation"].Properties.Select(p => p.Name));

        Assert.DoesNotContain(
            entity.GetProperties(),
            p => p.Name.Contains("Secret", StringComparison.OrdinalIgnoreCase)
                || p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)
                || p.Name.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ModeloPlantilla_UsaCodigoVersionUnicosDentroDelTenant()
    {
        using var db = CrearContexto();
        var entity = db.Model.FindEntityType(typeof(PlantillaEmailEmpresarial));

        Assert.NotNull(entity);
        Assert.Equal("PlantillasEmailEmpresarial", entity!.GetTableName());

        var version = Assert.Single(
            entity.GetIndexes(),
            i => i.GetDatabaseName() == "UX_PlantillasEmail_Empresa_Codigo_Version");
        Assert.True(version.IsUnique);
        Assert.Equal(
            new[]
            {
                nameof(PlantillaEmailEmpresarial.EmpresaId),
                nameof(PlantillaEmailEmpresarial.Codigo),
                nameof(PlantillaEmailEmpresarial.Version)
            },
            version.Properties.Select(p => p.Name));
    }
}
