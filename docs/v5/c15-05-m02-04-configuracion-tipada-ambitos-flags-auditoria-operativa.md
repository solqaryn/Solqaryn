# SOLQARYN V5 FINAL — C15-05.M02.04: configuracion tipada, ambitos y feature flags de auditoria operativa

**Responsable:** PRIMARY DEV 03 JAVIER_CHATGPT, TASK_ID \`6adc2e4bd8a48191ac7d8641ed95a1a3\`. **Repo:** \`solqaryn/Solqaryn@dev\`; \`PROJECT_SCOPE_LOCK=STRICT\`. **Naturaleza:** contrato DOCUMENTAL/PROPUESTO. Ninguna clave CFG de este documento se agrega a appsettings, entornos Angular, Google Sheets CONFIG, variables reales, servicios, API, workers, eventos, webhooks ni programaciones fisicas.

## 1. Autoridad, dependencia y diferencia respecto del runtime

Unica fuente ejecutable: [STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); estado y trazas: [PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). El alcance C15-05 es validar dependencias, estados, evidencia, rollback, concurrencia y ausencia de trabajos duplicados. Este contrato no certifica aun esa ausencia.

Fuentes previas: [M02.01](c15-05-m02-01-modelo-ownership-invariantes-auditoria-operativa.md), con OM01–OM14, INV01–INV16 y AT01–AT12; [M02.02](c15-05-m02-02-api-dto-errores-compatibilidad-auditoria-operativa.md), cuyas interfaces son NO_IMPLEMENTADAS; y [M02.03](c15-05-m02-03-persistencia-indices-migraciones-fixtures-auditoria-operativa.md), con PD01–PD12, IX01–IX06 y fixtures no ejecutados. La fuente del estado real sigue en PLAN!H/I/K, COLA!G/N/O y DAG!F/G; BITACORA es append-only de 26 columnas, y AA:AN es ultimo autorreporte, NO señal nativa en tiempo real.

Preflight de \`C15-05.M02.04\`: PLAN H2315=PENDIENTE e I vacia, COLA G/O=SI, DAG A2276=ID exacto F=VALIDADO/G=SI. El claim requiere lectura previa, actualizacion de estado/owner, evento unico y readback. Una lectura+escritura de Sheets no prueba CAS transaccional intersistemas: futuros controles deben demostrar exclusividad real, no llamarla garantizada por redaccion.

## 2. Ambitos y precedencia para un contrato futuro

| Ambito | Autoridad real | Parametros aceptables | Negativo |
| --- | --- | --- | --- |
| PLATAFORMA | Skill, AGENTS, Scope Lock en HEAD \`dev\` | Identidad SOLQARYN, rama, allowlist estricta | Ningun usuario cambia reglas por flag |
| PROYECTO_DEV | GitHub HEAD SHA + documento canónico | Version de codigo y scope de cambios | QA/main/PROD no heredan permisos |
| MICROTAREA | PLAN H/I/K y BITACORA | ID exacto, owner y evidencia de A0–A5 | Campo editable no genera CAS ni A5 |
| DEPENDENCIA | DAG F/G y COLA G/O calculados | Solo lectura y readback con fecha | Ningun flag fuerza SI |
| CORRIDA | Automations.peek nativo + fila propia AA:AN | RunId acreditado, timestamp, autorreporte separado | No tratar ultimo reporte como heartbeat |
| AUDITORIA | Eventos A:Z append-only y Git SHA | Dedupe por EVENT_ID/IDEMPOTENCY_KEY | No borrar historico ni normalizar FAIL a PASS |
| TENANT | JWT/RBAC verificados en backend | Solo informacion de la empresa autorizada | EmpresaId, header, cookie o slug no otorgan permisos |

La configuracion futura siempre hereda restricciones superiores. Nunca transportar los secretos de proveedor a flags, docs o log; nunca crear una Task adicional por un valor de configuracion.

## 3. Catalogo tipado propuesto y desactivado (CFG01–CFG16)

| ID | Nombre conceptual | Tipo / default propuesto | Frontera y rechazo |
| --- | --- | --- | --- |
| CFG01 | \`OperationalAuditEnabled\` | boolean, false | False no crea proceso recurrente |
| CFG02 | \`AuditMode\` | enum DRY_RUN_ONLY | No aceptar MUTATE_PLAN sin autorizacion nueva |
| CFG03 | \`SourceRevision\` | string no vacio, requerido | Revision ausente/inconsistente => UNKNOWN |
| CFG04 | \`DevHeadSha\` | SHA-40, de servidor | SHA de navegador no concede A4 |
| CFG05 | \`PlanUniverseSize\` | integer de fuente, 2340 al corte V5 | El conteo no demuestra calidad o cero duplicados |
| CFG06 | \`AllowedTaskIds\` | conjunto inmutable de 6 IDs Javier | Task ajena o retirada => DENY |
| CFG07 | \`CurrentOwnerMode\` | enum SOURCE_ONLY | PLAN I prevalece; flag no asigna owner |
| CFG08 | \`ReadbackRequired\` | boolean, true | Sin readback no se certifica estado |
| CFG09 | \`CheckEvidenceMode\` | enum COMPLETED_SUCCESS_EXACT_SHA | SKIPPED/CANCELLED/IN_PROGRESS no PASS |
| CFG10 | \`ReviewPolicy\` | enum INDEPENDENT_JAVIER_TASK | Autor no puede autoaprobar A5 |
| CFG11 | \`DagGateMode\` | enum COMPUTED_READ_ONLY | No habilitar manualmente COLA/DAG |
| CFG12 | \`TelemetryMode\` | enum LAST_REPORT_ONLY | Corrida local no suplanta RUN_ID nativo |
| CFG13 | \`RedactionPolicy\` | enum STRICT_MINIMAL | No escribir tokens, prompts o PII |
| CFG14 | \`MaxPageSize\` | integer 1..100, default 25 | Rechazar valores negativos o no acotados |
| CFG15 | \`FutureEventDispatcher\` | boolean false | No activar webhook, worker o disparo entre Tasks |
| CFG16 | \`DeploymentScope\` | enum DEV_ONLY | QA/main/PROD exige orden nueva |

Estas claves **NO EXISTEN como configuracion implementada** por esta microtarea. Son contratos para una evaluacion posterior, sujetos a permisos reales, versionado, validacion en backend, pruebas y rollback. Las preferencias reales de negocio y los permisos \`RequierePermiso\` siguen bajo la arquitectura comercial vigente, no pueden duplicarse aqui.

## 4. Validaciones y casos de fallo futuro (VAL01–VAL12)

| Caso | Positivo de contrato futuro | Negativo que debe bloquear |
| --- | --- | --- |
| VAL01 | SourceRevision y DevHeadSha forman un corte coherente | SHA antiguo se anuncia como HEAD |
| VAL02 | Seis IDs Javier identifican ejecutores autorizados | Septima Task o identidad textual se acepta |
| VAL03 | PLAN H/I/K se lee por ID exacto antes/despues | Solo numero de fila determina owner |
| VAL04 | G/O y DAG F/G se consumen como calculados | Flag convierte gate NO en SI |
| VAL05 | A5 tiene revisor distinto y SHA de autor | Texto A5_PASS autoatribuido |
| VAL06 | RunId nativo solo desde provider verificado | CORRIDA_KEY_LOCAL se muestra como nativo |
| VAL07 | El permiso JWT/RBAC/tenant se valida en backend | Tenant A obtiene datos de B por header |
| VAL08 | MaxPageSize respeta 1..100 | Paginas sin limite exponen volumen sensible |
| VAL09 | Redaccion preserva evidencia minima sin secretos | Dump de BITACORA contiene tokens/PII |
| VAL10 | Error de GitHub/Drive mantiene UNKNOWN | Provider failure se registra como PASS |
| VAL11 | Un cambio documental deja Dispatcher=false | Un texto inicia webhook o run_now |
| VAL12 | Dev-only mantiene rollback documental | A5 sirve para promover QA/PROD automaticamente |

**VAL01–VAL12 son futuros oraculos positivos/negativos, NO_EJECUTADOS.** El contrato es comprobable mediante revisiones documentales actuales, no sustituye test de propiedades, HTTP, MySQL, seguridad en vivo ni concurrencia bajo carga.

## 5. Controles de consistencia, delegacion y fallos

1. En cada futura consulta, comparar \`microtaskId\` exacto en PLAN/COLA/DAG; registrar \`observedAtUtc\`, estado, owner, fuente y resultado. Si se detecta discrepancia, detener solo la operacion riesgosa; no rellenar celdas ni forzar gates.
2. Un claim exclusivo requiere soporte CAS/bloqueo real, ademas de verificar ausencia de otro escritor. Readback y evento idempotente no convierten API Sheets en transaccion distribuida. Relevo de propietario requiere evidencia adicional de abandono sin run activo.
3. Un job pertinente acredita PASS unicamente al finalizar \`completed/success\` en el SHA publicado. El resultado \`queued\`, \`in_progress\`, \`skipped\`, \`cancelled\` o \`failure\` deja esa auditoria pendiente o fallida segun causa; nunca se oculta.
4. Una revision A5 independiente debe enlazar autor, revisor permitido, commit original, diff, blobs, controles y evento. No promover por cumplir una cuota de cierres; A5 tecnico no autoriza QA/main/PROD.
5. Si un proveedor no responde, conservar el ultimo estado verificable con la marca de tiempo y \`INDETERMINADO\`. No extrapolar desde \`AA:AN\` ni asumir que la proxima Task ya se desperto; programacion nativa no equivale a dispatcher.
6. Nada de este diseno habilita auditoria de datos personales ni certificados de cero carreras/duplicados. Un futuro read model MySQL debe ser explicitamente autorizado, con migracion aditiva, aislamiento A/B, CI efimera y rollback de datos propio.

## 6. Impacto, A0–A5 y rollback

**Diff propuesto:** este Markdown y una seccion autocontenida al inicio de \`CHANGELOG_AI.md\`. No cambia \`API/Domain/Application/Infrastructure\`, Angular, JWT, RBAC, EF/MySQL, tablas, entidades, dispositivos, integraciones, observabilidad runtime, Task prompts ni horarios. Consumidores: M02.05/M02.06 de C15-05, bajo DAG y preflight separados.

- **A0:** comparar fuente V5, M02.01–03, autoridades, 16 CFG y 12 VAL; ausencia de dos ediciones simultaneas del mismo archivo. **A1:** comprobar coherencia documental de validaciones y negativos, no afirmar tests de endpoint. **A2:** superficie seguridad backend sin cambios; HTTP/tenant A/B \`NO_APLICA\` al Markdown y no PASS atribuido.
- **A3:** referencias de PLAN/COLA/DAG/BITACORA, GitHub y Automations originales; no alterar configuracion runtime ni formulas. **A4:** Git fast-forward con CAS contra HEAD, Scope Lock y procedencia CHANGELOG V5 \`completed/success\` exact SHA; readback institucional posterior. Jobs runtime no causales no se cuentan como PASS.
- **A5:** revisor Javier distinto de DEV03, aun PENDIENTE al cierre de A4; no autoridad para desplegar. **Rollback:** \`git revert\` normal del changeset, sin force-push, reset, borrado de BITACORA ni cambiar main/qa/PROD.
