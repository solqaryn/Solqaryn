-- ERP-N0.4 postdeploy: certifica autoridad RBAC relacional y ausencia legacy.
-- Debe devolver 0 después de aplicar la migración N0.4.

SET @legacy_columns := (
    SELECT COUNT(*)
      FROM information_schema.COLUMNS
     WHERE TABLE_SCHEMA = DATABASE()
       AND (
            (TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'Rol')
         OR (TABLE_NAME = 'RolPermisos' AND COLUMN_NAME IN ('Rol','Modulo','Accion','Permitido'))
       )
);

SET @usuarios_invalidos := (
    SELECT COUNT(*)
      FROM Usuarios u
     WHERE u.RolId IS NULL
        OR NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Id = u.RolId)
);

SET @grants_invalidos := (
    SELECT COUNT(*)
      FROM RolPermisos rp
     WHERE rp.RolId IS NULL
        OR rp.PermisoId IS NULL
        OR NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Id = rp.RolId)
        OR NOT EXISTS (SELECT 1 FROM Permisos p WHERE p.Id = rp.PermisoId)
);

SET @grants_duplicados := (
    SELECT COUNT(*)
      FROM (
          SELECT RolId, PermisoId
            FROM RolPermisos
           GROUP BY RolId, PermisoId
          HAVING COUNT(*) > 1
      ) d
);

SET @admin_grants_faltantes := (
    SELECT COUNT(*)
      FROM Roles r
      JOIN Permisos p ON p.Activo = 1 AND p.Eliminado = 0
      LEFT JOIN RolPermisos rp ON rp.RolId = r.Id AND rp.PermisoId = p.Id
     WHERE r.EsAdministrador = 1
       AND r.Activo = 1
       AND r.Eliminado = 0
       AND rp.Id IS NULL
);

SET @indice_unico_faltante := IF(
    EXISTS (
        SELECT 1
          FROM information_schema.STATISTICS
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME = 'RolPermisos'
           AND INDEX_NAME = 'IX_RolPermisos_RolId_PermisoId'
           AND NON_UNIQUE = 0
    ), 0, 1
);

SET @fk_rol_faltante := IF(
    EXISTS (
        SELECT 1
          FROM information_schema.KEY_COLUMN_USAGE
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME = 'RolPermisos'
           AND COLUMN_NAME = 'RolId'
           AND REFERENCED_TABLE_NAME = 'Roles'
           AND REFERENCED_COLUMN_NAME = 'Id'
    ), 0, 1
);

SET @fk_permiso_faltante := IF(
    EXISTS (
        SELECT 1
          FROM information_schema.KEY_COLUMN_USAGE
         WHERE TABLE_SCHEMA = DATABASE()
           AND TABLE_NAME = 'RolPermisos'
           AND COLUMN_NAME = 'PermisoId'
           AND REFERENCED_TABLE_NAME = 'Permisos'
           AND REFERENCED_COLUMN_NAME = 'Id'
    ), 0, 1
);

SET @migracion_faltante := IF(
    EXISTS (
        SELECT 1
          FROM __EFMigrationsHistory
         WHERE MigrationId = '20260811174745_N0_4_ConsolidarRbacRelacional'
    ), 0, 1
);

SET @violaciones := @legacy_columns
                  + @usuarios_invalidos
                  + @grants_invalidos
                  + @grants_duplicados
                  + @admin_grants_faltantes
                  + @indice_unico_faltante
                  + @fk_rol_faltante
                  + @fk_permiso_faltante
                  + @migracion_faltante;

SELECT @violaciones AS BloqueosN04;
