using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Extensions;
using Solqaryn.Infrastructure.Persistence;
using Xunit;

namespace Solqaryn.Tests;

public sealed class QaCommercialOwnershipSnapshotTests
{
    [Fact]
    public void OracleSnapshot_V4_MaterializaPropiedadesEIndicesSinConexion()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("Server=127.0.0.1;Database=qa_snapshot_only;User=root;Password=unused;",
                mysql => mysql.MigrationsAssembly("Solqaryn.Infrastructure.Migrations")).Options;
        using var db = new AppDbContext(options);
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot;
        Assert.NotNull(snapshot);
        var model = snapshot!.Model;
        foreach (var table in QaCommercialOwnershipV4.Tables)
        {
            var entity = model.FindEntityType(table.Entity.FullName!);
            Assert.NotNull(entity);
            Assert.Equal(typeof(int?), entity!.FindProperty("EmpresaId")!.ClrType);
            Assert.Contains(entity.GetIndexes(), index => index.IsUnique &&
                index.Properties.Select(p => p.Name).SequenceEqual(new[] { "EmpresaId", "Id" }));
        }
        foreach (var key in QaCommercialOwnershipV4.UniqueKeys)
        {
            var table = QaCommercialOwnershipV4.Tables.Single(t => t.Table == key.Table);
            var entity = model.FindEntityType(table.Entity.FullName!)!;
            Assert.Contains(entity.GetIndexes(), index => index.IsUnique &&
                index.Properties.Select(p => p.Name).SequenceEqual(new[] { "EmpresaId" }.Concat(key.Columns)));
        }
    }
}
