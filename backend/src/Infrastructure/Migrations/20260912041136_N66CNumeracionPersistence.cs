using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class N66CNumeracionPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecuenciasDocumento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    TipoDocumento = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UltimoValor = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    Prefijo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LongitudNumero = table.Column<int>(type: "int", nullable: false, defaultValue: 6),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    SucursalScopeKey = table.Column<int>(type: "int", nullable: false, computedColumnSql: "IFNULL(`SucursalId`, 0)", stored: true),
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
                    table.PrimaryKey("PK_SecuenciasDocumento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecuenciasDocumento_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecuenciasDocumento_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_SecuenciasDocumento_Empresa_Activa",
                table: "SecuenciasDocumento",
                columns: new[] { "EmpresaId", "Activa" });

            migrationBuilder.CreateIndex(
                name: "IX_SecuenciasDocumento_SucursalId",
                table: "SecuenciasDocumento",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "UX_SecuenciasDocumento_Empresa_Sucursal_Tipo",
                table: "SecuenciasDocumento",
                columns: new[] { "EmpresaId", "SucursalScopeKey", "TipoDocumento" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecuenciasDocumento");
        }
    }
}
