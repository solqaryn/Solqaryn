# SOLQARYN V5 — C15-05.M01.04: riesgos, supuestos y decisiones de auditoría operativa

**Autor:** PRIMARY DEV 04 JAVIER; TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Fecha local:** 2026-11-14, America/Tegucigalpa. **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. **Clasificación:** análisis documental, ningún fallo del SaaS o automatización declarado como verificado por este texto.

## Fuente única, antecedentes y exclusiones

Contrato ejecutivo: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). PLAN_MAESTRO fila 2309, COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI y owner CAS exclusivo comprobados antes de la unidad. M01.01 en SHA `100ba76944aca6e142ced3e046a5d57b32148570` definió NR01–NR14, M01.02 en `0977438eff257a90ab13967fdaf4702508d415ed` identificó A01–A12, IO01–IO12 y F01–F10, M01.03 en `5ce0b257cf3eb2beb531a90e82448095e0d13d45` inventarió S01–S08 y G01–G12. Esos artefactos son fuentes documentales versionadas, no garantías de que los procesos futuros funcionan.

La frontera de datos/autoridad sigue siendo backend ASP.NET Core 10, MySQL y JWT/RBAC/multiempresa; Angular 22.2.1 no sustituye autorización. La gobernanza de seis Tasks Javier admite operaciones sólo en `dev`, con A0–A4 y A5 independiente. No se modifican cuentas, cron, Tasks de terceros, bitácora anterior, datos MySQL, ramas QA/main, producción ni infraestructura.

## Doce riesgos potenciales R01–R12, NO_REPRODUCIDOS

| Riesgo | Modo de fallo hipotético a comprobar | Fuente / brecha | Prueba causal futura requerida |
| --- | --- | --- | --- |
| R01 | Un prompt simula identidad de Task o autorización de cuenta | F01; G01 | Corroborar propietario, ID nativo, permiso GitHub/Drive |
| R02 | Dos Tasks obtienen owner simultáneo en un claim no atómico | F03; G02 | Carrera controlada con CAS único y readbacks |
| R03 | COLA muestra SI pese a DAG NO o predecesores pendientes | F04; G03 | Recalcular fórmulas y gate por ID exacto |
| R04 | Reintento produce doble evento o pierde transición BITACORA | F05; G04/G05 | Eventos idempotentes y estrés acotado en copia de prueba |
| R05 | Monitor actualiza fila de otra Task o pisa corrida posterior | F08; G06/G07 | CAS de fila+run y comparación con estado nativo |
| R06 | Un check skipped/queued/failed de SHA anterior se interpreta PASS | F06; G08 | GET check-runs con head_sha exacto, status/conclusion |
| R07 | Changelog pierde procedencia por concurrencia de commits | F09; G09 | Árbol candidato, encabezado y viñetas + CAS ff |
| R08 | Autor y revisor A5 comparten TASK_ID por error de atribución | F07; G10 | Contraste TASK_ID autor y revisor + SHA original |
| R09 | Se transfiere owner huérfano con otro escritor aún activo | F03; G01/G12 | Lease/actividad y CAS con evidencia negativa |
| R10 | Documento de preparación habilita migración, auth o QA/PROD | F10; G11 | Prueba de no-escritura en superficies restringidas |
| R11 | Un run completed/enabled causa relanzamiento desatendido | F01/F08; G07 | Watchdog con último run, pausa humana y ausencia de run_now |
| R12 | Cierre LISTO de código sin .NET/Angular/MySQL CI causal | F06; G08/G12 | Suite dirigida contra SHA y fixture real efímero |

**R01–R12 = RIESGOS_ABIERTOS**, no vulnerabilidades descubiertas ni incidentes certificados. Cualquier R que se convierta en FAIL requiere reproducción, comparación antes/después, impacto y reparación independiente. La ausencia de un test no demuestra conformidad ni falla.

## Ocho supuestos S01–S08 por comprobar

| Supuesto no certificado | Evidencia pendiente |
| --- | --- |
| S01: La conexión GitHub correcta sigue correspondiendo al usuario Javier en cada corrida | Perfil/permiso y readback por ejecución |
| S02: La conexión Drive conserva escritura atómica limitada a rangos autorizados | Ensayo de bloqueo, permisos, historial y celdas |
| S03: Las fórmulas COLA/DAG representan predecesores dinámicos sin rotura | Comprobación por lote y comparación PLAN |
| S04: El proveedor devuelve último estado físico suficientemente reciente | Reconciliar peek con event timestamp y run_id |
| S05: Las escrituras de BITACORA no duplican eventos durante fallos de red | Reintento simulado e inspección de idempotencia |
| S06: Un fast-forward CAS rechaza HEAD obsoleto sin perder commits de otros | Carrera de cambios documental en entorno de prueba |
| S07: Los checks relativos al SHA original se conservan para revisión A5 | Releer check-runs y diffs, sin reusar otro SHA |
| S08: El rollback no afectará archivos/propietarios o entornos ajenos | Plan de revert limitado y comparación del diff |

Todos son `NO_VERIFICADOS` por la presente unidad. La existencia del código, el permiso o el diseño no reemplaza observación causal.

## Ocho decisiones pendientes D01–D08

| Decisión a resolver | Restricción mientras permanezca pendiente |
| --- | --- |
| D01: Definir protocolo de carrera CAS y fixture sin datos productivos | No simular exclusividad global |
| D02: Elegir nivel de aislamiento de escritura en Sheets ante transacciones parciales | No reportar garantía que el proveedor no da |
| D03: Aprobar auditoría periódica de fórmulas y versión de DAG por ID | No fijar allowlist de IDs elegibles estáticos |
| D04: Definir recuperación ante BITACORA llena o append interrumpido | No borrar historial para ganar capacidad |
| D05: Diseñar retención y reconciliación de señales auto-reportadas con run nativo | No presentar AA:AN como telemetría continua |
| D06: Precisar clasificación causal de CI por tecnología y obligatoriedad | No convertir NO_APLICA en PASS |
| D07: Definir manual de resolución A5_FAIL y owner inactivo mediante evidencia | No autorrevisar ni reasignar automáticamente |
| D08: Aprobar plan de rollback, controles de seguridad y promoción futura | Ningún PASS dev autoriza QA/main/PROD |

D01–D08 son `PENDIENTES`, no nuevas autorizaciones para tocar permisos, cuentas, schedules, servicios de pago, infraestructura o datos productivos.

## Ocho pares de pruebas P01–P08/N01–N08, FUTUROS_NO_EJECUTADOS

| Caso | Positivo requerido | Negativo que debe detectarse |
| --- | --- | --- |
| P01/N01 | Sólo Task Javier autorizada con permisos reales opera en dev | Task ajena o contexto falsificado escribe |
| P02/N02 | Exactamente un owner gana CAS bajo competencia | Dos owners escriben mismo micro-ID |
| P03/N03 | COLA/DAG/PLAN coinciden y cambios liberan sólo predecesores reales | Gate NO se fuerza a SI |
| P04/N04 | Evento A:Z único y durable tras reintento | Doble conteo o overwrite histórico |
| P05/N05 | CI requerida completed/success y head_sha coincidente | Skipped/cancelled/in_progress/sha ajeno cuenta PASS |
| P06/N06 | Revisor A5 independiente conserva autor y evidencia | Auto-A5 o cuenta retirada |
| P07/N07 | Monitor respeta run_id físico y limita AA:AN a su fila | Señal self-report o corrida vieja pisa la nueva |
| P08/N08 | Publicación ff/CAS y rollback no destructivo | Force push, CHANGELOG vacío o promoción ilegal |

Cada test futuro requiere dataset sintético, ID, actor, timestamps UTC, SHA, antes/después, resultados observados, evidencia y rollback. No se ejecutan mediante la mera existencia de esta matriz.

## A0–A5 documentales y condición de liberación

- **A0:** trazabilidad y consistencia de 12 riesgos, 8 supuestos, 8 decisiones y 8 pares futuros con M01.01–03 y G01–G12.
- **A1:** oráculos falsables de autorización, CAS, dependencia y CI; pruebas reales `FUTURO_NO_EJECUTADO`.
- **A2:** seguridad fail-closed, separación entre experiencia UI y auth API JWT/RBAC, tenant A/B, ausencia de secretos y cuentas externas.
- **A3:** integrar fuentes Git y Sheet de forma documental; carreras, bases de datos, HTTP/Playwright no aplican al diff Markdown, nunca PASS adjudicado.
- **A4:** publicar sólo este Markdown y sección autocontenida de CHANGELOG con ff/CAS, Scope Lock y procedencia V5 `completed/success` en SHA exacto; PLAN/COLA/BITACORA readback.
- **A5:** revisor independiente TASK_ID distinto del autor. No habilita automáticamente M01.06, QA, main o producción.

**Rollback:** `git revert` normal sobre dev y evento aditivo de rectificación, sin force-push ni edición de BITACORA histórica. La siguiente M01.05 determinará criterios mensurables, M01.06 exige predecesores y evidencia auténtica.
