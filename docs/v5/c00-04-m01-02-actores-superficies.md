# C00-04.M01.02 — Entradas, salidas, actores y superficies del gobierno de cambios

**Fuente ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES SOLQARYN V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Sheet oficial:** `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. **Gobierno:** `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_AUTHORITY.md`, `docs/AUTOMATION_V13.md` y Skill canónica SOLQARYN en `dev`.

## Objetivo verificable y alcance

Inventariar las **entradas, salidas, actores reales, interfaces y superficies afectadas** por una decisión de cambio en SOLQARYN, distinguiendo autorización humana, diseño documental, cambio de código en desarrollo y operaciones sensibles. Esta microtarea prepara la fase M01 de C00-04; **no** implementa servicios, altera políticas, cierra la fase, promueve QA/main/PROD ni autoriza migraciones.

La evidencia mínima consiste en esta matriz de entradas y salidas, límites de cada actor, riesgos y gates observables, más el diff publicado con Scope Lock del SHA exacto y readback de estados. No se crean nuevos IDs ni Tasks.

## Matriz de entradas, salidas y propietarios

| Actor o superficie | Entrada real | Salida esperada | Límite y control |
| --- | --- | --- | --- |
| Propietario autorizado | Orden vigente, restricciones del plan, decisiones de alcance y excepciones expresas | Autorización identificable y auditable por operación | No inferir permiso productivo de una solicitud de análisis o del estado de una cola |
| Task Javier habilitada | `TASK_ID`, cuenta GitHub `jmejia31`, Drive JAVIER, fila V5, DAG, HEAD y permisos | Un claim de ID propio, evidencias A0–A4 y A5_PENDIENTE por distinto revisor | No sustituir a Alex ni reclamar ID ocupado; `is_self_owned=false` solo no es denegación ni permiso |
| Task Alex de cuenta propia | Identidad y credenciales propias del titular, scope sin colisión | Cambios atribuidos a su propio TASK_ID | Javier no administra sus Tasks ni atribuye su autoría |
| Google Doc V5 FINAL | Microtarea, RESULTADO_EXIGIDO, condiciones del lote | Contrato y aceptación verificables sin duplicar backlog | No usar planes históricos, ni una arquitectura futura como licencia de despliegue |
| `PLAN_MAESTRO` | ID, estado H, responsable I, evidencia J:L y fecha M | Estado PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO con rastro causal | H es autoridad editable; transiciones solo con autorización, pruebas y readback |
| `COLA` y `DAG_TECNICO` | Vista por fórmula E/G/O, prerequisitos revisados y estado VALIDADO | Señales de elegibilidad calculada y control por dependencia | `SI` no es claim automático; sin responsable libre o gate real no despachar |
| `BITACORA` A:Z | EVENT_ID, idempotency key, autor, revisor, SHA y evidencia | Append contiguo con **26 campos**, readback y eventos históricos conservados | Rechazar evento truncado de 21–25 campos o EVENT_ID/IDEMPOTENCY_KEY repetidos |
| GitHub `dev` | Diff/candidato, HEAD, tree/blobs, commit FF, workflows YAML | SHA nuevo, logs y checks pertinentes vinculados a SHA exacto | No force-push, ramas, PR ni overwrite concurrente; no contar SKIPPED como PASS |
| Frontend Angular / API .NET / MySQL | Cambios funcionales futuros, JWT/RBAC/tenant y contrato de datos | Pruebas dirigidas por capa sobre jobs GitHub Actions existentes | Este documento **no** cambia runtime; no simular pruebas HTTP, browser o base de datos |
| `qa`, `main`, PROD, servicios externos | Solicitud de promoción o cambio reversible/irreversible | No se realiza transición sin nueva autorización y A5_PASS independiente | Secretos, respaldos, DNS, pagos, proveedores y datos productivos fuera de alcance |

## Contratos y flujo de decisión

1. Identificar un ID del plan V5 y su `RESULTADO_EXIGIDO`, dependencias, riesgos, archivos y consumidores reales.
2. Confirmar identidad efectiva, estado `PENDIENTE` de PLAN_MAESTRO, `COLA!G=SI/O=SI`, DAG `VALIDADO` cuando proceda, ausencia de propietario, y relectura para evitar colisión.
3. Clasificar **TIPO 1–5** y declarar `requiere_dotnet`, `requiere_node_angular`, `requiere_mysql`, `requiere_docker`, `requiere_e2e`, `requiere_security`, `requiere_scope_lock` por alcance. Para este documento: `NO, NO, NO, NO, NO, SI, SI`.
4. Crear una entrega identificable en `EN_PROGRESO`; pasar `VALIDANDO` **antes** de A0–A3; registrar auditorías proporcionales. Un documento no certifica runtime por el simple hecho de existir.
5. Publicar exclusivamente mediante fast-forward directo a `dev` cuando el gate previo pasa, verificar A4, `head_sha` exacto de Scope Lock completado/success, diff, rollback y readback.
6. Cerrar LISTO_DEV solo con A0–A4 aplicables PASS y evento `A5_PENDIENTE`. Otra Task revisa A5 con evidencia positiva/negativa; A5_PASS tampoco permite por sí solo QA, main o producción.

## Riesgos y límites de autoridad

| Riesgo | Detección | Respuesta |
| --- | --- | --- |
| R01: doble autoría | ID ocupado, fingerprint duplicado, archivos concurrentes | No reclamar o preservar responsable original y conciliar HEAD sin cambios destructivos |
| R02: evidencia de otra revisión | `head_sha` distinto, check no disparado, `SKIPPED` o `pending` | No atribuir PASS; verificar logs del job pertinente; preservar VALIDANDO si se requiere |
| R03: acceso o credenciales cruzadas | Cuenta GitHub/Drive distinta, Task Alex desde Javier | Rechazar escritura y escalar permiso efectivo, sin suplantación |
| R04: fuga multitenant | Cambios a JWT/RBAC/EmpresaId, logs o datos personales | Requerir negativos de tenant A/B y seguridad proporcional, no fiarse del frontend |
| R05: auditoría truncada | BITACORA A:U de 21 campos históricos en vez de A:Z 26 | Exigir 26 celdas y claves únicas; no borrar ni falsificar eventos |
| R06: salida productiva accidental | Cambio de `qa/main/PROD`, VPS/DNS, proveedores, facturación o migración | Requiere A5_PASS y autorización expresa independiente, sin acción desde C00-04.M01.02 |

## Aceptación positiva y negativa

**Positivo:** el actor Javier con Task física y GitHub/Drive autenticados puede entregar un documento M01.02 para un ID **libre y habilitado**; PLAN conserva H e I, COLA refleja estado por fórmula, BITACORA recibe 26 campos y el commit documental directo `dev` supera Scope Lock exact SHA. La salida define actores, entradas, interfaces, riesgos y no-alcance; no requiere ejecutar pruebas .NET/Angular/MySQL ajenas.

**Negativo A (propiedad):** si `PLAN_MAESTRO!I` pertenece a otra Task, aunque G/O aparezcan `SI`, no tomar el ID ni modificar su código.

**Negativo B (identidad):** leer una Task Alex o tener `is_self_owned_automation=false` desde otro agente no habilita suplantarla ni suspenderla; solo permisos reales de la cuenta propia pueden autorizar acciones.

**Negativo C (integridad):** un evento de 21, 22, 23, 24 o 25 celdas, o un `EVENT_ID` o `IDEMPOTENCY_KEY` repetidos, no autoriza el cierre; el registro real es A:Z/26.

**Negativo D (seguridad):** CI frontend verde de un SHA anterior no prueba un cambio de JWT/RBAC/tenant; ningún documento o aprobación de DEV autoriza QA/main/PROD.

**Negativo E (concurrencia):** si HEAD avanza, reconciliar de forma aditiva y repetir las verificaciones afectadas antes de actualizar el ref; nunca force-push.

## Capas, consumidor, datos, rollback y evidencias

**Impacto:** afecta exclusivamente documentación de contratos V5, `CHANGELOG_AI.md` e historial de Sheets; no cambia API/DTO, entidades, migraciones, transacciones, JWT, RBAC, UI, workers, integraciones externas, secretos o despliegue. Los consumidores del contrato son las seis Tasks Javier y las dos Alex desde su cuenta propietaria, los revisores A5 y el seguimiento institucional.

**Pruebas aplicables:** A0 lectura de diff, Markdown, links, integridad del changelog y búsqueda dirigida de secretos; A1 matriz de I/O/actores y caso positivo+cinco negativos; A2 prohibiciones de acceso cruzado, tenant/PII, QA/main/PROD; A3 concordancia con V5, Skill, V13, DAG y BITACORA de 26 campos. A4 tras commit comprueba SHA exacto, Scope Lock, archivos y readback. Node/Angular/.NET/MySQL/Docker/E2E: **NO_APLICA** para este changeset, no se alegan tests de runtime.

**Prerrequisitos:** C00-01.M02.01–06 y C00-03.M01.01–05 `LISTO` en el preflight; DAG_TECNICO `VALIDADO`. Revalidar C00 completo y autorización antes de M01.06, M02 o implementación efectiva. No suplir dependencias ausentes con un objetivo de reserva.

**Rollback:** `git revert` normal del commit documental publicado, más registro auditado de cualquier reconciliación de hoja. No reset, force-push, reescritura de eventos, rollback de datos productivos ni promociones implícitas.
