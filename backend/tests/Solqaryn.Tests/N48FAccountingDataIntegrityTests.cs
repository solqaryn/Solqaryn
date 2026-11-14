using Solqaryn.Domain.Entities.Contabilidad;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Solqaryn.Tests;

public class N48FAccountingDataIntegrityTests
{
    private readonly AppDbContext _context;

    public N48FAccountingDataIntegrityTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
    }

    [Fact]
    public void AsientoDetalle_Model_HasCheckConstraint_MontosNoNegativos()
    {
        var model = _context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(AsientoDetalle));
        Assert.NotNull(entityType);

        var checkConstraints = entityType.GetCheckConstraints();

        Assert.Contains(checkConstraints, c => c.Name == "CK_AsientoDetalles_MontosNoNegativos" && c.Sql == "`Debe` >= 0 AND `Haber` >= 0");
    }

    [Fact]
    public void AsientoDetalle_Model_HasCheckConstraint_UnSoloLado()
    {
        var model = _context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(AsientoDetalle));
        Assert.NotNull(entityType);

        var checkConstraints = entityType.GetCheckConstraints();

        Assert.Contains(checkConstraints, c => c.Name == "CK_AsientoDetalles_UnSoloLado" && c.Sql == "((`Debe` > 0 AND `Haber` = 0) OR (`Haber` > 0 AND `Debe` = 0))");
    }

    [Fact]
    public void AsientoContable_Model_HasCheckConstraint_Concepto()
    {
        var model = _context.GetService<IDesignTimeModel>().Model;
        var entityType = model.FindEntityType(typeof(AsientoContable));
        Assert.NotNull(entityType);

        var checkConstraints = entityType.GetCheckConstraints();

        Assert.Contains(checkConstraints, c => c.Name == "CK_AsientosContables_Concepto" && c.Sql == "CHAR_LENGTH(TRIM(`Concepto`)) > 0");
    }
}
