-- SOLQARYN / Microfase 1: inspección de identidad, sin cambios a MySQL.
-- Ejecutar SOLO con la conexión GitHub Environment DEV.
-- La identidad autorizada para evaluar es el login EXACTO 'Javier'.
-- Nunca usar Usuario.RolId (legacy) como autorización de plataforma.
SET SESSION TRANSACTION READ ONLY;
START TRANSACTION READ ONLY;

SELECT CONCAT('DB=', DATABASE(), ';LOGIN_SCOPE=Javier');
SELECT CONCAT('EXACT_LOGIN_COUNT=', COUNT(*))
FROM Usuarios u WHERE BINARY u.NombreUsuario = BINARY 'Javier';
SELECT CONCAT('CASE_INSENSITIVE_LOGIN_COUNT=', COUNT(*))
FROM Usuarios u WHERE LOWER(u.NombreUsuario) = LOWER('Javier');
SELECT CONCAT('ACCOUNT_STATUS=', COALESCE((
    SELECT CASE
        WHEN u.Activo = 1 AND u.Bloqueado = 0 AND u.Eliminado = 0
            THEN 'ACTIVE'
        ELSE 'DISABLED_BLOCKED_OR_DELETED'
    END
    FROM Usuarios u
    WHERE BINARY u.NombreUsuario = BINARY 'Javier'
    LIMIT 1
), 'NOT_FOUND'));
SELECT CONCAT('LEGACY_ROLE=', COALESCE((
    SELECT CONCAT(r.Nombre, ';AMBITO=', r.Ambito,
                  ';ADMIN=', IF(r.EsAdministrador = 1, 'YES', 'NO'),
                  ';ROLE_ACTIVE=', IF(r.Activo = 1 AND r.Eliminado = 0, 'YES', 'NO'))
    FROM Usuarios u
    JOIN Roles r ON r.Id = u.RolId
    WHERE BINARY u.NombreUsuario = BINARY 'Javier'
    LIMIT 1
), 'NONE'));
-- El único permiso global válido requiere UsuarioRolesPlataforma + Rol de Plataforma,
-- ambos activos, usuario habilitado y sin EmpresaId en el rol.
SELECT CONCAT('PLATFORM_ASSIGNMENTS_TOTAL=', COUNT(*))
FROM UsuarioRolesPlataforma urp
JOIN Usuarios u ON u.Id = urp.UsuarioId
WHERE BINARY u.NombreUsuario = BINARY 'Javier';
SELECT CONCAT('ACTIVE_PLATFORM_ADMIN_ASSIGNMENTS=', COUNT(*))
FROM UsuarioRolesPlataforma urp
JOIN Usuarios u ON u.Id = urp.UsuarioId
JOIN Roles r ON r.Id = urp.RolId
WHERE BINARY u.NombreUsuario = BINARY 'Javier'
  AND u.Activo = 1 AND u.Bloqueado = 0 AND u.Eliminado = 0
  AND urp.Activa = 1
  AND r.Ambito = 2 AND r.EmpresaId IS NULL
  AND r.EsAdministrador = 1 AND r.Activo = 1 AND r.Eliminado = 0;
SELECT CONCAT('ACTIVE_PLATFORM_ADMIN_ROLE_CANDIDATES=', COUNT(*))
FROM Roles r
WHERE r.Ambito = 2 AND r.EmpresaId IS NULL
  AND r.EsAdministrador = 1 AND r.Activo = 1 AND r.Eliminado = 0;
SELECT CONCAT('ENTERPRISE_MEMBERSHIPS_TOTAL=', COUNT(*))
FROM UsuarioEmpresas m JOIN Usuarios u ON u.Id = m.UsuarioId
WHERE BINARY u.NombreUsuario = BINARY 'Javier';
SELECT CONCAT('ACTIVE_ENTERPRISE_MEMBERSHIPS=', COUNT(*))
FROM UsuarioEmpresas m
JOIN Usuarios u ON u.Id = m.UsuarioId
JOIN Empresas e ON e.Id = m.EmpresaId
JOIN Roles r ON r.Id = m.RolId
WHERE BINARY u.NombreUsuario = BINARY 'Javier'
  AND m.Activa = 1 AND e.Activa = 1
  AND r.Ambito = 1 AND r.Activo = 1 AND r.Eliminado = 0;
SELECT CONCAT('COMPANIES_ACTIVE=', COALESCE(SUM(CASE WHEN Activa = 1 THEN 1 ELSE 0 END), 0),
              ';COMPANIES_TOTAL=', COUNT(*))
FROM Empresas;

ROLLBACK;
