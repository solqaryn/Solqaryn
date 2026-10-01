using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260912121400_N67CConfiguracionEmpresaPersistence")]
    public partial class N67CConfiguracionEmpresaPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RTN",
                table: "Empresas",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "Empresas",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Empresas",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LogoPublicId",
                table: "Empresas",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_Empresas_RTN",
                table: "Empresas",
                column: "RTN",
                unique: true);

            migrationBuilder.CreateTable(
                name: "ConfigEmpresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Moneda = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, defaultValue: "HNL")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZonaHoraria = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: "America/Tegucigalpa")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ImpuestosJson = table.Column<string>(type: "longtext", nullable: false, defaultValue: "{}")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmisionJson = table.Column<string>(type: "longtext", nullable: false, defaultValue: "{}")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CorreoRemitente = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CorreoNombreRemitente = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CorreoConfigurado = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CorreoSecretoReferencia = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
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
                    table.PrimaryKey("PK_ConfigEmpresas", x => x.Id);
                    table.CheckConstraint("CK_ConfigEmpresas_Moneda", "CHAR_LENGTH(TRIM(`Moneda`)) = 3");
                    table.CheckConstraint("CK_ConfigEmpresas_Version", "`Version` > 0");
                    table.ForeignKey(
                        name: "FK_ConfigEmpresas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PlantillasCorreoEmpresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    TipoPlantilla = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Asunto = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cuerpo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_PlantillasCorreoEmpresa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantillasCorreoEmpresa_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_ConfigEmpresas_EmpresaId",
                table: "ConfigEmpresas",
                column: "EmpresaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PlantillasCorreoEmpresa_Empresa_Tipo",
                table: "PlantillasCorreoEmpresa",
                columns: new[] { "EmpresaId", "TipoPlantilla" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PlantillasCorreoEmpresa");
            migrationBuilder.DropTable(name: "ConfigEmpresas");

            migrationBuilder.DropIndex(
                name: "UX_Empresas_RTN",
                table: "Empresas");

            migrationBuilder.DropColumn(name: "RTN", table: "Empresas");
            migrationBuilder.DropColumn(name: "Direccion", table: "Empresas");
            migrationBuilder.DropColumn(name: "LogoUrl", table: "Empresas");
            migrationBuilder.DropColumn(name: "LogoPublicId", table: "Empresas");
        }
    }
}
