# SOLQARYN V5 FINAL — C00-02.M01.06: bloqueo, rollback y condición de liberación de M01

**Responsable:** PRIMARY DEV 01 — JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT, 2026-11-13. **Clase:** definición DOCUMENTAL del cierre del modelo de trabajo; no implementación de despachador, locks ni cambios a código.

**Autoridades:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill y Scope Lock canónicos. El resultado exigido de C00-02 es definir cola, dependencias, autoría, estados y cierre sin duplicación; M01.06 documenta las **puertas** de liberación, no despacha automáticamente las tareas M02.

## Inventario causal del lote M01

Al preflight, PLAN H38:H42 = LISTO para las cinco predecesoras; cada una registra **A5_PASS de otra Task Javier autorizada** sobre su SHA vigente o corregido, sin eliminar A5_FAIL históricos. Además, C00-01.M06.06 = LISTO, con A5_PASS independiente J00 sobre fuente e5d54809af2d902cd497f1be5803c9ae36725561. DAG_TECNICO `C00-02.M01.06` F=VALIDADO/G=SI exige precisamente estos seis requisitos; COLA G/O=SI y PLAN H=PENDIENTE/I vacío antes del claim.

| Fuente de M01 / blob real en dev | Estado / revisión | Qué aporta al cierre |
| --- | --- | --- |
| [M01.01 objetivo](c00-02-m01-01-alcance-modelo.md) — `22245b85a8643a387b40f1cd54ac44902d8cbb5c` | LISTO, A5_PASS corregido; A5_FAIL antiguo preservado | Autoridad V5 única y no-regresión |
| [M01.02 interfaces](c00-02-m01-02-interfaces-actor-superficies.md) — `b93dbfa7e1a1340fe41646d1c2abd1dde5dc54df` | LISTO, A5_PASS corregido | Actores, entradas, salidas, límites |
| [M01.03 fuentes](c00-02-m01-03-fuentes-contratos-evidencias.md) — `d1959f143b483719d068f09fe53e16a149ab3841` | LISTO, A5_PASS correctivo | Fuentes Git/Sheet, estados, rastro de 26 campos |
| [M01.04 riesgos](c00-02-m01-04-riesgos-supuestos-decisiones.md) — `ba347d07e1197858ffd1ff839f34c88806351a97` | LISTO, A5_PASS corregido | Colisiones, dependencia, permisos y rollback |
| [M01.05 aceptación](c00-02-m01-05-criterios-aceptacion-modelo.md) — `e7f485f72b0b0fbcb162c8f05751e16da317592f` | LISTO, A5_PASS independiente | Casos positivos/negativos de cierre V5 |

Los SHA de la tabla son **blobs**, no SHA de CI. La revisión independiente A5 es causal para autoría/historia de estas entregas documentales, **no** una certificación de funcionamiento runtime de futuras fases. Mantener A5_FAIL en BITACORA sin sobrescribirlo.

## Contrato de estados, ownership y eventos

| Estado PLAN_MAESTRO H | Quién puede operar | Puerta / salida |
| --- | --- | --- |
| PENDIENTE | Una Task Javier autorizada, con owner I vacío, COLA G/O=SI y DAG VALIDADO | Claim idempotente hacia EN_PROGRESO, con autor/TASK_ID y readback |
| EN_PROGRESO | Exclusivamente la misma Task propietaria (salvo CAS de rescate autorizado) | Implementación/candidato real y evento de entrega |
| VALIDANDO | Mismo propietario sobre candidato exacto, sin liberación de claim | A0 estática; A1 funcional; A2 seguridad; A3 integración según artefacto |
| LISTO | Cierre institucional ya probado, no queda libre para segundo claim | A4 del SHA publicado y readback, evento A5_PENDIENTE, LISTO idempotente |
| BLOQUEADO / CANCELADO | Solo por causa auditada y autoridad vigente | No reabrir ni liberar por fórmula o por pausa de plataforma |

`PLAN_MAESTRO!H` es el único ESTADO mutable; `COLA!E` replica fórmula y no se sobrescribe manualmente. `COLA!G/O` y `DAG_TECNICO` son puertas derivadas, no permisos de nueva Task. Concurrencia independiente por ID, exclusión de trabajo simultáneo sobre mismo ID/archivos, compromiso fast-forward sin force-push. No usar un lease perpetuo ni reclamar filas en cascada para una reserva objetivo.

## Matriz de liberación y negativos fail-closed

| Caso | Tipo | Condición y resultado |
| --- | --- | --- |
| P01 | Positivo documental | Las cinco M01.01–05 están LISTO con A5 causal y C00-01.M06.06 LISTO + A5; DAG VALIDADO, COLA G/O=SI, owner vacío: se permite reclamar **M01.06** |
| P02 | Positivo documental | Al completar A0–A4 de M01.06 y registrar A5_PENDIENTE, LISTO_DEV se registra una vez; luego M02 **solo se reconsidera** mediante nuevo preflight de cada ID |
| N01 | Negativo dependencia | Cualquier antecesora en PENDIENTE/VALIDANDO, DAG no VALIDADO o A5 causal requerido ausente: M01.06 no se reclama/declara LISTO |
| N02 | Negativo colisión | Otra Task posee PLAN I o un commit cambió HEAD: no tomar ID ajeno; revalidar, conciliar sin force-push |
| N03 | Negativo autoaprobación | A5 del autor o de ejecutor ajeno al allowlist de seis Javier: no aceptar A5_PASS; mantener historia y pendiente |
| N04 | Negativo estado duplicado | Reintento de mismo EVENT_ID/IDEMPOTENCY_KEY: no append duplicado ni segunda transición |
| N05 | Negativo fuentes | COLA E manual divergente de PLAN H: detectar y conciliar con evidencia, no despachar basándose en dato obsoleto |
| N06 | Negativo CI | Scope Lock FAILURE o check obligatorio IN_PROGRESS/SKIPPED/CANCELLED: no afirmar A4 PASS |
| N07 | Negativo seguridad | Ausencia de permiso GitHub/Drive, actor incorrecto, tenant/secretos o Scope Lock STRICT violado: NO_GO de esa operación |
| N08 | Negativo backlog | No crear una 2.341 microtarea, no ejecutar V3/V13 backlog anterior ni PR/branches comerciales |
| N09 | Negativo salto fase | LISTO M01.06 no altera PLAN H de M02 ni habilita QA/main/PROD por sí mismo |
| N10 | Negativo reloj | Task pausada o runtime agotado no justifica marcar LISTO; conservar estado/autor y evidencia real |
| N11 | Negativo BITACORA | Evento de 21 columnas, RUN_ID ficticio o borrado histórico: rechazo; 26 campos A:Z y claves únicas |
| N12 | Negativo false test | Documento de criterios no sustituye .NET, Angular, MySQL o E2E cuando otro scope modifique runtime |

Los casos P/N son **pruebas de contrato documental** de la puerta, no ejecución de un workflow despachador ni pruebas live MySQL/HTTP.

## Criterios de bloqueo y recuperación segura

- Bloqueo por permisos, Scope Lock fallido, dueño activo, DAG no validado, dependencia real sin LISTO/A5 causal o error crítico de seguridad: registrar fingerprint, evidencia, estado real y siguiente acción; continuar otra tarea independiente.
- Rescate de un owner verdaderamente indisponible: comprobación de actor, lease y ausencia de escritura, CAS con un único ganador, rastro del dueño previo/nuevo en BITACORA; nunca suplantar revisión ni adoptar trabajo que aún progresa.
- CI asíncrona: mantener solo esa unidad VALIDANDO hasta job pertinente exact-SHA; no esperar suites tecnológicas irrelevantes para documentos.
- Rollback de commit: `git revert` normal después de leer nuevo HEAD; preservar historia y eventos BITACORA; reconciliar estado Sheet por transición compensatoria trazable, jamás borrar ni falsear A5_FAIL.

## Cierre de M01.06 y condición futura de M02

A0 revisar diff documental, esquema de 26 campos, SHA de fuentes, secretos y contrato. A1 comprobar P01/P02 y N01–N12 contra dependencias y escenarios; A2 revisión de permisos e independencia, sin cambiar JWT/RBAC/tenancy; A3 coherencia DAG/PLAN/COLA y consumidores M02; A4 publicar fast-forward y verificar SHA exacto, Scope Lock SUCCESS, diff y readback. `requiere_dotnet=NO_APLICA`, `requiere_node_angular=NO_APLICA`, `requiere_mysql=NO_APLICA`, `requiere_docker=NO_APLICA`, `requiere_e2e=NO_APLICA`; `requiere_security=SI` (autoría, aislamiento de permisos/claims y control de registros), `requiere_scope_lock=SI` (commit en dev). A5 de otra Task Javier distinta del autor permanece pendiente para promoción.

M02.01–M02.06 **no** pasan a EN_PROGRESO por el mero LISTO de M01.06: deben observar `COLA!G/O`, DAG, estado PLAN I/H, fuente y A0–A5 según su propio contenido antes de un claim. Nunca crear nuevas filas auxiliares ni tocar QA/main/PROD, secretos o ejecutores ajenos.
