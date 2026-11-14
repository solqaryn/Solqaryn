using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// N3.10.C — persistencia aditiva y fail-closed de la configuración de crédito del cliente.
/// No materializa fórmulas de consumo/disponible, scoring, thresholds adicionales, RBAC ni efectos comerciales.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260827061000_N3_10_CreditoClientePersistencia")]
public sealed class N3_10_CreditoClientePersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N310CGuard;
            CREATE TEMPORARY TABLE __N310CGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N310C_Guard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N310CGuard (Id, Violaciones)
            SELECT 1, COUNT(*)
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name = 'CreditosCliente';

            INSERT INTO __N310CGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name = 'Clientes';

            DROP TEMPORARY TABLE __N310CGuard;
            """);

        migrationBuilder.Sql("""
            CREATE TABLE `CreditosCliente` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `ClienteId` int NOT NULL,
                `Moneda` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
                `LimiteCredito` decimal(18,4) NOT NULL,
                `DiasCredito` int NOT NULL,
                `UmbralAlertaPorcentaje` decimal(18,4) NULL,
                `BloqueadoAutomaticamente` tinyint(1) NOT NULL,
                `MotivoBloqueo` longtext CHARACTER SET utf8mb4 NULL,
                `BloqueadoUtc` datetime(6) NULL,
                `MontoExcepcion` decimal(18,4) NULL,
                `ExcepcionVigenteHastaUtc` datetime(6) NULL,
                `ExcepcionAutorizadaPor` longtext CHARACTER SET utf8mb4 NULL,
                `ExcepcionAutorizadaUtc` datetime(6) NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                `CreadoPorUsuarioId` int NULL,
                `CreadoPorNombreUsuario` longtext CHARACTER SET utf8mb4 NULL,
                `ActualizadoPorUsuarioId` int NULL,
                `ActualizadoPorNombreUsuario` longtext CHARACTER SET utf8mb4 NULL,
                CONSTRAINT `PK_CreditosCliente` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_CreditosCliente_ClienteId` CHECK (`ClienteId` > 0),
                CONSTRAINT `CK_CreditosCliente_Moneda` CHECK (CHAR_LENGTH(`Moneda`) = 3),
                CONSTRAINT `CK_CreditosCliente_LimiteCredito` CHECK (`LimiteCredito` >= 0),
                CONSTRAINT `CK_CreditosCliente_DiasCredito` CHECK (`DiasCredito` >= 0),
                CONSTRAINT `CK_CreditosCliente_UmbralAlerta` CHECK (`UmbralAlertaPorcentaje` IS NULL OR (`UmbralAlertaPorcentaje` > 0 AND `UmbralAlertaPorcentaje` <= 100)),
                CONSTRAINT `CK_CreditosCliente_MontoExcepcion` CHECK (`MontoExcepcion` IS NULL OR `MontoExcepcion` > 0),
                CONSTRAINT `FK_CreditosCliente_Clientes_ClienteId`
                    FOREIGN KEY (`ClienteId`) REFERENCES `Clientes` (`Id`) ON DELETE RESTRICT
            ) CHARACTER SET=utf8mb4 ENGINE=InnoDB;

            CREATE INDEX `IX_CreditosCliente_ClienteId`
                ON `CreditosCliente` (`ClienteId`);
            """);

        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N310CPostGuard;
            CREATE TEMPORARY TABLE __N310CPostGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N310C_PostGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N310CPostGuard (Id, Violaciones)
            SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
              FROM information_schema.tables
             WHERE table_schema = DATABASE()
               AND table_name = 'CreditosCliente';

            INSERT INTO __N310CPostGuard (Id, Violaciones)
            SELECT 2, CASE WHEN COUNT(*) = 6 THEN 0 ELSE 1 END
              FROM information_schema.table_constraints
             WHERE constraint_schema = DATABASE()
               AND constraint_type = 'CHECK'
               AND constraint_name IN (
                    'CK_CreditosCliente_ClienteId',
                    'CK_CreditosCliente_Moneda',
                    'CK_CreditosCliente_LimiteCredito',
                    'CK_CreditosCliente_DiasCredito',
                    'CK_CreditosCliente_UmbralAlerta',
                    'CK_CreditosCliente_MontoExcepcion');

            INSERT INTO __N310CPostGuard (Id, Violaciones)
            SELECT 3, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
              FROM information_schema.referential_constraints
             WHERE constraint_schema = DATABASE()
               AND constraint_name = 'FK_CreditosCliente_Clientes_ClienteId';

            INSERT INTO __N310CPostGuard (Id, Violaciones)
            SELECT 4, CASE WHEN COUNT(DISTINCT index_name) = 1 THEN 0 ELSE 1 END
              FROM information_schema.statistics
             WHERE table_schema = DATABASE()
               AND table_name = 'CreditosCliente'
               AND index_name = 'IX_CreditosCliente_ClienteId';

            INSERT INTO __N310CPostGuard (Id, Violaciones)
            SELECT 5, COUNT(*) FROM CreditosCliente;

            DROP TEMPORARY TABLE __N310CPostGuard;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TEMPORARY TABLE IF EXISTS __N310CDownGuard;
            CREATE TEMPORARY TABLE __N310CDownGuard
            (
                Id TINYINT NOT NULL PRIMARY KEY,
                Violaciones BIGINT NOT NULL,
                CONSTRAINT CK_N310C_DownGuard_Cero CHECK (Violaciones = 0)
            );

            INSERT INTO __N310CDownGuard (Id, Violaciones)
            SELECT 1, COUNT(*) FROM CreditosCliente;

            DROP TEMPORARY TABLE __N310CDownGuard;
            """);

        migrationBuilder.DropTable(name: "CreditosCliente");
    }
}
