using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260827232000_N4_1_C_CajaPersistencia")]
    public partial class N4_1_C_CajaPersistencia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N41CPreGuard;
                CREATE TEMPORARY TABLE __N41CPreGuard (Id TINYINT NOT NULL PRIMARY KEY, Violaciones BIGINT NOT NULL, CONSTRAINT CK_N41C_PreGuard_Cero CHECK (Violaciones = 0));
                INSERT INTO __N41CPreGuard (Id, Violaciones)
                SELECT 1, COUNT(*) FROM information_schema.tables
                 WHERE table_schema = DATABASE() AND table_name IN ('Cajas','CajaSesiones','CajaMovimientos');
                DROP TEMPORARY TABLE __N41CPreGuard;
                """);

            migrationBuilder.CreateTable(
                name: "Cajas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    SesionActivaId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cajas", x => x.Id);
                    table.CheckConstraint("CK_Cajas_Estado", "`Estado` IN (1, 2)");
                    table.CheckConstraint("CK_Cajas_SesionActivaId", "`SesionActivaId` IS NULL OR `SesionActivaId` > 0");
                });

            migrationBuilder.CreateTable(
                name: "CajaSesiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CajaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FondoInicial = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalIngresos = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalRetiros = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalDepositos = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SaldoEsperado = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SaldoContado = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Diferencia = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ObservacionesArqueo = table.Column<string>(type: "longtext", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajaSesiones", x => x.Id);
                    table.CheckConstraint("CK_CajaSesiones_CajaId", "`CajaId` > 0");
                    table.CheckConstraint("CK_CajaSesiones_UsuarioId", "`UsuarioId` > 0");
                    table.CheckConstraint("CK_CajaSesiones_Estado", "`Estado` IN (1, 2, 3, 4)");
                    table.CheckConstraint("CK_CajaSesiones_FondoInicial", "`FondoInicial` >= 0");
                    table.CheckConstraint("CK_CajaSesiones_TotalIngresos", "`TotalIngresos` >= 0");
                    table.CheckConstraint("CK_CajaSesiones_TotalRetiros", "`TotalRetiros` >= 0");
                    table.CheckConstraint("CK_CajaSesiones_TotalDepositos", "`TotalDepositos` >= 0");
                    table.CheckConstraint("CK_CajaSesiones_SaldoContado", "`SaldoContado` IS NULL OR `SaldoContado` >= 0");
                    table.CheckConstraint("CK_CajaSesiones_FechaCierre", "(`Estado` = 4 AND `FechaCierre` IS NOT NULL) OR (`Estado` <> 4 AND `FechaCierre` IS NULL)");
                    table.ForeignKey("FK_CajaSesiones_Cajas_CajaId", x => x.CajaId, "Cajas", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_CajaSesiones_Usuarios_UsuarioId", x => x.UsuarioId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CajaMovimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CajaSesionId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Referencia = table.Column<string>(type: "longtext", nullable: false),
                    FechaOperacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CajaMovimientos", x => x.Id);
                    table.CheckConstraint("CK_CajaMovimientos_CajaSesionId", "`CajaSesionId` > 0");
                    table.CheckConstraint("CK_CajaMovimientos_UsuarioId", "`UsuarioId` > 0");
                    table.CheckConstraint("CK_CajaMovimientos_Tipo", "`Tipo` IN (1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_CajaMovimientos_Monto", "`Monto` > 0");
                    table.CheckConstraint("CK_CajaMovimientos_Referencia", "CHAR_LENGTH(TRIM(`Referencia`)) > 0");
                    table.ForeignKey("FK_CajaMovimientos_CajaSesiones_CajaSesionId", x => x.CajaSesionId, "CajaSesiones", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_CajaMovimientos_Usuarios_UsuarioId", x => x.UsuarioId, "Usuarios", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_Cajas_Estado", "Cajas", "Estado");
            migrationBuilder.CreateIndex("UX_Cajas_SesionActivaId", "Cajas", "SesionActivaId", unique: true);
            migrationBuilder.CreateIndex("IX_CajaSesiones_CajaId", "CajaSesiones", "CajaId");
            migrationBuilder.CreateIndex("IX_CajaSesiones_UsuarioId", "CajaSesiones", "UsuarioId");
            migrationBuilder.CreateIndex("IX_CajaSesiones_CajaId_Estado", "CajaSesiones", new[] { "CajaId", "Estado" });
            migrationBuilder.CreateIndex("IX_CajaMovimientos_Sesion_Fecha", "CajaMovimientos", new[] { "CajaSesionId", "FechaOperacion" });
            migrationBuilder.CreateIndex("IX_CajaMovimientos_UsuarioId", "CajaMovimientos", "UsuarioId");
            migrationBuilder.CreateIndex("IX_CajaMovimientos_Tipo", "CajaMovimientos", "Tipo");

            migrationBuilder.AddForeignKey(
                name: "FK_Cajas_CajaSesiones_SesionActivaId",
                table: "Cajas",
                column: "SesionActivaId",
                principalTable: "CajaSesiones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N41CPostGuard;
                CREATE TEMPORARY TABLE __N41CPostGuard (Id TINYINT NOT NULL PRIMARY KEY, Violaciones BIGINT NOT NULL, CONSTRAINT CK_N41C_PostGuard_Cero CHECK (Violaciones = 0));
                INSERT INTO __N41CPostGuard (Id, Violaciones)
                SELECT 1, CASE WHEN COUNT(*) = 3 THEN 0 ELSE 1 END FROM information_schema.tables
                 WHERE table_schema = DATABASE() AND table_name IN ('Cajas','CajaSesiones','CajaMovimientos');
                INSERT INTO __N41CPostGuard (Id, Violaciones)
                SELECT 2, CASE WHEN COUNT(*) = 5 THEN 0 ELSE 1 END FROM information_schema.referential_constraints
                 WHERE constraint_schema = DATABASE() AND constraint_name IN ('FK_Cajas_CajaSesiones_SesionActivaId','FK_CajaSesiones_Cajas_CajaId','FK_CajaSesiones_Usuarios_UsuarioId','FK_CajaMovimientos_CajaSesiones_CajaSesionId','FK_CajaMovimientos_Usuarios_UsuarioId');
                DROP TEMPORARY TABLE __N41CPostGuard;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TEMPORARY TABLE IF EXISTS __N41CDownGuard;
                CREATE TEMPORARY TABLE __N41CDownGuard (Id TINYINT NOT NULL PRIMARY KEY, Violaciones BIGINT NOT NULL, CONSTRAINT CK_N41C_DownGuard_Cero CHECK (Violaciones = 0));
                INSERT INTO __N41CDownGuard (Id, Violaciones)
                SELECT 1,
                    (SELECT COUNT(*) FROM Cajas) +
                    (SELECT COUNT(*) FROM CajaSesiones) +
                    (SELECT COUNT(*) FROM CajaMovimientos);
                DROP TEMPORARY TABLE __N41CDownGuard;
                """);

            migrationBuilder.DropForeignKey("FK_Cajas_CajaSesiones_SesionActivaId", "Cajas");
            migrationBuilder.DropTable("CajaMovimientos");
            migrationBuilder.DropTable("CajaSesiones");
            migrationBuilder.DropTable("Cajas");
        }
    }
}
