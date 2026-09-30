using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fases18SesionConfigPermisosCalculos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_Rol_Modulo_Accion",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_RolId",
                table: "RolPermisos");

            migrationBuilder.AddColumn<string>(
                name: "ImpuestoCodigoSnapshot",
                table: "VentaImpuestos",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "© 2026 Storefront. Todos los derechos reservados.")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Copyright",
                table: "EmpresaConfiguraciones",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "Gestión de Inventario")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DescripcionSistema",
                table: "EmpresaConfiguraciones",
                type: "varchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EncabezadoActivo",
                table: "EmpresaConfiguraciones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "EncabezadoTexto",
                table: "EmpresaConfiguraciones",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Facebook",
                table: "EmpresaConfiguraciones",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaActualizacion",
                table: "EmpresaConfiguraciones",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP(6)");

            migrationBuilder.AddColumn<string>(
                name: "FormatoFecha",
                table: "EmpresaConfiguraciones",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "dd/MM/yyyy")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "InformacionFiscal",
                table: "EmpresaConfiguraciones",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Instagram",
                table: "EmpresaConfiguraciones",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LogoPublicId",
                table: "EmpresaConfiguraciones",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MensajeLogin",
                table: "EmpresaConfiguraciones",
                type: "varchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "Inicia sesión para administrar Storefront")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Moneda",
                table: "EmpresaConfiguraciones",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "HNL")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "MostrarCopyright",
                table: "EmpresaConfiguraciones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreVisibleSistema",
                table: "EmpresaConfiguraciones",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "Storefront")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "PiePaginaActivo",
                table: "EmpresaConfiguraciones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PiePaginaTexto",
                table: "EmpresaConfiguraciones",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RazonSocial",
                table: "EmpresaConfiguraciones",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SitioWeb",
                table: "EmpresaConfiguraciones",
                type: "varchar(250)",
                maxLength: 250,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TextoFactura",
                table: "EmpresaConfiguraciones",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TextoLegal",
                table: "EmpresaConfiguraciones",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TextoReportes",
                table: "EmpresaConfiguraciones",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "UsarAnioAutomaticoCopyright",
                table: "EmpresaConfiguraciones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsApp",
                table: "EmpresaConfiguraciones",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ZonaHoraria",
                table: "EmpresaConfiguraciones",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "America/Tegucigalpa")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ImpuestoCodigoSnapshot",
                table: "CompraImpuestos",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_Rol_Modulo_Accion",
                table: "RolPermisos",
                columns: new[] { "Rol", "Modulo", "Accion" });

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_RolId_Modulo_Accion",
                table: "RolPermisos",
                columns: new[] { "RolId", "Modulo", "Accion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_RolId_PermisoId",
                table: "RolPermisos",
                columns: new[] { "RolId", "PermisoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmpresaConfiguraciones_Activa",
                table: "EmpresaConfiguraciones",
                column: "Activa");

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_Rol_Modulo_Accion",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_RolId_Modulo_Accion",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_RolId_PermisoId",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_EmpresaConfiguraciones_Activa",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "ImpuestoCodigoSnapshot",
                table: "VentaImpuestos");

            migrationBuilder.DropColumn(
                name: "Copyright",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "DescripcionSistema",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "EncabezadoActivo",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "EncabezadoTexto",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "Facebook",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "FechaActualizacion",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "FormatoFecha",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "InformacionFiscal",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "Instagram",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "LogoPublicId",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "MensajeLogin",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "Moneda",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "MostrarCopyright",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "NombreVisibleSistema",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "PiePaginaActivo",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "PiePaginaTexto",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "RazonSocial",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "SitioWeb",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "TextoFactura",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "TextoLegal",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "TextoReportes",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "UsarAnioAutomaticoCopyright",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "WhatsApp",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "ZonaHoraria",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "ImpuestoCodigoSnapshot",
                table: "CompraImpuestos");

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_Rol_Modulo_Accion",
                table: "RolPermisos",
                columns: new[] { "Rol", "Modulo", "Accion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_RolId",
                table: "RolPermisos",
                column: "RolId");

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
