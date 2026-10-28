# P0-005.A — Reproducibilidad de backup y restore DEV: evidencia disponible y brecha

**Estado operativo:** `EN_PROGRESO / NO_GO_LISTO`. **Fecha:** 2026-10-26. **Proyecto:** SOLQARYN, rama `dev`, PR comercial pendiente. **Task propia:** `PRIMARY_DEV_02` / `6adc2e4bd8a48191ac7d8641ed95a1a3`, GitHub `jmejia31`. **Fuente comercial:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V3, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, L1 y L14, fila `PLAN_MAESTRO!10`. **Dependencia:** `P0-012.B=LISTO`; `COLA!G10=SI` en claim. **BASE_DEV:** `35c67e330f876ee66f8015e889ffa151d6d4643a`.

## Hallazgos de fuentes actuales y delimitación

- En el HEAD examinado, `.github/workflows/backup-dev-operativo.yml` orquesta el certificado de proveedor, un backup cifrado y un restore del **mismo artefacto** sobre MySQL local y descartable del runner. El workflow es **potencial**, no evidencia de que ese trabajo se haya ejecutado en este corte.
- `scripts/backup_dev.sh` usa `mysqldump --single-transaction --skip-lock-tables`, cifra AES256, anota checksum y conteos por tabla. `scripts/restore_dev.sh` verifica hash externo/interno, conteos de tablas y migraciones, datos fila por fila y restringe explícitamente el destino a `localhost/127.0.0.1/::1`, base descartable y marcador `ALLOW_DESTRUCTIVE_RESTORE`.
- Lectura GitHub Actions de 2026-10-26: run de workflow programado [40339821803](https://github.com/solqaryn/Solqaryn/actions/runs/40339821803), evento `schedule`, SHA `003cceea22bbd85a8f3f542c46816c6befbe5ee3`, conclusión **SKIPPED**. **No se acepta SKIPPED como backup ni restore.**
- Históricos `docs/CERTIFICACION_N8_9_RESTORE.md`, `docs/evidencias/backup-restore/BACKUP_MANIFEST.md` y antiguos runs acreditan procedimientos de otra fecha, pero no demuestran el **último backup real** de la instancia DEV vigente ni un restore causal de este corte. No reutilizar nombres, instancias o credenciales heredadas.
- En esta Task **no se leyó un artefacto de backup**, no se verificó el último ID de backup real ni su checksum, **no se restauró ninguna base de datos**, ni se comprobaron conteos/hash de un restore nuevo. No se abrió Aiven ni se consultaron valores secretos.

## Cambio reproducible delimitado

`scripts/verify-p0-005-a-restore-evidence.mjs` verifica **offline** una declaración JSON sanitizada y rechaza sin excepción: identidad o SHA ajenos a DEV, ejecución `skipped`/no `success`, backup sin cifrado/sha256, uso de proveedor de PROD, restore remoto o sobre base origen, artefacto diferente, hash interno/externo sin comprobar, conteos/migraciones divergentes, API no ready, ausencia de cleanup, timestamps incoherentes y exposición de secretos.

**Regresión automatizada:** `node scripts/verify-p0-005-a-restore-evidence.mjs --self-test`. Incluye un fixture **POSITIVE_SYNTHETIC** que verifica solo lógica, más **20 negativas**. Ninguna prueba usa red o credenciales ni ejecuta backup/restore. Para consumir evidencia sanitizada: `node scripts/verify-p0-005-a-restore-evidence.mjs --evidence <sanitized.json>`. La aprobación de esquema no valida la procedencia del informe: solo se puede certificar con workflow/run GitHub **auténtico**, artefacto SHA verificable y readback de restauración real contra runner descartable.

## Condiciones antes de VALIDANDO/LISTO

1. Identificar **último** backup DEV vigente mediante fuente de proveedor/Actions autorizada, ID, timestamp, status `success`, SHA origen, cifrado AES256 y checksum SHA256 real; no registrar dump, credenciales ni PII.
2. Confirmar una ejecución efectiva autorizada que restaure el **mismo artefacto cifrado** sobre base MySQL **descartable local de CI**, con hashes, conteos exactos por tabla/migración y health/ready. No ejecutar restore ni alterar la base activa Aiven.
3. Confirmar reporte de limpieza de temporales, ausencia de secretos y PROD untouched. Validar procedencia nativa (GitHub Actions run/job/artifact y SHA) frente a la declaración offline.
4. Completar CI del SHA final, revisión GitHub formal **de identidad distinta** a `jmejia31`, merge protegido `dev` y readback `PLAN_MAESTRO!H10:M10`, `COLA!E10` (solo fórmula) y `BITACORA`. `P0-005.B` NO se libera mientras A no sea LISTO.

## Impacto y reversión

**Componente:** evidencia de operación backup/restore en DEV. **Capas:** documentación, validador Node aislado y workflow de comprobación estática, sin cambio API/DTO, Angular, .NET, Domain/Application/Infrastructure o esquema MySQL. **Datos del changeset:** ninguno; el backup cifrado permanece únicamente en el artefacto temporal de Actions, fuera del repositorio. **Seguridad:** salvaguardas fail-closed para PROD, servidores remotos, origen de artefacto, hashes y fuga de secretos. **Observabilidad:** workflow y bitácora con IDs/SHA verificables, sin inferir éxito de un `skipped`. **Rollback:** revert mediante PR protegido de estos archivos sin destruir historial; preservar auditorías. **Arquitectura canónica:** sin alteraciones. **QA/main/PROD, pagos, Render/Vercel y VAEP/Jules:** fuera del alcance.

**Dictamen al corte inicial:** el restore real seguía `NO_VERIFICADO`; la evidencia posterior registrada abajo cambia ese punto a PASS, pero el cierre general sigue bloqueado por readiness, revisión, merge y readback pendientes.

## Observación real posterior — backup/restore Aiven DEV (2026-10-28)

El workflow manual [#40733383019](https://github.com/solqaryn/Solqaryn/actions/runs/40733383019), ejecutado contra el HEAD vivo `dev@8f7fe8cda3cdd11773fc79b614c46514c74492b4`, terminó `success`. El certificado autenticado confirma MySQL `8.4.11`, proveedor Aiven y TLS obligatorio, sin imprimir credenciales. Se creó el backup cifrado AES256 `solqaryn-dev-20261028T171251Z`; el job generó su archivo SHA-256 y la verificación `sha256sum -c` devolvió `OK` antes de restaurar.

En el mismo job se restauró ese mismo archivo cifrado sobre `127.0.0.1` en la base descartable `solqaryn_restore_drill`. La ejecución terminó `success`: el script verificó los checksums internos/externos, conteo exacto de filas de todas las tablas, cantidad de tablas y migraciones, y publicó el artefacto `solqaryn-backup-dev-40733383019` (Artifact ID `12835153901`, 5,485,920 bytes; retención de 14 días). `productionTouched=false` fue condición de éxito. No se descargó el dump cifrado.

**Brechas que permanecen:** la corrida anterior no levantó la API contra la base restaurada; no se atribuye readiness retroactiva. Este PR añade al workflow un smoke de API loopback sobre esa base, con migraciones deshabilitadas y readback del contador de migraciones antes/después. El smoke detiene la API, elimina `solqaryn_restore_drill`, verifica que ya no exista y añade `apiReady`, `disposableCleanupVerified` y `secretsExposed=0` al reporte de restore solo tras superar esas comprobaciones. Este nuevo smoke todavía debe ejecutarse después de integrar la modificación. También faltan revisión independiente sobre el SHA final, CI exacto, merge protegido y readback institucional requeridos para LISTO. P0-005.A sigue `EN_PROGRESO`.
