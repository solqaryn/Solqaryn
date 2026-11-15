# SOLQARYN V5 — C15-05.M01.03: inventario de fuentes y brechas de evidencia operativa

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`; **fecha:** 2026-11-14 America/Tegucigalpa; **repositorio:** `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`. Resultado: lectura documental de contratos y código sin certificar comportamiento de servicios.

## Identidad, alcance y antecedentes

El documento V5 FINAL STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, define las unidades existentes y fases; el Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` contiene el estado operativo. En el preflight propio el ID C15-05.M01.03 figuraba en PLAN_MAESTRO H=PENDIENTE/I vacío, COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI; se realizó claim condicional exclusivo y readback.

Los antecedentes M01.01, commit `100ba76944aca6e142ced3e046a5d57b32148570`, blob `c7c26c211404fd00edc87d49914996d9eae9e748`, establecen NR01–NR14 y P01–P08/N01–N08. M01.02, commit `0977438eff257a90ab13967fdaf4702508d415ed`, blob `59e2628650693037b77fd143f03ae74df1077ea6`, define A01–A12, IO01–IO12 y F01–F10. Son marcos de auditoría futura, **NO resultados de pruebas de concurrencia, aplicación o seguridad runtime**.

## Ocho fuentes de código y gobierno, leídas por blob SHA en el HEAD de entrada

| Ref | Ruta exacta de SOLQARYN | SHA del blob leído | Lo que permite afirmar / límite |
| --- | --- | --- | --- |
| S01 | `AGENTS.md` | `063d342891ec6e0b23b9adb195ef3b358b51548d` | Identidad de rama, Scope Lock y 6 Tasks, no su estado físico nativo |
| S02 | `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Restricciones, allowlist y límites, no CI actual por sí solo |
| S03 | `docs/AUTOMATION_STATUS.md` | `4b25c0e88e8fb593cbafb5f8836db42c7d703fca` | Contrato de continuidad y monitor, no RUN_ID real |
| S04 | `docs/AUTOMATION_AUTHORITY.md` | `83fd8cb8fdd611eab8972927c83e52cb3e3ccd05` | Flujo de claim, A0–A5, revisión, no permiso del proveedor implícito |
| S05 | `.agents/skills/solqaryn-project-governance/SKILL.md` | `304e400d5c0f89fcfdcc65caece86c34276734b5` | Skill local canónica cotejada, no suplantación de identidad |
| S06 | `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Stack y fronteras declarados, no builds ni pruebas empíricas |
| S07 | `scripts/quality/verify-v5-changelog-provenance.mjs` | `00e0b1414695b433f9c48cf7ad1f099a0e4b010b` | Código de verificación de atribución, no reemplaza GitHub Actions |
| S08 | `PROJECT_CONTEXT.md` | `48064b93299e973c33145662897d772712d2fa29` | Contexto y riesgos de cambios, con historia no autoritativa |

La lectura de cada blob se hizo contra la cuenta GitHub autorizada y no desde un mirror. Los ocho blobs constituyen una **línea base delimitada, no inventario exhaustivo** de workflows, controles de permisos, formularios, runners o la base de datos.

## Fuentes operativas externas y separación de autoridad

| Fuente | Papel real | Control negativo |
| --- | --- | --- |
| PLAN_MAESTRO H/I | Estado/owner institucional de cada ID | No inferir LISTO sólo del commit |
| COLA G/O | Elegibilidad derivada y disponibilidad | No convertir SI en bypass DAG |
| DAG_TECNICO A/F/G | Predecesores técnicos y gate | No inventar VALIDADO o SI |
| BITACORA A:Z | Historia append-only, EVENT_ID e idempotencia | Nunca corregir historia sobrescribiendo |
| TAREAS_PROGRAMADAS AA:AN | Último autorreporte limitado a fila propia | No declarar estado nativo por AE |
| Automations.peek | Enabled, run actual y RUN_ID cuando esté disponible | `is_self_owned_automation=false` no prueba autorización o denegación |
| GitHub check-runs exact SHA | Prueba de CI pertinente completada y exitosa | `skipped/cancelled/in_progress/failed` no es PASS |

## Doce brechas abiertas de evidencia (G01–G12), sin suponer defectos observados

| Brecha | Comprobación faltante y evidencia exigible |
| --- | --- |
| G01 | Reconciliar cada owner con la Task física en el momento de claim y posible lease activo |
| G02 | Demostrar exclusividad real de CAS en una competencia controlada sin dos escritores |
| G03 | Inventariar fórmulas y dependencias de COLA/DAG por ID, detectando referencias rotas |
| G04 | Confirmar eventos A:Z idempotentes ante reintentos concurrentes y fallos parciales |
| G05 | Confirmar no reescritura histórica y capacidad disponible de filas de BITACORA |
| G06 | Probar que la telemetría no pisa filas ajenas o una corrida propia más nueva |
| G07 | Correlacionar native run_id/start/completed con señal local sin falsos positivos |
| G08 | Demostrar validación de check-run requerido por head_sha y conclusión en commits sucesivos |
| G09 | Reproducir conflictos de HEAD y fast-forward sin intercalado de CHANGELOG |
| G10 | Verificar A5 de TASK_ID distinto al autor sobre el SHA y documento original |
| G11 | Comprobar rollback aditivo sin alterar datos productivos, ramas restringidas o secretos |
| G12 | Registrar fallos y estados huérfanos con pruebas de ausencia de escritura simultánea |

**G01–G12 están ABIERTAS/NO_VERIFICADAS por esta unidad**. Un hallazgo real requiere prueba causal, fechas, ID, SHA, logs/salida redactada, severidad sustentada, dueño y reparación comprobada. Una diferencia de documentación histórica no implica automáticamente fallo operativo actual.

## Ocho pares de pruebas positivas y negativas futuras (no ejecutadas)

| Caso | Positivo observable a demostrar | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | Task Javier con identidad autorizada reclama ID libre tras preflight | Task ajena o owner ocupado consigue claim |
| P02/N02 | DAG VALIDADO/SI y dependencias satisfechas se evalúan por ID | COLA SI sobre DAG NO libera tarea |
| P03/N03 | Dos claims en carrera producen exactamente un owner | Ambos creen ser propietarios y publican |
| P04/N04 | A0–A4 causal y exact-SHA habilitan LISTO con readback | CI queued/cancelled/skipped/failed marca LISTO |
| P05/N05 | Revisor A5 distinto del autor comprueba artefacto original | Auto-A5, SHA antiguo o cuenta fuera allowlist |
| P06/N06 | Monitor reconcilia señal local y estado nativo preservando run_id | RUN_ID inventado o self-report confundido con estado real |
| P07/N07 | Evento BITACORA único A:Z sobrevive a reintento idempotente | Duplicación o sobrescritura de fila histórica |
| P08/N08 | Commit FF/CAS conserva CHANGELOG y rollback ordenado | Force-push, encabezado vacío o cambio a QA/main/PROD |

Los P01–P08/N01–N08 son **FUTUROS_NO_EJECUTADOS**. La prueba posterior deberá indicar SHA y trigger, dueño, fixture sintético, secuencia, timestamp, resultado observado, artefacto, URL CI, readback y rollback. No se ejecutaron pruebas de race, fallos del proveedor, E2E, HTTP o MySQL en este cambio documental.

## Seguridad y ruta de auditoría

La integración SaaS conserva Angular 22.2.1 y API ASP.NET Core net10.0 como autoridad de JWT/RBAC y tenant A/B. Las identidades de Tasks, las hojas de Google o los checks documentales jamás sustituyen una prueba de aislamiento comercial del servidor. Fases de infraestructura, workers, webhooks, migraciones y QA/main/PROD no son autorizadas por esta unidad.

M01.04 documentará riesgos, supuestos y decisiones derivados de G01–G12; M01.05 definirá aceptación comprobable; M01.06 sólo puede cerrar con requisitos reales, A5 independiente y revalidación dinámica. Ningún DAG SI por sí solo convierte faltantes en PASS.

**A0:** inventario de ocho archivos con hashes y doce brechas. **A1:** trazabilidad a oráculos P/N futuros sin ejecutarlos. **A2:** no modificar JWT/RBAC, tenant A/B, permisos, Tasks físicas ni scopes de terceros. **A3:** correlación documental con M01.01/02, PLAN/COLA/DAG/BITACORA; pruebas runtime `NO_APLICA_DOCUMENTAL` (no PASS). **A4:** publicación de este Markdown más sección completa CHANGELOG, fast-forward/CAS, Scope Lock y procedencia V5 exact SHA `completed/success`, readback PLAN/COLA/BITACORA. **A5:** independiente de otra Task Javier.

**Rollback:** `git revert` normal sin reescritura y registro de rectificación aditivo. Esta entrega no implementa código funcional, cambia horarios o certifica el SaaS completo.
