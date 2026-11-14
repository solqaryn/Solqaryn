using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260902195317_N43_3_C_AddConciliacionBancaria")]
    public partial class N43_3_C_AddConciliacionBancaria : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConciliacionesBancarias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CuentaBancariaId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SaldoInicialBanco = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoFinalBanco = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
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
                    table.PrimaryKey("PK_ConciliacionesBancarias", x => x.Id);
                    table.CheckConstraint("CK_ConciliacionesBancarias_CuentaBancariaId", "`CuentaBancariaId` > 0");
                    table.CheckConstraint("CK_ConciliacionesBancarias_SaldoInicialBanco", "`SaldoInicialBanco` >= 0");
                    table.CheckConstraint("CK_ConciliacionesBancarias_SaldoFinalBanco", "`SaldoFinalBanco` >= 0");
                    table.ForeignKey(
                        name: "FK_ConciliacionesBancarias_CuentasBancarias_CuentaBancariaId",
                        column: x => x.CuentaBancariaId,
                        principalTable: "CuentasBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MovimientosEstadoCuenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConciliacionBancariaId = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaMovimiento = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Concepto = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Referencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MovimientosEstadoCuenta", x => x.Id);
                    table.CheckConstraint("CK_MovimientosEstadoCuenta_ConciliacionBancariaId", "`ConciliacionBancariaId` > 0");
                    table.CheckConstraint("CK_MovimientosEstadoCuenta_Monto", "`Monto` > 0");
                    table.ForeignKey(
                        name: "FK_MovimientosEstadoCuenta_ConciliacionesBancarias_ConciliacionBancariaId",
                        column: x => x.ConciliacionBancariaId,
                        principalTable: "ConciliacionesBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MatchesConciliacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MovimientoEstadoCuentaId = table.Column<int>(type: "int", nullable: false),
                    MovimientoFinancieroId = table.Column<int>(type: "int", nullable: false),
                    MontoAplicado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipoMatch = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_MatchesConciliacion", x => x.Id);
                    table.CheckConstraint("CK_MatchesConciliacion_MovimientoEstadoCuentaId", "`MovimientoEstadoCuentaId` > 0");
                    table.CheckConstraint("CK_MatchesConciliacion_MovimientoFinancieroId", "`MovimientoFinancieroId` > 0");
                    table.CheckConstraint("CK_MatchesConciliacion_MontoAplicado", "`MontoAplicado` > 0");
                    table.ForeignKey(
                        name: "FK_MatchesConciliacion_MovimientosEstadoCuenta_MovimientoEstadoCuentaId",
                        column: x => x.MovimientoEstadoCuentaId,
                        principalTable: "MovimientosEstadoCuenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MatchesConciliacion_MovimientosFinancieros_MovimientoFinancieroId",
                        column: x => x.MovimientoFinancieroId,
                        principalTable: "MovimientosFinancieros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ConciliacionesBancarias_CuentaBancariaId",
                table: "ConciliacionesBancarias",
                column: "CuentaBancariaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConciliacionesBancarias_Cuenta_Periodo",
                table: "ConciliacionesBancarias",
                columns: new[] { "CuentaBancariaId", "FechaInicio", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_ConciliacionesBancarias_Estado",
                table: "ConciliacionesBancarias",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "UX_MovimientosEstadoCuenta_Conciliacion_IdempotencyKey",
                table: "MovimientosEstadoCuenta",
                columns: new[] { "ConciliacionBancariaId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosEstadoCuenta_Conciliacion_Fecha",
                table: "MovimientosEstadoCuenta",
                columns: new[] { "ConciliacionBancariaId", "FechaMovimiento" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosEstadoCuenta_Estado",
                table: "MovimientosEstadoCuenta",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "UX_MatchesConciliacion_MovimientoEstadoCuenta_MovimientoFinanciero",
                table: "MatchesConciliacion",
                columns: new[] { "MovimientoEstadoCuentaId", "MovimientoFinancieroId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchesConciliacion_MovimientoFinancieroId",
                table: "MatchesConciliacion",
                column: "MovimientoFinancieroId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MatchesConciliacion");
            migrationBuilder.DropTable(name: "MovimientosEstadoCuenta");
            migrationBuilder.DropTable(name: "ConciliacionesBancarias");
        }
    }
}
