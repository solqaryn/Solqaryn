using Solqaryn.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations;

/// <summary>
/// Repara exclusivamente un estado parcialmente aplicado de
/// 20260808165129_M1EndurecerIntegridadRelacional.
///
/// MySQL confirma DDL de forma implícita. Si un despliegue se interrumpe durante
/// una migración extensa, EF puede no registrar la migración en
/// __EFMigrationsHistory aunque algunos DROP/ADD/CREATE ya hayan quedado aplicados.
/// En el siguiente arranque la migración se reintenta desde el inicio y puede fallar
/// al encontrar un índice ya eliminado o un artefacto ya creado.
///
/// Esta migración no cambia el modelo objetivo. Solo devuelve ese M1 pendiente a un
/// baseline reintentable. Si M1 ya figura en el historial, todas las operaciones son
/// deliberadamente no-op.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260808165128_M1ReconciliarReintentoIntegridadRelacional")]
public sealed class M1ReconciliarReintentoIntegridadRelacional : Migration
{
    private const string TargetMigration = "20260808165129_M1EndurecerIntegridadRelacional";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Si una ejecución anterior alcanzó el final de M1, nunca tocar ese esquema.
        // La condición TargetPending se incluye además en cada DDL para mantener
        // comportamiento fail-safe incluso si esta migración llega a un entorno donde
        // M1 ya fue aplicada antes de que esta reconciliación existiera.

        // 1) Retirar FKs que M1 pudo haber alcanzado a crear en un intento parcial.
        DropForeignKeyIfExists(migrationBuilder, "DescuentoCategorias", "FK_DescuentoCategorias_Categorias_CategoriaId");
        DropForeignKeyIfExists(migrationBuilder, "DescuentoClientes", "FK_DescuentoClientes_Clientes_ClienteId");
        DropForeignKeyIfExists(migrationBuilder, "DescuentoProductos", "FK_DescuentoProductos_Productos_ProductoId");
        DropForeignKeyIfExists(migrationBuilder, "DescuentoRoles", "FK_DescuentoRoles_Roles_RolId");
        DropForeignKeyIfExists(migrationBuilder, "ImpuestoCategorias", "FK_ImpuestoCategorias_Categorias_CategoriaId");
        DropForeignKeyIfExists(migrationBuilder, "ImpuestoClientes", "FK_ImpuestoClientes_Clientes_ClienteId");
        DropForeignKeyIfExists(migrationBuilder, "ImpuestoProductos", "FK_ImpuestoProductos_Productos_ProductoId");
        DropForeignKeyIfExists(migrationBuilder, "ImpuestoProveedores", "FK_ImpuestoProveedores_Proveedores_ProveedorId");

        // 2) Restaurar primero los índices simples legacy que respaldan FKs existentes.
        // Así los índices compuestos creados parcialmente pueden retirarse sin dejar
        // a InnoDB sin un índice válido para sus relaciones.
        CreateIndexIfMissing(migrationBuilder, "ImpuestoProveedores", "IX_ImpuestoProveedores_ImpuestoId", "`ImpuestoId`");
        CreateIndexIfMissing(migrationBuilder, "ImpuestoProductos", "IX_ImpuestoProductos_ImpuestoId", "`ImpuestoId`");
        CreateIndexIfMissing(migrationBuilder, "ImpuestoOperaciones", "IX_ImpuestoOperaciones_ImpuestoId", "`ImpuestoId`");
        CreateIndexIfMissing(migrationBuilder, "ImpuestoClientes", "IX_ImpuestoClientes_ImpuestoId", "`ImpuestoId`");
        CreateIndexIfMissing(migrationBuilder, "ImpuestoCategorias", "IX_ImpuestoCategorias_ImpuestoId", "`ImpuestoId`");
        CreateIndexIfMissing(migrationBuilder, "DescuentoRoles", "IX_DescuentoRoles_DescuentoId", "`DescuentoId`");
        CreateIndexIfMissing(migrationBuilder, "DescuentoProductos", "IX_DescuentoProductos_DescuentoId", "`DescuentoId`");
        CreateIndexIfMissing(migrationBuilder, "DescuentoClientes", "IX_DescuentoClientes_DescuentoId", "`DescuentoId`");
        CreateIndexIfMissing(migrationBuilder, "DescuentoCategorias", "IX_DescuentoCategorias_DescuentoId", "`DescuentoId`");

        // 3) Retirar índices de estado objetivo que pudieron quedar creados parcialmente.
        // Los dos IX_*_Nombre se recrean luego como índices transitorios baseline; no
        // se exige unicidad porque M1 precisamente deja de usar Nombre como identidad.
        DropIndexIfExists(migrationBuilder, "Proveedores", "IX_Proveedores_Nombre");
        DropIndexIfExists(migrationBuilder, "Proveedores", "UX_Proveedores_Documento_Normalizado");
        DropIndexIfExists(migrationBuilder, "ImpuestoProveedores", "IX_ImpuestoProveedores_ProveedorId");
        DropIndexIfExists(migrationBuilder, "ImpuestoProveedores", "UX_ImpuestoProveedores_Impuesto_Proveedor");
        DropIndexIfExists(migrationBuilder, "ImpuestoProductos", "IX_ImpuestoProductos_ProductoId");
        DropIndexIfExists(migrationBuilder, "ImpuestoProductos", "UX_ImpuestoProductos_Impuesto_Producto");
        DropIndexIfExists(migrationBuilder, "ImpuestoOperaciones", "UX_ImpuestoOperaciones_Impuesto_Operacion");
        DropIndexIfExists(migrationBuilder, "ImpuestoClientes", "IX_ImpuestoClientes_ClienteId");
        DropIndexIfExists(migrationBuilder, "ImpuestoClientes", "UX_ImpuestoClientes_Impuesto_Cliente");
        DropIndexIfExists(migrationBuilder, "ImpuestoCategorias", "IX_ImpuestoCategorias_CategoriaId");
        DropIndexIfExists(migrationBuilder, "ImpuestoCategorias", "UX_ImpuestoCategorias_Impuesto_Categoria");
        DropIndexIfExists(migrationBuilder, "DescuentoRoles", "IX_DescuentoRoles_RolId");
        DropIndexIfExists(migrationBuilder, "DescuentoRoles", "UX_DescuentoRoles_Descuento_Rol");
        DropIndexIfExists(migrationBuilder, "DescuentoProductos", "IX_DescuentoProductos_ProductoId");
        DropIndexIfExists(migrationBuilder, "DescuentoProductos", "UX_DescuentoProductos_Descuento_Producto");
        DropIndexIfExists(migrationBuilder, "DescuentoClientes", "IX_DescuentoClientes_ClienteId");
        DropIndexIfExists(migrationBuilder, "DescuentoClientes", "UX_DescuentoClientes_Descuento_Cliente");
        DropIndexIfExists(migrationBuilder, "DescuentoCategorias", "IX_DescuentoCategorias_CategoriaId");
        DropIndexIfExists(migrationBuilder, "DescuentoCategorias", "UX_DescuentoCategorias_Descuento_Categoria");
        DropIndexIfExists(migrationBuilder, "EmpresaConfiguraciones", "UX_EmpresaConfiguraciones_Activa");
        DropIndexIfExists(migrationBuilder, "CostosEnvio", "UX_CostosEnvio_PredeterminadoActivo");
        DropIndexIfExists(migrationBuilder, "Clientes", "IX_Clientes_Nombre");
        DropIndexIfExists(migrationBuilder, "Clientes", "UX_Clientes_IdentidadORTN_Normalizada");

        // 4) Retirar columnas generadas agregadas por un intento incompleto de M1.
        DropColumnIfExists(migrationBuilder, "Proveedores", "DocumentoNormalizado");
        DropColumnIfExists(migrationBuilder, "EmpresaConfiguraciones", "ActivaUnica");
        DropColumnIfExists(migrationBuilder, "CostosEnvio", "PredeterminadoActivoUnico");
        DropColumnIfExists(migrationBuilder, "Clientes", "IdentidadORTNNormalizada");

        // 5) M1 comienza eliminando estos nombres. Deben existir para que su secuencia
        // original pueda ejecutarse incluso si un despliegue anterior ya los retiró.
        // Se crean NO UNIQUE de forma transitoria para no bloquear datos legítimos que
        // M1 justamente permite repetir por Nombre.
        CreateIndexIfMissing(migrationBuilder, "Proveedores", "IX_Proveedores_Nombre", "`Nombre`");
        CreateIndexIfMissing(migrationBuilder, "Clientes", "IX_Clientes_Nombre", "`Nombre`");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No hay cambio de modelo propio que revertir. Esta migración es únicamente
        // una reconciliación previa y su efecto final es consumido inmediatamente por M1.
    }

    private static string TargetPendingCondition =>
        $"NOT EXISTS (SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '{TargetMigration}')";

    private static void DropIndexIfExists(MigrationBuilder migrationBuilder, string table, string index)
    {
        var exists = $"EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{Escape(table)}' AND INDEX_NAME = '{Escape(index)}')";
        ExecuteConditional(migrationBuilder, exists, $"ALTER TABLE `{EscapeIdentifier(table)}` DROP INDEX `{EscapeIdentifier(index)}`");
    }

    private static void CreateIndexIfMissing(MigrationBuilder migrationBuilder, string table, string index, string columnsSql)
    {
        var missing = $"NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{Escape(table)}' AND INDEX_NAME = '{Escape(index)}')";
        ExecuteConditional(migrationBuilder, missing, $"CREATE INDEX `{EscapeIdentifier(index)}` ON `{EscapeIdentifier(table)}` ({columnsSql})");
    }

    private static void DropColumnIfExists(MigrationBuilder migrationBuilder, string table, string column)
    {
        var exists = $"EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{Escape(table)}' AND COLUMN_NAME = '{Escape(column)}')";
        ExecuteConditional(migrationBuilder, exists, $"ALTER TABLE `{EscapeIdentifier(table)}` DROP COLUMN `{EscapeIdentifier(column)}`");
    }

    private static void DropForeignKeyIfExists(MigrationBuilder migrationBuilder, string table, string constraint)
    {
        var exists = $"EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = DATABASE() AND TABLE_NAME = '{Escape(table)}' AND CONSTRAINT_NAME = '{Escape(constraint)}' AND CONSTRAINT_TYPE = 'FOREIGN KEY')";
        ExecuteConditional(migrationBuilder, exists, $"ALTER TABLE `{EscapeIdentifier(table)}` DROP FOREIGN KEY `{EscapeIdentifier(constraint)}`");
    }

    private static void ExecuteConditional(MigrationBuilder migrationBuilder, string objectCondition, string ddl)
    {
        var escapedDdl = ddl.Replace("'", "''", StringComparison.Ordinal);
        migrationBuilder.Sql($"SET @solqaryn_m1_repair_sql = IF(({TargetPendingCondition}) AND ({objectCondition}), '{escapedDdl}', 'SELECT 1');");
        migrationBuilder.Sql("PREPARE solqaryn_m1_repair_stmt FROM @solqaryn_m1_repair_sql;");
        migrationBuilder.Sql("EXECUTE solqaryn_m1_repair_stmt;");
        migrationBuilder.Sql("DEALLOCATE PREPARE solqaryn_m1_repair_stmt;");
    }

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
    private static string EscapeIdentifier(string value) => value.Replace("`", "``", StringComparison.Ordinal);
}
