using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260819143000_N2_3_RecepcionCompraOrigenKardex")]
public sealed class N2_3_RecepcionCompraOrigenKardex : Migration
{
    private const string ConstraintOrigen = "CK_MovimientosInventario_OrigenTipado_Exclusivo_N06";
    private const string TriggerInsert = "TR_MovimientosInventario_N06_OrigenTipado_BI";
    private const string TriggerUpdate = "TR_MovimientosInventario_N06_OrigenTipado_BU";
    private const string RecepcionFk = "FK_MovInv_RecepcionCompraId_N23";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "RecepcionCompraId",
            table: "MovimientosInventario",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_RecepcionCompraId",
            table: "MovimientosInventario",
            column: "RecepcionCompraId");

        migrationBuilder.CreateIndex(
            name: "IX_MovInv_RecepcionCompra_Fecha_N23",
            table: "MovimientosInventario",
            columns: new[] { "RecepcionCompraId", "Fecha" });

        migrationBuilder.AddForeignKey(
            name: RecepcionFk,
            table: "MovimientosInventario",
            column: "RecepcionCompraId",
            principalTable: "RecepcionesCompra",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        RecrearBridge(migrationBuilder, incluirRecepcion: true);
        CrearConstraint(migrationBuilder, incluirRecepcion: true);

        migrationBuilder.Sql($"""
            DROP TEMPORARY TABLE IF EXISTS __N23RecepcionKardexGuard;
            CREATE TEMPORARY TABLE __N23RecepcionKardexGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N23RecepcionKardexGuard_Cero CHECK (Violaciones = 0)
            );
            INSERT INTO __N23RecepcionKardexGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM MovimientosInventario
             WHERE (CompraId IS NOT NULL) + (VentaId IS NOT NULL) +
                   (ConsumoInsumoId IS NOT NULL) + (AjusteInventarioId IS NOT NULL) +
                   (TransferenciaInventarioId IS NOT NULL) + (RecepcionCompraId IS NOT NULL) > 1;
            INSERT INTO __N23RecepcionKardexGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
              FROM information_schema.referential_constraints
             WHERE constraint_schema = DATABASE()
               AND table_name = 'MovimientosInventario'
               AND constraint_name = '{RecepcionFk}';
            DROP TEMPORARY TABLE __N23RecepcionKardexGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N23RecepcionKardexDownGuard;
            CREATE TEMPORARY TABLE __N23RecepcionKardexDownGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N23RecepcionKardexDownGuard_Cero CHECK (Violaciones = 0)
            );
            INSERT INTO __N23RecepcionKardexDownGuard (Id, Violaciones)
            SELECT 1, COUNT(*) FROM MovimientosInventario WHERE RecepcionCompraId IS NOT NULL;
            DROP TEMPORARY TABLE __N23RecepcionKardexDownGuard;
            """);

        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerUpdate};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerInsert};");
        migrationBuilder.Sql($"ALTER TABLE MovimientosInventario DROP CHECK {ConstraintOrigen};");

        migrationBuilder.DropForeignKey(name: RecepcionFk, table: "MovimientosInventario");
        migrationBuilder.DropIndex(name: "IX_MovInv_RecepcionCompra_Fecha_N23", table: "MovimientosInventario");
        migrationBuilder.DropIndex(name: "IX_MovimientosInventario_RecepcionCompraId", table: "MovimientosInventario");
        migrationBuilder.DropColumn(name: "RecepcionCompraId", table: "MovimientosInventario");

        CrearBridgeCincoOrigenes(migrationBuilder);
        CrearConstraint(migrationBuilder, incluirRecepcion: false);
    }

    private static void RecrearBridge(MigrationBuilder migrationBuilder, bool incluirRecepcion)
    {
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerUpdate};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerInsert};");
        migrationBuilder.Sql($"ALTER TABLE MovimientosInventario DROP CHECK {ConstraintOrigen};");
        if (incluirRecepcion)
            CrearBridgeSeisOrigenes(migrationBuilder);
        else
            CrearBridgeCincoOrigenes(migrationBuilder);
    }

    private static void CrearBridgeSeisOrigenes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerInsert}
            BEFORE INSERT ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.CompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.VentaId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.ConsumoInsumoId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.AjusteInventarioId ELSE NULL END,
                NEW.TransferenciaInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.TransferenciaInventarioId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.RecepcionCompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.RecepcionCompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('RecepcionCompra' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END;
            """);
        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerUpdate}
            BEFORE UPDATE ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.CompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.VentaId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.ConsumoInsumoId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.AjusteInventarioId ELSE NULL END,
                NEW.TransferenciaInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.TransferenciaInventarioId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.RecepcionCompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL OR NEW.RecepcionCompraId IS NOT NULL THEN NEW.RecepcionCompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('RecepcionCompra' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END;
            """);
    }

    private static void CrearBridgeCincoOrigenes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerInsert}
            BEFORE INSERT ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.CompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.VentaId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.ConsumoInsumoId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.AjusteInventarioId ELSE NULL END,
                NEW.TransferenciaInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.TransferenciaInventarioId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END;
            """);
        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerUpdate}
            BEFORE UPDATE ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.CompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.VentaId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.ConsumoInsumoId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.AjusteInventarioId ELSE NULL END,
                NEW.TransferenciaInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.TransferenciaInventarioId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END;
            """);
    }

    private static void CrearConstraint(MigrationBuilder migrationBuilder, bool incluirRecepcion)
    {
        var recepcionConteo = incluirRecepcion ? " + (RecepcionCompraId IS NOT NULL)" : string.Empty;
        var recepcionNull = incluirRecepcion ? " AND RecepcionCompraId IS NULL" : string.Empty;
        var recepcionCaso = incluirRecepcion
            ? " OR (CAST(ReferenciaTipo AS BINARY) = CAST('RecepcionCompra' AS BINARY) AND RecepcionCompraId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL)"
            : string.Empty;
        var exclusiones = incluirRecepcion ? ", CAST('RecepcionCompra' AS BINARY)" : string.Empty;

        migrationBuilder.Sql($$"""
            ALTER TABLE MovimientosInventario
            ADD CONSTRAINT {{ConstraintOrigen}}
            CHECK (
                ((CompraId IS NOT NULL) + (VentaId IS NOT NULL) + (ConsumoInsumoId IS NOT NULL) + (AjusteInventarioId IS NOT NULL) + (TransferenciaInventarioId IS NOT NULL){{recepcionConteo}} <= 1)
                AND
                (
                    (CAST(ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) AND CompraId = ReferenciaId AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL{{recepcionNull}})
                    OR (CAST(ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) AND VentaId = ReferenciaId AND CompraId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL{{recepcionNull}})
                    OR (CAST(ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) AND ConsumoInsumoId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL{{recepcionNull}})
                    OR (CAST(ReferenciaTipo AS BINARY) = CAST('AjusteInventario' AS BINARY) AND AjusteInventarioId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND TransferenciaInventarioId IS NULL{{recepcionNull}})
                    OR (CAST(ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY) AND TransferenciaInventarioId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL{{recepcionNull}})
                    {{recepcionCaso}}
                    OR (CAST(Tipo AS BINARY) = CAST('Ajuste' AS BINARY) AND CAST(ReferenciaTipo AS BINARY) NOT IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY), CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY), CAST('ConsumoInsumo' AS BINARY), CAST('AjusteInventario' AS BINARY), CAST('TransferenciaInventario' AS BINARY){{exclusiones}}) AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL{{recepcionNull}})
                )
            );
            """);
    }
}
