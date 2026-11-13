# SOLQARYN V5 FINAL — C00-04.M01.06: bloqueo, rollback y condición de liberación del gobierno de cambios

**Clase:** TIPO 1, contrato documental; no implementa un aprobador, dispatcher, migración, API, worker ni política productiva. **Responsable:** PRIMARY DEV 04 — JAVIER_CHATGPT, `TASK_ID=6aeacb7820ac8191b7d768a6f33dfa09`. **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-13.

**Autoridad única de backlog:** `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; seguimiento `PLAN_MAESTRO`, `COLA`, `DAG_TECNICO`, `BITACORA` en Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Solo son autorizadas las seis Tasks Javier contempladas en `docs/PROJECT_SCOPE_LOCK.md` y `docs/AUTOMATION_STATUS.md` del HEAD.

## 1. Resultado exigido y alcance

C00-04 exige **fijar qué requiere autorización, qué es reversible y qué no se ejecuta automáticamente**. Esta M01.06 sintetiza decisiones, riesgos, condiciones de bloqueo, ruta documental de rollback y puertas verificables **para evaluar** M02. No ejecuta cambios ni certifica el cierre de C00, no crea séptima Task o 2.341 microtarea y no libera cualquier ID por reserva `MAX(24,3*N)`.

En el preflight institucional: `PLAN_MAESTRO!H110:H114=LISTO` para las cinco M01.01–.05, con A5_PASS correctivo independiente de J20 sobre el CHANGELOG corregido SHA `8ee14f8b40114041a587e9a38f188851c9807505` sin borrar FAIL histórico; `C00-01.M06.06=LISTO` con A5 independiente J00, SHA `e5d54809af2d902cd497f1be5803c9ae36725561`. `DAG_TECNICO` fila C00-04.M01.06 tenía F=VALIDADO/G=SI y exigía exactamente esos seis prerrequisitos. Antes del claim, `PLAN!H115=PENDIENTE/I115` vacío, `COLA!G115/O115=SI`; después del claim, estado EN_PROGRESO con titular propio y `COLA!O115=NO` por fórmula. La lectura del DAG es permiso de **evaluación** de este ID, no habilitación global de M02.

## 2. Contratos de entrada M01 y rastros de origen

| ID | Artefacto `dev` / blob SHA comprobado | Condición de salida |
| --- | --- | --- |
| M01.01 | `c00-04-m01-01-alcance-gobierno-cambios.md` / `bebf45dad2f2be1f5597d2f3840f5642048fcc39` | Objetivo, exclusiones, no-regresión |
| M01.02 | `c00-04-m01-02-actores-superficies.md` / `fda09fc62876023a8fe786e8c1066fac1f554c15` | Actores, entradas/salidas, fronteras |
| M01.03 | `c00-04-m01-03-fuentes-codigo-evidencias.md` / `1659f27bbee144fc50abee22e98d45c61c1316f3` | Fuentes verificables y límites de pruebas |
| M01.04 | `c00-04-m01-04-riesgos-supuestos-decisiones.md` / `14a42f137ec0f205156a031a2969789b03a35a57` | Registro R01–R10, supuestos, decisiones |
| M01.05 | `c00-04-m01-05-criterios-aceptacion.md` / `e480b4eac5b966aca492e557ff2f3a7d0da714ad` | AC01–AC12, oráculos positivos/negativos |
| Transversal C00-01.M06.06 | `PLAN_MAESTRO!H37=LISTO`, revisión A5 de J00 | Gate del bloque base, no autorización de producción |

Los identificadores anteriores son hashes de blobs o referencias de estado, **no** sustituyen `check_runs.head_sha` ni acreditan tests runtime. Un A5_FAIL histórico se conserva; su corrección exige evidencia causal y revisor permitido.

## 3. Decisiones, bloqueos y retorno

| Gate | Acción permitida | Bloqueo y retorno seguro |
| --- | --- | --- |
| Identidad, cuenta y permisos | Solo una Task Javier habilitada con GitHub `jmejia31` y Drive JAVIER, sin colisión | Task fuera de allowlist o permiso denegado: NO_GO de operación; no suplantar Alex |
| Estado único | `PLAN_MAESTRO!H` como autoridad, `I` como titular; `COLA!E/G/O` como fórmulas | Estado divergente, claim ajeno o CAS no demostrable: no escribir; registrar fingerprint |
| DAG y prerequisitos | M01.06 solo cuando sus seis anteriores figuran LISTO y A5 causal cuando se exige, DAG VALIDADO | `G/O=SI` aislado, predecesor incompleto o fase no liberada: NO_GO de ese ID |
| Calidad y seguridad | A0–A4 proporcionales; JWT/RBAC/tenant A/B, secretos, PII y autorización siempre preservados | Scope Lock fallido, prueba pertinente pendiente o fuga de datos: mantener VALIDANDO o corregir |
| Publicación | Git commit fast-forward directo `dev` con HEAD fresco, sin PR/branch/force-push | HEAD avanza o archivos intercalados: conciliar, reauditar; jamás reescribir historia |
| Auditoría | 26 campos A:Z, EVENT_ID e IDEMPOTENCY_KEY únicos; RUN_ID únicamente del proveedor | Evento repetido o de 21–25 celdas: rechazar; nunca alterar eventos antiguos |
| Revisión A5 | Revisor Javier de TASK_ID distinto del autor sobre SHA publicado | A5_FAIL o autoaprobación no autoriza QA/main/PROD, aun con LISTO_DEV |
| Entorno y reversibilidad | Diseño documental y `git revert` ordinario condicionado a HEAD y dependencias | DNS, R2, Hostinger, MySQL productivo, cobros, secretos, main/QA/PROD: prohibidos sin orden expresa y pruebas propias |

**Rollback por alcance:** esta entrega modifica únicamente un archivo Markdown y una sección aditiva de `CHANGELOG_AI.md`; una reversión técnica es `git revert` normal después de inspeccionar HEAD y consumidores. No borrar bitácora, no revertir datos productivos mediante Git, no force-push. Si una celda necesita reconciliación, usar transición compensatoria auditable con 26 campos y lectura posterior.

## 4. Condiciones de liberación de M02 y rechazo de falsas evidencias

1. Tras el cierre M01.06 en `LISTO_DEV_A0_A4_PASS;A5_PENDIENTE`, **reexaminar cada ID M02 independientemente**: RESULTADO_EXIGIDO, PLAN H/I, COLA G/O, DAG y dependencias, permisos, archivos, colisiones y herramienta real. `LISTO` M01.06 no autoriza claims en cascada.
2. Un alcance TIPO 1 puede verificar documentos, fórmulas, diff, SHA y Scope Lock. Un futuro cambio .NET/Angular/MySQL/Docker/E2E debe aportar jobs pertinentes reales sobre SHA exacto; un job `SKIPPED`, `CANCELLED` o `IN_PROGRESS` no es PASS cuando es obligatorio.
3. El protocolo exige primero REVIEW_FIRST propio; luego resolver bloqueos de owner realmente indisponible solo con comprobación de escritura/lease y transferencia CAS con único ganador; después PENDIENTE libre. No hay despacho automático ni webhooks activos.
4. Cierre documental M01.06 **no** activa infraestructura, revocación JWT, migraciones, permisos, QA/main/PROD, ni sustituye aprobación humana o A5 independiente.
5. `C15-05.M06.06` permanece terminal condicionado a las 2.339 anteriores. No inventar otros IDs ni usar planes V3/V13 como backlog ejecutable.

## 5. Oráculos de aceptación documentales (no pruebas del runtime)

| Caso | Signo | Condición y respuesta obligatoria |
| --- | --- | --- |
| P01 | + | Cinco M01 LISTO con A5 correctivo independiente, C00-01.M06.06 LISTO+A5 y DAG VALIDADO → M01.06 puede reclamarse si H=PENDIENTE/I vacío, G/O=SI |
| P02 | + | Candidato documental completo, A0–A3 documentales verificadas, commit FF, Scope Lock + procedencia exact-SHA y readback → LISTO_DEV con A5_PENDIENTE |
| P03 | + | M02 con prerequisitos propios realmente verificados y alcance seguro → evaluar preflight individual, no activación automática |
| N01 | − | Falta C00-01.M06.06 o cualquier M01.01–05 → no reclamar/cerrar M01.06 |
| N02 | − | A5_FAIL histórico sin corrección, revisor autor o cuenta excluida → no inferir A5_PASS causal |
| N03 | − | COLA G/O SI con DAG faltante o prerrequisito incompleto → no despachar M02 |
| N04 | − | Otra Task posee PLAN I o toca mismos archivos → no segunda escritura ni traslado del owner |
| N05 | − | CI de otro SHA, `SKIPPED`, `CANCELLED` o pendiente en prueba requerida → no A4 PASS |
| N06 | − | BITACORA A:U, 25 campos, EVENT_ID/IDEMPOTENCY_KEY repetidos o RUN_ID inventado → rechazo |
| N07 | − | Documento que afirma compilar .NET, migrar MySQL o certificar E2E sin ejecución real → NO_APLICA, nunca PASS |
| N08 | − | Fuga de JWT, PII, secrets, privilegios RBAC o datos tenant B hacia A → NO_GO y prueba negativa causal |
| N09 | − | Intentar QA/main/PROD, Domain/DNS, proveedores de pago o migración desde este gate → NO_GO |
| N10 | − | HEAD cambia al publicar o CHANGELOG contiene encabezado vacío/intercalado → abortar, conciliar y volver a verificar |

Estos casos son **oráculos de diseño comprobables por inspección**, no tests de API/DB/Playwright implementados por este documento.

## 6. Auditoría proporcional, consumidores y reversibilidad

- **A0:** Markdown coherente, seis evidencias de entrada, sin secretos en diff, encabezados aditivos de CHANGELOG con viñetas inseparables.
- **A1:** comprobar P01–P03, N01–N10 contra estados, fuentes y decisiones, sin afirmaciones de funcionalidad nueva.
- **A2:** límites de identidad, A5 independiente, JWT/RBAC/tenancy/PII, bloqueo de QA/main/PROD, seguridad fail-closed.
- **A3:** comparar blobs M01, DAG F=VALIDADO/G=SI, PLAN/COLA y BITACORA A:Z. No compila, testa ni modifica backend/frontend/DB.
- **A4:** después de publicar FF directo en dev verificar parent, blob/diff, `verify-solqaryn-project-scope` y check V5 de procedencia `completed/success` del `head_sha` exacto, PLAN/COLA/BITACORA, rollback.
- **A5:** otra de las cinco Tasks Javier del allowlist, distinto TASK_ID y pruebas positivas/negativas; registrar `A5_PENDIENTE` antes de LISTO_DEV, no autootorgar A5_PASS.

`requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO`, `requiere_security=SI_DOCUMENTAL`, `requiere_scope_lock=SI`.

**Capas no afectadas:** backend Domain/Application/Infrastructure/API y DTO, frontend Angular, MySQL/EF/migraciones, workers, Docker, CI, secretos, proveedores y arquitectura vigente. **Consumidores:** revisores y las seis Tasks Javier, siguientes microtareas M02 cuando sean elegibles. La arquitectura futura y el inventario de riesgos siguen sujetos a autorización diferenciada.
