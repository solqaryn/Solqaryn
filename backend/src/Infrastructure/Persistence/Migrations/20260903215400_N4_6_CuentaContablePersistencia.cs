using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// N4.6.C — persistencia aditiva del plan de cuentas jerárquico.
/// No crea asientos, saldos, seeds contables ni efectos comerciales.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260903215400_N4_6_CuentaContablePersistencia")]
public sealed class N4_6_CuentaContablePersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N46CGuard;
            CREATE TEMPORARY TABLE __N46CGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N46C_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N46CGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name = 'CuentasContables';

            DROP TEMPORARY TABLE __N46CGuard;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE `CuentasContables` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `Codigo` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
                `Nombre` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                `Descripcion` varchar(1000) CHARACTER SET utf8mb4 NULL,
                `Tipo` int NOT NULL,
                `CuentaPadreId` int NULL,
                `AceptaMovimientos` tinyint(1) NOT NULL DEFAULT TRUE,
                `Activa` tinyint(1) NOT NULL DEFAULT TRUE,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` varchar(150) CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_CuentasContables` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_CuentasContables_Codigo` CHECK (CHAR_LENGTH(TRIM(`Codigo`)) > 0),
                CONSTRAINT `CK_CuentasContables_Nombre` CHECK (CHAR_LENGTH(TRIM(`Nombre`)) > 0),
                CONSTRAINT `CK_CuentasContables_Tipo` CHECK (`Tipo` BETWEEN 1 AND 6),
                CONSTRAINT `FK_CuentasContables_CuentasContables_CuentaPadreId`
                    FOREIGN KEY (`CuentaPadreId`) REFERENCES `CuentasContables` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE UNIQUE INDEX `UX_CuentasContables_Codigo`
                ON `CuentasContables` (`Codigo`);

            CREATE INDEX `IX_CuentasContables_CuentaPadreId`
                ON `CuentasContables` (`CuentaPadreId`);

            CREATE INDEX `IX_CuentasContables_Tipo_Activa`
                ON `CuentasContables` (`Tipo`, `Activa`);
            """);

        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N46CPostGuard;
            CREATE TEMPORARY TABLE __N46CPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N46C_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N46CPostGuard (Id, Violaciones)
            SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name = 'CuentasContables';

            INSERT INTO __N46CPostGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
              FROM information_schema.table_constraints
             WHERE constraint_schema = DATABASE()
               AND table_name = 'CuentasContables'
               AND constraint_type = 'CHECK'
               AND constraint_name IN (
                    'CK_CuentasContables_Codigo',
                    'CK_CuentasContables_Nombre',
                    'CK_CuentasContables_Tipo');

            INSERT INTO __N46CPostGuard (Id, Violaciones)
            SELECT 3, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
              FROM information_schema.referential_constraints
             WHERE constraint_schema = DATABASE()
               AND constraint_name = 'FK_CuentasContables_CuentasContables_CuentaPadreId';

            INSERT INTO __N46CPostGuard (Id, Violaciones)
            SELECT 4, CASE WHEN COUNT(DISTINCT index_name) = 3 THEN 0 ELSE 1 END
              FROM information_schema.statistics
             WHERE table_schema = DATABASE()
               AND table_name = 'CuentasContables'
               AND index_name IN (
                    'UX_CuentasContables_Codigo',
                    'IX_CuentasContables_CuentaPadreId',
                    'IX_CuentasContables_Tipo_Activa');

            INSERT INTO __N46CPostGuard (Id, Violaciones)
            SELECT 5, COUNT(*) FROM `CuentasContables`;

            DROP TEMPORARY TABLE __N46CPostGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N46CDownGuard;
            CREATE TEMPORARY TABLE __N46CDownGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N46C_DownGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N46CDownGuard (Id, Violaciones)
            SELECT 1, COUNT(*) FROM `CuentasContables`;

            DROP TEMPORARY TABLE __N46CDownGuard;
            """);

        migrationBuilder.DropTable(name: "CuentasContables");
    }
}
