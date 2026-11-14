# SOLQARYN V5 FINAL — C03-04.M01.03: inventario de contratos, código y evidencia de continuidad

**Alcance:** preparación documental M01.03; lectura verificable de código, contratos y evidencia sin certificar restauración real. **Autor:** PRIMARY DEV 04 — JAVIER, TASK_ID \`6aeacb7820ac8191b7d768a6f33dfa09\`. Repositorio \`solqaryn/Solqaryn@dev\`, PROJECT_SCOPE_LOCK=STRICT. Fecha: 2026-11-13.

**Fuente exclusiva:** [Standard V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica y Scope Lock. DAG \`VALIDADO\`, gate \`SI\` y 15 predecesoras de C03-01/C03-02/C03-03 M01.01–05 comprobadas LISTO. **No habilita** M01.06/M02, QA/main/PROD, incident response, migraciones ni proveedores.

## 1. Contratos observados: archivos y blobs exactos

| Fuente consultada en dev | Blob Git SHA de contenido | Dato observado | No puede certificar por sí sola |
| --- | --- | --- | --- |
| \`scripts/backup_dev.sh\` | \`d2e0ca8bf48dc8c21fcbfd5ab5256c770bdbdcf3\` | \`set -Eeuo pipefail\`, \`umask 077\`, comandos mysql/mysqldump/gpg/sha256sum, variables de entorno exigidas, allowlist dev/development/ci, rechazo de nombre PROD | Backup exitoso, integridad del artefacto y secretos disponibles |
| \`scripts/restore_dev.sh\` | \`9be58822ae5c6189e7a84cf785998bf42a1b576c\` | Exige \`ALLOW_DESTRUCTIVE_RESTORE=YES_SOLQARYN_DISPOSABLE_RESTORE\`, entorno ci/dev-disposable y host loopback | Autorización del owner, validación de esquema/archivos, restore efectivamente realizado |
| \`.github/workflows/backup-restore-dev.yml\` | \`cee9cdd1e22d006c21d9a23cc40ead6223d7295c\` | Push dev condicionado a rutas, \`workflow_dispatch\`, MySQL 8.4 como contenedor de servicio y job \`backup-restore-drill\` | Un push documental ajeno no dispara su job ni acredita restore |
| \`.github/workflows/backup-restore-backend-smoke.yml\` | \`9ae22277208c78a13f6d7f81b8be740845a057fd\` | Job con condición \`[SOLQARYN-BACKUP-SMOKE]\`, .NET 10.0.401, MySQL 8.4 y salida a directorio temporal | Acreditación de health/tenant A/B de una corrida con proveedor real |
| \`.github/workflows/backup-restore-topology.yml\` | \`43e3f3c64b5161e61520bf57f3d0650e5708aab1\` | Requiere marcador \`[SOLQARYN-BACKUP-TOPOLOGY]\` y rutas específicas, compara topología tras restore en MySQL efímero | Su existencia no equivale a ejecución ni demuestra RPO/RTO |
| \`docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md\` | \`81e8b11557912fffcd741ff0f34faf69e0ed6f47\` | Contrato documentado de backup cifrado, settings/secretos externos y diferencia de \`DEV\`/\`Desarrollo\` | Identidad real del environment, secretos o permisos actuales |
| \`docs/ROLLBACK_RUNBOOK.md\` | \`b7424464a5a79f0d5ff858a7ec4d2356a30355f0\` | Separación rollback DEV vs PROD, health/ready, prohibición de fallback legacy | Una versión de despliegue concreta ni restauración segura de datos |
| \`backend/tests/Solqaryn.Tests/I8009CRevocacionRenovacionTests.cs\` | \`cb79e9e1b0e78df46adb8fba5074e0b2773b0901\` | Pruebas unitarias con mocks de renovación contra usuario bloqueado/inactivo | HTTP, firma de JWT, revocación de tokens ya emitidos, tenant A/B |
| \`backend/src/API/Controllers/AuthController.cs\` | \`af437c869b4800a499c42aeff00347de3a3cc521\` | Login anónimo con rate limit y auditoría de éxito/error; renovación autenticada | Reautenticación global, recovery, MFA, persistencia auditada obligatoria |
| \`docs/v5/c03-04-m01-01-objetivo-continuidad-incidentes.md\` | \`1f5ff7892c6fc0e907f689a467bd3ef23e4d7f21\` | Objetivos/16 oráculos documentales futuros | Capacidad de recuperación implementada |

**Tipo de evidencia:** estos son hashes de blobs leídos en GitHub. No se han ejecutado pruebas .NET/MySQL en este changeset, ni backups, restores ni llamadas a Aiven/Render/Vercel/Cloudinary. El hash documenta el archivo exacto inspeccionado; un check verde de HEAD diferente no prueba el flujo.

## 2. Fronteras y dependencias cruzadas

El orden deseado de recuperación en una implementación posterior es: incidente detectado → owner y ámbito A/B autenticados → evaluación de blast radius → contención/revocación delimitada → selección de copia íntegra y cifrada → confirmación de destino descartable y permisos → restauración aislada desde el mismo artifact → comparación de schema/filas/archivos/relaciones → health+ready+RBAC y smoke A/B → evidencia de rollback y decisión de promoción separada. Los pasos no se disparan desde esta microtarea.

La salida de C03-04 debe proveer contratos para C10/C12, pero **no ejecuta** controles nuevos por dependencia documental. Continuidad física y RPO/RTO dependen de servicios vivos y mediciones; no inferir garantía por una ruta de GitHub Actions. Los scripts y jobs de backup permiten auditar la intención y los gates, no sustituir una restauración causal.

## 3. Matriz de cobertura positiva/negativa para pruebas posteriores (NO ejecutadas aquí)

| ID | Signo | Escenario | Evidencia necesaria cuando se implemente |
| --- | --- | --- | --- |
| IV01 | + | Backup autorizado en entorno DEV con credencial válida | Artefacto cifrado + manifest + digest + identidad del origen |
| IV02 | - | Se invoca backup con env/nombre de BD PROD | Bloqueo sin lectura ni filtrado de secretos |
| IV03 | - | Restore sin bandera destructiva explícita | Salida fallida antes de modificar destino |
| IV04 | - | Restore a host no-loopback o ambiente PROD | Rechazo fail-closed, no efectos |
| IV05 | + | Restore desde backup original en mysql:8.4 descartable | Igualdad de digest y datos/topología y trazabilidad de job |
| IV06 | - | Checksum alterado o passphrase ausente | No restauración parcial ni declaración PASS |
| IV07 | + | Backend recuperado tras restore aislado | /health, /health/ready y suite A/B sobre mismo SHA |
| IV08 | - | Usuario empresa A intenta consultar/reparar datos B | 403/denegación y cero exposición/efectos |
| IV09 | + | Usuario desactivado intenta renovar JWT | Renovación denegada con pruebas HTTP/server-side |
| IV10 | - | Antiguo token sigue vigente tras revocación crítica | Prueba causal de política de invalidación/fail-closed |
| IV11 | - | Run \`SKIPPED\`, \`IN_PROGRESS\` o SHA distinto | No contabilizar PASS, log y artifact obligatorios |
| IV12 | - | Falta environment/secret aprobado | Registrar TEST_GATE_UNAVAILABLE, sin bypass |
| IV13 | + | Registro de incidente sin PII ni credencial | Evento estructurado, autor, severidad y correlación saneada |
| IV14 | - | Dos operadores intentan restore del mismo destino | Exclusión y owner único sin datos sobrescritos |
| IV15 | - | Workspace dice \`DEV\` y runbook \`Desarrollo\` | Inspección autenticada y no inferencia de equivalencia |
| IV16 | + | Rollback reversible tras restore de prueba | Comparación pre/post, ausencia de pérdida de datos legítimos |

La matriz no equivale a pruebas ejecutadas. En tareas ejecutables posteriores exigir job, step, resultado, \`head_sha\`, artifact y regresión proporcional; MySQL solo es PASS si realmente se ejecutó, no si job no disparó.

## 4. Brechas y decisiones abiertas de forma explícita

- **BR01**: fijar inventario y perímetro de respaldo de base y archivos Cloudinary; evitar inconsistencia de adjuntos.
- **BR02**: comprobar configuración real de environment \`DEV\`/\`Desarrollo\`, proveedores y permisos antes de invocar un workflow; ninguna escritura en secrets por esta tarea.
- **BR03**: medir RPO/RTO en ensayos autorizados. No hay cifras certificadas aquí.
- **BR04**: reconciliar revocación de tokens ya emitidos vs solo renovación denegada; incluir JWT, membresía, tenant A/B, acceso a recursos.
- **BR05**: practicar restore con fallas controladas, checksum inválido y destino denegado para demostrar fail-closed.
- **BR06**: asegurar evidencias desensibilizadas; no exportar datos reales a logs o artifacts accesibles.
- **BR07**: clasificar incidentes, severidad, tiempos de escalamiento y owner por tipo de proveedor.
- **BR08**: preservar cambios concurrentes, permisos y rutas de rollback sin force-push ni sobreescrituras.
- **BR09**: requiere autorización separada para QA/main/PROD, acceso excepcional o destrucción de datos.

## 5. A0–A5 y salida verificable

**A0:** revisar diff documental y CHANGELOG aditivo; enlaces, blobs, no secretos, no modificaciones de scripts/workflows o arquitectura. **A1 positiva:** al menos 10 fuentes estáticas y 16 pruebas futuras IV01–IV16 con positivos y negativos. **A1 negativa:** ningún mock, YAML o documento equivale a restore realmente verificado. **A2:** fail-closed, JWT/RBAC/membresía/tenant A/B, PII/secretos, segregación de entorno y no autoaprobación. **A3:** DAG VALIDADO, quince prerrequisitos LISTO, controles C10/C12 y distinción entre job condicional y ejecución. .NET, Angular/Node, MySQL, Docker y E2E = \`NO_APLICA\` para este documento sin runtime; Scope Lock del commit sí aplica. **A4 tras publicación:** FF dev, SHA/check-run Scope Lock \`completed/success\` para \`head_sha\` exacto, diff/blob, PLAN/COLA/BITACORA A:Z y git revert normal como rollback. **A5:** PENDIENTE para otra Task Javier diferente; nunca A5_PASS propio.

**Resultado M01.03:** inventario fiable de código/contratos con límites probatorios explícitos; no autoriza M01.06/M02, ningún entorno QA/main/PROD ni uso de datos/secretos reales.
