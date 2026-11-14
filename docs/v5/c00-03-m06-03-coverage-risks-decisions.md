# C00-03.M06.03 — Riesgos abiertos y decisiones de cobertura integrada V5

**Resultado exigido:** documentar limitaciones, riesgos abiertos y decisiones de la matriz integrada C00-03, con evidencia causal para los seis entregables M05.01–06. Esta microtarea es **documental**, sin modificar runtime, API, UI, MySQL, autorizaciones, Tasks o dependencias de fase. Gobierno: `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`; Plan Comercial V5 FINAL; `PLAN_MAESTRO!H98:H103=LISTO`; `DAG_TECNICO!F134=VALIDADO/G134=SI`, `COLA!G106/O106=SI` al reclamar. Esas condiciones habilitan el análisis, **no** acreditan pruebas live ni M06.06.

## Evidencia causal examinada

| M05 | SHA de origen | Contrato efectivamente probado y límite |
| --- | --- | --- |
| .01 | `f22e544177d1017cd894c7984aec0edd70eb14df` | xUnit de reglas, duplicados, SHA obsoleto y evidencia ausente; no sincronización de Sheet real. |
| .02 | `8d6e12e88922140eca880f3f0d754a8d07dcbbbb` | TestServer: 200 permitido / 400 denegado con DTO; ruta stub y fixtures, no JWT ni API productiva. |
| .03 | `f320739bb1c57c8872c49c8bb13cd38d4be89e75` | Adaptador `V5CoverageMemoryAdapter` + proyección: read/write, rechazo de scope ajeno y revisión vencida; memoria, no MySQL/CAS/lease distribuido. Security/auth fueron aprobadas en reruns dirigidos del SHA; los originales cancelados siguen CANCELLED. |
| .04 | `eced2da14dc3c812e137f1837e007ea6c22108c9` | Chromium y Angular en `frontend/e2e/c00-03-m04-05-accessibility.spec.ts`; tabla/403/foco; sesión sintética, no JWT live. |
| .05 | `1f04379da266451f54dd88669e3178481a88f7d8` | xUnit negativos de actor, scope, tenant y bindings mediante mapa confiable de test; no aislamiento persistente HTTP/MySQL. |
| .06 | `fe73c45f5ec1d4f8c6dfa2a69f000725898dc36a` | Chromium y Angular en `frontend/e2e/c00-03-m04-06-density.spec.ts`: 390/1280, teclado, presupuesto del runner, ausencia de llamadas operativas; no SLA producción. |

Fuente consolidada: `docs/v5/c00-03-m06-01-test-sufficiency-audit.md` (corregida por M06.02) y `docs/v5/c00-03-m06-02-reproducibility-manifest.md`, blob `400453ab3a60b0bd26dd10ebe2f712b6e55dcd1d`, con árbol, SHA y blobs exactos por entrega. M06.02 corrigió dos rutas equivocadas de M06.01: no atribuir el spec genérico `v5-plataforma-browser-guards.spec.ts` a los commits originales de M05.04/M05.06. La lectura de CI de origen no es reejecución en este cambio documental.

## Registro de riesgos sin falsas certificaciones

| ID | Riesgo verificable / vacío | Decisión de mitigación y responsable técnico propuesto |
| --- | --- | --- |
| R01 | `LISTO` en snapshot de prueba puede confundirse con evidencia verificada | Conservar `EVIDENCE_NOT_VERIFIED` hasta verificación institucional real; responsable Application/gobierno |
| R02 | Proveedor en memoria pierde datos al reiniciar y no protege varias instancias | No declarar persistencia ni lease; planificar proveedor duradero separado con pruebas transaccionales MySQL; Backend/datos |
| R03 | Prueba TestServer expone ruta stub y contrato DTO, no endpoint institucional | No prometer integración HTTP de producción sin control de auth en API real; API/seguridad |
| R04 | Autorización basada en bindings confiados por fixtures no prueba JWT/BCrypt/RBAC | Exigir pruebas de rechazo servidor por credencial válida/inválida cuando ese código exista; seguridad |
| R05 | Aislamiento de scope y tenant en pruebas no equivale a empresas A/B reales | No certificar tenancy persistida hasta pruebas backend+DB en tenant A/B; seguridad/datos |
| R06 | Playwright intercepta sesión y respuestas 403/200 | Etiquetar los resultados como pruebas de UX/guard; no presentar sesión simulada como autenticación live; frontend/QA |
| R07 | M05.03 tuvo jobs originales CANCELLED y reruns posteriores | Conservar ambas evidencias, exigir `status=completed`, `conclusion=success` y `head_sha` exacto para cada gate pertinente; autor/revisor |
| R08 | Versiones/documentos pueden citar ruta o blob erróneo | Usar manifiesto M06.02 y comparar SHA/ruta/blob del commit fuente; corregir por nuevo commit, sin reescribir BITACORA; documentación |
| R09 | Escritores concurrentes en dev y CHANGELOG pueden intercalar secciones | Releer HEAD, insertar bloque encabezado+viñetas atómico y publicar fast-forward con CAS; gobernanza GitHub |
| R10 | Independencia A5 y 26 campos de BITACORA se pueden sobreatribuir | Solo otro TASK_ID Javier permitido puede emitir A5_PASS; RUN_ID vacío sin evidencia; append idempotente A:Z; gobierno |
| R11 | `COLA G/O=SI` o DAG G=SI puede interpretarse como aprobación terminal | No liberar `C00-03.M06.06` ni QA/main/PROD por esta nota; su cierre exige auditorías y dependencias propias; coordinador de fase |
| R12 | La matriz integrada podría activar webhooks o workers antes de autorización | Registrar como diseño futuro exclusivamente; `NO_IMPLEMENTAR_EVENTOS_TODAVIA`, sin dispatcher, run_now ni nuevas Tasks; owner |

## Decisiones y próximos controles

- **D01 — alcance cerrado:** la presente entrega **documenta** R01–R12 y sus dueños propuestos; no los resuelve con cambios de código, ni garantiza seguridad, persistencia o sincronización live.
- **D02 — evidencias válidas:** los seis M05 están LISTO en `PLAN_MAESTRO` y tienen origen Git comprobable; checks cancelados, omitidos o pendientes no cuentan como aprobación. Las rutas originales de M05.04/M05.06 son las corregidas por M06.02.
- **D03 — pruebas futuras separadas:** CAS/leases Sheets, API con JWT/RBAC real, aislamiento tenant A/B persistido y MySQL 8.4 requieren microtareas de implementación y CI específica. No fabricarlos mediante fixtures, tests in-memory o reportes.
- **D04 — A0–A5:** para **este diff documental**, `requiere_dotnet=NO_APLICA`, `requiere_node_angular=NO_APLICA`, `requiere_mysql=NO_APLICA`, `requiere_docker=NO_APLICA`, `requiere_e2e=NO_APLICA`; `requiere_security=SI` por honestidad JWT/RBAC/tenant y datos, `requiere_scope_lock=SI`. A0 diff/secretos/CHANGELOG; A1 positivo y negativos; A2 límites de seguridad; A3 relación seis M05 y manifiesto; A4 commit fast-forward, Scope Lock/CHANGELOG exact-SHA y readback; A5 pendiente por **otra Task Javier**, no autorrevisión.
- **D05 — secuencia:** M06.04 ensaya reversión sin datos legítimos borrados; M06.05 solicita revisión independiente; M06.06 gate terminal propio. Ninguno queda automáticamente LISTO por este registro.

## Matriz de aceptación

- **P1 positivo:** seis entradas M05.01–06 con SHA fuente y contrato probado, frente a `PLAN_MAESTRO!H98:H103=LISTO`.
- **P2 positivo:** doce riesgos con decisión, owner sugerido, límites y vínculo con M06.01/M06.02.
- **N1 negativo:** un `200` de TestServer o un `LISTO` de fixture **no** prueba JWT/RBAC ni aprobación institucional.
- **N2 negativo:** el proveedor en memoria **no** acredita CAS Sheets, MySQL, tenancy persistida o recuperación interproceso.
- **N3 negativo:** un check `CANCELLED`, `SKIPPED` o `IN_PROGRESS`, y un SHA distinto, **no** son PASS de gate requerido.
- **N4 negativo:** este documento **no** autoriza M06.06, auto-A5, QA/main/PROD ni despachos de Tasks.

**Rollback:** `git revert <SHA_DOCUMENTAL>` sobre dev tras lectura de HEAD y resolución no destructiva. No force-push, borrar eventos BITACORA ni revertir cambios de otros autores. La revisión A5 y la certificación de controles nuevos siguen pendientes por separado.
