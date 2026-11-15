# SOLQARYN V5 — C15-05.M01.02: actores, interfaces y fronteras de auditoría operativa

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`; **fecha:** 2026-11-14 America/Tegucigalpa; **rama:** `solqaryn/Solqaryn@dev`; **protección:** `PROJECT_SCOPE_LOCK=STRICT`. Preparación documental, no activación de ejecutores o pruebas runtime.

## Autoridad y antecedente

STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO fila 2307; COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI, claim exclusivo de la Task autoría verificado. Antecedente M01.01 documento `docs/v5/c15-05-m01-01-alcance-no-regresion-auditoria-operativa-automatizaciones.md` (commit `100ba76944aca6e142ced3e046a5d57b32148570`, blob `c7c26c211404fd00edc87d49914996d9eae9e748`) define NR01–NR14 y P01–P08/N01–N08 como pruebas FUTURAS_NO_EJECUTADAS.

La arquitectura/autoridad procede de `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `docs/AUTOMATION_AUTHORITY.md`, la Skill canónica local y `references/architecture.md` del repositorio. La hoja institucional distingue estado autoritativo de PLAN, cálculos de COLA/DAG, BITACORA append-only y telemetría de último autorreporte. Los llamados nativos a Automations.peek no confieren permisos por sí solos; `is_self_owned_automation=false` no es ni permiso ni prohibición definitiva.

## Doce actores y sus permisos (A01–A12)

| Actor | Rol operativo | Límite verificable |
| --- | --- | --- |
| A01 | Propietario humano autorizado | Decide cambios de gobernanza; el texto no altera privilegios físicos |
| A02 | PRIMARY DEV 01 Javier | Ejecutor limitado a su TASK_ID del allowlist vigente |
| A03 | PRIMARY DEV 02 Javier | Idéntico límite de Task, no autorrevisión A5 |
| A04 | PRIMARY DEV 03 Javier | Idéntico límite y lectura de propiedad de microtareas |
| A05 | PRIMARY DEV 04 Javier | Task autora de este documento; no puede revisar su propio SHA |
| A06 | PRIMARY DEV 05 Javier | Task independiente posible, su estado físico se corrobora |
| A07 | PRIMARY DEV 06 Javier | Task independiente posible, su estado físico se corrobora |
| A08 | GitHub conectado de Javier | Lee/escribe exclusivamente `solqaryn/Solqaryn@dev` con CAS y CI |
| A09 | Google Drive conectado de Javier | Lee documento/Sheet, aplica escritura sólo a rangos autorizados |
| A10 | GitHub Actions runner | Prueba código y gobernanza del SHA; no es estado institucional |
| A11 | Revisor independiente A5 | Una de las otras cinco Tasks Javier, diferente al TASK_ID autor |
| A12 | Consumidor institucional de auditoría | Consulta PLAN/COLA/BITACORA, no deriva permiso de registros históricos |

**Censo válido:** seis IDs `6adbb9fff1548191a1bd5b30d9e9984a`, `6af16bb9492c8191b8e8c7d606f11507`, `6adc2e4bd8a48191ac7d8641ed95a1a3`, `6aeacb7820ac8191b7d768a6f33dfa09`, `6adc2e55f47c81919f6374e561687076`, `6af16bbad26881918c283abe06e1f32a`. Ninguna otra cuenta o Task puede reclamar, vigilar ni aprobar bajo este documento.

## Doce interfaces de entrada/salida y evidencia (IO01–IO12)

| Interfaz | Entrada autorizada | Salida y control |
| --- | --- | --- |
| IO01 | Automations.peek(Task Javier) | Estado nativo, run_id sólo si acreditado, sin interpretar completed como fallo |
| IO02 | GitHub HEAD dev | SHA actual y árbol para preflight/candidate CAS |
| IO03 | Skill/AGENTS/Scope Lock | Identidad, versión, arquitectura y fail-closed |
| IO04 | Google Doc V5 FINAL | Alcance microtarea existente; no filas o reservas añadidas |
| IO05 | PLAN_MAESTRO H/I | Estado y owner; cambio condicional de un solo ganador |
| IO06 | COLA G/O | Prefiltro PENDIENTE libre, no sustituye DAG |
| IO07 | DAG_TECNICO A/F/G | Predecesores, revisión validada y gate real por ID |
| IO08 | GitHub diff y CHANGELOG | Commit fast-forward, procedencia y sin encabezados vacíos |
| IO09 | GitHub check-runs SHA | Status completed y conclusion success de checks pertinentes |
| IO10 | BITACORA A:Z append-only | EVENT_ID/IDEMPOTENCY_KEY únicos, readback, sin editar historia |
| IO11 | TAREAS_PROGRAMADAS AA:AN | Sólo fila propia verificada; contadores hechos, no intenciones |
| IO12 | PLAN/COLA post-publicación | Recalcular elegibles, confirmar estado LISTO y autoría preservada |

Cada salida exige evidencia y lectura posterior. No atribuir pruebas positivas o negativas reales por especificar IO01–IO12.

## Diez fronteras de confianza y oráculos positivos/negativos (F01–F10)

| Frontera | Positivo futuro a acreditar | Negativo futuro a bloquear |
| --- | --- | --- |
| F01 | Cuenta Javier usa TASK_ID del allowlist | Task de otra cuenta actúa por historial o prompt |
| F02 | Lector de Skill coincide con copia canónica en HEAD | Skill desactualizada autoriza cambio |
| F03 | PLAN owner coincide con claim exclusivo CAS | Dos escritoras sobrescriben estado |
| F04 | COLA y DAG concuerdan con dependencias reales | COLA=SI fuerza liberación DAG=NO |
| F05 | Auditoría BITACORA tiene claves únicas y 26 columnas | Sobrescritura histórica o evento fantasma |
| F06 | CI requerida pertenece al commit y concluye success | SHA antiguo/queued/skipped/cancelled se trata PASS |
| F07 | A5 independiente conserva autor original | Autor se aprueba a sí mismo |
| F08 | Monitor separa señal local de runtime físico | FINALIZADA auto-reportado afirma run nativo completed |
| F09 | Git publica por parent exacto y sin force-push | Concurrencia destruye commits o separa viñetas |
| F10 | Rollback y promoción respetan autorización diferenciada | A5_DEV habilita main, PROD, secretos o migración |

Todos los positivos/negativos son **FUTUROS_NO_EJECUTADOS**. Un test real posterior registrará Task, run acreditado, ID, actor, owner, estado antes/después, head_sha, CI, fecha UTC, evidencias redactadas sin datos sensibles y la acción de rollback. Ni un resultado de GitHub Actions aislado ni un texto de BITACORA bastan para certificar simultaneidad segura sin readback.

## Seguridad, integración y fases siguientes

Autorización efectiva de recursos comerciales continúa en backend .NET 10, JWT/RBAC y aislamiento multitenant A/B; Angular 22.2.1 es interfaz, no autoridad de acceso. El protocolo no crea ejecutores, servicios de eventos, reservas artificiales, CI alternativa, ni caminos hacia QA/main/PROD. A5 es revisión técnica independiente, no despliegue. Si check requerido está en curso, sólo el ID afectado permanece VALIDANDO; otra unidad elegible puede avanzar sin escrituras concurrentes al mismo scope.

M01.03 debe inventariar fuentes y evidencia causal; M01.04 clasificar riesgos, supuestos y decisiones; M01.05 definir aceptación; M01.06 requiere A0–A4, A5 y predecesores reales, sin liberar fases por una marca aislada de DAG. Los riesgos de doble escritura y pérdida de auditoría deben investigarse en sandbox/estado sintético antes de cambios en el sistema real.

## Auditorías A0–A5 y reversión

**A0:** enumeración consistente de A01–A12, IO01–IO12 y F01–F10 sobre las fuentes del plan. **A1:** oráculos lógicos positivos/negativos futuros `NO_EJECUTADOS`; no resultados de carreras concurrentes inventados. **A2:** fronteras Task ID, permisos, JWT/RBAC/tenant y no-autorrevisión preservadas sin cambios de código. **A3:** rastreo contra M01.01, PLAN/COLA/DAG, GitHub y BITACORA; HTTP/MySQL/Playwright `NO_APLICA_RUNTIME_DOCUMENTAL` para este diff. **A4:** sólo nuevo Markdown y sección autocontenida CHANGELOG, commit directo fast-forward/CAS, Scope Lock y check V5 exact-SHA `completed/success`, readback estado y evento. **A5:** revisión posterior por Task Javier distinta, previa a cualquier promoción autorizada.

**Rollback:** `git revert` normal, sin force-push, preservando evidencia histórica; ningún cambio en cuentas, programaciones, datos reales, QA, main, PROD o infraestructura.
