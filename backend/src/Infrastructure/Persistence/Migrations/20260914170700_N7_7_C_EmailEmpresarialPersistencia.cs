using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Persistence.Migrations;

/// <summary>
/// N7.7.C: persistencia aditiva/reversible del correo empresarial durable.
/// Agrega únicamente cola/estado de entrega, idempotencia, correlación del
/// proveedor, bounce y versiones de plantilla. No contiene credenciales ni
/// valores secretos de SMTP/proveedor y no ejecuta backfill.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260914170700_N7_7_C_EmailEmpresarialPersistencia")]
public sealed class N7_7_C_EmailEmpresarialPersistencia : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE `PlantillasEmailEmpresarial` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `EmpresaId` int NOT NULL,
                `Codigo` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
                `Version` int NOT NULL,
                `Nombre` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                `AsuntoPlantilla` varchar(998) CHARACTER SET utf8mb4 NOT NULL,
                `CuerpoHtmlPlantilla` longtext CHARACTER SET utf8mb4 NOT NULL,
                `CuerpoTextoPlantilla` longtext CHARACTER SET utf8mb4 NULL,
                `Activa` tinyint(1) NOT NULL DEFAULT TRUE,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                CONSTRAINT `PK_PlantillasEmailEmpresarial` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_PlantillasEmailEmpresarial_Empresas_EmpresaId`
                    FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `CK_PlantillasEmail_EmpresaId_Positivo` CHECK (`EmpresaId` > 0),
                CONSTRAINT `CK_PlantillasEmail_Codigo_NoVacio` CHECK (CHAR_LENGTH(TRIM(`Codigo`)) > 0),
                CONSTRAINT `CK_PlantillasEmail_Version_Positiva` CHECK (`Version` > 0),
                CONSTRAINT `CK_PlantillasEmail_Nombre_NoVacio` CHECK (CHAR_LENGTH(TRIM(`Nombre`)) > 0),
                CONSTRAINT `CK_PlantillasEmail_Asunto_NoVacio` CHECK (CHAR_LENGTH(TRIM(`AsuntoPlantilla`)) > 0),
                CONSTRAINT `CK_PlantillasEmail_Html_NoVacio` CHECK (CHAR_LENGTH(TRIM(`CuerpoHtmlPlantilla`)) > 0)
            ) CHARACTER SET=utf8mb4;

            CREATE UNIQUE INDEX `UX_PlantillasEmail_Empresa_Codigo_Version`
                ON `PlantillasEmailEmpresarial` (`EmpresaId`, `Codigo`, `Version`);
            CREATE INDEX `IX_PlantillasEmail_Empresa_Codigo_Activa`
                ON `PlantillasEmailEmpresarial` (`EmpresaId`, `Codigo`, `Activa`);

            CREATE TABLE `EmailsEmpresariales` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `MensajeId` char(36) NOT NULL,
                `EmpresaId` int NOT NULL,
                `Destinatario` varchar(320) CHARACTER SET utf8mb4 NOT NULL,
                `Asunto` varchar(998) CHARACTER SET utf8mb4 NULL,
                `CuerpoHtml` longtext CHARACTER SET utf8mb4 NULL,
                `CuerpoTexto` longtext CHARACTER SET utf8mb4 NULL,
                `PlantillaCodigo` varchar(120) CHARACTER SET utf8mb4 NULL,
                `PlantillaVersion` int NULL,
                `VariablesJson` longtext CHARACTER SET utf8mb4 NULL,
                `ClaveIdempotencia` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
                `CorrelationId` varchar(160) CHARACTER SET utf8mb4 NULL,
                `Estado` int NOT NULL DEFAULT 0,
                `Intentos` int NOT NULL DEFAULT 0,
                `CreadoEnUtc` datetime(6) NOT NULL,
                `DisponibleDesdeUtc` datetime(6) NOT NULL,
                `ProcesandoDesdeUtc` datetime(6) NULL,
                `AceptadoProveedorEnUtc` datetime(6) NULL,
                `EntregadoEnUtc` datetime(6) NULL,
                `RebotadoEnUtc` datetime(6) NULL,
                `UltimoIntentoEnUtc` datetime(6) NULL,
                `ProviderMessageId` varchar(320) CHARACTER SET utf8mb4 NULL,
                `TipoRebote` int NULL,
                `CodigoRebote` varchar(160) CHARACTER SET utf8mb4 NULL,
                `UltimoError` varchar(2000) CHARACTER SET utf8mb4 NULL,
                `FechaCreacion` datetime(6) NOT NULL,
                `FechaActualizacion` datetime(6) NOT NULL,
                CONSTRAINT `PK_EmailsEmpresariales` PRIMARY KEY (`Id`),
                CONSTRAINT `FK_EmailsEmpresariales_Empresas_EmpresaId`
                    FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `CK_EmailsEmpresariales_EmpresaId_Positivo` CHECK (`EmpresaId` > 0),
                CONSTRAINT `CK_EmailsEmpresariales_Destinatario_NoVacio` CHECK (CHAR_LENGTH(TRIM(`Destinatario`)) > 0),
                CONSTRAINT `CK_EmailsEmpresariales_ClaveIdempotencia_NoVacia` CHECK (CHAR_LENGTH(TRIM(`ClaveIdempotencia`)) > 0),
                CONSTRAINT `CK_EmailsEmpresariales_Estado_Valido` CHECK (`Estado` BETWEEN 0 AND 6),
                CONSTRAINT `CK_EmailsEmpresariales_Intentos_NoNegativo` CHECK (`Intentos` >= 0),
                CONSTRAINT `CK_EmailsEmpresariales_Contenido_O_Plantilla` CHECK (
                    (`PlantillaCodigo` IS NULL AND `PlantillaVersion` IS NULL AND `VariablesJson` IS NULL AND `Asunto` IS NOT NULL AND `CuerpoHtml` IS NOT NULL)
                    OR
                    (`PlantillaCodigo` IS NOT NULL AND `PlantillaVersion` IS NOT NULL AND `PlantillaVersion` > 0 AND `VariablesJson` IS NOT NULL AND `Asunto` IS NULL AND `CuerpoHtml` IS NULL AND `CuerpoTexto` IS NULL)
                )
            ) CHARACTER SET=utf8mb4;

            CREATE UNIQUE INDEX `UX_EmailsEmpresariales_Empresa_MensajeId`
                ON `EmailsEmpresariales` (`EmpresaId`, `MensajeId`);
            CREATE UNIQUE INDEX `UX_EmailsEmpresariales_Empresa_Idempotencia`
                ON `EmailsEmpresariales` (`EmpresaId`, `ClaveIdempotencia`);
            CREATE INDEX `IX_EmailsEmpresariales_Claim`
                ON `EmailsEmpresariales` (`Estado`, `DisponibleDesdeUtc`, `Id`);
            CREATE INDEX `IX_EmailsEmpresariales_Empresa_Estado_Disponible`
                ON `EmailsEmpresariales` (`EmpresaId`, `Estado`, `DisponibleDesdeUtc`);
            CREATE UNIQUE INDEX `UX_EmailsEmpresariales_Empresa_ProviderMessageId`
                ON `EmailsEmpresariales` (`EmpresaId`, `ProviderMessageId`);
            CREATE INDEX `IX_EmailsEmpresariales_Empresa_Correlation`
                ON `EmailsEmpresariales` (`EmpresaId`, `CorrelationId`);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE `EmailsEmpresariales`;
            DROP TABLE `PlantillasEmailEmpresarial`;
            """);
    }
}
