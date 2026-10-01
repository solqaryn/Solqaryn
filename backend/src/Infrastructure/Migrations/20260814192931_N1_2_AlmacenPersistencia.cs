using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <summary>
    /// ERP-N1.2 C: persistencia aditiva de Almacenes como hijos de Sucursales.
    /// No migra stock ni movimientos; esas autoridades pertenecen a ERP-N1.4.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20260814192931_N1_2_AlmacenPersistencia")]
    public partial class N1_2_AlmacenPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fail-closed: no adoptar una tabla Almacenes preexistente y exigir que
            // la dependencia Sucursales ya exista antes de crear la FK autoritativa.
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N12CGuard;
                CREATE TEMPORARY TABLE __N12CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N12C_Guard_Cero CHECK (Violaciones = 0)
                );

                INSERT INTO __N12CGuard (Id, Violaciones)
                SELECT 1, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'Almacenes';

                INSERT INTO __N12CGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'Sucursales';

                DROP TEMPORARY TABLE __N12CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "Almacenes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SucursalId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CodigoActivoUnico = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true, computedColumnSql: "IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)", stored: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Almacenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Almacenes_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Almacenes_SucursalId",
                table: "Almacenes",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_Almacenes_Tipo_Estado",
                table: "Almacenes",
                columns: new[] { "Tipo", "Activo", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "UX_Almacenes_Codigo_Activo",
                table: "Almacenes",
                column: "CodigoActivoUnico",
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE Almacenes
                    ADD CONSTRAINT CK_Almacenes_Codigo_NoVacio CHECK (CHAR_LENGTH(TRIM(Codigo)) > 0),
                    ADD CONSTRAINT CK_Almacenes_Nombre_NoVacio CHECK (CHAR_LENGTH(TRIM(Nombre)) > 0),
                    ADD CONSTRAINT CK_Almacenes_Tipo_Valido CHECK (Tipo BETWEEN 1 AND 5);
                """);

            // No existe histórico de Almacenes que backfillear: el snapshot lógico previo
            // es vacío. Este postcheck verifica estructura, FK, índices y checks físicos.
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N12CPostGuard;
                CREATE TEMPORARY TABLE __N12CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N12C_PostGuard_Cero CHECK (Violaciones = 0)
                );

                INSERT INTO __N12CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'Almacenes';

                INSERT INTO __N12CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(DISTINCT index_name) = 3 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'Almacenes'
                   AND index_name IN (
                        'UX_Almacenes_Codigo_Activo',
                        'IX_Almacenes_SucursalId',
                        'IX_Almacenes_Tipo_Estado');

                INSERT INTO __N12CPostGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.referential_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'Almacenes'
                   AND constraint_name = 'FK_Almacenes_Sucursales_SucursalId'
                   AND referenced_table_name = 'Sucursales';

                INSERT INTO __N12CPostGuard (Id, Violaciones)
                SELECT 4, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
                  FROM information_schema.table_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'Almacenes'
                   AND constraint_type = 'CHECK'
                   AND constraint_name IN (
                        'CK_Almacenes_Codigo_NoVacio',
                        'CK_Almacenes_Nombre_NoVacio',
                        'CK_Almacenes_Tipo_Valido');

                DROP TEMPORARY TABLE __N12CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback fail-closed: una tabla que ya recibió maestros no se destruye.
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N12CDownGuard;
                CREATE TEMPORARY TABLE __N12CDownGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N12C_DownGuard_Cero CHECK (Violaciones = 0)
                );

                INSERT INTO __N12CDownGuard (Id, Violaciones)
                SELECT 1, COUNT(*) FROM Almacenes;

                DROP TEMPORARY TABLE __N12CDownGuard;
                """);

            migrationBuilder.DropTable(name: "Almacenes");
        }
    }
}
