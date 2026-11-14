using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// N4.7.C — persistencia aditiva para asientos contables y sus líneas.
/// Mantiene integridad referencial, precisión monetaria y exclusión Debe/Haber a nivel de base de datos.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260904071000_N4_7_AsientosPersistencia")]
public sealed class N4_7_AsientosPersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N47CGuard;
            CREATE TEMPORARY TABLE __N47CGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N47C_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N47CGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('AsientosContables', 'AsientoDetalles');

            DROP TEMPORARY TABLE __N47CGuard;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE `AsientosContables` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Fecha` datetime(6) NOT NULL,
                `Concepto` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
                `Numero` varchar(50) CHARACTER SET utf8mb4 NULL,
                `DocumentoOrigenId` int NULL,
                `TipoDocumentoOrigen` varchar(100) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_AsientosContables` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_AsientosContables_Concepto` CHECK (CHAR_LENGTH(TRIM(`Concepto`)) > 0)
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE UNIQUE INDEX `UX_AsientosContables_Numero`
                ON `AsientosContables` (`Numero`);
            CREATE INDEX `IX_AsientosContables_Fecha`
                ON `AsientosContables` (`Fecha`);
            CREATE INDEX `IX_AsientosContables_Origen`
                ON `AsientosContables` (`TipoDocumentoOrigen`, `DocumentoOrigenId`);
            """);

        migrationBuilder.Sql("""
            CREATE TABLE `AsientoDetalles` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `AsientoContableId` int NOT NULL,
                `CuentaContableId` int NOT NULL,
                `Debe` decimal(18,2) NOT NULL DEFAULT 0,
                `Haber` decimal(18,2) NOT NULL DEFAULT 0,
                `Referencia` varchar(250) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_AsientoDetalles` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_AsientoDetalles_MontosNoNegativos` CHECK (`Debe` >= 0 AND `Haber` >= 0),
                CONSTRAINT `CK_AsientoDetalles_UnSoloLado` CHECK ((`Debe` > 0 AND `Haber` = 0) OR (`Haber` > 0 AND `Debe` = 0)),
                CONSTRAINT `FK_AsientoDetalles_AsientosContables_AsientoContableId`
                    FOREIGN KEY (`AsientoContableId`) REFERENCES `AsientosContables` (`Id`) ON DELETE CASCADE,
                CONSTRAINT `FK_AsientoDetalles_CuentasContables_CuentaContableId`
                    FOREIGN KEY (`CuentaContableId`) REFERENCES `CuentasContables` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE INDEX `IX_AsientoDetalles_AsientoContableId`
                ON `AsientoDetalles` (`AsientoContableId`);
            CREATE INDEX `IX_AsientoDetalles_CuentaContableId`
                ON `AsientoDetalles` (`CuentaContableId`);
            """);

        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N47CPostGuard;
            CREATE TEMPORARY TABLE __N47CPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N47C_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N47CPostGuard (Id, Violaciones)
            SELECT 1, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name IN ('AsientosContables', 'AsientoDetalles');

            INSERT INTO __N47CPostGuard (Id, Violaciones)
            SELECT 2, COUNT(*) FROM `AsientosContables`;
            INSERT INTO __N47CPostGuard (Id, Violaciones)
            SELECT 3, COUNT(*) FROM `AsientoDetalles`;

            DROP TEMPORARY TABLE __N47CPostGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AsientoDetalles");
        migrationBuilder.DropTable(name: "AsientosContables");
    }
}
