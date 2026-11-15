# SOLQARYN V5 — C15-05.M01.01: alcance, exclusiones y no-regresión de auditoría operativa

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Corte:** 2026-11-14, America/Tegucigalpa. **Repositorio autorizado:** `solqaryn/Solqaryn`, exclusivamente rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** preparación documental M01.01, no certificación general ni cambio del runtime.

## Objetivo y resultado verificable

Delimitar el control transversal de la ejecución comercial V5: **dependencias dinámicas, estados, evidencias, reversión, concurrencia, identidad y ausencia de duplicados**. El resultado de esta unidad es un conjunto de invariantes auditables con oráculos positivos y negativos, límites de autoridad y un mapa de comprobaciones futuras. No reemplaza el desarrollo, las pruebas causales, la auditoría A5 independiente ni la evidencia del cierre M01.06.

Fuentes rectoras verificadas: `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md`, `docs/AUTOMATION_AUTHORITY.md`, la Skill canónica `.agents/skills/solqaryn-project-governance/SKILL.md` y `references/architecture.md` en `dev`; documento `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` ID Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet institucional `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. PLAN_MAESTRO fila 2306 identificó el ID C15-05.M01.01; COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI, estado PENDIENTE libre, seguidos de claim condicional y readback de un solo owner. Los recuentos de la cola son instantáneas, nunca autorizaciones permanentes.

## Fuentes y observaciones de control (sin atribuir pruebas no realizadas)

| Ref | Fuente | Alcance de la evidencia y limitación |
| --- | --- | --- |
| F01 | Google Doc V5 FINAL | Define alcance de 2.340 unidades existentes, no prueba implementación |
| F02 | PLAN_MAESTRO H/I | Estado y propietario de cada ID, únicos para toma de decisiones |
| F03 | COLA G/O | Prefiltro calculado de elegibilidad; por sí solo no autoriza un claim |
| F04 | DAG_TECNICO A/F/G | Prerrequisitos por ID y gate técnico; exige preflight causal adicional |
| F05 | BITACORA A:Z | Historial append-only de eventos y claves idempotentes, no sustituto de estado |
| F06 | TAREAS_PROGRAMADAS AA:AN | Último autorreporte de la Task, no estado nativo en tiempo real |
| F07 | Automations.peek | Estado físico y RUN_ID sólo cuando el proveedor los acredita |
| F08 | GitHub `dev` y check-runs | HEAD, diffs y comprobaciones sobre `head_sha` exacto |
| F09 | Skill/AGENTS/Scope Lock | Identidad, arquitectura, allowlist y límites obligatorios |

Al preflight se observaron seis Tasks Javier habilitadas, sin atribuir propiedad positiva sólo a `is_self_owned_automation`; las otras cinco tenían última corrida completada en esa consulta. **El conjunto autorizado es exclusivamente** `6adbb9fff1548191a1bd5b30d9e9984a`, `6af16bb9492c8191b8e8c7d606f11507`, `6adc2e4bd8a48191ac7d8641ed95a1a3`, `6aeacb7820ac8191b7d768a6f33dfa09`, `6adc2e55f47c81919f6374e561687076`, `6af16bbad26881918c283abe06e1f32a`. Es un censo institucional, **no** autorización de Tasks adicionales ni de cuentas retiradas.

## Dentro y fuera del alcance

**Dentro de futuras comprobaciones:** inventario consistente de PLAN/COLA/DAG, exclusividad de claims, autores y revisores diferenciados, historia de transiciones, pruebas por causa, integridad de CHANGELOG, recuperación de fallos sin concurrencia, rollback no destructivo, identidades autorizadas y señalización de esperas de CI sólo para el scope afectado.

**Fuera de M01.01:** modificar tareas físicas, programación, títulos, cuentas, permisos, scopes ajenos, pruebas .NET/Angular/MySQL reales, secretos, migraciones, datos productivos, workers, webhooks, runner externo, QA, main, PROD, Hostinger, Cloudflare o adjudicar permisos desde una tabla. La estructura registrada no equivale a un motor de ejecución permanente.

## Catorce invariantes de no-regresión

| Invariante | Condición que deberá preservarse | Negativo prohibido o detectado |
| --- | --- | --- |
| NR01 | Sólo repositorio `solqaryn/Solqaryn` y rama `dev` con Scope Lock STRICT | Escritura en repo/entorno no autorizado |
| NR02 | Únicamente las seis Tasks Javier del allowlist pueden operar o revisar | Task de otra cuenta aparece como A5 válido |
| NR03 | Cada ID debe existir en PLAN y corresponder al Doc V5 original | Tarea inventada, archivada o PR legado reaprovechado |
| NR04 | Claim parte de PENDIENTE/I vacío y COLA G/O=SI | Owner ocupado, columna derivada manipulada o tarea duplicada |
| NR05 | DAG F=VALIDADO y G=SI por ID con prerequisitos verificables | Prefiltro COLA SI libera DAG ausente/NO |
| NR06 | CAS de un solo ganador, con readback antes de cambios de código | Dos escritores toman mismo ID |
| NR07 | Estado progresa sólo con evidencia EN_PROGRESO, VALIDANDO, LISTO | LISTO marcado al publicar sin pruebas |
| NR08 | A0–A4 son pertinentes y CI requerida completa en SHA exacto | Queued/skipped/cancelled/failed equivale a PASS |
| NR09 | A5 proviene de TASK_ID distinto al autor, con evidencia original | Autoaprobación o revisión por Task retirada |
| NR10 | BITACORA A:Z sólo agrega EVENT_ID e IDEMPOTENCY_KEY únicos | Reescritura histórica o doble evento idempotente |
| NR11 | Telemetría propia se limita a AA:AN en fila/ID verificados | Se sobrescribe fila ajena o corrida propia más reciente |
| NR12 | CI en progreso bloquea únicamente su microtarea, no toda la cola | Fallo auxiliar provoca cierres inventados o lock global |
| NR13 | CHANGELOG conserva encabezados con viñetas y CAS fast-forward | Se intercalan secciones o se fuerza la rama |
| NR14 | Rollback y promoción respetan límites de seguridad y entornos | A5 dev autoriza producción, secretos o migración |

**NR01–NR14 son criterios para futuras inspecciones y regresiones; su redacción no demuestra que todos los escenarios hayan sido ejecutados ni que no existan defectos.**

## Ocho pares de pruebas positivas y negativas FUTURAS_NO_EJECUTADAS

| Caso | Positivo verificable requerido | Negativo que debe rechazarse |
| --- | --- | --- |
| P01/N01 | ID autorizado y libre supera PLAN/COLA/DAG y CAS único | DAG NO o fila sin coincidencia habilita claim |
| P02/N02 | Dos ejecutores compiten y exactamente uno conserva owner | Ambos publican sobre mismo ID |
| P03/N03 | Cada transición tiene evento único y readback de 26 campos | Evento duplicado o estado perdido se contabiliza LISTO |
| P04/N04 | Job requerido `completed/success` en `head_sha` correcto | CI en curso, omitida, cancelada o de SHA anterior cuenta PASS |
| P05/N05 | Revisor independiente distinto del autor, verificado en historial | Propia Task u otra cuenta certifica A5 |
| P06/N06 | Pausa autorizada no se revierte y completed no dispara rescate | Monitor relanza Tasks o deshace pausa humana |
| P07/N07 | CHANGELOG sin sección vacía, commits fast-forward no destructivos | Encabezado separado de viñetas o force-push |
| P08/N08 | Telemetría refleja sólo hechos acreditados, con run nativo correcto | RUN_ID inventado o estado self-report llamado estado del proveedor |

Un caso futuro debe registrar resultado observado real, quién ejecutó, repositorio, ID, owner, SHA, datos sintéticos cuando proceda, timestamp UTC, consola/logs redactados, rutas/archivos, contraejemplo y rollback. Ninguna prueba de esta matriz se ejecutó contra el servidor de producción.

## Dependencias, seguridad y criterios de aceptación futura

La fase M01 define preparación. M01.02 deberá clasificar entradas/salidas y roles, M01.03 inventariar evidencia original por SHA, M01.04 documentar riesgos/decisiones, M01.05 especificar oráculos de aceptación. M01.06 no libera etapas sin los gates vigentes, dependencias requeridas y A5 independiente. `DAG_TECNICO` es una fuente dinámica, no una excepción general que habilite JWT/RBAC, cambios tenant, migraciones, M06, deployment o QA/PROD.

**Seguridad transversal:** el backend ASP.NET Core 10 es autoridad real de JWT/RBAC/tenant A/B; el frontend Angular 22.2.1 y sus guards sólo controlan la experiencia. Un cambio de automatización documental no puede saltarse la revisión de API, persistencia, auditoría, rutas, protección de secretos ni control de concurrencia. No se añadieron workers, webhooks ni triggers.

**A0 documental:** inspección de catorce invariantes, ocho pares y nueve fuentes, sin confundir textos con resultados ejecutados. **A1:** consistencia positiva/negativa de oráculos, runtime `FUTURO_NO_EJECUTADO`. **A2:** aislamiento multiempresa, permisos y exclusividad preservados sin tocar código. **A3:** trazabilidad a PLAN/COLA/DAG, Skill, GitHub y telemetría, sin afirmar CI runtime adicional. **A4:** diff exclusivo Markdown más sección de CHANGELOG, publicación por fast-forward/CAS, Scope Lock y procedencia V5 `completed/success` en SHA exacto y readbacks PLAN/COLA/BITACORA. **A5:** revisión posterior por otra Task Javier; no autoaprobación.

**Rollback:** revert Git ordinario del changeset con registro aditivo de rectificación si procede. Jamás force-push, cambio de historial de BITACORA o intervención productiva. Este artefacto no certifica la operación integral del SaaS, los estados físicos futuros ni ausencia global de fallos.
