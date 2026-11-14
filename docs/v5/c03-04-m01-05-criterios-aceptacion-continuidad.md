# SOLQARYN V5 FINAL — C03-04.M01.05: criterios observables de aceptación de continuidad

**Tipo:** 1, contrato documental; no ejecuta backup, restore, revocación ni modifica runtime. **Responsable:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Rama:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-13.

**Resultado exigido V5:** definir aceptación funcional y técnica observable para incidentes, revocaciones, respaldo, restauración y continuidad; dejar un runbook base y criterios de restauración/rollback para C10 y C12. Estos criterios son **obligaciones futuras**, no afirmaciones de pruebas ejecutadas.

## 1. Autoridad, alcance y prerequisitos

- Fuente: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES — V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, `AGENTS.md`, Scope Lock.
- `DAG_TECNICO` exige quince `C03-01/02/03.M01.01–05`: todos consultados como **LISTO** antes del claim de M01.05; `COLA!G/O=SI` y owner vacío. Esta autorización solo permite trabajo documental de preparación. No libera M01.06, M02, IAM, base de datos, C10/C12 ni despliegues.
- Componentes consumidores futuros: servicios API de incidentes/revocación, repositorios y DB MySQL, workflows efímeros de recuperación, observabilidad, frontend de administración y runbooks del operador. No se añaden endpoints, DTOs, migraciones, esquemas, secrets, roles, jobs ni permisos aquí.
- La seguridad la valida siempre el backend; una UI o un log nunca sustituyen JWT/RBAC, aislamiento A/B y prueba de denegación. Datos y secretos no se copian a evidencias.

## 2. Fuentes técnicas verificadas en dev (evidencia estática, NO ejecución)

| Fuente + Git blob | Capacidad observada | Límite de prueba |
| --- | --- | --- |
| `.github/workflows/backup-restore-backend-smoke.yml` — `9ae22277208c78a13f6d7f81b8be740845a057fd` | Workflow de push a dev con path y marcador `[SOLQARYN-BACKUP-SMOKE]`; job MySQL 8.4, .NET 10 y restore efímero | No se dispara por este documento; un YAML no es un `SUCCESS` causal |
| `.github/workflows/backup-restore-topology.yml` — `43e3f3c64b5161e61520bf57f3d0650e5708aab1` | Push condicionado por paths y marcador `[SOLQARYN-BACKUP-TOPOLOGY]`; compara checksums/topología | No equivale a haber validado una copia concreta ni datos vivos |
| `docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md` — `81e8b11557912fffcd741ff0f34faf69e0ed6f47` | Describe backup cifrado y restore temporal DEV | Contradicción `DEV`/`Desarrollo` en el texto: verificar config efectiva antes de operar |
| `backend/tests/Solqaryn.Tests/I8009CRevocacionRenovacionTests.cs` — `cb79e9e1b0e78df46adb8fba5074e0b2773b0901` | Tests unitarios Moq para usuario bloqueado/inactivo/activo | No prueba expiración de JWT previamente emitido ni HTTP/tenant real |
| `docs/v5/c03-04-m01-04-riesgos-supuestos-decisiones-continuidad.md` — vigente en dev | Riesgos R01–R16, supuestos S01–S08, decisiones D01–D09 | Registro documental, no aceptación de riesgo ni drill ejecutado |

## 3. Matriz de aceptación futura, positiva y negativa

**Protocolo de una prueba real:** escenario y actor, empresa A/B, estado previo, ambiente/destino, comando/job y trigger, SHA exacto, hora UTC, evidencia desensibilizada, resultado esperado/observado, rollback, aprobación independiente cuando aplique. Un resultado no probado se registra `PENDIENTE`, nunca `PASS`. Sin permiso real se marca `NO_GO` del escenario sin modificar producción.

| ID | Clase | Estímulo y precondición | Oráculo medible y evidencia exigible para PASS futuro |
| --- | --- | --- | --- |
| AC01 | + incidente | Operador con permiso crea incidente de severidad clasificada | ID/correlación, owner, hora, impacto y mitigación registrables sin secretos ni PII cruda |
| AC02 | - autorización | Actor sin RBAC intenta abrir/cerrar incidente o ejecutar restore | HTTP 403/denegación backend, ningún cambio durable ni fuga de existencia entre tenants |
| AC03 | + revocación | Usuario activo cambia a bloqueado/inactivo | Renovación rechazada y ningún JWT nuevo; pruebas reales de servicio y HTTP con marca temporal |
| AC04 | - token persistente | JWT antiguo o permiso retirado intenta recurso sensible A/B | Rechazo por mecanismo de revocación vigente, no reutilización de token ni lectura cross-tenant |
| AC05 | + respaldo | Backup autorizado en DEV identificado por manifest, versión y SHA-256 | Artefacto cifrado existe; checksum verificable; logs redactados; alcance de datos clasificado |
| AC06 | - integridad | Copia alterada, checksum incorrecto o clave ausente | Rechazo antes de ejecutar sentencias restauradoras; salida FAIL y ausencia de mutación |
| AC07 | + restore | Copia de AC05 se restaura en MySQL 8.4 descartable y autorizado | Mismo artefacto validado, schema, filas, migraciones, FKs e índices cotejados; `productionTouched=false` |
| AC08 | - destino | Destino no allowlisted/producción y solicitud destructiva | Fail-closed antes de conectar/escribir; evidencia de destino y rechazo sin datos secretos |
| AC09 | + consistencia | Dataset de prueba contiene DB y referencias a archivos válidos | Manifest de objetos/Cloudinary y referencias congruentes; reconocer faltantes y límites reales |
| AC10 | - cross-tenant | Operador tenant A solicita contenido, backup o incidente exclusivo de B | Denegación backend y prueba A/B con DB efímera; ningún ID, objeto ni PII de B filtrado |
| AC11 | + auditoría | Restore termina con informe verificable | Actor, motivación, correlación, SHA/job_id y resultado persistidos sin credenciales; inventario de fallos abiertos |
| AC12 | - concurrencia | Dos operadores intentan restaurar el mismo scope simultáneamente | Sólo un lease/escritor; segundo aborta por CAS; no sobrescribe historial legítimo |
| AC13 | + rollback | Cambio de prueba reversible en ambiente descartable | `git revert`/restore dirigido conserva commits ajenos; before/after, owner y hash recuperables |
| AC14 | - rollback destructivo | Rollback pretende eliminar otra operación confirmada | Bloqueo por HEAD/owner divergente y registro del conflicto; no force-push ni reset destructivo |
| AC15 | + continuidad | Simulación de incidente termina con verificación funcional | Health, autenticación, aislamiento y operaciones esenciales validados; duración observada sin inventar SLA/RTO |
| AC16 | - CI causal | Job `SKIPPED`, `CANCELLED`, `IN_PROGRESS` o SHA anterior se ofrece como prueba | Rechazar PASS; consultar `/commits/<SHA>/check-runs?per_page=100` y job/steps |
| AC17 | - privacidad | Log o ticket incluye token, cadena de conexión o PII cruda | Invalidar evidencia, segregar/redactar y bloquear promoción hasta corregir exposición |
| AC18 | - ambiente | Workflow referencia `DEV` pero runbook indica `Desarrollo` | Bloquear ejecución hasta reconciliar environment real con permisos y configuración autenticada |

## 4. Gates de aceptación y responsables

| Entregable futuro | Gates mínimos antes de declararlo operativo |
| --- | --- |
| Incidentes y revocación | Revisión de amenazas, JWT/RBAC/tenant A/B, positivos/negativos HTTP+DB y logs minimizados; owner y revisión independiente |
| Backup cifrado | Inventario de perímetro, manifest/sha, acceso limitado a secretos, artefacto recuperable; no sólo existencia YAML |
| Restauración | Drills con MySQL descartable desde mismo artifact, integridad datos/schema/objetos, rollback, no PROD; job `completed/success` y SHA causal |
| Continuidad | Objetivos RPO/RTO/retención aprobados de manera expresa y medidos; incidentes y fallback seguros, no garantías por nombre de workflow |
| Cierre comercial V5 | A0–A4 con evidencia de ese artefacto, `PLAN/COLA/BITACORA` readback y A5 pendiente por otra Task JAVIER; M01.06 tiene dependencias propias |

## 5. Clasificación de pruebas de este changeset (documental)

- `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`: no hay cambios ejecutables ni aceptación de comportamiento de runtime. No reportar `PASS` de estas tecnologías por evidencia documental.
- `requiere_security=SI` (revisión de fronteras JWT/RBAC, tenant A/B, credenciales y PII en el contrato); `requiere_scope_lock=SI` (check `verify-solqaryn-project-scope` sobre el commit exacto).
- A0: diff Markdown/CHANGELOG aditivo, sin secretos, links correctos, fuentes blob y límites explícitos; A1: contar AC01–AC18 con resultados falsables, incluidos negativos; A2: denegación por defecto, tenant A/B, PII y ambiente; A3: consistencia de fuentes y 15 predecesoras LISTO. Estos controles son documentales y no equivalen a ejecutar los AC.
- A4 **solo tras publicación FF en dev**: comprobar commit/parent/tree/blobs/diff, Scope Lock exact SHA `completed/success`, readback `PLAN_MAESTRO/COLA/BITACORA` A:Z. A5 = `PENDIENTE` de otra de las cinco Tasks Javier; no QA/main/PROD.
- Rollback de este cambio: `git revert` normal del commit documental (nunca force). Restaurar o alterar bases de datos/archivos reales no forma parte de este changeset.

**Resultado atómico M01.05:** contrato de criterios de aceptación funcional/técnica y fallos seguros reproducibles para implementación posterior; ninguna restauración, revocación o RPO/RTO real queda certificada.
