using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// Finaliza la transición de los maestros normalizados creada en M1.
/// El backfill inicial preservó los IDs de CatalogosProducto mediante claves explícitas;
/// una vez preservadas esas referencias, las nuevas altas deben usar AUTO_INCREMENT.
/// MySQL conserva los IDs existentes y continúa desde MAX(Id) + 1.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260809134500_M1HabilitarIdentidadesMaestros")]
public sealed class M1HabilitarIdentidadesMaestros : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // MySQL no permite alterar una PK referenciada aunque únicamente se agregue
        // AUTO_INCREMENT. Se retiran temporalmente las FKs, sin tocar filas ni índices,
        // se altera la estrategia de identidad y se recrea exactamente la integridad.
        DropMasterForeignKeys(migrationBuilder);

        migrationBuilder.Sql("ALTER TABLE `Marcas` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT;");
        migrationBuilder.Sql("ALTER TABLE `Modelos` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT;");
        migrationBuilder.Sql("ALTER TABLE `Colores` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT;");
        migrationBuilder.Sql("ALTER TABLE `Tallas` MODIFY COLUMN `Id` int NOT NULL AUTO_INCREMENT;");

        AddMasterForeignKeys(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropMasterForeignKeys(migrationBuilder);

        migrationBuilder.Sql("ALTER TABLE `Modelos` MODIFY COLUMN `Id` int NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE `Marcas` MODIFY COLUMN `Id` int NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE `Colores` MODIFY COLUMN `Id` int NOT NULL;");
        migrationBuilder.Sql("ALTER TABLE `Tallas` MODIFY COLUMN `Id` int NOT NULL;");

        AddMasterForeignKeys(migrationBuilder);
    }

    private static void DropMasterForeignKeys(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ProductoVariantes_Marcas_MarcaId",
            table: "ProductoVariantes");
        migrationBuilder.DropForeignKey(
            name: "FK_ProductoVariantes_Modelos_ModeloId",
            table: "ProductoVariantes");
        migrationBuilder.DropForeignKey(
            name: "FK_ProductoVariantes_Colores_ColorId",
            table: "ProductoVariantes");
        migrationBuilder.DropForeignKey(
            name: "FK_ProductoVariantes_Tallas_TallaId",
            table: "ProductoVariantes");
        migrationBuilder.DropForeignKey(
            name: "FK_Modelos_Marcas_MarcaId",
            table: "Modelos");
    }

    private static void AddMasterForeignKeys(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            name: "FK_Modelos_Marcas_MarcaId",
            table: "Modelos",
            column: "MarcaId",
            principalTable: "Marcas",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_ProductoVariantes_Marcas_MarcaId",
            table: "ProductoVariantes",
            column: "MarcaId",
            principalTable: "Marcas",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_ProductoVariantes_Modelos_ModeloId",
            table: "ProductoVariantes",
            column: "ModeloId",
            principalTable: "Modelos",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_ProductoVariantes_Colores_ColorId",
            table: "ProductoVariantes",
            column: "ColorId",
            principalTable: "Colores",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(
            name: "FK_ProductoVariantes_Tallas_TallaId",
            table: "ProductoVariantes",
            column: "TallaId",
            principalTable: "Tallas",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
