#!/usr/bin/env bash
set -euo pipefail

# Operación EXCLUSIVA sobre el MySQL efímero de CI. El script SQL de fixture
# retira primero las FKs empresariales; MySQL deja índices implícitos huérfanos.
# Sólo retiramos índices asociados exactamente a las FKs descartadas.
mysql_container="${1:?Se requiere el identificador del contenedor MySQL efímero}"
archivo="scripts/ci/commercial-tenant-legacy-fixture.sql"
test -f "$archivo"

awk '/^ALTER TABLE .* DROP FOREIGN KEY / {t=$3; n=$7; gsub(/[`;]/,"",t); gsub(/[`;]/,"",n); print t "\t" n;}' "$archivo" |
while IFS=$'\t' read -r tabla indice; do
    [[ -n "$tabla" && -n "$indice" ]] || continue
    encontrados="$(docker exec "$mysql_container" mysql -uroot -proot solqaryn_tenant_expand -Nse "SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='$tabla' AND INDEX_NAME='$indice';")"
    if [[ "$encontrados" -gt 0 ]]; then
        docker exec "$mysql_container" mysql -uroot -proot solqaryn_tenant_expand -e "ALTER TABLE \`$tabla\` DROP INDEX \`$indice\`;"
    fi
done
