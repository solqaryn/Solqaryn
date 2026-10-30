#!/usr/bin/env bash
# MF3 DEV: lectura agregada de pertenencias A/B/C en Aiven, sin PII ni escrituras.
set -Eeuo pipefail
umask 077

stop() { printf 'MF3_PREFLIGHT=BLOCKED reason=%s\n' "$1" >&2; exit 1; }
[[ "${GITHUB_REPOSITORY:-}" == 'solqaryn/Solqaryn' ]] || stop 'repository'
[[ "${GITHUB_REF:-}" == 'refs/heads/dev' ]] || stop 'branch'
[[ "${DB_HOST:-}" == 'solqaryn-mysql-solqaryn.h.aivencloud.com' ]] || stop 'db_host'
[[ "${DB_PORT:-}" == '14402' ]] || stop 'db_port'
[[ "${DB_NAME:-}" == 'solqaryn_dev' ]] || stop 'db_name'
[[ "${DB_USER:-}" == 'solqaryn_dev_user' ]] || stop 'db_user'
[[ -n "${DB_PASSWORD:-}" ]] || stop 'db_secret_missing'
[[ -n "${GITHUB_ENV:-}" ]] || stop 'github_env_missing'

query() {
  MYSQL_PWD="$DB_PASSWORD" mysql \
    --protocol=TCP -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" \
    --ssl-mode=REQUIRED --batch --skip-column-names --silent "$DB_NAME" \
    --execute="SET SESSION TRANSACTION READ ONLY; START TRANSACTION READ ONLY; $1; COMMIT;"
}
[[ "$(query 'SELECT DATABASE()')" == 'solqaryn_dev' ]] || stop 'db_identity'
[[ "$(query "SELECT SUBSTRING_INDEX(CURRENT_USER(),'@',1)")" == 'solqaryn_dev_user' ]] || stop 'db_principal'
version="$(query 'SELECT VERSION()')"
[[ "$version" == 8.4.* ]] || stop 'db_version'
[[ "$(query 'SELECT COUNT(*) FROM Empresas WHERE Activa=1')" -ge 3 ]] || stop 'three_active_companies_missing'

declare -A seen_users=()
declare -A seen_companies=()
for label in A B C; do
  user_key="SOLQARYN_DEV_${label}_USERNAME"
  username="${!user_key:-}"
  [[ -n "$username" ]] || stop "username_${label}_missing"
  # Username va por UNHEX ASCII/UTF8; nunca interpolar texto no confiable en SQL.
  hex="$(printf '%s' "$username" | od -An -tx1 -v | tr -d ' \n')"
  [[ "$hex" =~ ^[[:xdigit:]]+$ ]] || stop "username_${label}_invalid"
  # La membresía exige usuario, empresa y rol empresarial activos y congruentes.
  rows="$(query "
    SELECT CONCAT(u.Id,':',ue.EmpresaId)
    FROM Usuarios u
    JOIN UsuarioEmpresas ue ON ue.UsuarioId=u.Id AND ue.Activa=1
    JOIN Empresas e ON e.Id=ue.EmpresaId AND e.Activa=1
    JOIN Roles r ON r.Id=ue.RolId AND r.Activo=1 AND r.Eliminado=0
    WHERE u.NombreUsuario=CONVERT(UNHEX('$hex') USING utf8mb4)
      AND u.Activo=1 AND u.Bloqueado=0 AND u.Eliminado=0
      AND r.Ambito=1
      AND (r.EmpresaId=e.Id OR (r.EmpresaId IS NULL AND r.EsSistema=1))
  ")"
  [[ "$rows" =~ ^[1-9][0-9]*:[1-9][0-9]*$ ]] ||
    stop "identity_${label}_not_exclusive_active_tenant"
  uid="${rows%%:*}"
  cid="${rows##*:}"
  [[ -z "${seen_users[$uid]:-}" ]] || stop 'duplicate_user_identity'
  [[ -z "${seen_companies[$cid]:-}" ]] || stop 'users_share_same_company'
  seen_users[$uid]=1
  seen_companies[$cid]=1
  printf 'MF3_COMPANY_%s_ID=%s\n' "$label" "$cid" >> "$GITHUB_ENV"
  if [[ "$label" == A || "$label" == B ]]; then
    role_id="$(query "
      SELECT Id
      FROM Roles
      WHERE EmpresaId=$cid AND Ambito=1 AND EsSistema=0
        AND Activo=1 AND Eliminado=0
      ORDER BY Id
      LIMIT 1
    ")"
    [[ "$role_id" =~ ^[1-9][0-9]*$ ]] || stop "tenant_role_${label}_missing"
    printf 'MF3_ROLE_%s_ID=%s\n' "$label" "$role_id" >> "$GITHUB_ENV"
    printf 'MF3_TENANT_ROLE_%s=PREFLIGHT_PASS\n' "$label"
  fi
  printf 'MF3_IDENTITY_%s=PREFLIGHT_PASS\n' "$label"
done

echo 'MF3_THREE_DISTINCT_ACTIVE_TENANTS=PASS'
