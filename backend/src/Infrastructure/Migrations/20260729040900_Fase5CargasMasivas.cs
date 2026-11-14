using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fase5CargasMasivas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CargasMasivas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Tipo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NombreArchivo = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Extension = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContentType = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    HashArchivo = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DatosNormalizadosJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalFilas = table.Column<int>(type: "int", nullable: false),
                    FilasValidas = table.Column<int>(type: "int", nullable: false),
                    FilasConError = table.Column<int>(type: "int", nullable: false),
                    FilasConAdvertencia = table.Column<int>(type: "int", nullable: false),
                    FilasProcesadas = table.Column<int>(type: "int", nullable: false),
                    RegistrosCreados = table.Column<int>(type: "int", nullable: false),
                    RegistrosActualizados = table.Column<int>(type: "int", nullable: false),
                    FechaValidacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FechaConfirmacion = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConfirmadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ConfirmadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorGeneral = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargasMasivas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CargaMasivaErrores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CargaMasivaId = table.Column<int>(type: "int", nullable: false),
                    NumeroFila = table.Column<int>(type: "int", nullable: false),
                    Campo = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Codigo = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mensaje = table.Column<string>(type: "varchar(700)", maxLength: 700, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValorOriginal = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EsAdvertencia = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargaMasivaErrores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CargaMasivaErrores_CargasMasivas_CargaMasivaId",
                        column: x => x.CargaMasivaId,
                        principalTable: "CargasMasivas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CargaMasivaErrores_CargaMasivaId_NumeroFila",
                table: "CargaMasivaErrores",
                columns: new[] { "CargaMasivaId", "NumeroFila" });

            migrationBuilder.CreateIndex(
                name: "IX_CargasMasivas_Estado_FechaCreacion",
                table: "CargasMasivas",
                columns: new[] { "Estado", "FechaCreacion" });

            migrationBuilder.CreateIndex(
                name: "IX_CargasMasivas_Tipo_HashArchivo",
                table: "CargasMasivas",
                columns: new[] { "Tipo", "HashArchivo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CargaMasivaErrores");

            migrationBuilder.DropTable(
                name: "CargasMasivas");
        }
    }
}
