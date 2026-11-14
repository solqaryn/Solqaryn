-- ERP-N0.4 preflight: no modifica datos.
-- Debe devolver 0 antes de retirar columnas RBAC legacy.

SET @violaciones :=
    (SELECT COUNT(*)
       FROM Usuarios u
      WHERE u.RolId IS NULL
        AND (u.Rol IS NULL OR TRIM(u.Rol) = ''))
  + (SELECT COUNT(*)
       FROM Usuarios u
      WHERE u.RolId IS NOT NULL
        AND NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Id = u.RolId))
  + (SELECT COUNT(*)
       FROM RolPermisos rp
      WHERE rp.Permitido = 1
        AND rp.RolId IS NULL
        AND rp.Rol NOT IN (1, 2))
  + (SELECT COUNT(*)
       FROM RolPermisos rp
      WHERE rp.Permitido = 1
        AND rp.RolId IS NOT NULL
        AND NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Id = rp.RolId))
  + (SELECT COUNT(*)
       FROM RolPermisos rp
      WHERE rp.Permitido = 1
        AND rp.PermisoId IS NULL
        AND NOT EXISTS (
            SELECT 1
              FROM Permisos p
             WHERE p.Modulo = rp.Modulo
               AND p.Accion = rp.Accion
        )
        AND (rp.Modulo NOT BETWEEN 1 AND 31 OR rp.Accion NOT BETWEEN 1 AND 29))
  + (SELECT COUNT(*)
       FROM RolPermisos rp
      WHERE rp.Permitido = 1
        AND rp.PermisoId IS NOT NULL
        AND NOT EXISTS (SELECT 1 FROM Permisos p WHERE p.Id = rp.PermisoId)
        AND (rp.Modulo NOT BETWEEN 1 AND 31 OR rp.Accion NOT BETWEEN 1 AND 29));

SELECT @violaciones AS BloqueosN04;
