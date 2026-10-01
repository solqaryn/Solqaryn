using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solqaryn.Infrastructure.Migrations
{
    /// <summary>
    /// ERP-N0.4: consolida RBAC relacional sin borrar usuarios ni grants efectivos.
    /// La existencia de RolPermiso representa permiso concedido; la ausencia, denegado.
    /// </summary>
    public partial class N0_4_ConsolidarRbacRelacional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Asegura roles requeridos para poder transformar datos legacy antes de
            // convertir las FK relacionales en NOT NULL. No se elimina ningún usuario.
            migrationBuilder.Sql(@"
INSERT INTO Roles (Nombre, NombreNormalizado, Descripcion, EsSistema, EsAdministrador, Activo, Eliminado, FechaCreacion)
SELECT 'Administrador', 'ADMINISTRADOR', 'Rol de sistema con grants administrativos explícitos.', 1, 1, 1, 0, UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE NombreNormalizado = 'ADMINISTRADOR');
");

            migrationBuilder.Sql(@"
INSERT INTO Roles (Nombre, NombreNormalizado, Descripcion, EsSistema, EsAdministrador, Activo, Eliminado, FechaCreacion)
SELECT 'Vendedor', 'VENDEDOR', 'Rol de sistema para operación comercial con permisos administrables.', 1, 0, 1, 0, UTC_TIMESTAMP(6)
WHERE NOT EXISTS (SELECT 1 FROM Roles WHERE NombreNormalizado = 'VENDEDOR')
  AND (
      EXISTS (SELECT 1 FROM Usuarios WHERE UPPER(TRIM(Rol)) = 'VENDEDOR')
      OR EXISTS (SELECT 1 FROM RolPermisos WHERE Rol = 2)
  );
");

            // Preserva roles dinámicos que aún existan únicamente en Usuarios.Rol.
            migrationBuilder.Sql(@"
INSERT INTO Roles (Nombre, NombreNormalizado, Descripcion, EsSistema, EsAdministrador, Activo, Eliminado, FechaCreacion)
SELECT src.Nombre, src.NombreNormalizado, 'Rol migrado desde Usuarios.Rol por ERP-N0.4.', 0, 0, 1, 0, UTC_TIMESTAMP(6)
FROM (
    SELECT MIN(TRIM(Rol)) AS Nombre, UPPER(TRIM(Rol)) AS NombreNormalizado
    FROM Usuarios
    WHERE Rol IS NOT NULL AND TRIM(Rol) <> ''
    GROUP BY UPPER(TRIM(Rol))
) src
LEFT JOIN Roles r ON r.NombreNormalizado = src.NombreNormalizado
WHERE r.Id IS NULL;
");

            migrationBuilder.Sql(@"
UPDATE Usuarios u
JOIN Roles r ON r.NombreNormalizado = UPPER(TRIM(u.Rol))
SET u.RolId = r.Id
WHERE u.RolId IS NULL;
");

            // En N0.4 una denegación legacy se representa por ausencia de grant.
            // Retirarla antes del backfill evita que una fila Permitido=false ya
            // relacionada colisione con el grant efectivo que vamos a normalizar.
            migrationBuilder.Sql("DELETE FROM RolPermisos WHERE Permitido = 0;");

            // Retry-safe para bases reales con mezcla de filas legacy y relacionales:
            // si ya existe un grant relacional equivalente, conserva ese registro y
            // elimina sólo la copia legacy aún no mapeada.
            migrationBuilder.Sql(@"
DELETE legacy
FROM RolPermisos legacy
JOIN Roles targetRole ON (
    (legacy.Rol = 1 AND targetRole.NombreNormalizado = 'ADMINISTRADOR') OR
    (legacy.Rol = 2 AND targetRole.NombreNormalizado = 'VENDEDOR')
)
JOIN RolPermisos mapped
  ON mapped.RolId = targetRole.Id
 AND mapped.Modulo = legacy.Modulo
 AND mapped.Accion = legacy.Accion
 AND mapped.Permitido = 1
 AND mapped.Id <> legacy.Id
WHERE legacy.Permitido = 1
  AND legacy.RolId IS NULL;
");

            // También consolida duplicados puramente legacy antes de asignar RolId.
            // Se conserva determinísticamente la fila de menor Id para no perder el
            // grant efectivo y para respetar el índice único relacional existente.
            migrationBuilder.Sql(@"
DELETE newer
FROM RolPermisos newer
JOIN RolPermisos older
  ON older.Id < newer.Id
 AND older.Rol = newer.Rol
 AND older.Modulo = newer.Modulo
 AND older.Accion = newer.Accion
 AND older.Permitido = 1
 AND newer.Permitido = 1
 AND older.RolId IS NULL
 AND newer.RolId IS NULL
WHERE newer.RolId IS NULL;
");

            // RolUsuario histórico: Administrador=1, Vendedor=2.
            migrationBuilder.Sql(@"
UPDATE RolPermisos rp
JOIN Roles r ON (
    (rp.Rol = 1 AND r.NombreNormalizado = 'ADMINISTRADOR') OR
    (rp.Rol = 2 AND r.NombreNormalizado = 'VENDEDOR')
)
SET rp.RolId = r.Id
WHERE rp.Permitido = 1 AND rp.RolId IS NULL;
");

            // La base productiva puede tener grants legacy válidos cuyo par
            // Modulo/Accion aún no fue materializado en Permisos porque el seeder
            // moderno corre después de Database.MigrateAsync(). Preserva esos grants:
            // crea únicamente pares dentro de los enums históricos conocidos.
            migrationBuilder.Sql(@"
INSERT INTO Permisos
    (Codigo, Nombre, Descripcion, Modulo, Accion, EsSistema, Activo, Eliminado, FechaCreacion)
SELECT
    CONCAT('LEGACY.N04.', rp.Modulo, '.', rp.Accion),
    CONCAT('Permiso legacy ', rp.Modulo, ' - ', rp.Accion),
    'Permiso preservado desde RolPermisos legacy durante ERP-N0.4.',
    rp.Modulo,
    rp.Accion,
    1,
    1,
    0,
    UTC_TIMESTAMP(6)
FROM RolPermisos rp
LEFT JOIN Permisos p
  ON p.Modulo = rp.Modulo
 AND p.Accion = rp.Accion
WHERE rp.Permitido = 1
  AND p.Id IS NULL
  AND rp.Modulo BETWEEN 1 AND 31
  AND rp.Accion BETWEEN 1 AND 29
GROUP BY rp.Modulo, rp.Accion;
");

            // Normaliza PermisoId por el par legacy autoritativo. Esto también
            // repara un PermisoId parcial/obsoleto si el par sí es representable.
            migrationBuilder.Sql(@"
UPDATE RolPermisos rp
JOIN Permisos p ON p.Modulo = rp.Modulo AND p.Accion = rp.Accion
SET rp.PermisoId = p.Id
WHERE rp.Permitido = 1
  AND (rp.PermisoId IS NULL OR rp.PermisoId <> p.Id);
");

            // En el modelo N0.4 una denegación es ausencia de grant, no una fila Permitido=false.
            migrationBuilder.Sql("DELETE FROM RolPermisos WHERE Permitido = 0;");

            // Fail closed: si queda información que no puede representarse en el RBAC
            // normalizado se aborta la migración antes de retirar columnas legacy.
            // Guard fail-closed y diagnóstico compatible con Aiven: si queda
            // un usuario inválido, la colisión de PK incluye su Id/RolId en el error.
            migrationBuilder.Sql(@"
SET @n04_bad_user_detail := (
    SELECT CONCAT('BAD_USER:id=', u.Id, ':rolId=', COALESCE(CAST(u.RolId AS CHAR), 'NULL'))
    FROM Usuarios u
    WHERE u.RolId IS NULL
       OR NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Id = u.RolId)
    ORDER BY u.Id
    LIMIT 1
);
");
            migrationBuilder.Sql("DROP TEMPORARY TABLE IF EXISTS __n04_guard_users;");
            migrationBuilder.Sql("CREATE TEMPORARY TABLE __n04_guard_users (Detalle VARCHAR(191) NOT NULL PRIMARY KEY);");
            migrationBuilder.Sql("INSERT INTO __n04_guard_users (Detalle) SELECT @n04_bad_user_detail WHERE @n04_bad_user_detail IS NOT NULL;");
            migrationBuilder.Sql("INSERT INTO __n04_guard_users (Detalle) SELECT @n04_bad_user_detail WHERE @n04_bad_user_detail IS NOT NULL;");
            migrationBuilder.Sql("DROP TEMPORARY TABLE __n04_guard_users;");

            // Si queda un grant no representable, falla antes de retirar columnas
            // legacy y deja en el propio error los valores necesarios para diagnosticarlo.
            migrationBuilder.Sql(@"
SET @n04_bad_grant_detail := (
    SELECT CONCAT(
        'BAD_GRANT:id=', rp.Id,
        ':rol=', COALESCE(CAST(rp.Rol AS CHAR), 'NULL'),
        ':mod=', COALESCE(CAST(rp.Modulo AS CHAR), 'NULL'),
        ':acc=', COALESCE(CAST(rp.Accion AS CHAR), 'NULL'),
        ':rolId=', COALESCE(CAST(rp.RolId AS CHAR), 'NULL'),
        ':permisoId=', COALESCE(CAST(rp.PermisoId AS CHAR), 'NULL')
    )
    FROM RolPermisos rp
    WHERE rp.RolId IS NULL
       OR rp.PermisoId IS NULL
       OR NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Id = rp.RolId)
       OR NOT EXISTS (SELECT 1 FROM Permisos p WHERE p.Id = rp.PermisoId)
    ORDER BY rp.Id
    LIMIT 1
);
");
            migrationBuilder.Sql("DROP TEMPORARY TABLE IF EXISTS __n04_guard_grants;");
            migrationBuilder.Sql("CREATE TEMPORARY TABLE __n04_guard_grants (Detalle VARCHAR(191) NOT NULL PRIMARY KEY);");
            migrationBuilder.Sql("INSERT INTO __n04_guard_grants (Detalle) SELECT @n04_bad_grant_detail WHERE @n04_bad_grant_detail IS NOT NULL;");
            migrationBuilder.Sql("INSERT INTO __n04_guard_grants (Detalle) SELECT @n04_bad_grant_detail WHERE @n04_bad_grant_detail IS NOT NULL;");
            migrationBuilder.Sql("DROP TEMPORARY TABLE __n04_guard_grants;");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_Rol_Modulo_Accion",
                table: "RolPermisos");

            migrationBuilder.DropIndex(
                name: "IX_RolPermisos_RolId_Modulo_Accion",
                table: "RolPermisos");

            migrationBuilder.DropColumn(name: "Rol", table: "Usuarios");
            migrationBuilder.DropColumn(name: "Accion", table: "RolPermisos");
            migrationBuilder.DropColumn(name: "Modulo", table: "RolPermisos");
            migrationBuilder.DropColumn(name: "Permitido", table: "RolPermisos");
            migrationBuilder.DropColumn(name: "Rol", table: "RolPermisos");

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "RolPermisos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PermisoId",
                table: "RolPermisos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // EsAdministrador no autoriza por bypass. Si ya existe catálogo de permisos,
            // materializa grants explícitos para todos los administradores activos.
            migrationBuilder.Sql(@"
INSERT INTO RolPermisos (RolId, PermisoId)
SELECT r.Id, p.Id
FROM Roles r
CROSS JOIN Permisos p
LEFT JOIN RolPermisos rp ON rp.RolId = r.Id AND rp.PermisoId = p.Id
WHERE r.EsAdministrador = 1
  AND r.Activo = 1
  AND r.Eliminado = 0
  AND p.Activo = 1
  AND p.Eliminado = 0
  AND rp.Id IS NULL;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // El formato legacy solo admite RolUsuario Administrador/Vendedor en RolPermisos.
            // Bloquea un downgrade que colapsaría roles dinámicos y perdería seguridad.
            migrationBuilder.Sql(@"
SET @n04_down_unsupported := (
    SELECT COUNT(*)
    FROM RolPermisos rp
    JOIN Roles r ON r.Id = rp.RolId
    WHERE r.NombreNormalizado NOT IN ('ADMINISTRADOR', 'VENDEDOR')
);
");
            migrationBuilder.Sql(@"
SET @n04_bad_down_detail := (
    SELECT CONCAT('BAD_DOWNGRADE:rolId=', r.Id, ':rol=', r.NombreNormalizado)
    FROM RolPermisos rp
    JOIN Roles r ON r.Id = rp.RolId
    WHERE r.NombreNormalizado NOT IN ('ADMINISTRADOR', 'VENDEDOR')
    ORDER BY r.Id
    LIMIT 1
);
");
            migrationBuilder.Sql("DROP TEMPORARY TABLE IF EXISTS __n04_guard_down;");
            migrationBuilder.Sql("CREATE TEMPORARY TABLE __n04_guard_down (Detalle VARCHAR(191) NOT NULL PRIMARY KEY);");
            migrationBuilder.Sql("INSERT INTO __n04_guard_down (Detalle) SELECT @n04_bad_down_detail WHERE @n04_bad_down_detail IS NOT NULL;");
            migrationBuilder.Sql("INSERT INTO __n04_guard_down (Detalle) SELECT @n04_bad_down_detail WHERE @n04_bad_down_detail IS NOT NULL;");
            migrationBuilder.Sql("DROP TEMPORARY TABLE __n04_guard_down;");

            migrationBuilder.DropForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos");

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "Usuarios",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Rol",
                table: "Usuarios",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "RolId",
                table: "RolPermisos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PermisoId",
                table: "RolPermisos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(name: "Accion", table: "RolPermisos", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<int>(name: "Modulo", table: "RolPermisos", type: "int", nullable: false, defaultValue: 0);
            migrationBuilder.AddColumn<bool>(name: "Permitido", table: "RolPermisos", type: "tinyint(1)", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<int>(name: "Rol", table: "RolPermisos", type: "int", nullable: false, defaultValue: 0);

            migrationBuilder.Sql(@"
UPDATE Usuarios u
JOIN Roles r ON r.Id = u.RolId
SET u.Rol = LEFT(r.Nombre, 30);
");

            migrationBuilder.Sql(@"
UPDATE RolPermisos rp
JOIN Roles r ON r.Id = rp.RolId
JOIN Permisos p ON p.Id = rp.PermisoId
SET rp.Rol = CASE WHEN r.NombreNormalizado = 'ADMINISTRADOR' THEN 1 ELSE 2 END,
    rp.Modulo = p.Modulo,
    rp.Accion = p.Accion,
    rp.Permitido = 1;
");

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_Rol_Modulo_Accion",
                table: "RolPermisos",
                columns: new[] { "Rol", "Modulo", "Accion" });

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_RolId_Modulo_Accion",
                table: "RolPermisos",
                columns: new[] { "RolId", "Modulo", "Accion" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RolPermisos_Roles_RolId",
                table: "RolPermisos",
                column: "RolId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
