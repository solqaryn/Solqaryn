using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// ERP-N0.6 C3: certifica la integridad histórica del backfill tipado y
/// establece integridad permanente para los orígenes documentales mapeables
/// (Compra/Venta/ConsumoInsumo). Durante la transición hacia N0.6.D, triggers
/// derivan las FKs desde ReferenciaTipo/ReferenciaId. Los movimientos de ajuste
/// no documentales conservan exclusivamente el snapshot legacy y cero FKs.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260812084900_N0_6_C3_IntegridadOrigenTipadoMovimientoInventario")]
public sealed class N0_6_C3_IntegridadOrigenTipadoMovimientoInventario : Migration
{
    private const string ConstraintOrigen =
        "CK_MovimientosInventario_OrigenTipado_Exclusivo_N06";
    private const string TriggerInsert =
        "TR_MovimientosInventario_N06_OrigenTipado_BI";
    private const string TriggerUpdate =
        "TR_MovimientosInventario_N06_OrigenTipado_BU";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N06C3Guard;
            CREATE TEMPORARY TABLE __N06C3Guard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N06C3_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N06C3Guard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM MovimientosInventario m
             WHERE (m.CompraId IS NOT NULL)
                 + (m.VentaId IS NOT NULL)
                 + (m.ConsumoInsumoId IS NOT NULL) > 1;

            INSERT INTO __N06C3Guard (Id, Violaciones)
            SELECT 2, COUNT(*)
              FROM MovimientosInventario m
             WHERE (CAST(m.ReferenciaTipo AS BINARY) IN (
                        CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                    AND (m.CompraId IS NULL OR m.CompraId <> m.ReferenciaId
                         OR m.VentaId IS NOT NULL OR m.ConsumoInsumoId IS NOT NULL))
                OR (CAST(m.ReferenciaTipo AS BINARY) IN (
                        CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                    AND (m.VentaId IS NULL OR m.VentaId <> m.ReferenciaId
                         OR m.CompraId IS NOT NULL OR m.ConsumoInsumoId IS NOT NULL))
                OR (CAST(m.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                    AND (m.ConsumoInsumoId IS NULL OR m.ConsumoInsumoId <> m.ReferenciaId
                         OR m.CompraId IS NOT NULL OR m.VentaId IS NOT NULL));

            INSERT INTO __N06C3Guard (Id, Violaciones)
            SELECT 3, COUNT(*)
              FROM MovimientosInventario m
             WHERE CAST(m.ReferenciaTipo AS BINARY) NOT IN (
                       CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY),
                       CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY),
                       CAST('ConsumoInsumo' AS BINARY))
               AND (CAST(m.Tipo AS BINARY) <> CAST('Ajuste' AS BINARY)
                    OR m.CompraId IS NOT NULL OR m.VentaId IS NOT NULL OR m.ConsumoInsumoId IS NOT NULL);

            DROP TEMPORARY TABLE __N06C3Guard;
            """);

        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerInsert}
            BEFORE INSERT ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                    THEN NEW.ReferenciaId ELSE NULL END;
            """);

        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerUpdate}
            BEFORE UPDATE ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                    THEN NEW.ReferenciaId ELSE NULL END;
            """);

        migrationBuilder.Sql($"""
            ALTER TABLE MovimientosInventario
            ADD CONSTRAINT {ConstraintOrigen}
            CHECK (
                ((CompraId IS NOT NULL) + (VentaId IS NOT NULL) + (ConsumoInsumoId IS NOT NULL) <= 1)
                AND
                (
                    (CAST(ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                        AND CompraId = ReferenciaId AND VentaId IS NULL AND ConsumoInsumoId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                        AND VentaId = ReferenciaId AND CompraId IS NULL AND ConsumoInsumoId IS NULL)
                    OR
                    (CAST(ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                        AND ConsumoInsumoId = ReferenciaId AND CompraId IS NULL AND VentaId IS NULL)
                    OR
                    (CAST(Tipo AS BINARY) = CAST('Ajuste' AS BINARY)
                        AND CAST(ReferenciaTipo AS BINARY) NOT IN (
                            CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY),
                            CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY),
                            CAST('ConsumoInsumo' AS BINARY))
                        AND CompraId IS NULL AND VentaId IS NULL AND ConsumoInsumoId IS NULL)
                )
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerUpdate};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerInsert};");
        migrationBuilder.Sql($"""
            ALTER TABLE MovimientosInventario
            DROP CHECK {ConstraintOrigen};
            """);
    }
}
