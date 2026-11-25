using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations.Oracle
{
    public partial class CategoriasGalerias : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EnsureCreated de las bases de prueba ya incluye el modelo actual.
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS `CategoriasGalerias` (
                  `EmpresaId` int NOT NULL,
                  `CategoriaId` int NOT NULL,
                  `ImagenesJson` longtext NOT NULL,
                  `Version` varchar(32) NOT NULL,
                  PRIMARY KEY (`EmpresaId`, `CategoriaId`),
                  KEY `IX_CategoriasGalerias_CategoriaId` (`CategoriaId`),
                  CONSTRAINT `FK_CategoriasGalerias_Empresas_EmpresaId` FOREIGN KEY (`EmpresaId`) REFERENCES `Empresas` (`Id`) ON DELETE RESTRICT,
                  CONSTRAINT `FK_CategoriasGalerias_Categorias_CategoriaId` FOREIGN KEY (`CategoriaId`) REFERENCES `Categorias` (`Id`) ON DELETE RESTRICT
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Respaldar las galerías antes de revertir físicamente esta migración.
            migrationBuilder.DropTable("CategoriasGalerias");
        }
    }
}
