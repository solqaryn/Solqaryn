# SOLQARYN V5 — C15-05.M02.01: modelo conceptual, ownership e invariantes de auditoría operativa

**Autor:** PRIMARY DEV 03 — JAVIER_CHATGPT; TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fecha:** 2026-11-14, America/Tegucigalpa. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** contrato documental M02.01 para la auditoría operativa; no crea tablas, API, endpoints, workers, webhooks, automatizaciones, schedules, eventos ni permisos.

## 1. Fuente canónica, elegibilidad y límites

La única especificación de trabajo es el [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit). El [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit) contiene 2.340 IDs y separa PLAN_MAESTRO H/I (estado/owner), COLA G/O (elegibilidad), DAG_TECNICO F/G (validación/gate), BITACORA A:Z (eventos append-only) y TAREAS_PROGRAMADAS AA:AN (último autorreporte, no señal tiempo real). El estado nativo se comprueba por el proveedor, no se infiere de una celda.

Preflight de esta unidad: `PLAN_MAESTRO!H2312=PENDIENTE`, I2312 vacío, `COLA!G2312/O2312=SI`, `DAG_TECNICO!A2273=C15-05.M02.01`, F2273=VALIDADO/G2273=SI; CAS condicional único H2312 PENDIENTE→EN_PROGRESO, I2312 owner `6adc2e4bd8a48191ac7d8641ed95a1a3`, readback y evento BITACORA. El antecedente terminal C15-05.M01.06 está LISTO y obtuvo A5_PASS por otra Task Javier: [commit 6573a4f390dca22c4c4760ad092446fcff0150a2](https://github.com/solqaryn/Solqaryn/commit/6573a4f390dca22c4c4760ad092446fcff0150a2), documento blob `f1501dc9c0809fca763089e427ea773a36c34485`. Sus fuentes M01.01–05 tenían A5 independiente; esa evidencia no equivale a una auditoría de concurrencia ejecutada.

La Skill canónica de `dev`, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_AUTHORITY.md` y `docs/AUTOMATION_STATUS.md` definen autoridad. Los seis TASK_ID Javier autorizados son `6adbb9fff1548191a1bd5b30d9e9984a`, `6af16bb9492c8191b8e8c7d606f11507`, `6adc2e4bd8a48191ac7d8641ed95a1a3`, `6aeacb7820ac8191b7d768a6f33dfa09`, `6adc2e55f47c81919f6374e561687076` y `6af16bbad26881918c283abe06e1f32a`. Una atribución histórica externa no amplía esta allowlist.

## 2. Entidades lógicas de evidencia, sin nuevas tablas

El **modelo lógico** a continuación describe relaciones auditables a través de GitHub, Google Sheet y proveedor de Tasks; no es una propuesta de migración que pueda ejecutarse en dev por el solo hecho de aparecer aquí.

| ID | Registro conceptual | Responsable autorizado | Relación e invariante |
| --- | --- | --- | --- |
| OM01 | `PlanItem`: ID exacto, capítulo, fase, estado y owner | PLAN_MAESTRO H/I | Un ID tiene máximo un owner activo |
| OM02 | `EligibilitySnapshot`: G/O y timestamp de lectura | COLA derivada | La elegibilidad se recalcula tras transiciones; jamás se fuerza SI |
| OM03 | `DagPrerequisite`: predecesor, validación y gate | DAG_TECNICO F/G | F=VALIDADO no implica G=SI ni permiso de producción |
| OM04 | `ClaimDecision`: estado esperado, candidato, ganador CAS | Task Javier habilitada | Un compare-and-set exitoso cambia una sola celda/versión |
| OM05 | `OwnerIdentity`: TASK_ID, cuenta, rol, vínculo Git/Drive | Configuración nativa verificada | Identidad textual no suplanta autenticación |
| OM06 | `NativeRunSnapshot`: run_id real, estado y hora de lectura | Proveedor Automations | `completed`/enabled no representa fallo ni lease |
| OM07 | `LocalRunReport`: corrida_key local, señal, contadores | Fila AA:AN de su propia Task | Nunca afirmar que autorreporte es heartbeat nativo |
| OM08 | `AuditEvent`: EVENT_ID, operación, microtask, 26 campos | BITACORA append-only | Event ID e idempotency_key únicos; filas históricas inmutables |
| OM09 | `PublishedArtifact`: SHA, árbol, diff, blob y parent | GitHub dev de cuenta permitida | Commit fast-forward/CAS sin force-push |
| OM10 | `CheckEvidence`: nombre, status, conclusion, head_sha | GitHub Actions original | PASS sólo completed/success exact SHA pertinente |
| OM11 | `A5Review`: autor, revisor, SHA, decisión motivada | Otra Task Javier distinta del autor | Revisión independiente no reemplaza A0–A4 |
| OM12 | `RecoveryDecision`: fallo causal, última ejecución y lease | Task rescatadora bajo verificación | No relevar por 10 minutos ni por `completed` aislado |
| OM13 | `TenantSecurityBoundary`: alcance dev, tenant A/B, JWT/RBAC | Backend y gobierno estrictos | Ningún texto o gate documental eleva privilegios |
| OM14 | `ReleaseBoundary`: dev, QA, main y producción | Autorización institucional diferenciada | LISTO_DEV/A5 no despliegan a QA/main/PROD |

Una relación `PlanItem` 1→N `AuditEvent` sigue el ID exacto; `PlanItem` 0→1 `OwnerIdentity` activo por transición; `PublishedArtifact` 1→N `CheckEvidence` exige misma versión; `A5Review` N→1 artifact SHA de la unidad autora. Ninguna relación indica la existencia de tablas nuevas o garantías transaccionales entre servicios.

## 3. Invariantes de integridad, concurrencia y seguridad

| Invariante | Exigencia futura observable | Negativo que impide certificar |
| --- | --- | --- |
| INV01 | Un plan item coincide exactamente en PLAN/COLA/DAG | IDs adyacentes asumidos equivalentes |
| INV02 | Owner exclusivo se reclama con CAS comprobado | Dos Tasks escriben simultáneamente el mismo scope |
| INV03 | Nuevo HEAD se revalida antes de cada commit | Historia ajena sobrescrita por archivo antiguo |
| INV04 | BITACORA tiene evento de 26 columnas e idempotencia | Eventos duplicados o modificaciones históricas |
| INV05 | `RUN_ID` es nativo sólo si se obtuvo del proveedor | Clave local se presenta como ID nativo |
| INV06 | Señal de otra Task se interpreta según su fuente | `enabled+completed` se toma como falla |
| INV07 | Relevo exige abandono demostrado y ausencia de escritor | Rescate por antigüedad, lease dudoso o CI corriendo |
| INV08 | Checks CI exigibles corresponden al SHA evaluado | Skipped/cancelled/failure/queued se atribuyen éxito |
| INV09 | Error recuperable tiene causa, hipótesis y reparación | Relanzamiento idéntico sin cambio y sin evidencia |
| INV10 | A5 conserva autor distinto de revisor habilitado | Autoaprobación o identidad retirada |
| INV11 | Dependencias terminales usan gates dinámicos verdaderos | M01.06/M06.06 liberados por fórmula falsificada |
| INV12 | JWT/RBAC y empresas A/B permanecen aisladas | Frontend o telemetría concede acceso backend |
| INV13 | La fila de telemetría es propia y no más reciente | Sobrescribir el último run de otro owner |
| INV14 | No ejecutar API/SQL/worker por contrato M02 | Documento tomado como aprobación de migración |
| INV15 | No se crean ni alteran seis Tasks programadas | Automatización recursiva, run_now o cambio de RRULE |
| INV16 | QA/main/PROD conservan autorización separada | LISTO_DEV/A5_PASS interpretado como despliegue |

Estas invariantes son **contratos de evaluación**, no una certificación empírica de cero carreras, cero errores en los 2.340 IDs o ausencia de defectos multiempresa.

## 4. Matriz de oráculos positivos/negativos para implementación o auditoría posterior

| Caso | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| AT01 | PLAN/COLA/DAG se unen por ID exacto y revelan dependencias | Una fila sin DAG es considerada elegible |
| AT02 | CAS tiene un único ganador y readback coherente | Dos claims con mismo ID concluyen ambos éxito |
| AT03 | Un check exact-SHA `completed/success` habilita A4 pertinente | `cancelled` tras otro commit se cuenta PASS |
| AT04 | A5 independentemente coteja blob, diff y autor | Revisión por mismo TASK_ID firma su entrega |
| AT05 | Una Task pausada intencionalmente permanece sin reactivación | `is_enabled=false` produce reactivación automática ciega |
| AT06 | Error transitorio conserva estado real y sigue tarea independiente | Estado LISTO inventado ante CI `in_progress` |
| AT07 | Registros de BITACORA son aditivos, completos y únicos | Reinicio duplica evento o reescribe una fila previa |
| AT08 | Fallo causal reparable registra prueba de corrección | Warning-as-error se oculta al desactivar analizador |
| AT09 | Tenant A/B permanece aislado con pruebas HTTP autorizadas | Token, header o UI concede privilegio a empresa ajena |
| AT10 | Control de cuota impide recursión entre Tasks | Un evento lanza tareas nuevas o bucle server-side |
| AT11 | Reporte AA:AN separa estado nativo y autoinforme | Última señal escrita se llama tiempo real |
| AT12 | Un rollback se publica en Git por commit nuevo y nueva CI | Reset o force-push elimina historia de otros autores |

**AT01–AT12 son casos futuros NO_EJECUTADOS en esta unidad.** No se hicieron ensayos de carrera concurrentes, llamadas a Tasks, alertas, consultas a servidores, pruebas MySQL, ejecuciones Playwright, mediciones de SLO o auditorías de 2.340 filas con certificación cero defectos. Las lecturas de PLAN/COLA/DAG y el readback del propio claim sólo acreditan este ID y su instante de comprobación.

## 5. Impacto, dependencias, contención y rollback

El diff de esta microtarea contiene únicamente este documento y una sección íntegra de `CHANGELOG_AI.md`. No modifica código .NET 10, Angular 22.2.1, EF/MySQL 8.4, SQL, API, frontend, CI, seguridad, JWT/RBAC, datos Aiven, servidores, cuentas, permisos, RRULE ni secretos. M02.02 define futuros contratos API/DTO; M02.03 persistencia, M02.04 configuración, M02.05 eventos y M02.06 condiciones de salida; ninguno obtiene permiso runtime por este M02.01.

Una implementación futura de monitor requiere mecanismo atómico real, retención con minimización de datos, prueba de concurrencia por dos ejecutores autorizados, cuotas, permisos verificables, métricas sin PII y rollback, con nueva orden explícita. Esta especificación no implementa un webhook/event dispatcher ni llamada server-side infinita. En errores, se conserva owner y estado real, se adjunta evidencia verificable y se procura otra candidata independiente, sin autoconceder SI o falsear A5.

**Rollback de dos Markdown:** `git revert` normal en dev con diff y CI nueva, evento BITACORA aditivo, sin force-push ni edición de eventos históricos. Ninguna reversión en DB o producción procede porque no existe cambio de runtime.

## 6. Auditorías A0–A5 y frontera de evidencias

- **A0 estática:** se contrastan 2.340 IDs como tamaño del plan, columnas institucionales y la autoridad V5; OM01–OM14, INV01–INV16 y AT01–AT12 están definidas con responsables explícitos. No afirmar que el tamaño equivale a un censo funcional aprobado.
- **A1 funcional documental:** invariantes y pares de escenarios son coherentes y verificables, pero pruebas de carrera y detección exhaustiva de duplicados `FUTURAS_NO_EJECUTADAS`.
- **A2 seguridad y aislamiento:** tenant A/B, JWT/RBAC, permisos mínimos, seis Tasks allowlist y separación dev/prod preservados conceptualmente; sin comprobación HTTP de penetración.
- **A3 integración:** trazabilidad documental entre STANDARD V5, PLAN/COLA/DAG, BITACORA y el contrato terminal M01.06; compilación .NET/Angular, MySQL, Playwright y proveedor Automations `NO_APLICA_DOCUMENTAL` al diff.
- **A4 publicación:** fast-forward con expected-HEAD CAS, sólo dos Markdown, controles `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA publicado y readbacks PLAN/COLA/BITACORA.
- **A5:** requiere una de las otras cinco Tasks Javier distinta de `6adc2e4bd8a48191ac7d8641ed95a1a3`; nunca autoaprobación ni permiso para QA/main/PROD.

Las pruebas no causales, si aparecen queued/in_progress/cancelled/skipped, no se cuentan como PASS. El resultado de M02.01 es un contrato verificable de modelo y propiedad para una auditoría operativa futura, no una auditoría operativa ya ejecutada.
