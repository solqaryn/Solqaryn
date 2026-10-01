using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260919011500_StorefrontFase12CuentaCliente")]
    public partial class StorefrontFase12CuentaCliente : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateTableIfMissing(
                migrationBuilder,
                "TiendaCuentasCliente",
                """
                CREATE TABLE TiendaCuentasCliente (
                    Id int NOT NULL AUTO_INCREMENT,
                    ClienteId int NOT NULL,
                    Nombre varchar(120) NOT NULL,
                    Correo varchar(160) NOT NULL,
                    CorreoNormalizado varchar(160) NOT NULL,
                    PasswordHash varchar(100) NOT NULL,
                    Activa tinyint(1) NOT NULL,
                    UltimoAccesoUtc datetime(6) NULL,
                    FechaCreacion datetime(6) NOT NULL,
                    FechaActualizacion datetime(6) NOT NULL,
                    CONSTRAINT PK_TiendaCuentasCliente PRIMARY KEY (Id),
                    CONSTRAINT FK_TiendaCuentasCliente_Clientes_ClienteId
                        FOREIGN KEY (ClienteId) REFERENCES Clientes (Id) ON DELETE RESTRICT
                ) CHARACTER SET=utf8mb4
                """);

            CreateTableIfMissing(
                migrationBuilder,
                "TiendaDireccionesCliente",
                """
                CREATE TABLE TiendaDireccionesCliente (
                    Id int NOT NULL AUTO_INCREMENT,
                    CuentaClienteId int NOT NULL,
                    Alias varchar(60) NOT NULL,
                    Recibe varchar(120) NOT NULL,
                    Telefono varchar(30) NOT NULL,
                    Direccion varchar(300) NOT NULL,
                    Predeterminada tinyint(1) NOT NULL,
                    FechaCreacion datetime(6) NOT NULL,
                    FechaActualizacion datetime(6) NOT NULL,
                    CONSTRAINT PK_TiendaDireccionesCliente PRIMARY KEY (Id),
                    CONSTRAINT FK_TiendaDireccionesCliente_TiendaCuentasCliente_CuentaClienteId
                        FOREIGN KEY (CuentaClienteId) REFERENCES TiendaCuentasCliente (Id) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4
                """);

            CreateTableIfMissing(
                migrationBuilder,
                "TiendaFavoritosCliente",
                """
                CREATE TABLE TiendaFavoritosCliente (
                    Id int NOT NULL AUTO_INCREMENT,
                    CuentaClienteId int NOT NULL,
                    ProductoId int NOT NULL,
                    FechaCreacion datetime(6) NOT NULL,
                    FechaActualizacion datetime(6) NOT NULL,
                    CONSTRAINT PK_TiendaFavoritosCliente PRIMARY KEY (Id),
                    CONSTRAINT FK_TiendaFavoritosCliente_Productos_ProductoId
                        FOREIGN KEY (ProductoId) REFERENCES Productos (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_TiendaFavoritosCliente_TiendaCuentasCliente_CuentaClienteId
                        FOREIGN KEY (CuentaClienteId) REFERENCES TiendaCuentasCliente (Id) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4
                """);

            CreateTableIfMissing(
                migrationBuilder,
                "TiendaSesionesCliente",
                """
                CREATE TABLE TiendaSesionesCliente (
                    Id int NOT NULL AUTO_INCREMENT,
                    CuentaClienteId int NOT NULL,
                    TokenHash char(64) NOT NULL,
                    ExpiraUtc datetime(6) NOT NULL,
                    RevocadaUtc datetime(6) NULL,
                    UltimoUsoUtc datetime(6) NULL,
                    FechaCreacion datetime(6) NOT NULL,
                    FechaActualizacion datetime(6) NOT NULL,
                    CONSTRAINT PK_TiendaSesionesCliente PRIMARY KEY (Id),
                    CONSTRAINT FK_TiendaSesionesCliente_TiendaCuentasCliente_CuentaClienteId
                        FOREIGN KEY (CuentaClienteId) REFERENCES TiendaCuentasCliente (Id) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4
                """);

            AddIndexIfMissing(migrationBuilder, "TiendaCuentasCliente", "UX_TiendaCuentasCliente_Correo",
                "CREATE UNIQUE INDEX UX_TiendaCuentasCliente_Correo ON TiendaCuentasCliente (CorreoNormalizado)");
            AddIndexIfMissing(migrationBuilder, "TiendaCuentasCliente", "UX_TiendaCuentasCliente_Cliente",
                "CREATE UNIQUE INDEX UX_TiendaCuentasCliente_Cliente ON TiendaCuentasCliente (ClienteId)");
            AddIndexIfMissing(migrationBuilder, "TiendaDireccionesCliente", "IX_TiendaDireccionesCliente_Cuenta_Predeterminada",
                "CREATE INDEX IX_TiendaDireccionesCliente_Cuenta_Predeterminada ON TiendaDireccionesCliente (CuentaClienteId, Predeterminada)");
            AddIndexIfMissing(migrationBuilder, "TiendaFavoritosCliente", "IX_TiendaFavoritosCliente_ProductoId",
                "CREATE INDEX IX_TiendaFavoritosCliente_ProductoId ON TiendaFavoritosCliente (ProductoId)");
            AddIndexIfMissing(migrationBuilder, "TiendaFavoritosCliente", "UX_TiendaFavoritosCliente_Cuenta_Producto",
                "CREATE UNIQUE INDEX UX_TiendaFavoritosCliente_Cuenta_Producto ON TiendaFavoritosCliente (CuentaClienteId, ProductoId)");
            AddIndexIfMissing(migrationBuilder, "TiendaSesionesCliente", "IX_TiendaSesionesCliente_Cuenta_Expira",
                "CREATE INDEX IX_TiendaSesionesCliente_Cuenta_Expira ON TiendaSesionesCliente (CuentaClienteId, ExpiraUtc)");
            AddIndexIfMissing(migrationBuilder, "TiendaSesionesCliente", "UX_TiendaSesionesCliente_TokenHash",
                "CREATE UNIQUE INDEX UX_TiendaSesionesCliente_TokenHash ON TiendaSesionesCliente (TokenHash)");

            migrationBuilder.Sql(
                """
                SET @solqaryn_fase12_schema_ok = (
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
                     WHERE TABLE_SCHEMA = DATABASE()
                       AND TABLE_NAME IN ('TiendaCuentasCliente','TiendaDireccionesCliente','TiendaFavoritosCliente','TiendaSesionesCliente')) = 4
                    AND
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                     WHERE TABLE_SCHEMA = DATABASE()
                       AND (
                         (TABLE_NAME='TiendaCuentasCliente' AND COLUMN_NAME IN ('Id','ClienteId','Nombre','Correo','CorreoNormalizado','PasswordHash','Activa','UltimoAccesoUtc','FechaCreacion','FechaActualizacion'))
                         OR (TABLE_NAME='TiendaDireccionesCliente' AND COLUMN_NAME IN ('Id','CuentaClienteId','Alias','Recibe','Telefono','Direccion','Predeterminada','FechaCreacion','FechaActualizacion'))
                         OR (TABLE_NAME='TiendaFavoritosCliente' AND COLUMN_NAME IN ('Id','CuentaClienteId','ProductoId','FechaCreacion','FechaActualizacion'))
                         OR (TABLE_NAME='TiendaSesionesCliente' AND COLUMN_NAME IN ('Id','CuentaClienteId','TokenHash','ExpiraUtc','RevocadaUtc','UltimoUsoUtc','FechaCreacion','FechaActualizacion'))
                       )) = 32
                    AND
                    (SELECT COUNT(DISTINCT INDEX_NAME) FROM INFORMATION_SCHEMA.STATISTICS
                     WHERE TABLE_SCHEMA = DATABASE()
                       AND INDEX_NAME IN (
                         'UX_TiendaCuentasCliente_Correo',
                         'UX_TiendaCuentasCliente_Cliente',
                         'IX_TiendaDireccionesCliente_Cuenta_Predeterminada',
                         'IX_TiendaFavoritosCliente_ProductoId',
                         'UX_TiendaFavoritosCliente_Cuenta_Producto',
                         'IX_TiendaSesionesCliente_Cuenta_Expira',
                         'UX_TiendaSesionesCliente_TokenHash'
                       )) = 7
                    AND
                    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
                     WHERE CONSTRAINT_SCHEMA = DATABASE()
                       AND CONSTRAINT_NAME IN (
                         'FK_TiendaCuentasCliente_Clientes_ClienteId',
                         'FK_TiendaDireccionesCliente_TiendaCuentasCliente_CuentaClienteId',
                         'FK_TiendaFavoritosCliente_Productos_ProductoId',
                         'FK_TiendaFavoritosCliente_TiendaCuentasCliente_CuentaClienteId',
                         'FK_TiendaSesionesCliente_TiendaCuentasCliente_CuentaClienteId'
                       )) = 5
                );
                SET @solqaryn_fase12_assert = IF(
                    @solqaryn_fase12_schema_ok,
                    'SELECT 1',
                    'SELECT * FROM __SOLQARYN_SCHEMA_MISMATCH_StorefrontFase12CuentaCliente__'
                );
                PREPARE solqaryn_stmt FROM @solqaryn_fase12_assert;
                EXECUTE solqaryn_stmt;
                DEALLOCATE PREPARE solqaryn_stmt;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TiendaDireccionesCliente");
            migrationBuilder.DropTable(name: "TiendaFavoritosCliente");
            migrationBuilder.DropTable(name: "TiendaSesionesCliente");
            migrationBuilder.DropTable(name: "TiendaCuentasCliente");
        }

        private static void CreateTableIfMissing(
            MigrationBuilder migrationBuilder,
            string table,
            string createSql)
        {
            var escapedCreateSql = createSql.Replace("'", "''", StringComparison.Ordinal);

            migrationBuilder.Sql(
                $"""
                SET @solqaryn_table_sql = IF(
                    (
                        SELECT COUNT(*)
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_SCHEMA = DATABASE()
                          AND TABLE_NAME = '{table}'
                    ) = 0,
                    '{escapedCreateSql}',
                    'SELECT 1'
                );
                PREPARE solqaryn_stmt FROM @solqaryn_table_sql;
                EXECUTE solqaryn_stmt;
                DEALLOCATE PREPARE solqaryn_stmt;
                """);
        }

        private static void AddIndexIfMissing(
            MigrationBuilder migrationBuilder,
            string table,
            string index,
            string createSql)
        {
            var escapedCreateSql = createSql.Replace("'", "''", StringComparison.Ordinal);

            migrationBuilder.Sql(
                $"""
                SET @solqaryn_index_sql = IF(
                    (
                        SELECT COUNT(*)
                        FROM INFORMATION_SCHEMA.STATISTICS
                        WHERE TABLE_SCHEMA = DATABASE()
                          AND TABLE_NAME = '{table}'
                          AND INDEX_NAME = '{index}'
                    ) = 0,
                    '{escapedCreateSql}',
                    'SELECT 1'
                );
                PREPARE solqaryn_stmt FROM @solqaryn_index_sql;
                EXECUTE solqaryn_stmt;
                DEALLOCATE PREPARE solqaryn_stmt;
                """);
        }
    }
}
