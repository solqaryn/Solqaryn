using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Solqaryn.Infrastructure.Persistence;

#nullable disable
namespace Solqaryn.Infrastructure.Migrations.Oracle;

[DbContext(typeof(AppDbContext))]
[Migration("20261031140000_P1003ATemporaryPlatformSupportSessions")]
public sealed class P1003ATemporaryPlatformSupportSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE `PlataformaSesionesSoporte` (
                `Id` int NOT NULL AUTO_INCREMENT,
                `OperadorUsuarioId` int NOT NULL,
                `EmpresaId` int NOT NULL,
                `Modulo` int NOT NULL,
                `Motivo` varchar(500) NOT NULL,
                `TokenHash` char(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
                `CreadaUtc` datetime(6) NOT NULL,
                `ExpiraUtc` datetime(6) NOT NULL,
                `RevocadaUtc` datetime(6) NULL,
                `RevocadaPorUsuarioId` int NULL,
                `Version` int NOT NULL DEFAULT 1,
                CONSTRAINT `PK_PlataformaSesionesSoporte` PRIMARY KEY (`Id`),
                CONSTRAINT `CK_PlataformaSesionesSoporte_Modulo`
                    CHECK (`Modulo` IN (2,3)),
                CONSTRAINT `CK_PlataformaSesionesSoporte_Motivo`
                    CHECK (CHAR_LENGTH(TRIM(`Motivo`)) BETWEEN 10 AND 500),
                CONSTRAINT `CK_PlataformaSesionesSoporte_Vigencia`
                    CHECK (`ExpiraUtc` > `CreadaUtc`),
                CONSTRAINT `CK_PlataformaSesionesSoporte_Revocacion`
                    CHECK ((`RevocadaUtc` IS NULL AND `RevocadaPorUsuarioId` IS NULL) OR
                           (`RevocadaUtc` IS NOT NULL AND `RevocadaPorUsuarioId` IS NOT NULL)),
                UNIQUE KEY `UX_PlataformaSesionesSoporte_TokenHash` (`TokenHash`),
                KEY `IX_PlataformaSesionesSoporte_Empresa_Vigencia`
                    (`EmpresaId`, `ExpiraUtc`, `RevocadaUtc`),
                KEY `IX_PlataformaSesionesSoporte_Operador_Creada`
                    (`OperadorUsuarioId`, `CreadaUtc`),
                CONSTRAINT `FK_PlataformaSesionesSoporte_Usuarios_Operador`
                    FOREIGN KEY (`OperadorUsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_PlataformaSesionesSoporte_Empresas_Empresa`
                    FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas` (`Id`) ON DELETE RESTRICT,
                CONSTRAINT `FK_PlataformaSesionesSoporte_Usuarios_Revocador`
                    FOREIGN KEY (`RevocadaPorUsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "PlataformaSesionesSoporte");
}