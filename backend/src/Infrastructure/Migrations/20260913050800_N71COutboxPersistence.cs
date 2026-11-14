using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260913050800_N71COutboxPersistence")]
    public partial class N71COutboxPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MensajesOutbox",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EventoId = table.Column<Guid>(type: "char(36)", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    TipoEvento = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PayloadJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaveIdempotencia = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoAgregado = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IdAgregado = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CorrelationId = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Intentos = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreadoEnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DisponibleDesdeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcesandoDesdeUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EntregadoEnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UltimoIntentoEnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UltimoError = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensajesOutbox", x => x.Id);
                    table.CheckConstraint("CK_MensajesOutbox_EmpresaId_Positivo", "`EmpresaId` > 0");
                    table.CheckConstraint("CK_MensajesOutbox_Estado_Valido", "`Estado` BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_MensajesOutbox_Intentos_NoNegativo", "`Intentos` >= 0");
                    table.CheckConstraint("CK_MensajesOutbox_ClaveIdempotencia_NoVacia", "CHAR_LENGTH(TRIM(`ClaveIdempotencia`)) > 0");
                    table.CheckConstraint("CK_MensajesOutbox_Payload_NoVacio", "CHAR_LENGTH(TRIM(`PayloadJson`)) > 0");
                    table.CheckConstraint("CK_MensajesOutbox_TipoEvento_NoVacio", "CHAR_LENGTH(TRIM(`TipoEvento`)) > 0");
                    table.ForeignKey(
                        name: "FK_MensajesOutbox_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MensajesOutbox_Claim",
                table: "MensajesOutbox",
                columns: new[] { "Estado", "DisponibleDesdeUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_MensajesOutbox_EmpresaId_Estado_Disponible",
                table: "MensajesOutbox",
                columns: new[] { "EmpresaId", "Estado", "DisponibleDesdeUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_MensajesOutbox_EmpresaId_ClaveIdempotencia",
                table: "MensajesOutbox",
                columns: new[] { "EmpresaId", "ClaveIdempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MensajesOutbox_EventoId",
                table: "MensajesOutbox",
                column: "EventoId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MensajesOutbox");
        }
    }
}
