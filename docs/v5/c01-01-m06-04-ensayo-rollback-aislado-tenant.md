# SOLQARYN V5 — C01-01.M06.04: ensayo aislado de reversión sin pérdida de datos

**Unidad:** `C01-01.M06.04` (PLAN_MAESTRO fila 179); **autor:** PRIMARY DEV 05 JAVIER_CHATGPT, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Origen operativo:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, `PLAN_MAESTRO`, `COLA` y `DAG_TECNICO`; repositorio único `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** ensayo de Git con datos sintéticos y contrato de desactivación, no rollback de una versión real ni restauración de MySQL.

## Alcance, predecesores y protección de datos

El contrato M06 exige `Ensayar rollback o desactivación sin borrar datos legítimos` para el bloque C01-01 (identidad universal, membresía, tenant/sucursal y revocación). La elegibilidad individual se comprobó con PLAN H179=PENDIENTE, I179 vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI, y CLAIM único por CAS a EN_PROGRESO con owner DEV05. Se consultaron M06.01 (cobertura real versus dobles), M06.02 (manifiesto de SHA/blobs), M06.03 (riesgos R01–R12 y decisiones D01–D06) y M06.05 (solicitud A5), sin apropiarse del trabajo de sus autores.

Se eligió un **ensayo reversible en un repositorio Git temporal**: una flag de identidad está separada de las identidades y membresías legítimas. Un cambio que intente tocar datos del tenant se RECHAZA antes de publicarse. La reversión del commit que cambia únicamente la flag conserva los registros A/B. Ningún programa bajo `backend/`, `frontend/`, `Infrastructure/`, migraciones o configuraciones de proveedores es modificado en esta microtarea.

## Ensayo realmente ejecutado: Git temporal y fixtures 100 % sintéticos

**Entorno:** Git 2.47.3 y shell POSIX/bash en sandbox local aislado (15-11-2026 UTC), sin remoto ni conexión a servicios. Se inicializó un nuevo repositorio Git `dev` bajo directorio temporal fuera del checkout SOLQARYN. Se crearon `tenant-fixture.csv` con tres filas (A/user-a activo; B/user-b activo; B/user-c revocado) e `identity-feature.cfg` con `identity_feature_enabled=false`.

Secuencia reproducible, **únicamente en un directorio temporal recién creado y nunca en la carpeta de proyecto ni contra una base real**:

```bash
set -eu
dir="$(mktemp -d)"
cd "$dir"
git init --initial-branch=dev -q
git config user.name 'Solqaryn V5 synthetic rehearsal'
git config user.email 'v5-test@example.invalid'
printf 'tenant_id,member_id,state\nA,user-a,active\nB,user-b,active\nB,user-c,revoked\n' > tenant-fixture.csv
printf 'identity_feature_enabled=false\n' > identity-feature.cfg
git add tenant-fixture.csv identity-feature.cfg
git commit -qm 'baseline: synthetic tenants and feature disabled'
before="$(sha256sum tenant-fixture.csv | cut -d' ' -f1)"
printf 'identity_feature_enabled=true\n' > identity-feature.cfg
git add identity-feature.cfg
git commit -qm 'feature: enable synthetic identity feature'
test "$(git diff --name-only HEAD^ HEAD)" = 'identity-feature.cfg'
# Rechazo negativo: un intento de alterar datos no cumple la allowlist.
printf 'tenant_id,member_id,state\nA,user-a,active\n' > tenant-fixture.csv
test "$(git diff --name-only HEAD)" = 'tenant-fixture.csv'
git restore -- tenant-fixture.csv
git revert --no-edit HEAD
test "$(sha256sum tenant-fixture.csv | cut -d' ' -f1)" = "$before"
test "$(tail -n +2 tenant-fixture.csv | wc -l | tr -d ' ')" = 3
test "$(cat identity-feature.cfg)" = 'identity_feature_enabled=false'
```

**Resultado observado (6/6 aserciones/controles del ensayo local, NO xUnit/CI GitHub):**

| Control | Evidencia real | Resultado |
| --- | --- | --- |
| R1, diff acotado | Cambio publicado temporalmente sólo de `identity-feature.cfg` | PASS |
| R2, baseline preservado | Tres membresías sintéticas, incluidas B activa y B revocada | PASS |
| R3, rechazo negativo | Intento de modificar `tenant-fixture.csv` detectado antes de commit y descartado mediante `git restore` | PASS |
| R4, reversión concreta | `git revert --no-edit HEAD` generó un nuevo commit; flag volvió a `false` | PASS |
| R5, no pérdida del fixture | SHA-256 `8ade74dd43af6babefc3392df2e24dbc87a127ec470914701585918a46b1fcbc` sin cambio y 3/3 filas | PASS |
| R6, límite operativo | La ejecución ocurrió en repositorio temporal aislado, sin SQL/HTTP/JWT/EF, Aiven, QA, main o PROD | PASS (inspección de comandos) |

Identificadores **exclusivamente del repositorio de prueba, no de GitHub SOLQARYN**: baseline `c2dec664d2193fbb4e0f2ac9acfac6493c4dcec5`, feature `dffd3f79464c10e5d2d5e2e6de5179ccf8d36fdf`, revert `65b7b8891ef947ca4c6de800ace28d15d3e387e1`. Estos SHA no se usan para A4 de SOLQARYN ni se comparan con checks remotos.

## Runbook de reversión real futura — NO ejecutado ni autorizado por este ensayo

1. Identificar el commit productivo exacto, diff, archivos, owner, dependencias y evidencia A0–A5; bloquear únicamente el cambio concreto si un escritor o CI requerida sigue activo.
2. Requerir mecanismo de desactivación tipado y server-side que **deniegue por defecto**; nunca aceptar `tenant_id`, rol o pertenencia suministrada por cliente como autoridad. Preservar historial de membresías/revocaciones, auditar el cambio y retener claves/identidades válidas.
3. En un entorno **aislado y autorizado**, tomar snapshot verificable de conteos, integridad referencial, tenant A/B, sesiones/revocación y trazas antes y después; demostrar rechazo cruzado y de tenant no autorizado.
4. Si el diff es únicamente de código/configuración y la rama/permiso lo permite, realizar reversión con commit nuevo `git revert <SHA>` y CAS fast-forward, nunca reset/force-push. Las migraciones de BD exigen plan de respaldo, transformación reversible, test MySQL 8.4 efímero y autorización adicional; **Git revert no revierte registros persistidos**.
5. Exigir tests funcionales, negativos, aislamiento A/B, regresión y CI por SHA pertinente; comparar evidencias y decidir PASS/FAIL. Ante degradación, mantener el scope en VALIDANDO y reparar, sin marcar LISTO ni A5_PASS.
6. Dejar auditoría y readback idempotentes en PLAN/BITACORA, con revisión A5 por otra Task Javier. Ningún paso autoriza QA/main/PROD.

## A0–A5 y límites de certificación de la unidad

- **A0 (estática):** plan/DAG/COLA, autoría, exclusividad, fronteras de capas y diff previsto `docs/v5/...` + `CHANGELOG_AI.md`.
- **A1 (positivo/negativo):** ensayo aislado real de seis controles con `git revert`, comprobación de tres filas e intento de modificación del fixture rechazado. **No prueba rollback de una aplicación desplegada**.
- **A2 (seguridad):** no se tocaron usuarios reales, tokens, secretos, JWT, RBAC, pertenencias ni tenant A/B productivos; la prueba sintética conserva los tres registros.
- **A3 (integración):** **NO_APLICA a esta entrega documental**: no se ejecutan API, EF, MySQL, migraciones ni pruebas HTTP; los tests originales M05 se referencian con sus límites independientes, sin reatribuir PASS.
- **A4 (publicación):** sólo puede acreditarse después de commit GitHub individual dev fast-forward/CAS, diff/blob/documento/CHANGELOG, Scope Lock y control V5 de procedencia `completed/success` en SHA de publicación, PLAN/BITACORA y readbacks.
- **A5 (independiente):** `A5_PENDIENTE` para otra Task de las seis Javier distinta de DEV05. Ni este ensayo ni LISTO_DEV autorizan automáticamente el cierre `C01-01.M06.06`, QA/main/PROD o migraciones.

**Rollback de este documento:** `git revert` ordinario del commit que contiene exclusivamente el presente Markdown y la sección autocontenida de `CHANGELOG_AI.md`, sin reescribir historia ni borrar BITACORA. Cualquier ejecución de reversión sobre código real es un scope y gate distintos.
