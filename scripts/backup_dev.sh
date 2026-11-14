#!/usr/bin/env bash
set -Eeuo pipefail

umask 077

fail() {
  echo "[SOLQARYN-BACKUP][ERROR] $*" >&2
  exit 1
}

log() {
  echo "[SOLQARYN-BACKUP] $*"
}

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || fail "Falta el comando requerido: $1"
}

require_env() {
  local name="$1"
  [[ -n "${!name:-}" ]] || fail "Falta la variable requerida: $name"
}

for cmd in mysql mysqldump tar sha256sum gpg python3 find git; do
  require_cmd "$cmd"
done

for name in SOLQARYN_ENVIRONMENT DB_HOST DB_PORT DB_NAME DB_USER DB_PASSWORD BACKUP_PASSPHRASE; do
  require_env "$name"
done

ENV_NORMALIZED="$(printf '%s' "$SOLQARYN_ENVIRONMENT" | tr '[:upper:]' '[:lower:]')"
DB_NORMALIZED="$(printf '%s' "$DB_NAME" | tr '[:upper:]' '[:lower:]')"
DB_SSL_MODE="${DB_SSL_MODE:-PREFERRED}"
DB_SSL_MODE="$(printf '%s' "$DB_SSL_MODE" | tr '[:lower:]' '[:upper:]')"

case "$ENV_NORMALIZED" in
  dev|development|ci) ;;
  *) fail "SOLQARYN solo permite backup de DEV/CI. Entorno recibido: $SOLQARYN_ENVIRONMENT" ;;
esac

case "$DB_SSL_MODE" in
  DISABLED|PREFERRED|REQUIRED|VERIFY_CA|VERIFY_IDENTITY) ;;
  *) fail "DB_SSL_MODE no soportado: $DB_SSL_MODE" ;;
esac

if [[ "$ENV_NORMALIZED" == *prod* || "$ENV_NORMALIZED" == *production* || "$DB_NORMALIZED" == *prod* || "$DB_NORMALIZED" == *production* ]]; then
  fail "Protección fail-closed: el entorno o la base parecen PROD. Operación abortada."
fi

REPO_ROOT="${REPO_ROOT:-$(git rev-parse --show-toplevel 2>/dev/null || pwd)}"
OUTPUT_DIR="${OUTPUT_DIR:-$REPO_ROOT/.solqaryn-backups}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"
BACKUP_LABEL="${BACKUP_LABEL:-dev}"

[[ "$RETENTION_DAYS" =~ ^[0-9]+$ ]] || fail "RETENTION_DAYS debe ser entero no negativo."
mkdir -p "$OUTPUT_DIR"
chmod 700 "$OUTPUT_DIR"

TIMESTAMP="$(date -u +'%Y%m%dT%H%M%SZ')"
BACKUP_ID="solqaryn-${BACKUP_LABEL}-${TIMESTAMP}"
WORKDIR="$(mktemp -d)"
PAYLOAD="$WORKDIR/$BACKUP_ID"
mkdir -p "$PAYLOAD/database" "$PAYLOAD/configuration" "$PAYLOAD/repository-docs" "$PAYLOAD/references"

cleanup() {
  rm -rf "$WORKDIR"
  unset MYSQL_PWD BACKUP_PASSPHRASE DB_PASSWORD
}
trap cleanup EXIT

export MYSQL_PWD="$DB_PASSWORD"
MYSQL=(mysql --protocol=TCP -h "$DB_HOST" -P "$DB_PORT" -u "$DB_USER" --default-character-set=utf8mb4 "--ssl-mode=$DB_SSL_MODE")
MYSQL_DB=("${MYSQL[@]}" "$DB_NAME")

log "Validando conectividad con la base autorizada usando SSL_MODE=$DB_SSL_MODE..."
"${MYSQL_DB[@]}" --batch --skip-column-names -e 'SELECT 1;' | grep -qx '1' || fail "No se pudo validar la conexión MySQL."

MYSQL_VERSION="$("${MYSQL_DB[@]}" --batch --skip-column-names -e 'SELECT VERSION();' | head -n 1)"
GIT_SHA="$(git -C "$REPO_ROOT" rev-parse HEAD 2>/dev/null || printf 'unknown')"
MIGRATION_TABLE_EXISTS="$("${MYSQL_DB[@]}" --batch --skip-column-names -e "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='__EFMigrationsHistory';" | head -n 1)"
if [[ "$MIGRATION_TABLE_EXISTS" == "1" ]]; then
  MIGRATION_COUNT="$("${MYSQL_DB[@]}" --batch --skip-column-names -e "SELECT COUNT(*) FROM __EFMigrationsHistory;" | head -n 1)"
else
  MIGRATION_COUNT="0"
fi
TABLE_COUNT="$("${MYSQL_DB[@]}" --batch --skip-column-names -e "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE';" | head -n 1)"

log "Generando dump transaccional MySQL..."
MYSQL_PWD="$DB_PASSWORD" mysqldump \
  --protocol=TCP \
  -h "$DB_HOST" \
  -P "$DB_PORT" \
  -u "$DB_USER" \
  "--ssl-mode=$DB_SSL_MODE" \
  --single-transaction \
  --skip-lock-tables \
  --quick \
  --hex-blob \
  --default-character-set=utf8mb4 \
  --no-tablespaces \
  --triggers \
  --set-gtid-purged=OFF \
  "$DB_NAME" > "$PAYLOAD/database/mysql.sql"

[[ -s "$PAYLOAD/database/mysql.sql" ]] || fail "El dump MySQL quedó vacío."

log "Registrando conteos exactos para validación de restauración..."
: > "$PAYLOAD/database/integrity-row-counts.tsv"
while IFS= read -r table_name; do
  [[ -n "$table_name" ]] || continue
  [[ "$table_name" =~ ^[A-Za-z0-9_]+$ ]] || fail "Nombre de tabla inesperado: $table_name"
  count="$("${MYSQL_DB[@]}" --batch --skip-column-names -e "SELECT COUNT(*) FROM \`$table_name\`;" | head -n 1)"
  printf '%s\t%s\n' "$table_name" "$count" >> "$PAYLOAD/database/integrity-row-counts.tsv"
done < <("${MYSQL_DB[@]}" --batch --skip-column-names -e "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME;")

export_reference_if_table_exists() {
  local table="$1"
  local query="$2"
  local output="$3"
  local exists
  exists="$("${MYSQL_DB[@]}" --batch --skip-column-names -e "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='$table';" | head -n 1)"
  if [[ "$exists" == "1" ]]; then
    "${MYSQL_DB[@]}" --batch --raw -e "$query" > "$output"
  else
    printf 'Tabla\tEstado\n%s\tNO_EXISTE\n' "$table" > "$output"
  fi
}

log "Exportando referencias de imágenes y documentos externos..."
export_reference_if_table_exists \
  'ProductoImagenes' \
  'SELECT Id, ProductoId, ProductoVarianteId, Url, PublicId, Orden, EsPrincipal, FechaCreacion FROM ProductoImagenes ORDER BY Id;' \
  "$PAYLOAD/references/producto-imagenes.tsv"

export_reference_if_table_exists \
  'CompraDocumentos' \
  'SELECT Id, CompraId, NombreOriginal, ContentType, SizeBytes, Url, PublicId, ResourceType, Eliminado, FechaCreacion FROM CompraDocumentos ORDER BY Id;' \
  "$PAYLOAD/references/compra-documentos.tsv"

log "Copiando configuración versionada segura (sin archivos de secretos)..."
SAFE_FILES=(
  'render.yaml'
  'Dockerfile'
  '.dockerignore'
  'backend/Dockerfile'
  'backend/src/API/appsettings.json'
  'frontend/angular.json'
  'frontend/package.json'
  'frontend/package-lock.json'
  'frontend/src/environments/environment.ts'
  'frontend/src/environments/environment.prod.ts'
)
for rel in "${SAFE_FILES[@]}"; do
  src="$REPO_ROOT/$rel"
  if [[ -f "$src" ]]; then
    dest="$PAYLOAD/configuration/$rel"
    mkdir -p "$(dirname "$dest")"
    cp -p "$src" "$dest"
  fi
done

if [[ -d "$REPO_ROOT/docs" ]]; then
  tar -C "$REPO_ROOT" -cf - docs | tar -C "$PAYLOAD/repository-docs" -xf -
fi

cat > "$PAYLOAD/ASSET_LISTADO.md" <<'EOF'
# Inventario respaldado SOLQARYN

- `database/mysql.sql`: estructura y datos MySQL de DEV.
- `database/integrity-row-counts.tsv`: conteos exactos por tabla para validar restore.
- `configuration/`: configuración versionada permitida por allowlist; nunca `.env`, certificados ni secretos locales.
- `repository-docs/docs/`: documentación versionada del proyecto.
- `references/producto-imagenes.tsv`: referencias URL/PublicId de imágenes de productos/variantes.
- `references/compra-documentos.tsv`: referencias URL/PublicId y metadata de documentos de compra.

Los binarios alojados en proveedores externos no se confunden con el dump SQL. Sus referencias y metadata quedan respaldadas para inventario, auditoría y recuperación controlada.
EOF

python3 - "$PAYLOAD/metadata.json" <<PY
import json, sys
from datetime import datetime, timezone
metadata = {
    "formatVersion": "SOLQARYN_BACKUP_V1",
    "backupId": "${BACKUP_ID}",
    "createdAtUtc": datetime.now(timezone.utc).isoformat(),
    "environment": "${SOLQARYN_ENVIRONMENT}",
    "databaseName": "${DB_NAME}",
    "databaseServerVersion": "${MYSQL_VERSION}",
    "databaseSslMode": "${DB_SSL_MODE}",
    "efMigrationCount": int("${MIGRATION_COUNT}"),
    "baseTableCount": int("${TABLE_COUNT}"),
    "gitCommit": "${GIT_SHA}",
    "retentionDays": int("${RETENTION_DAYS}"),
    "encryption": "OpenPGP symmetric AES256",
    "checksum": "SHA-256",
    "containsSecrets": False,
    "productionInScope": False,
    "assetStrategy": "database + safe config + repository docs + external asset references"
}
with open(sys.argv[1], "w", encoding="utf-8") as f:
    json.dump(metadata, f, ensure_ascii=False, indent=2)
    f.write("\n")
PY

log "Calculando checksums internos..."
(
  cd "$PAYLOAD"
  find . -type f ! -name 'MANIFEST.sha256' -print0 \
    | sort -z \
    | xargs -0 sha256sum > MANIFEST.sha256
)

ARCHIVE="$WORKDIR/$BACKUP_ID.tar.gz"
tar -C "$WORKDIR" -czf "$ARCHIVE" "$BACKUP_ID"
[[ -s "$ARCHIVE" ]] || fail "No se pudo crear el archivo de backup."

ENCRYPTED="$OUTPUT_DIR/$BACKUP_ID.tar.gz.gpg"
log "Cifrando backup antes de conservarlo..."
printf '%s' "$BACKUP_PASSPHRASE" \
  | gpg --batch --yes --no-tty --pinentry-mode loopback --passphrase-fd 0 \
      --symmetric --cipher-algo AES256 --digest-algo SHA256 \
      --output "$ENCRYPTED" "$ARCHIVE"

[[ -s "$ENCRYPTED" ]] || fail "El backup cifrado quedó vacío."
chmod 600 "$ENCRYPTED"

(
  cd "$OUTPUT_DIR"
  sha256sum "$(basename "$ENCRYPTED")" > "$(basename "$ENCRYPTED").sha256"
  chmod 600 "$(basename "$ENCRYPTED").sha256"
)

python3 - "$OUTPUT_DIR/$BACKUP_ID.meta.json" <<PY
import json, sys
from datetime import datetime, timezone
public = {
    "formatVersion": "SOLQARYN_BACKUP_V1",
    "backupId": "${BACKUP_ID}",
    "createdAtUtc": datetime.now(timezone.utc).isoformat(),
    "environment": "${SOLQARYN_ENVIRONMENT}",
    "databaseNameRedacted": True,
    "databaseServerVersion": "${MYSQL_VERSION}",
    "databaseSslMode": "${DB_SSL_MODE}",
    "gitCommit": "${GIT_SHA}",
    "retentionDays": int("${RETENTION_DAYS}"),
    "encrypted": True,
    "productionInScope": False
}
with open(sys.argv[1], "w", encoding="utf-8") as f:
    json.dump(public, f, ensure_ascii=False, indent=2)
    f.write("\n")
PY
chmod 600 "$OUTPUT_DIR/$BACKUP_ID.meta.json"

log "Aplicando retención local de $RETENTION_DAYS días solo al patrón SOLQARYN autorizado..."
find "$OUTPUT_DIR" -maxdepth 1 -type f \
  \( -name 'solqaryn-dev-*.tar.gz.gpg' -o -name 'solqaryn-dev-*.tar.gz.gpg.sha256' -o -name 'solqaryn-dev-*.meta.json' \) \
  -mtime "+$RETENTION_DAYS" -delete

log "Backup SOLQARYN creado y cifrado: $ENCRYPTED"
printf 'SOLQARYN_BACKUP_FILE=%s\n' "$ENCRYPTED"
printf 'SOLQARYN_BACKUP_SHA256=%s\n' "$ENCRYPTED.sha256"
printf 'SOLQARYN_BACKUP_METADATA=%s\n' "$OUTPUT_DIR/$BACKUP_ID.meta.json"
