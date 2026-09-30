using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260810174200_M12AutomatizacionTransversal")]
public sealed class M12AutomatizacionTransversal : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS `AutomatizacionConfiguraciones` (
                `Id` INT NOT NULL,
                `DiasBorradorVentaAlerta` INT NOT NULL DEFAULT 2,
                `DiasBorradorCompraAlerta` INT NOT NULL DEFAULT 7,
                `DiasCargaPendienteAlerta` INT NOT NULL DEFAULT 1,
                `DiasMovimientoFinancieroPendienteAlerta` INT NOT NULL DEFAULT 7,
                `LimiteSugerencias` INT NOT NULL DEFAULT 20,
                `LimiteAutocompletado` INT NOT NULL DEFAULT 10,
                `MostrarRecordatoriosDashboard` TINYINT(1) NOT NULL DEFAULT 1,
                `FechaActualizacion` DATETIME(6) NULL,
                `ActualizadoPor` VARCHAR(120) NULL,
                CONSTRAINT `PK_AutomatizacionConfiguraciones` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_AutomatizacionConfiguraciones_Id` CHECK (`Id` = 1),
                CONSTRAINT `CK_AutomatizacionConfiguraciones_Venta` CHECK (`DiasBorradorVentaAlerta` BETWEEN 1 AND 90),
                CONSTRAINT `CK_AutomatizacionConfiguraciones_Compra` CHECK (`DiasBorradorCompraAlerta` BETWEEN 1 AND 180),
                CONSTRAINT `CK_AutomatizacionConfiguraciones_Carga` CHECK (`DiasCargaPendienteAlerta` BETWEEN 1 AND 30),
                CONSTRAINT `CK_AutomatizacionConfiguraciones_Finanzas` CHECK (`DiasMovimientoFinancieroPendienteAlerta` BETWEEN 1 AND 180),
                CONSTRAINT `CK_AutomatizacionConfiguraciones_Sugerencias` CHECK (`LimiteSugerencias` BETWEEN 5 AND 100),
                CONSTRAINT `CK_AutomatizacionConfiguraciones_Autocomplete` CHECK (`LimiteAutocompletado` BETWEEN 5 AND 50)
            ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

            INSERT INTO `AutomatizacionConfiguraciones`
                (`Id`, `DiasBorradorVentaAlerta`, `DiasBorradorCompraAlerta`, `DiasCargaPendienteAlerta`,
                 `DiasMovimientoFinancieroPendienteAlerta`, `LimiteSugerencias`, `LimiteAutocompletado`,
                 `MostrarRecordatoriosDashboard`, `ActualizadoPor`)
            VALUES (1, 2, 7, 1, 7, 20, 10, 1, 'migracion-m12')
            ON DUPLICATE KEY UPDATE `Id` = `Id`;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS `AutomatizacionConfiguraciones`;");
    }
}
