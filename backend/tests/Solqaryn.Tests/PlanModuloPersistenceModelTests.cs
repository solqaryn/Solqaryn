using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class PlanModuloPersistenceModelTests
{
    [Fact]
    public void Modelo_Persistente_Blinda_Regla_Plan_Modulo_Y_Codigo_Canonico()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"n610c-model-{Guid.NewGuid():N}")
            .Options;

        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(PlanModulo));

        Assert.NotNull(entity);
        Assert.Equal("PlanModulos", entity!.GetTableName());

        Assert.Equal(80, entity.FindProperty(nameof(PlanModulo.PlanCodigo))!.GetMaxLength());
        Assert.Equal(120, entity.FindProperty(nameof(PlanModulo.ModuloClave))!.GetMaxLength());

        var unique = Assert.Single(entity.GetIndexes().Where(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(new[]
            {
                nameof(PlanModulo.PlanId),
                nameof(PlanModulo.ModuloClave)
            })));
        Assert.True(unique.IsUnique);
        Assert.Equal("UX_PlanModulos_PlanId_ModuloClave", unique.GetDatabaseName());

        var planFk = Assert.Single(entity.GetForeignKeys());
        Assert.Equal(DeleteBehavior.Restrict, planFk.DeleteBehavior);
        Assert.Equal(typeof(Plan), planFk.PrincipalEntityType.ClrType);
        Assert.Equal(
            new[] { nameof(PlanModulo.PlanId), nameof(PlanModulo.PlanCodigo) },
            planFk.Properties.Select(p => p.Name));
        Assert.Equal(
            new[] { nameof(Plan.Id), nameof(Plan.Codigo) },
            planFk.PrincipalKey.Properties.Select(p => p.Name));
    }
}
