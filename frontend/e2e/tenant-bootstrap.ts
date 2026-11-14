import { execFileSync } from 'node:child_process';

function parseConnectionString(value: string): Record<string, string> {
  return Object.fromEntries(
    value
      .split(';')
      .map(segment => segment.trim())
      .filter(Boolean)
      .map(segment => {
        const separator = segment.indexOf('=');
        return separator < 0
          ? [segment.toLowerCase(), '']
          : [segment.slice(0, separator).trim().toLowerCase(), segment.slice(separator + 1).trim()];
      })
  );
}

function sqlLiteral(value: string): string {
  return value.replaceAll("'", "''");
}

/**
 * Materializa exclusivamente en CI la membresía tenant del SeedAdmin dentro de
 * la base MySQL descartable del workflow. Los permisos productivos permanecen
 * fail-closed: este bootstrap no existe en runtime de la aplicación y sólo se
 * ejecuta cuando GitHub Actions expone CI=true y una conexión de prueba.
 */
export function ensureSeedAdminTenantMembershipForCi(tenantId: string): void {
  if (!process.env['CI']) return;

  const connectionString = process.env['ConnectionStrings__DefaultConnection'];
  const username = process.env['SeedAdmin__Username'] ?? process.env['PHASE7_ADMIN_USERNAME'];
  if (!connectionString || !username) return;

  const requestedTenantId = Number.parseInt(tenantId, 10);
  if (!Number.isInteger(requestedTenantId) || requestedTenantId <= 0) {
    throw new Error(`E2E_TENANT_ID inválido: ${tenantId}`);
  }

  const connection = parseConnectionString(connectionString);
  const host = connection['server'] ?? connection['host'];
  const port = connection['port'] ?? '3306';
  const database = connection['database'];
  const user = connection['user'] ?? connection['user id'] ?? connection['uid'];
  const password = connection['password'] ?? connection['pwd'] ?? '';

  if (!host || !database || !user) {
    throw new Error('No se pudo resolver host, database y user para el bootstrap tenant E2E.');
  }

  const safeUsername = sqlLiteral(username);
  const sql = `
    SET @tenant_id = ${requestedTenantId};
    INSERT INTO Empresas (Id, Nombre, Activa, FechaCreacion, FechaActualizacion)
    SELECT @tenant_id, CONCAT('CI Tenant ', @tenant_id), 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()
    WHERE NOT EXISTS (SELECT 1 FROM Empresas WHERE Id = @tenant_id);

    UPDATE Empresas
    SET Activa = 1, FechaActualizacion = UTC_TIMESTAMP()
    WHERE Id = @tenant_id;

    UPDATE UsuarioEmpresas ue
    INNER JOIN Usuarios u ON u.Id = ue.UsuarioId
    SET ue.RolId = u.RolId,
        ue.Activa = 1,
        ue.FechaActualizacion = UTC_TIMESTAMP()
    WHERE u.NombreUsuario = '${safeUsername}'
      AND ue.EmpresaId = @tenant_id;

    INSERT INTO UsuarioEmpresas (UsuarioId, EmpresaId, RolId, Activa, FechaCreacion, FechaActualizacion)
    SELECT u.Id, @tenant_id, u.RolId, 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()
    FROM Usuarios u
    WHERE u.NombreUsuario = '${safeUsername}'
      AND NOT EXISTS (
        SELECT 1
        FROM UsuarioEmpresas ue
        WHERE ue.UsuarioId = u.Id
          AND ue.EmpresaId = @tenant_id
      );
  `;

  const mysqlArgs = [
    '--protocol=tcp',
    '-h', host,
    '-P', port,
    '-u', user,
    database,
    '--batch',
    '--skip-column-names'
  ];
  const mysqlEnv = { ...process.env, MYSQL_PWD: password };

  execFileSync('mysql', [...mysqlArgs, '-e', sql], {
    stdio: 'inherit',
    env: mysqlEnv
  });

  const verificationSql = `
    SELECT COUNT(*)
    FROM UsuarioEmpresas ue
    INNER JOIN Usuarios u ON u.Id = ue.UsuarioId
    INNER JOIN Empresas e ON e.Id = ue.EmpresaId
    WHERE u.NombreUsuario = '${safeUsername}'
      AND ue.EmpresaId = ${requestedTenantId}
      AND ue.Activa = 1
      AND e.Activa = 1;
  `;
  const membershipCount = execFileSync(
    'mysql',
    [...mysqlArgs, '-e', verificationSql],
    { encoding: 'utf8', env: mysqlEnv }
  ).trim();

  if (membershipCount !== '1') {
    throw new Error(
      `Bootstrap tenant E2E inválido para ${username}: membresías activas=${membershipCount || '0'}.`
    );
  }
}
