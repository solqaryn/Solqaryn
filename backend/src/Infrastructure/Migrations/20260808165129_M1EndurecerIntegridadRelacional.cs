using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M1EndurecerIntegridadRelacional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nombre deja de ser identidad de negocio para clientes/proveedores.
            migrationBuilder.DropIndex(
                name: "IX_Proveedores_Nombre",
                table: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "EmpresaConfiguraciones",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RTN",
                table: "EmpresaConfiguraciones",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "EmpresaConfiguraciones",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "EmpresaConfiguraciones",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "EmpresaConfiguraciones",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DocumentoNormalizado",
                table: "Proveedores",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                computedColumnSql: "NULLIF(LOWER(REPLACE(REPLACE(TRIM(Documento), '-', ''), ' ', '')), '')",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ActivaUnica",
                table: "EmpresaConfiguraciones",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                computedColumnSql: "IF(Activa = 1, 'ACTIVE', NULL)",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PredeterminadoActivoUnico",
                table: "CostosEnvio",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                computedColumnSql: "IF(EsPredeterminado = 1 AND Activo = 1 AND Eliminado = 0, 'DEFAULT', NULL)",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IdentidadORTNNormalizada",
                table: "Clientes",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                computedColumnSql: "NULLIF(LOWER(REPLACE(REPLACE(TRIM(IdentidadORTN), '-', ''), ' ', '')), '')",
                stored: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Nombre",
                table: "Proveedores",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "UX_Proveedores_Documento_Normalizado",
                table: "Proveedores",
                column: "DocumentoNormalizado",
                unique: true);

            // Primero se crean los índices compuestos que pueden respaldar las FKs
            // existentes hacia Impuestos/Descuentos. Solo después se retiran los
            // índices simples legacy; MySQL no permite hacerlo en el orden inverso.
            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoProveedores_ProveedorId",
                table: "ImpuestoProveedores",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "UX_ImpuestoProveedores_Impuesto_Proveedor",
                table: "ImpuestoProveedores",
                columns: new[] { "ImpuestoId", "ProveedorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoProductos_ProductoId",
                table: "ImpuestoProductos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "UX_ImpuestoProductos_Impuesto_Producto",
                table: "ImpuestoProductos",
                columns: new[] { "ImpuestoId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ImpuestoOperaciones_Impuesto_Operacion",
                table: "ImpuestoOperaciones",
                columns: new[] { "ImpuestoId", "Operacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoClientes_ClienteId",
                table: "ImpuestoClientes",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "UX_ImpuestoClientes_Impuesto_Cliente",
                table: "ImpuestoClientes",
                columns: new[] { "ImpuestoId", "ClienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoCategorias_CategoriaId",
                table: "ImpuestoCategorias",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "UX_ImpuestoCategorias_Impuesto_Categoria",
                table: "ImpuestoCategorias",
                columns: new[] { "ImpuestoId", "CategoriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoRoles_RolId",
                table: "DescuentoRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "UX_DescuentoRoles_Descuento_Rol",
                table: "DescuentoRoles",
                columns: new[] { "DescuentoId", "RolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoProductos_ProductoId",
                table: "DescuentoProductos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "UX_DescuentoProductos_Descuento_Producto",
                table: "DescuentoProductos",
                columns: new[] { "DescuentoId", "ProductoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoClientes_ClienteId",
                table: "DescuentoClientes",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "UX_DescuentoClientes_Descuento_Cliente",
                table: "DescuentoClientes",
                columns: new[] { "DescuentoId", "ClienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoCategorias_CategoriaId",
                table: "DescuentoCategorias",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "UX_DescuentoCategorias_Descuento_Categoria",
                table: "DescuentoCategorias",
                columns: new[] { "DescuentoId", "CategoriaId" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_ImpuestoProveedores_ImpuestoId",
                table: "ImpuestoProveedores");

            migrationBuilder.DropIndex(
                name: "IX_ImpuestoProductos_ImpuestoId",
                table: "ImpuestoProductos");

            migrationBuilder.DropIndex(
                name: "IX_ImpuestoOperaciones_ImpuestoId",
                table: "ImpuestoOperaciones");

            migrationBuilder.DropIndex(
                name: "IX_ImpuestoClientes_ImpuestoId",
                table: "ImpuestoClientes");

            migrationBuilder.DropIndex(
                name: "IX_ImpuestoCategorias_ImpuestoId",
                table: "ImpuestoCategorias");

            migrationBuilder.DropIndex(
                name: "IX_DescuentoRoles_DescuentoId",
                table: "DescuentoRoles");

            migrationBuilder.DropIndex(
                name: "IX_DescuentoProductos_DescuentoId",
                table: "DescuentoProductos");

            migrationBuilder.DropIndex(
                name: "IX_DescuentoClientes_DescuentoId",
                table: "DescuentoClientes");

            migrationBuilder.DropIndex(
                name: "IX_DescuentoCategorias_DescuentoId",
                table: "DescuentoCategorias");

            migrationBuilder.CreateIndex(
                name: "UX_EmpresaConfiguraciones_Activa",
                table: "EmpresaConfiguraciones",
                column: "ActivaUnica",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CostosEnvio_PredeterminadoActivo",
                table: "CostosEnvio",
                column: "PredeterminadoActivoUnico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "UX_Clientes_IdentidadORTN_Normalizada",
                table: "Clientes",
                column: "IdentidadORTNNormalizada",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DescuentoCategorias_Categorias_CategoriaId",
                table: "DescuentoCategorias",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DescuentoClientes_Clientes_ClienteId",
                table: "DescuentoClientes",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DescuentoProductos_Productos_ProductoId",
                table: "DescuentoProductos",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DescuentoRoles_Roles_RolId",
                table: "DescuentoRoles",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImpuestoCategorias_Categorias_CategoriaId",
                table: "ImpuestoCategorias",
                column: "CategoriaId",
                principalTable: "Categorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImpuestoClientes_Clientes_ClienteId",
                table: "ImpuestoClientes",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImpuestoProductos_Productos_ProductoId",
                table: "ImpuestoProductos",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImpuestoProveedores_Proveedores_ProveedorId",
                table: "ImpuestoProveedores",
                column: "ProveedorId",
                principalTable: "Proveedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DescuentoCategorias_Categorias_CategoriaId",
                table: "DescuentoCategorias");

            migrationBuilder.DropForeignKey(
                name: "FK_DescuentoClientes_Clientes_ClienteId",
                table: "DescuentoClientes");

            migrationBuilder.DropForeignKey(
                name: "FK_DescuentoProductos_Productos_ProductoId",
                table: "DescuentoProductos");

            migrationBuilder.DropForeignKey(
                name: "FK_DescuentoRoles_Roles_RolId",
                table: "DescuentoRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_ImpuestoCategorias_Categorias_CategoriaId",
                table: "ImpuestoCategorias");

            migrationBuilder.DropForeignKey(
                name: "FK_ImpuestoClientes_Clientes_ClienteId",
                table: "ImpuestoClientes");

            migrationBuilder.DropForeignKey(
                name: "FK_ImpuestoProductos_Productos_ProductoId",
                table: "ImpuestoProductos");

            migrationBuilder.DropForeignKey(
                name: "FK_ImpuestoProveedores_Proveedores_ProveedorId",
                table: "ImpuestoProveedores");

            // Restaurar primero los índices simples que respaldaban las FKs
            // originales; así los compuestos pueden retirarse sin romper MySQL.
            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoProveedores_ImpuestoId",
                table: "ImpuestoProveedores",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoProductos_ImpuestoId",
                table: "ImpuestoProductos",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoOperaciones_ImpuestoId",
                table: "ImpuestoOperaciones",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoClientes_ImpuestoId",
                table: "ImpuestoClientes",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_ImpuestoCategorias_ImpuestoId",
                table: "ImpuestoCategorias",
                column: "ImpuestoId");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoRoles_DescuentoId",
                table: "DescuentoRoles",
                column: "DescuentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoProductos_DescuentoId",
                table: "DescuentoProductos",
                column: "DescuentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoClientes_DescuentoId",
                table: "DescuentoClientes",
                column: "DescuentoId");

            migrationBuilder.CreateIndex(
                name: "IX_DescuentoCategorias_DescuentoId",
                table: "DescuentoCategorias",
                column: "DescuentoId");

            migrationBuilder.DropIndex(
                name: "IX_Proveedores_Nombre",
                table: "Proveedores");

            migrationBuilder.DropIndex(
                name: "UX_Proveedores_Documento_Normalizado",
                table: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_ImpuestoProveedores_ProveedorId",
                table: "ImpuestoProveedores");

            migrationBuilder.DropIndex(
                name: "UX_ImpuestoProveedores_Impuesto_Proveedor",
                table: "ImpuestoProveedores");

            migrationBuilder.DropIndex(
                name: "IX_ImpuestoProductos_ProductoId",
                table: "ImpuestoProductos");

            migrationBuilder.DropIndex(
                name: "UX_ImpuestoProductos_Impuesto_Producto",
                table: "ImpuestoProductos");

            migrationBuilder.DropIndex(
                name: "UX_ImpuestoOperaciones_Impuesto_Operacion",
                table: "ImpuestoOperaciones");

            migrationBuilder.DropIndex(
                name: "IX_ImpuestoClientes_ClienteId",
                table: "ImpuestoClientes");

            migrationBuilder.DropIndex(
                name: "UX_ImpuestoClientes_Impuesto_Cliente",
                table: "ImpuestoClientes");

            migrationBuilder.DropIndex(
                name: "IX_ImpuestoCategorias_CategoriaId",
                table: "ImpuestoCategorias");

            migrationBuilder.DropIndex(
                name: "UX_ImpuestoCategorias_Impuesto_Categoria",
                table: "ImpuestoCategorias");

            migrationBuilder.DropIndex(
                name: "UX_EmpresaConfiguraciones_Activa",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropIndex(
                name: "IX_DescuentoRoles_RolId",
                table: "DescuentoRoles");

            migrationBuilder.DropIndex(
                name: "UX_DescuentoRoles_Descuento_Rol",
                table: "DescuentoRoles");

            migrationBuilder.DropIndex(
                name: "IX_DescuentoProductos_ProductoId",
                table: "DescuentoProductos");

            migrationBuilder.DropIndex(
                name: "UX_DescuentoProductos_Descuento_Producto",
                table: "DescuentoProductos");

            migrationBuilder.DropIndex(
                name: "IX_DescuentoClientes_ClienteId",
                table: "DescuentoClientes");

            migrationBuilder.DropIndex(
                name: "UX_DescuentoClientes_Descuento_Cliente",
                table: "DescuentoClientes");

            migrationBuilder.DropIndex(
                name: "IX_DescuentoCategorias_CategoriaId",
                table: "DescuentoCategorias");

            migrationBuilder.DropIndex(
                name: "UX_DescuentoCategorias_Descuento_Categoria",
                table: "DescuentoCategorias");

            migrationBuilder.DropIndex(
                name: "UX_CostosEnvio_PredeterminadoActivo",
                table: "CostosEnvio");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "UX_Clientes_IdentidadORTN_Normalizada",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "DocumentoNormalizado",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "ActivaUnica",
                table: "EmpresaConfiguraciones");

            migrationBuilder.DropColumn(
                name: "PredeterminadoActivoUnico",
                table: "CostosEnvio");

            migrationBuilder.DropColumn(
                name: "IdentidadORTNNormalizada",
                table: "Clientes");

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "EmpresaConfiguraciones",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "RTN",
                table: "EmpresaConfiguraciones",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "LogoUrl",
                table: "EmpresaConfiguraciones",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Direccion",
                table: "EmpresaConfiguraciones",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(500)",
                oldMaxLength: 500,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Correo",
                table: "EmpresaConfiguraciones",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(150)",
                oldMaxLength: 150,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Nombre",
                table: "Proveedores",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes",
                column: "Nombre",
                unique: true);
        }
    }
}
