using System;
using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260914114200_N76CWhatsAppProviderPersistence")]
    public partial class N76CWhatsAppProviderPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesWhatsAppEmpresa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    NumeroTelefonoE164 = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TokenSecretoReferencia = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WebhookSecretoReferencia = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Activa = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                    CreadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ActualizadoPorUsuarioId = table.Column<int>(type: "int", nullable: true),
                    ActualizadoPorNombreUsuario = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesWhatsAppEmpresa", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracionesWhatsAppEmpresa_EmpresaId_Positivo", "`EmpresaId` > 0");
                    table.CheckConstraint("CK_ConfiguracionesWhatsAppEmpresa_Numero_E164", "`NumeroTelefonoE164` REGEXP '^\\+[0-9]{8,15}$'");
                    table.CheckConstraint("CK_ConfiguracionesWhatsAppEmpresa_Token_Referencia", "INSTR(`TokenSecretoReferencia`, '://') > 1");
                    table.CheckConstraint("CK_ConfiguracionesWhatsAppEmpresa_Version_Positiva", "`Version` > 0");
                    table.CheckConstraint("CK_ConfiguracionesWhatsAppEmpresa_Webhook_Referencia", "INSTR(`WebhookSecretoReferencia`, '://') > 1");
                    table.ForeignKey(
                        name: "FK_ConfiguracionesWhatsAppEmpresa_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "UX_ConfiguracionesWhatsAppEmpresa_EmpresaId",
                table: "ConfiguracionesWhatsAppEmpresa",
                column: "EmpresaId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ConfiguracionesWhatsAppEmpresa");
        }
    }
}
