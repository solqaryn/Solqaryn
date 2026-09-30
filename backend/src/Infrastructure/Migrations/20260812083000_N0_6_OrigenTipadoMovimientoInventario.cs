using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// ERP-N0.6 C2: incorpora referencias tipadas nullable a MovimientoInventario y
/// realiza un backfill determinista desde ReferenciaTipo/ReferenciaId.
/// Las columnas legacy se preservan intactas durante la transición.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260812083000_N0_6_OrigenTipadoMovimientoInventario")]
public sealed class N0_6_OrigenTipadoMovimientoInventario : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Fail-closed antes de cualquier DDL/backfill. C1 ya certifica este contrato,
        // pero la migración vuelve a comprobarlo para no depender del orden operativo.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N06C2Guard;
            CREATE TEMPORARY TABLE __N06C2Guard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N06C2_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N06C2Guard (Id, Violaciones)
            SELECT 1,
                (SELECT COUNT(*)
                   FROM MovimientosInventario m
                  WHERE m.ReferenciaTipo IS NULL
                     OR m.ReferenciaId IS NULL
                     OR m.ReferenciaId <= 0
                     OR CAST(m.ReferenciaTipo AS BINARY) NOT IN (
                          CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY),
                          CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY),
                          CAST('ConsumoInsumo' AS BINARY)))
              + (SELECT COUNT(*)
                   FROM MovimientosInventario m
                   LEFT JOIN Compras c ON c.Id = m.ReferenciaId
                  WHERE CAST(m.ReferenciaTipo AS BINARY) IN (
                          CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                    AND c.Id IS NULL)
              + (SELECT COUNT(*)
                   FROM MovimientosInventario m
                   LEFT JOIN Ventas v ON v.Id = m.ReferenciaId
                  WHERE CAST(m.ReferenciaTipo AS BINARY) IN (
                          CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                    AND v.Id IS NULL)
              + (SELECT COUNT(*)
                   FROM MovimientosInventario m
                   LEFT JOIN ConsumosInsumos c ON c.Id = m.ReferenciaId
                  WHERE CAST(m.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                    AND c.Id IS NULL);

            DROP TEMPORARY TABLE __N06C2Guard;
            """);

        migrationBuilder.AddColumn<int>(
            name: "CompraId",
            table: "MovimientosInventario",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "VentaId",
            table: "MovimientosInventario",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ConsumoInsumoId",
            table: "MovimientosInventario",
            type: "int",
            nullable: true);

        // Snapshot temporal del contrato legacy para demostrar que el backfill no lo altera.
        // Se declara PK explícita para ser compatible con MySQL administrado cuando
        // sql_require_primary_key está habilitado, preservando los tipos legacy exactos.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N06C2Antes;
            CREATE TEMPORARY TABLE __N06C2Antes
            (
                Id INT NOT NULL PRIMARY KEY,
                ReferenciaTipo VARCHAR(30) NOT NULL,
                ReferenciaId INT NOT NULL
            );
            INSERT INTO __N06C2Antes (Id, ReferenciaTipo, ReferenciaId)
                SELECT Id, ReferenciaTipo, ReferenciaId
                  FROM MovimientosInventario;
            """);

        migrationBuilder.Sql("""
            UPDATE MovimientosInventario
               SET CompraId = ReferenciaId
             WHERE CAST(ReferenciaTipo AS BINARY) IN (
                     CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY));

            UPDATE MovimientosInventario
               SET VentaId = ReferenciaId
             WHERE CAST(ReferenciaTipo AS BINARY) IN (
                     CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY));

            UPDATE MovimientosInventario
               SET ConsumoInsumoId = ReferenciaId
             WHERE CAST(ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY);
            """);

        // Postcheck local de C2: exactamente una FK tipada y equivalencia 1:1 con legacy.
        // C3 añadirá los constraints permanentes de exclusividad y el postcheck final.
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N06C2PostGuard;
            CREATE TEMPORARY TABLE __N06C2PostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N06C2_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N06C2PostGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM MovimientosInventario m
             WHERE (m.CompraId IS NOT NULL) + (m.VentaId IS NOT NULL) + (m.ConsumoInsumoId IS NOT NULL) <> 1
                OR (CAST(m.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                    AND (m.CompraId IS NULL OR m.CompraId <> m.ReferenciaId OR m.VentaId IS NOT NULL OR m.ConsumoInsumoId IS NOT NULL))
                OR (CAST(m.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                    AND (m.VentaId IS NULL OR m.VentaId <> m.ReferenciaId OR m.CompraId IS NOT NULL OR m.ConsumoInsumoId IS NOT NULL))
                OR (CAST(m.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                    AND (m.ConsumoInsumoId IS NULL OR m.ConsumoInsumoId <> m.ReferenciaId OR m.CompraId IS NOT NULL OR m.VentaId IS NOT NULL));

            INSERT INTO __N06C2PostGuard (Id, Violaciones)
            SELECT 2, COUNT(*)
              FROM MovimientosInventario m
              JOIN __N06C2Antes a ON a.Id = m.Id
             WHERE NOT (CAST(m.ReferenciaTipo AS BINARY) <=> CAST(a.ReferenciaTipo AS BINARY))
                OR NOT (m.ReferenciaId <=> a.ReferenciaId);

            DROP TEMPORARY TABLE __N06C2PostGuard;
            DROP TEMPORARY TABLE __N06C2Antes;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_CompraId",
            table: "MovimientosInventario",
            column: "CompraId");

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_VentaId",
            table: "MovimientosInventario",
            column: "VentaId");

        migrationBuilder.CreateIndex(
            name: "IX_MovimientosInventario_ConsumoInsumoId",
            table: "MovimientosInventario",
            column: "ConsumoInsumoId");

        migrationBuilder.AddForeignKey(
            name: "FK_MovimientosInventario_Compras_CompraId_N06",
            table: "MovimientosInventario",
            column: "CompraId",
            principalTable: "Compras",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_MovimientosInventario_Ventas_VentaId_N06",
            table: "MovimientosInventario",
            column: "VentaId",
            principalTable: "Ventas",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_MovimientosInventario_ConsumosInsumos_ConsumoInsumoId_N06",
            table: "MovimientosInventario",
            column: "ConsumoInsumoId",
            principalTable: "ConsumosInsumos",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_MovimientosInventario_Compras_CompraId_N06",
            table: "MovimientosInventario");
        migrationBuilder.DropForeignKey(
            name: "FK_MovimientosInventario_Ventas_VentaId_N06",
            table: "MovimientosInventario");
        migrationBuilder.DropForeignKey(
            name: "FK_MovimientosInventario_ConsumosInsumos_ConsumoInsumoId_N06",
            table: "MovimientosInventario");

        migrationBuilder.DropIndex(name: "IX_MovimientosInventario_CompraId", table: "MovimientosInventario");
        migrationBuilder.DropIndex(name: "IX_MovimientosInventario_VentaId", table: "MovimientosInventario");
        migrationBuilder.DropIndex(name: "IX_MovimientosInventario_ConsumoInsumoId", table: "MovimientosInventario");

        migrationBuilder.DropColumn(name: "CompraId", table: "MovimientosInventario");
        migrationBuilder.DropColumn(name: "VentaId", table: "MovimientosInventario");
        migrationBuilder.DropColumn(name: "ConsumoInsumoId", table: "MovimientosInventario");
    }
}
