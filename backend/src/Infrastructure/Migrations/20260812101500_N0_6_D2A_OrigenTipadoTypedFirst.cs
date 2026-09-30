using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// ERP-N0.6 D2A: convierte el bridge transitorio de origen de inventario a typed-first.
/// Si la aplicación aporta una FK tipada, el trigger la preserva y el CHECK C3 valida
/// su equivalencia con el snapshot legacy. Solo escritores antiguos que no aportan
/// ninguna FK tipada continúan recibiendo derivación desde ReferenciaTipo/ReferenciaId.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260812101500_N0_6_D2A_OrigenTipadoTypedFirst")]
public sealed class N0_6_D2A_OrigenTipadoTypedFirst : Migration
{
    private const string TriggerInsert = "TR_MovimientosInventario_N06_OrigenTipado_BI";
    private const string TriggerUpdate = "TR_MovimientosInventario_N06_OrigenTipado_BU";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerUpdate};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerInsert};");

        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerInsert}
            BEFORE INSERT ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL
                    THEN NEW.CompraId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL
                    THEN NEW.VentaId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL
                    THEN NEW.ConsumoInsumoId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                    THEN NEW.ReferenciaId ELSE NULL END;
            """);

        migrationBuilder.Sql($"""
            CREATE TRIGGER {TriggerUpdate}
            BEFORE UPDATE ON MovimientosInventario
            FOR EACH ROW
            SET
                NEW.CompraId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL
                    THEN NEW.CompraId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Compra' AS BINARY), CAST('CompraAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.VentaId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL
                    THEN NEW.VentaId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) IN (CAST('Venta' AS BINARY), CAST('VentaAnulada' AS BINARY))
                    THEN NEW.ReferenciaId ELSE NULL END,
                NEW.ConsumoInsumoId = CASE
                    WHEN NEW.CompraId IS NOT NULL OR NEW.VentaId IS NOT NULL OR NEW.ConsumoInsumoId IS NOT NULL
                    THEN NEW.ConsumoInsumoId
                    WHEN CAST(NEW.ReferenciaTipo AS BINARY) = CAST('ConsumoInsumo' AS BINARY)
                    THEN NEW.ReferenciaId ELSE NULL END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerUpdate};");
        migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerInsert};");

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
    }
}
