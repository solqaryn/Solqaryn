using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260918162000_StorefrontFase7ProductoDestacado")]
    public partial class StorefrontFase7ProductoDestacado : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @solqaryn_add_es_destacado = IF(
                    (
                        SELECT COUNT(*)
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = DATABASE()
                          AND TABLE_NAME = 'Productos'
                          AND COLUMN_NAME = 'EsDestacado'
                    ) = 0,
                    'ALTER TABLE `Productos` ADD `EsDestacado` tinyint(1) NOT NULL DEFAULT FALSE',
                    'SELECT 1'
                );
                PREPARE solqaryn_stmt FROM @solqaryn_add_es_destacado;
                EXECUTE solqaryn_stmt;
                DEALLOCATE PREPARE solqaryn_stmt;
                """);

            migrationBuilder.Sql(
                """
                SET @solqaryn_add_es_destacado_idx = IF(
                    (
                        SELECT COUNT(*)
                        FROM INFORMATION_SCHEMA.STATISTICS
                        WHERE TABLE_SCHEMA = DATABASE()
                          AND TABLE_NAME = 'Productos'
                          AND INDEX_NAME = 'IX_Productos_EsDestacado_Activo'
                    ) = 0,
                    'CREATE INDEX `IX_Productos_EsDestacado_Activo` ON `Productos` (`EsDestacado`, `Activo`)',
                    'SELECT 1'
                );
                PREPARE solqaryn_stmt FROM @solqaryn_add_es_destacado_idx;
                EXECUTE solqaryn_stmt;
                DEALLOCATE PREPARE solqaryn_stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET @solqaryn_drop_es_destacado_idx = IF(
                    (
                        SELECT COUNT(*)
                        FROM INFORMATION_SCHEMA.STATISTICS
                        WHERE TABLE_SCHEMA = DATABASE()
                          AND TABLE_NAME = 'Productos'
                          AND INDEX_NAME = 'IX_Productos_EsDestacado_Activo'
                    ) > 0,
                    'DROP INDEX `IX_Productos_EsDestacado_Activo` ON `Productos`',
                    'SELECT 1'
                );
                PREPARE solqaryn_stmt FROM @solqaryn_drop_es_destacado_idx;
                EXECUTE solqaryn_stmt;
                DEALLOCATE PREPARE solqaryn_stmt;
                """);

            migrationBuilder.Sql(
                """
                SET @solqaryn_drop_es_destacado = IF(
                    (
                        SELECT COUNT(*)
                        FROM INFORMATION_SCHEMA.COLUMNS
                        WHERE TABLE_SCHEMA = DATABASE()
                          AND TABLE_NAME = 'Productos'
                          AND COLUMN_NAME = 'EsDestacado'
                    ) > 0,
                    'ALTER TABLE `Productos` DROP COLUMN `EsDestacado`',
                    'SELECT 1'
                );
                PREPARE solqaryn_stmt FROM @solqaryn_drop_es_destacado;
                EXECUTE solqaryn_stmt;
                DEALLOCATE PREPARE solqaryn_stmt;
                """);
        }
    }
}
