# C00-04.M01.04 — Registro de riesgos, supuestos y decisiones pendientes

**Proyecto:** SOLQARYN / V5 FINAL / C00-04 Gobierno de cambios y decisiones / M01.04. **Fecha de revisión:** 2026-11-13. **Tipo:** preparación documental de alcance, no cambios ejecutables.
**Fuente V5:** `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` de Google Drive, documento `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet oficial `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Gobernanza:** Skill canónica, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_V13.md` y `docs/AUTOMATION_AUTHORITY.md` del HEAD `dev`.

## Objetivo y criterios de no-regresión

Registrar riesgos, supuestos no demostrados y decisiones que exigen una autorización inequívoca antes de ejecutar cambios. La salida de M01 es el **registro de decisiones, riesgos y gates**, no el cierre global de C00 ni la activación de servicios, proveedores, migraciones o permisos nuevos. Evitar que la interpretación de `COLA!G/O=SI` o el objetivo de reserva N se conviertan en autorización de despacho sin autor, capacidades o dependencias reales.

## Matriz de riesgos con respuesta y evidencia

| ID | Riesgo y superficie | Severidad provisional | Control/gate verificable | Estado y respuesta |
| --- | --- | --- | --- | --- |
| R01 | Claim duplicado de microtarea o escritura concurrente sobre `dev`/Sheet | Alta | PLAN H/I y COLA E/G/O vivos, DAG `VALIDADO`, HEAD y fingerprint antes de cada transición; Git FF sin force | No reclamar otra Task; si HEAD cambia, conciliar aditivamente y repetir verificaciones |
| R02 | CI de SHA anterior, job omitido o check auxiliar `SKIPPED` presentado como PASS | Alta | GET check-runs del SHA exacto; `status=completed`, `conclusion=success` en job requerido y log causal | Mantener VALIDANDO del ID afectado; no esperar job técnico ajeno a documento sin runtime |
| R03 | Evento BITACORA incompleto o idempotencia rota | Alta | Encabezado real A:Z 26 campos; `EVENT_ID` A, `IDEMPOTENCY_KEY` R, revisión/readback de todas las columnas | Rechazar 21–25 campos, no duplicar evento, registrar corrección aditiva sin borrar historia |
| R04 | Autoaprobación de A5 o confusión LISTO_DEV con habilitación de QA/PROD | Crítica | `REVIEWER_TASK_ID` distinto del autor, prueba positiva/negativa y A5_PASS por SHA; orden adicional de propietario | Bloquear promoción; conservar A5_FAIL y registrar nueva revisión solo tras corrección real |
| R05 | JWT, BCrypt, RBAC, tenant A/B, PII o secretos expuestos por una corrección | Crítica | Tests negativos de aislamiento, revisión estática y API/MySQL reales cuando código afectado | NO_GO scope inseguro y reparación causal; documento no sustituye prueba backend |
| R06 | Migración destructiva, operación productiva o rollback no probado | Crítica | Autorización explícita, respaldo íntegro y restauración demostrable, sin conexión a PROD de prueba | No ejecutar; `git revert` normal sirve solo para commit documental, no deshace datos productivos |
| R07 | Entrada antigua VAEP/Jules, PR/branches históricos, fuentes ajenas | Alta | Fuente única V5, Scope Lock STRICT, Skill canónica y allowlist de referencias | Rechazar nueva Task, rama, PR o cola histórica no autorizada |
| R08 | Suplantación de cuenta Alex desde Javier o señales de Tasks mal interpretadas | Alta | GitHub `jmejia31`, Drive JAVIER, título/slot/ID en peek; `self_owned=false` no es permiso ni denegación | No administrar Alex; si falta prueba positiva de identidad, no atribuir escrituras |
| R09 | Puerta DAG omitida o fase adelantada | Alta | `DAG_TECNICO` revisado `VALIDADO` y prerequisitos `LISTO`; gates conservadores fuera del DAG | No usar reserva objetivo como permiso, revalidar C00 antes de M01.06/M02 |
| R10 | Certificación no sustentada por un inventario puramente documental | Media | Separar lectura de código/contratos, evidencia CI de ejecución y alcance real de cambios | Registrar `NO_APLICA` para pruebas no pertinentes, nunca PASS de runtime sintético |

La severidad es una prioridad provisional de revisión, **no** certificación de explotación o incidencia productiva. Ninguna fila de esta matriz modifica un control nativo ni concede permisos.

## Supuestos y límites de verificación

| Supuesto | Evidencia y nivel de confianza | Acción si falla |
| --- | --- | --- |
| Las seis Tasks Javier están habilitadas | Lectura nativa actual; puede cambiar después de la consulta | Revisar identidades y recuperar solo las autorizadas bajo la orden vigente; nunca Alex |
| El plan contiene 2.340 IDs únicos | Fuente Doc V5 y seguimiento Sheet actual, no un contador de trabajo completado | No crear 2.341 ni actuar sobre planes V3/V13 históricos |
| `PLAN_MAESTRO!H` es estado canónico | Fórmulas `COLA!E` deben reflejarlo | Registrar discrepancia, no escribir E manualmente |
| Los 11 prerrequisitos para C00-04.M01.04 están cerrados | C00-01.M02.01–06 y C00-03.M01.01–05 en LISTO al preflight; DAG `VALIDADO` | Suspender solo el claim afectado si se invalida dependencia |
| El runner GitHub Actions puede compilar .NET/Angular en jobs configurados | YAML del repositorio es evidencia de capacidad, no de ejecución exact-SHA | Revisar check-runs, steps y logs del candidato relevante |
| Un SHA de código de DEV no autoriza producción | Gobernanza V13 A0–A5 y Scope Lock estrictos | Requerir A5_PASS más orden expresa del propietario y controles de despliegue |

## Decisiones y autorizaciones pendientes

- **D01 — Documentos de gobierno:** permitidos como análisis en `dev`, con diferencia acotada, A0–A4 documentales y A5 independiente posterior; sin cambios de entorno.
- **D02 — Cambios de permisos, seguridad, secretos, proveedores o infraestructura:** requieren alcance, aprobación y evidencia técnica adicional; este registro no los ejecuta.
- **D03 — QA/main/PROD:** prohibidos por este flujo. Incluso A5_PASS técnico no representa una orden productiva.
- **D04 — Gestión de riesgo transversal pendiente:** un A5_FAIL histórico en `C00-01.M04.03` debe respetarse para promoción del SHA afectado; no afirmar que el cierre de otras microtareas lo subsana ni cambiar su responsable.
- **D05 — Cierre de fase C00:** revalidar todos los prerrequisitos y A5/seguridad aplicables; `C00-04.M01.04` solo documenta decisiones y no libera por sí sola M01.06/M02.

## Casos de aceptación positivos y negativos

**Positivo:** a una microtarea documental `PENDIENTE`, libre y con DAG `VALIDADO`, se asigna un TASK_ID auténtico; se registra EN_PROGRESO y VALIDANDO, se auditan A0–A3 documentales, se publica FF directo `dev`, Scope Lock del SHA exacto concluye `SUCCESS`, A4 y readback PLAN/COLA/BITACORA permiten LISTO_DEV con A5_PENDIENTE independiente.

**Negativo A — CI:** si `verify-solqaryn-project-scope` está `queued`, `skipped`, `failed` o es de otro SHA, no declarar A4_PASS ni LISTO del commit.

**Negativo B — identidad:** una Task Alex no se reclama ni se reanuda desde Javier; `is_self_owned_automation=false` solo no sustituye verificación de cuenta, ni implica revocación.

**Negativo C — persistencia:** una migración real requiere MySQL de prueba y rollback, y no puede certificarse con una tabla Markdown.

**Negativo D — auditoría:** evento BITACORA con 21, 22, 23, 24 o 25 columnas o clave duplicada no habilita cierre.

**Negativo E — promoción:** A5 realizado por el autor original, A5_FAIL sin corregir o ausencia de autorización adicional impide QA/main/PROD, sin bloquear otras tareas independientes aptas para DEV.

## Riesgo de implementación, alcance, pruebas y rollback

Esta entrega altera solo un documento de `docs/v5`, `CHANGELOG_AI.md` y el seguimiento institucional; no toca API/DTO, frontend Angular, repositorios EF, MySQL, transacciones, contratos JWT/RBAC, secretos, Docker, workers o infraestructura. La revisión de seguridad A2 es de **límites y prohibiciones**, no una aseveración de pruebas multitenant en runtime.

- A0: integridad Markdown, diff y changelog aditivo, búsqueda dirigida de secretos y código muerto no aplicable.
- A1: registro R01–R10 con controles observables, tabla de supuestos, D01–D05 y caso positivo/cinco negativos.
- A2: separación de cuentas, no QA/main/PROD, JWT/RBAC/PII, fail-closed.
- A3: consistencia V5, V13, DAG y columnas A:Z (26); .NET/Node/Angular/MySQL/Docker/E2E **NO_APLICA** al documento.
- A4: verificar commit FF, blob/diff y Scope Lock completado/success en SHA exacto, readback institucional.
- A5: revisor con TASK_ID distinto, sin auto-PASS ni promoción implícita.

**Rollback:** `git revert` normal para los dos archivos documentales del commit. Si la historia institucional requiere rectificación, usar un evento nuevo, único y de 26 campos; no borrar filas, ni force-push ni reset.
