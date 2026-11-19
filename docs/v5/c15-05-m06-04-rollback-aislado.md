# C15-05.M06.04 — ensayo real de rollback/desactivación aislados (V22)

**Contrato:** SOLQARYN / solqaryn/Solqaryn@dev / PROJECT_SCOPE_LOCK=STRICT. Autor: DEV01 Task ID 6adbb9fff1548191a1bd5b30d9e9984a. **PLAN_MAESTRO D2339:** Ensayar rollback o desactivación sin borrar datos legítimos. PLAN E es meta acumulativa M06, no cierre automático de fase.

## A0: alcance e impacto

Riesgo R2: operación de revert y conservación de datos, 100 % aislada. Se crean un test Node 24 y workflow GitHub-hosted dirigido; se agrega sección autónoma del CHANGELOG. No se cambian backend/src, frontend/src, JWT/RBAC, MySQL, API, migraciones, proveedores, cuentas, Tasks, RRULE ni infraestructura activa. No se borran filas del Sheet institucional.

El ensayo ejecuta git init, commits reales locales de una desactivación operacional **sintética** y de actividad legítima posterior en un repositorio temporal sin remotos. Ejecuta git revert --no-edit REAL del commit de desactivación bajo guardas exactas de SHA, HEAD limpio, ancestro y rutas permitidas. No usa git reset, force-push ni credenciales.

## A1–A3: casos y límites

1. Positivo: revert restaura configuración habilitada sin borrar balances sintéticos de tenant A/B, BITACORA E0/E1 ni su historial; SHA-256 íntegros, repo limpio e historia aditiva de cuatro commits.
2. Negativo de protección: rechaza revert de commit que modifica datos empresariales y BITACORA, antes de cualquier cambio.
3. Negativo de concurrencia: expected HEAD obsoleto rechaza la operación y conserva la desactivación original.
4. Negativo de error: SHA malformado y worktree sucio fallan de forma cerrada.

A2: seguridad basada en allowlist de rutas, SHA, HEAD y exclusión de modificaciones de datos. A3 integración pertinente: comandos Git reales ejecutados dentro de un filesystem/repo efímeros GitHub-hosted, no un mock del comando. Las entidades y la bandera del ensayo son sintéticas, para impedir un efecto R3.

**No acreditado:** rollback distribuido de Google Sheets/namedRange, restauración MySQL 8.4/Aiven, JWT/RBAC HTTP, scheduler nativo, datos productivos, QA/main/PROD, migración o pagos. Nunca trasladar el PASS local a estos sistemas. La prueba R2 solo cubre la salida atómica D2339.

## A4–A5 y rollback

Exigir check dirigido V22 C15-05.M06.04 real isolated Git rollback con 4/4 completed/success exact-SHA, además de verify-solqaryn-project-scope, V5 procedencia CHANGELOG y CI pertinente. SKIPPED/CANCELLED/IN_PROGRESS/FAIL requeridos no son PASS. Publicar en dev por fast-forward CAS, con V17 namedRange en PLAN H:I fila 2339 y BITACORA idempotente; mantener VALIDANDO cuando la CI esté pendiente.

A5 pendiente: otra Task JAVIER distinta de DEV01 y de cualquier reparador debe revisar código, pruebas y límites independientemente. No se desbloquea DAG manualmente ni se certifica fase M06. Para revertir este changeset futuro: git revert normal tras HEAD/lock actuales, nunca reescritura de historia; sin borrar BITACORA institucional.
