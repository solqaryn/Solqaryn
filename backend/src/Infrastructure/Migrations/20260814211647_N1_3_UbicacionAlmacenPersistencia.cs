using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260814211647_N1_3_UbicacionAlmacenPersistencia")]
    public partial class N1_3_UbicacionAlmacenPersistencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N13CGuard;
                CREATE TEMPORARY TABLE __N13CGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N13C_Guard_Cero CHECK (Violaciones = 0)
                );

                INSERT INTO __N13CGuard (Id, Violaciones)
                SELECT 1, COUNT(*)
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen';

                INSERT INTO __N13CGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'Almacenes';

                DROP TEMPORARY TABLE __N13CGuard;
                """);

            migrationBuilder.CreateTable(
                name: "UbicacionesAlmacen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlmacenId = table.Column<int>(type: "int", nullable: false),
                    UbicacionPadreId = table.Column<int>(type: "int", nullable: true),
                    Codigo = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Eliminado = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EliminadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CodigoActivoUnico = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true, computedColumnSql: "IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)", stored: true)
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
                    table.PrimaryKey("PK_UbicacionesAlmacen", x => x.Id);
                    table.UniqueConstraint("AK_UbicacionesAlmacen_AlmacenId_Id", x => new { x.AlmacenId, x.Id });
                    table.ForeignKey(
                        name: "FK_UbicacionesAlmacen_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UbicacionesAlmacen_Padre_MismoAlmacen",
                        columns: x => new { x.AlmacenId, x.UbicacionPadreId },
                        principalTable: "UbicacionesAlmacen",
                        principalColumns: new[] { "AlmacenId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_UbicacionesAlmacen_AlmacenId",
                table: "UbicacionesAlmacen",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_UbicacionesAlmacen_Padre",
                table: "UbicacionesAlmacen",
                columns: new[] { "AlmacenId", "UbicacionPadreId" });

            migrationBuilder.CreateIndex(
                name: "IX_UbicacionesAlmacen_Tipo_Estado",
                table: "UbicacionesAlmacen",
                columns: new[] { "Tipo", "Activa", "Eliminado" });

            migrationBuilder.CreateIndex(
                name: "UX_UbicacionesAlmacen_Almacen_Codigo_Activo",
                table: "UbicacionesAlmacen",
                columns: new[] { "AlmacenId", "CodigoActivoUnico" },
                unique: true);

            migrationBuilder.Sql("""
                ALTER TABLE UbicacionesAlmacen
                    ADD CONSTRAINT CK_UbicacionesAlmacen_Codigo_NoVacio CHECK (CHAR_LENGTH(TRIM(Codigo)) > 0),
                    ADD CONSTRAINT CK_UbicacionesAlmacen_Nombre_NoVacio CHECK (CHAR_LENGTH(TRIM(Nombre)) > 0),
                    ADD CONSTRAINT CK_UbicacionesAlmacen_Tipo_Valido CHECK (Tipo BETWEEN 1 AND 6);
                """);

            // MySQL 8.4 no permite que un CHECK referencie Id AUTO_INCREMENT.
            // La misma invariante se protege físicamente con triggers fail-closed.
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_UbicacionesAlmacen_NoSelf_Insert
                AFTER INSERT ON UbicacionesAlmacen
                FOR EACH ROW
                BEGIN
                    IF NEW.UbicacionPadreId IS NOT NULL AND NEW.UbicacionPadreId = NEW.Id THEN
                        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Una ubicacion no puede ser su propio padre.';
                    END IF;
                END
                """);

            migrationBuilder.Sql("""
                CREATE TRIGGER TR_UbicacionesAlmacen_NoSelf_Update
                BEFORE UPDATE ON UbicacionesAlmacen
                FOR EACH ROW
                BEGIN
                    IF NEW.UbicacionPadreId IS NOT NULL AND NEW.UbicacionPadreId = NEW.Id THEN
                        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Una ubicacion no puede ser su propio padre.';
                    END IF;
                END
                """);

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N13CPostGuard;
                CREATE TEMPORARY TABLE __N13CPostGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N13C_PostGuard_Cero CHECK (Violaciones = 0)
                );

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.tables
                 WHERE table_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen';

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(DISTINCT index_name) = 4 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND index_name IN (
                        'UX_UbicacionesAlmacen_Almacen_Codigo_Activo',
                        'IX_UbicacionesAlmacen_AlmacenId',
                        'IX_UbicacionesAlmacen_Padre',
                        'IX_UbicacionesAlmacen_Tipo_Estado');

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 3, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.statistics
                 WHERE table_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND index_name = 'UX_UbicacionesAlmacen_Almacen_Codigo_Activo'
                   AND non_unique = 0;

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 4, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.table_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'AK_UbicacionesAlmacen_AlmacenId_Id'
                   AND constraint_type = 'UNIQUE';

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 5, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.referential_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'FK_UbicacionesAlmacen_Almacenes_AlmacenId'
                   AND referenced_table_name = 'Almacenes'
                   AND delete_rule = 'RESTRICT';

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 6, CASE WHEN COUNT(*) = 1 THEN 0 ELSE 1 END
                  FROM information_schema.referential_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'FK_UbicacionesAlmacen_Padre_MismoAlmacen'
                   AND referenced_table_name = 'UbicacionesAlmacen'
                   AND delete_rule = 'RESTRICT';

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 7, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.key_column_usage
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_name = 'FK_UbicacionesAlmacen_Padre_MismoAlmacen'
                   AND column_name IN ('AlmacenId', 'UbicacionPadreId');

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 8, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END
                  FROM information_schema.table_constraints
                 WHERE constraint_schema = DATABASE()
                   AND table_name = 'UbicacionesAlmacen'
                   AND constraint_type = 'CHECK'
                   AND constraint_name IN (
                        'CK_UbicacionesAlmacen_Codigo_NoVacio',
                        'CK_UbicacionesAlmacen_Nombre_NoVacio',
                        'CK_UbicacionesAlmacen_Tipo_Valido');

                INSERT INTO __N13CPostGuard (Id, Violaciones)
                SELECT 9, CASE WHEN COUNT(*) = 2 THEN 0 ELSE 1 END
                  FROM information_schema.triggers
                 WHERE trigger_schema = DATABASE()
                   AND event_object_table = 'UbicacionesAlmacen'
                   AND trigger_name IN (
                        'TR_UbicacionesAlmacen_NoSelf_Insert',
                        'TR_UbicacionesAlmacen_NoSelf_Update');

                DROP TEMPORARY TABLE __N13CPostGuard;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N13CDownGuard;
                CREATE TEMPORARY TABLE __N13CDownGuard
                (
                    Id TINYINT NOT NULL PRIMARY KEY,
                    Violaciones BIGINT NOT NULL,
                    CONSTRAINT CK_N13C_DownGuard_Cero CHECK (Violaciones = 0)
                );

                INSERT INTO __N13CDownGuard (Id, Violaciones)
                SELECT 1, COUNT(*) FROM UbicacionesAlmacen;

                DROP TEMPORARY TABLE __N13CDownGuard;
                """);

            migrationBuilder.DropTable(
                name: "UbicacionesAlmacen");
        }
    }
}
