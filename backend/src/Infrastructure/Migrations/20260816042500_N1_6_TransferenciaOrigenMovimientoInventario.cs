using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// ERP-N1.6.C: incorpora TransferenciaInventario como quinto origen relacional
/// tipado de MovimientoInventario/Kardex y evoluciona el bridge typed-first.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260816042500_N1_6_TransferenciaOrigenMovimientoInventario")]
public sealed class N1_6_TransferenciaOrigenMovimientoInventario : Migration
{
    private const string ConstraintOrigen = "CK_MovimientosInventario_OrigenTipado_Exclusivo_N06";
    private const string TriggerInsert = "TR_MovimientosInventario_N06_OrigenTipado_BI";
    private const string TriggerUpdate = "TR_MovimientosInventario_N06_OrigenTipado_BU";
    private const string TransferenciaFk = "FK_MovInv_TransferenciaInventarioId_N16";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "TransferenciaInventarioId",
            table: "MovimientosInventario",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_TransferenciaInventarioId",
            table: "MovimientosInventario",
            column: "TransferenciaInventarioId");

        migrationBuilder.CreateIndex(
            name: "IX_MovInv_Transferencia_Fecha_N16",
            table: "MovimientosInventario",
            columns: new[] { "TransferenciaInventarioId", "Fecha" });

        migrationBuilder.AddForeignKey(
            name: TransferenciaFk,
            table: "MovimientosInventario",
            column: "TransferenciaInventarioId",
            principalTable: "TransferenciasInventario",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerUpdate};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerInsert};");
        migrationBuilder.Sql($"ALTER TABLE MovimientosInventario DROP CHECK {ConstraintOrigen};");

        CrearBridgeCincoOrigenes(migrationBuilder);
        CrearConstraintCincoOrigenes(migrationBuilder);

        migrationBuilder.Sql($"""
            DROP TEMPORARY TABLE IF EXISTS __N16OrigenPostGuard;
            CREATE TEMPORARY TABLE __N16OrigenPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N16Origen_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N16OrigenPostGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM MovimientosInventario
             WHERE (CompraId IS NOT NULL) + (VentaId IS NOT NULL) +
                   (ConsumoInsumoId IS NOT NULL) + (AjusteInventarioId IS NOT NULL) +
                   (TransferenciaInventarioId IS NOT NULL) > 1;

            INSERT INTO __N16OrigenPostGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
              FROM information_schema.referential_constraints
             WHERE constraint_schema = DATABASE()
               AND table_name = 'MovimientosInventario'
               AND constraint_name = '{TransferenciaFk}';

            DROP TEMPORARY TABLE __N16OrigenPostGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N16OrigenDownGuard;
            CREATE TEMPORARY TABLE __N16OrigenDownGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N16Origen_DownGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N16OrigenDownGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM MovimientosInventario
             WHERE TransferenciaInventarioId IS NOT NULL;

            DROP TEMPORARY TABLE __N16OrigenDownGuard;
            """);

        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerUpdate};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerInsert};");
        migrationBuilder.Sql($"ALTER TABLE MovimientosInventario DROP CHECK {ConstraintOrigen};");

        migrationBuilder.DropForeignKey(
            name: TransferenciaFk,
            table: "MovimientosInventario");
        migrationBuilder.DropIndex(
            name: "IX_MovInv_Transferencia_Fecha_N16",
            table: "MovimientosInventario");
        migrationBuilder.DropIndex(
            name: "IX_MovimientosInventario_TransferenciaInventarioId",
            table: "MovimientosInventario");
        migrationBuilder.DropColumn(
            name: "TransferenciaInventarioId",
            table: "MovimientosInventario");

        CrearBridgeCuatroOrigenes(migrationBuilder);
        CrearConstraintCuatroOrigenes(migrationBuilder);
    }

    private static void CrearBridgeCincoOrigenes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerInsert}
            BEFORE INSERT ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.CompraId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.VentaId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.ConsumoInsumoId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.AjusteInventarioId
                    ELSE NULL END,
                NEW.TransferenciaInventarioId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.TransferenciaInventarioId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END;
            """);

        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerUpdate}
            BEFORE UPDATE ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.CompraId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.VentaId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.ConsumoInsumoId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.AjusteInventarioId
                    ELSE NULL END,
                NEW.TransferenciaInventarioId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL OR NEW.TransferenciaInventarioId IS NOT NULL THEN NEW.TransferenciaInventarioId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END;
            """);
    }

    private static void CrearConstraintCincoOrigenes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            ALTER TABLE MovimientosInventario
            ADD CONSTRAINT {ConstraintOrigen}
            CHECK (
                ((CompraId IS NOT NULL) + (VentaId IS NOT NULL) + (ConsumoInsumoId IS NOT NULL) + (AjusteInventarioId IS NOT NULL) + (TransferenciaInventarioId IS NOT NULL) <= 1)
                AND
                (
                    (CAST(ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                        AND CompraId = ReferenciaId AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                        AND VentaId = ReferenciaId AND CompraId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                        AND ConsumoInsumoId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) = CAST('AjusteInventario' AS BINARY)
                        AND AjusteInventarioId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND TransferenciaInventarioId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) = CAST('TransferenciaInventario' AS BINARY)
                        AND TransferenciaInventarioId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL)
                    OR
                    (CAST(Tipo AS BINARY) = CAST('Ajuste' AS BINARY)
                        AND CAST(ReferenciaTipo AS BINARY) NOT IN (
                            CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY),
                            CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY),
                            CAST('ConsumoInsumo' AS BINARY), CAST('AjusteInventario' AS BINARY),
                            CAST('TransferenciaInventario' AS BINARY))
                        AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL AND TransferenciaInventarioId IS NULL)
                )
            );
            """);
    }

    private static void CrearBridgeCuatroOrigenes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerInsert}
            BEFORE INSERT ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL THEN NEW.CompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL THEN NEW.VentaId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL THEN NEW.ConsumoInsumoId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL THEN NEW.AjusteInventarioId ELSE NULL END;
            """);

        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerUpdate}
            BEFORE UPDATE ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL THEN NEW.CompraId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL THEN NEW.VentaId WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL THEN NEW.ConsumoInsumoId WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) THEN NEW.ReferenciaId ELSE NULL END,
                NEW.AjusteInventarioId = CASE WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL OR NEW.AjusteInventarioId IS NOT NULL THEN NEW.AjusteInventarioId ELSE NULL END;
            """);
    }

    private static void CrearConstraintCuatroOrigenes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            ALTER TABLE MovimientosInventario
            ADD CONSTRAINT {ConstraintOrigen}
            CHECK (
                ((CompraId IS NOT NULL) + (VentaId IS NOT NULL) + (ConsumoInsumoId IS NOT NULL) + (AjusteInventarioId IS NOT NULL) <= 1)
                AND
                (
                    (CAST(ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY)) AND CompraId = ReferenciaId AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY)) AND VentaId = ReferenciaId AND CompraId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY) AND ConsumoInsumoId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND AjusteInventarioId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) = CAST('AjusteInventario' AS BINARY) AND AjusteInventarioId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL)
                    OR
                    (CAST(Tipo AS BINARY) = CAST('Ajuste' AS BINARY)
                        AND CAST(ReferenciaTipo AS BINARY) NOT IN (
                            CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY),
                            CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY),
                            CAST('ConsumoInsumo' AS BINARY), CAST('AjusteInventario' AS BINARY))
                        AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL AND AjusteInventarioId IS NULL)
                )
            );
            """);
    }
}
