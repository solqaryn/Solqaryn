using Solqaryn.Domain.Entities;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Solqaryn.Tests;

public class NumeracionDocumentoPersistenceTests
{
    [Fact]
    public void ModeloEf_PersisteScopeTenantConUnicidadFisicaParaSucursalOpcional()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"numeracion-{Guid.NewGuid():N}")
            .Options;

        using var db = new AppDbContext(options);
        var entity = db.Model.FindEntityType(typeof(SecuenciaDocumento));

        Assert.NotNull(entity);
        Assert.Equal("SecuenciasDocumento", entity!.GetTableName());
        Assert.NotNull(entity.FindProperty("SucursalScopeKey"));
        Assert.Equal("IFNULL(`SucursalId`, 0)", entity.FindProperty("SucursalScopeKey")!.GetComputedColumnSql());
        Assert.True(entity.FindProperty(nameof(SecuenciaDocumento.UltimoValor))!.IsConcurrencyToken);

        var scopeIndex = Assert.Single(entity.GetIndexes(), index =>
            index.IsUnique && index.GetDatabaseName() == "UX_SecuenciasDocumento_Empresa_Sucursal_Tipo");
        Assert.Equal(
            new[] { "EmpresaId", "SucursalScopeKey", "TipoDocumento" },
            scopeIndex.Properties.Select(property => property.Name).ToArray());

        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Empresa) &&
            fk.Properties.Select(property => property.Name).SequenceEqual(new[] { "EmpresaId" }));
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(Sucursal) &&
            fk.Properties.Select(property => property.Name).SequenceEqual(new[] { "SucursalId" }));
    }
}
