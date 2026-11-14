# SOLQARYN V5 FINAL — C03-04.M01.04: registro de riesgos, supuestos y decisiones de continuidad

**Alcance:** M01.04, análisis documental de C03-04 (incidentes, revocaciones, backup, restore, continuidad). **Autor exclusivo:** PRIMARY DEV 04 — JAVIER, TASK_ID \`6aeacb7820ac8191b7d768a6f33dfa09\`. **Repositorio:** \`solqaryn/Solqaryn@dev\`, PROJECT_SCOPE_LOCK=STRICT, fecha 2026-11-13.

**Autoridad:** [Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, AGENTS y Scope Lock. El DAG de esta preparación está \`VALIDADO\` y gate \`SI\` con 15 predecesoras M01.01–05 de C03-01/C03-02/C03-03 comprobadas LISTO antes del claim. La autorización **no habilita** M01.06, M02, IAM operativo, recuperación live, QA/main ni PROD.

## 1. Propósito del registro y estado de certeza

La fase C03-04 exige definir incidencias, revocación, respaldo, restauración y continuidad como contrato base para C10 y C12. Este registro identifica **riesgos observados, supuestos pendientes y decisiones que requieren evidencia o autorización**; no modifica sistemas ni afirma certificación de recuperación. El flujo futuro debe asegurar control de acceso server-side, restauración reversible y verificable, aislamiento A/B, minimización PII, trazabilidad correlacionada y secreto nunca publicado.

**Diferenciar:** \`RIESGO\` = modo de fallo posible; \`EVIDENCIA\` = archivo o job inspeccionado; \`DECISION_PENDIENTE\` = elección no autorizada aún; \`TEST_FUTURO\` = obligación verificable, no prueba ejecutada. No fijar RPO/RTO, retención o proveedor por inferencia.

## 2. Fuentes observadas y limitación probatoria

| Fuente GitHub en dev (blob) | Contrato observado | Límite no certificado |
| --- | --- | --- |
| \`.github/workflows/backup-restore-backend-smoke.yml\` — \`9ae22277208c78a13f6d7f81b8be740845a057fd\` | Job condicional con MySQL 8.4 efímero, .NET 10 y marcador \`[SOLQARYN-BACKUP-SMOKE]\` | YAML no prueba restauración ni salud real en el SHA de este documento |
| \`.github/workflows/backup-restore-topology.yml\` — \`43e3f3c64b5161e61520bf57f3d0650e5708aab1\` | Control de topología condicionado por paths y marcador \`[SOLQARYN-BACKUP-TOPOLOGY]\` | No certifica permisos de proveedor, credenciales o run vigente |
| \`docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md\` — \`81e8b11557912fffcd741ff0f34faf69e0ed6f47\` | Runbook de DEV con backup cifrado y restore descartable; secretos externos | Texto contiene referencias \`DEV\`/\`Desarrollo\` que se deben reconciliar antes de cualquier ejecución |
| \`backend/tests/Solqaryn.Tests/I8009CRevocacionRenovacionTests.cs\` — \`cb79e9e1b0e78df46adb8fba5074e0b2773b0901\` | Tests aislados de renovación de usuario bloqueado/inactivo con mocks | No acredita revocación global de JWT, HTTP real o acceso cross-tenant |
| \`docs/v5/c03-04-m01-01-objetivo-continuidad-incidentes.md\` — \`1f5ff7892c6fc0e907f689a467bd3ef23e4d7f21\` | Objetivo y contratos CO01–CO16 documentales | Documento no es ensayo ejecutado ni fija RPO/RTO |

Los SHA de blob muestran contenidos revisados. Una CI de otro commit, un job omitido o un informe histórico no son evidencia causal de backup, restore o protección de datos en vivo.

## 3. Registro de riesgos R01–R16 y mitigaciones obligatorias futuras

| ID | Riesgo / impacto | Control y evidencia pendiente | Criticidad |
| --- | --- | --- | --- |
| R01 | Restore escribe en BD viva/equivocada: pérdida de datos | Destination allowlist, destino descartable y guard de operación destructiva; prueba negativa | Crítico |
| R02 | Backup incompleto/corrupto se interpreta recuperable | Manifest, tamaño, SHA-256, artefacto cifrado y restore real de la misma copia | Crítico |
| R03 | Secretos de backup/DB aparecen en logs o artefactos | No imprimir entornos/credenciales; redacción y escáner con negativos | Crítico |
| R04 | Acceso de empresa A se extiende a datos B durante incidentes | JWT/membresía/RBAC/tenant verificado en backend y ensayo A/B en MySQL descartable | Crítico |
| R05 | Administrador empresarial usa bypass global sin permiso | Separar roles empresariales/plataforma y control de aprobación excepcional con TTL | Crítico |
| R06 | Revocación de usuario no invalida sesiones preexistentes | Definir revocación real, pruebas JWT/HTTP y evidencia de denylist o mecanismo equivalente | Alto |
| R07 | Simultaneidad de dos restorers daña estado compartido | Exclusión de escritura, owner, lease y deduplicación con readback | Alto |
| R08 | Migración EF incompleta quiebra restore del backend | Comparar historia, índice, FK, schema y migraciones con CI de integración pertinente | Alto |
| R09 | RPO/RTO asumidos sin medición generan falsa garantía | Aprobación de objetivos, cronometraje de drill, revisión independiente | Alto |
| R10 | Llaves de backup rotadas sin capacidad de descifrar | Inventario de custodia, verificación aislada de descifrado, rotación autorizada | Alto |
| R11 | Datos personales se exponen en tickets, traces o exportaciones | Minimización, clasificación, ACL y redacción por superficie | Alto |
| R12 | Job \`SKIPPED\`/no disparado se registra como PASS | Verificar check-runs exact SHA, steps, artifact y trigger causal | Alto |
| R13 | Inconsistencia \`DEV\` vs \`Desarrollo\` lleva a ambiente incorrecto | Resolver environment real desde settings/YAML y permisos, nunca inferir equivalencia | Alto |
| R14 | Restauración parcial restaura DB pero pierde archivos Cloudinary | Definir perímetro de datos, inventario de objetos, checksums y enlace causal con BD | Alto |
| R15 | Incidente con proveedor inactivo dispara fallback inseguro | Fail-closed, registrar bloqueo, no usar credenciales de otra cuenta ni infraestructura retirada | Alto |
| R16 | Rollback borra trabajo concurrente legítimo | Revalidar HEAD, revert normal/no force, pre/post diff y dueño de cada cambio | Alto |

Ningún riesgo se da por cerrado por esta tabla. Un riesgo crítico sin evidencia causal se mantiene abierto para la futura implementación y no se compensa con un A5 documental.

## 4. Supuestos S01–S08, sujetos a revalidación

| ID | Supuesto no certificado | Cómo invalidarlo/verificarlo |
| --- | --- | --- |
| S01 | Los workflows de restore siguen presentes en \`dev\` | Leer YAML de HEAD exacto, triggers, condiciones, environment |
| S02 | Existe destino MySQL temporal aislado para drill | Verificar servicio \`mysql:8.4\` en ejecución real y DB destino |
| S03 | El backup tiene manifest y checksum utilizable | Leer artefacto cifrado, digest y log de restore exitoso |
| S04 | El alcance de datos incluye documentos/Cloudinary | Inventario de archivos y referencias; comparar restauración |
| S05 | Las pruebas de revocación cubren HTTP/tenant | Distinguir unit/mock de HTTP + DB y casos A/B |
| S06 | La custodia/rotación de llave está aprobada | Comprobar autorización, versión y segregación de privilegios |
| S07 | Ambientes \`DEV\` y \`Desarrollo\` son el mismo | Consultar configuración GitHub autenticada; no suplantar |
| S08 | Existe un SLA, RPO o RTO autorizado | Orden firmada y medición; no extraerlo del nombre del workflow |

## 5. Decisiones D01–D09 que NO se toman en esta microtarea

| ID | Decisión pendiente | Gate y responsable | Evidencia para futuro cierre |
| --- | --- | --- | --- |
| D01 | Objetivos RPO/RTO por clase de datos | Propietario / riesgo del negocio | Valores aprobados y tiempos de restore medidos |
| D02 | Retención y supresión de respaldos/PII | Propietario + seguridad/legal | Política y ensayo de borrado sin destruir legítimos |
| D03 | Método de revocación JWT y cierre de sesiones | Arquitectura/seguridad | Negativos tras bloqueo y renovaciones |
| D04 | Restauración consistente DB + archivos | Arquitectura / datos | Snapshot, manifest y enlaces íntegros |
| D05 | Acceso excepcional (break-glass) | Seguridad + dueño empresarial | Motivo, TTL, dos personas si exigido, auditoría |
| D06 | Runner y environment canónicos | Administrador GitHub propietario | YAML vivo, settings y acceso verificado |
| D07 | Ensayos de backup/restore y costes | Propietario autorizado | Workflow/host y coste aprobado cuando aplique |
| D08 | Plan de rollback de cada cambio irreversible | Responsable de implementación y control de datos | Restore descartable, pre/post y idempotencia |
| D09 | Autorización de QA/main/PROD | Propietario + A5 independiente | Orden expresa separada; LISTO_DEV no es autorización |

## 6. Contratos falsables positivos/negativos (pendientes de prueba futura)

- **P01:** un actor autorizado documenta incidente severo con alcance A, hora, mitigación y evidencia sin PII; B permanece inalterada.
- **P02:** backup DEV cifrado con manifest y checksum se restaura en MySQL efímero desde el mismo artefacto, se comparan schema/tablas/archivos y se valida rollback.
- **P03:** bloqueo de usuario revoca renovación y acceso a recursos sensibles según mecanismo definido y validado por HTTP.
- **N01:** usuario de A o actor no autorizado intenta recuperar datos B: denegar sin leer, modificar ni divulgar B.
- **N02:** artefacto con checksum incorrecto, clave ausente o destino PROD: detener antes de cualquier operación destructiva.
- **N03:** secretos, JWT, datos bancarios o PII crudos aparecen en evidencia/log: invalidar evidencia y aplicar contención autorizada.
- **N04:** job SKIPPED/CANCELLED o SHA viejo se ofrece como PASS de restore: rechazar certificación.
- **N05:** el mismo scope se toma por dos escritores: no permitir segundo claim; readback de owner y lease.

Estos ejemplos son **oráculos documentales**; ningún P01–P03 o N01–N05 se ejecuta contra servicios reales en este changeset. Las futuras pruebas deben preservar actor, resultado, SHA, logs y rollback antes de certificar.

## 7. Auditorías documentales y salida segura

**A0:** inspeccionar este Markdown y CHANGELOG aditivo (diff, links, secretos/patrones, no cambios de código ni autoridad). **A1:** comprobar registros R01–R16, S01–S08, D01–D09 y cinco casos negativos con decisiones verificables; negativo central: no sustituir pruebas reales por texto. **A2:** controlar JWT, RBAC, tenancy A/B, PII, secreto y acceso excepcional sin grants; nunca abrir PROD. **A3:** contrastar DAG 15 predecesoras LISTO, fuentes referenciadas y mapeo de gate. Para el cambio solo documental \`.NET\`, Node/Angular, MySQL, Docker y E2E son \`NO_APLICA\`; Scope Lock del SHA publicado sí es obligatorio. **A4:** commit FF directo dev, SHA/diff/blobs y \`verify-solqaryn-project-scope\` completado con \`success\` sobre SHA exacto, readback PLAN/COLA/BITACORA A:Z; rollback con \`git revert\` normal. **A5:** evento PENDIENTE para otro TASK_ID Javier, no autoaprobación ni promoción.

**Salida M01.04:** riesgos/supuestos/decisiones documentados; los M01.06, M02, C10/C12 y toda recuperación o migración siguen sujetos a sus propios gates y autorización.
