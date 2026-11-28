using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261128163400_QaCotizacionEstadoV5")]
public sealed class QaCotizacionEstadoMigrationV5 : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        // The model already requires 1..5. Reconcile the inherited 0..4
        // check in one InnoDB ALTER, without rewriting historical states.
        m.Sql("ALTER TABLE `Cotizaciones` DROP CHECK `CK_Cotizaciones_Estado`, ADD CONSTRAINT `CK_Cotizaciones_Estado` CHECK (`Estado` IN (1, 2, 3, 4, 5))");
    }

    protected override void Down(MigrationBuilder m) =>
        throw new NotSupportedException("Restore the verified pre-migration backup if recovery is required; never reinterpret converted quotations.");
}
