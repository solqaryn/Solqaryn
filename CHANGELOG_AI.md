## 2026-11-12 — G0-005.B: puerta estructural de readback DEV/QA/PROD (Javier :20)
- Se agregan scripts/quality/g0-005-b-readback-policy.mjs y 4 pruebas Node dirigidas en scripts/tests/g0-005-b-readback-policy.test.mjs; un control positivo sintético y 33 negativos fail-closed para recibos F/G/H, SSO 503, upstream 503, origen, proyecto, SHA y separación de referencias no secretas.
- El positivo solo afirma STRUCTURAL_CONSISTENCY_NOT_LIVE_ATTESTATION. No certifica proveedores reales, bases/caches/secretos reales, login ni aprobación QA/PROD; ninguna credencial se inspecciona, se almacena o se imprime.
- Implementación Node 22.16.0 sin dependencias nuevas ni modificación de Angular, backend, MySQL, auth, tenancy, QA/main/PROD. Rollback: revert aditivo selectivo de commit directo dev sin force.

## 2026-11-12 - G0-005.H - positivo estructural y regresión de SHA exacto (Javier :50)
- Plan Comercial Inicial V3, microtarea G0-005.H, predecesora G0-005.G LISTO. Agrega scripts/tests/g0-005-h-positive-readback.test.mjs, 5 pruebas dirigidas y regresión A/F/G conjunta 34/34 Node22 PASS. SHA GitHub base observado 323e3333e73ce28369d82032c442705a325a9739, no equivale a SHA READY de Vercel real.
- Caso positivo DEV (sintético): mismo SHA en HEAD, deployment metadata y readback, alias SSO esperado 403, upstream 200. El global permanece NO_GO si QA/PROD no observados. Un segundo positivo completamente sintético utiliza dominios .synthetic.invalid de QA/PROD exclusivamente en memoria para comprobar un contrato estructural, explícitamente NOT_LIVE_ATTESTATION; **NO** representa QA/PROD publicado ni crea configuraciones reales.
- Negativos SHA incorrecto, proyecto Vercel foráneo y no exposición de PII/secretos conservan NO_GO. A0 sintaxis Node + inspección secretos/dependencias; A1 positivos/negativos; A2 fail-closed; A3 regresión Node; A4 requiere readback commit dev tras publicarse; A5_PENDIENTE revisión por otro TASK_ID para promoción.
- Sin cambios de seguridad runtime, JWT/RBAC/tenancy, backend, frontend, datos, workflows, QA/main/PROD, secretos o servicios de pago. Reversión: revert selectivo del commit directo en dev, sin force-push, preservando trabajo concurrente.

## 2026-11-12 - G0-005.G - regresión dirigida 503 y READY ajeno (Javier :50)
- Autoridad Plan Comercial Inicial V3 G0-005.G; depende de G0-005.F ya LISTO. Se agregan 7 pruebas negativas dirigidas en scripts/tests/g0-005-g-negative-readback.test.mjs; se reutiliza sin modificar el clasificador G0-005.A y el fixture sintético G0-005.F.
- Rechaza explícitamente: alias SSO 503, API upstream 503 con SSO 403 permitido, Vercel READY de proyecto ajeno, SHA de deployment distinto a HEAD, origen DEV ajeno y cualquier retorno con PII/secreto. No se consulta ningún origen proveedor en vivo: son casos sintéticos que acreditan lógica del clasificador, no disponibilidad externa.
- A0 Node v22.16.0 node --check y revisión de nuevos secretos/dependencias, PASS; A1 7/7 negativos funcionales PASS; A2 fail-closed de PII y falsos positivos PASS; A3 regresión Node --test G0-005.A/F/G 29/29 PASS. A4 requiere commit SHA exacto dev y readback posterior; A5_PENDIENTE revisión independiente antes QA/main/PROD.
- Rollback por revert selectivo de este commit directo DEV, sin force-push ni modificación de QA/main/PROD, MySQL, seguridad runtime, credenciales ni workflows.

## 2026-11-12 - G0-005.F - fixture sintético 503 para readback DEV (Javier :50)
- Fuente Plan Comercial Inicial V3 G0-005.F; depende de G0-005.A LISTO. Prepara un caso medible de resultado esperado, NO un 503 real: fixtures/g0-005-f-operational-503.json con recibos G0-005.C/D/E, SSO alias 503, API upstream 503, QA y PROD NOT_OBSERVED. DEV SHA del fixture es un identificador SINTÉTICO, no prueba de READY de Vercel.
- Prueba nueva scripts/tests/g0-005-f-operational-503-fixture.test.mjs: 4 casos dirigidos; NO_GO ante 503, NO_GO cuando falta observación de QA/PROD aunque controles DEV se vuelvan sanos, no promoción ni filtración PII. Regresión conjunta con G0-005.A: 22/22 Node v22.16.0 PASS; JSON parse y node --check correctos, cero dependencias o secretos añadidos.
- No se consultaron ni cambiaron qa/main/PROD, proveedor real, permisos, sesiones, MySQL ni infraestructura; ningún resultado de prueba sintética certifica HTTP/SSO/despliegues. Se conserva la custodia del autor y A5_PENDIENTE para revisión distinta.
- Rollback: revert selectivo del commit directo en dev, sin force-push, preservando G0-005.A y sus recibos.

## 2026-11-12 - G0-005.A - cierre de readback operativo con clasificador fail-closed (Javier :50)
- Fuente exclusiva: Plan Comercial Inicial V3, G0-005.A. Se reutilizan sin reescritura los recibos GitHub G0-005.C=0cea0aaae291a4d8161791f2b170d7087d5bc0e1, G0-005.D=e9a486776cbecdec8d6dfb6a7b1172cec6c28877 y G0-005.E=544a28fca6f63a9d5827086ccd07ac645a8f61e6, comprobados contra los commits originales y COLA G335=SI.
- Archivo nuevo scripts/quality/g0-005-a-readback-closure.mjs: clasifica recibos, matriz DEV/QA/PROD, origen, proyecto, SHA GitHub/despliegue y HTTP. El alias público DEV esperado es solqaryn-dev.vercel.app y el alias SSO dev protegido es solqaryn-dev-solqaryn.vercel.app. SSO 401/403 => AUTH_GATED_EXPECTED; SSO 503 y API upstream 503 => HTTP_ANOMALOUS/NO_GO; Vercel READY de proyecto ajeno o SHA divergente => NO_GO. Salida estructural nunca equivale a despliegue vivo ni A5.
- Matriz de observabilidad: DEV origen conocido https://solqaryn-dev.vercel.app/; QA/PROD: origen, proyecto, despliegue SHA, status HTTP y motivos NO_OBSERVADOS en esta ejecución, no se alteraron entornos ni se inventó equivalencia LIVE. Los ejemplos de pruebas Node son sintéticos. Si falta proveedor/lectura autentica el clasificador responde NO_GO; no reemplaza CI ni reviews oficiales.
- Pruebas dirigidas: scripts/tests/g0-005-a-readback-closure.test.mjs, 18/18 PASS Node v22.16.0, 16 escenarios negativos; A0 --check, dependencias nuevas 0, revisión de secretos sin hallazgos; A1 funcional sintético, A2 seguridad fail-closed/PII, A3 integración de blob exacto G0-005.E y regresión; A4 requiere SHA dev publicado + readback. A5_PENDIENTE: revisor TASK_ID distinto antes de QA/main/PROD.
- Rollback: revertir exclusivamente el commit directo G0-005.A en dev sin force-push, preservando los recibos C/D/E y commits de terceros. No tocar runtime auth, tenancy, MySQL, frontend, workflows, qa/main/PROD ni secretos.

## 2026-11-12 — Recuperación entre Scopes de Javier y preferencia máxima
- Seis Javier verificadas enabled. El propietario autorizó a las seis Tasks a reactivar una compañera pausada con una sola acción nativa is_enabled=true, lectura posterior y sin cambiar permisos o configuración.
- Preferencia de máximo razonamiento: instrucción y documentación actualizadas; herramienta de Tasks no expone selección model/reasoning_effort, por lo que no se certifica el nivel nativo. Alex sigue fuera de alcance.
- Se mantiene continuidad sin tope de cierres por corrida, auditorías A0–A5 y control estricto de acceso.

## 2026-11-12 - G0-006.F: preparar caso medible de trazabilidad (Javier :30)
- Fixture sintetico para detectar recibo de G0-006.F que reutiliza requirementId G0-006.E; esperado contract/NO_GO, no certificacion institucional.
- Cinco controles Node dirigidos: lectura fixture, baseline sintetico positivo, requisito ajeno negativo, hash de prueba obsoleto negativo y readback BITACORA ausente negativo. El helper de produccion se reutiliza sin alterarlo.
- A0-A3 dirigidas Node 22.16.0, sin cambios de runtime, RBAC, tenants, DB, CI, API, secretos ni dependencias. A4 requiere SHA exacto publicado y readback; A5_PENDIENTE solicita otra Task antes de QA/PROD. Rollback por git revert del commit directo, sin force.

## 2026-11-12 — Continuidad productiva sin tope de cierres
- Se refuerza continuar en una misma ejecución después de 3, 4, 5 o más cierres, sólo si hay tiempo, elegibilidad y evidencia completa A0–A4, registrando A5 pendiente de revisor externo.
- No autopausar Tasks por metas o cola momentáneamente vacía; si el proveedor impone suspensión o requires_user_action, comunicar causa verificable por run_id. No se garantiza eliminar pausas del proveedor.
- PRIMARY DEV 03 :20 se constató pausada por acción requerida sin causa disponible en el registro.

## 2026-11-12 — G0-006.A: trazabilidad directa V13 A0-A5 (Javier :20)
- Cierre atómico del Plan Comercial Inicial: evidencia C/D/E LISTO y matriz requisito→consumidor→código→prueba→SHA en docs/G0-006-A-TRAZABILIDAD.md, sin crear otro plan.
- Puerta asesora scripts/quality/g0-006-a-closure.mjs y pruebas Node deterministas: 1 control positivo, 23 negativos de legado, A0 estática, A5 falso, SHAs/dependencias/readbacks. La puerta no escribe ni certifica externamente.
- A0-A3 dirigidas a Node 22.16.0; A4 requiere SHA fast-forward/readback dev; A5_PENDIENTE exige otro TASK_ID antes de promover QA/PROD. Rollback: revert aditivo del commit, sin force-push; runtime/tenant/DB intactos.

## 2026-11-12 — Calidad A0–A5, títulos Javier secuenciales y continuidad de Tasks
- Se añaden A0 estática exigente sobre candidato y A5 revisión independiente posdev con evidencia diferenciada. El cierre dev exige A0–A4 PASS + A5_PENDIENTE; qa/main/PROD requiere A5_PASS y autorización aparte. No se deshabilitan CI ni seguridad existentes.
- Los nombres de las seis Tasks Javier se normalizan por minuto, preservando ID, RRULE, propietario, historial y estado habilitado. Las dos Alex permanecen pendientes de actualización física desde su cuenta, con propuesta 07 :05 y 08 :35.
- Pausas automáticas de proveedor: no garantizables ni anulables por prompt; registrar requires_user_action y alertar sin bypass. Mantener throughput, sin nuevos PR, ramas, rulesets ni Tasks.

## 2026-11-12 - G0-005.E - readback operativo DEV negativo dirigido (Javier :50)
- Fuente Plan Comercial Inicial V3 G0-005.E, predecesora G0-005.D LISTO. Clasificador determinista sin llamadas externas para distinguir alias SSO protegido, upstream API 503, proyecto Vercel ajeno, READY de SHA distinto y evidencias incompletas.
- 15 casos sintéticos con Node test; no certifican un despliegue real, paridad externa, login, tenants ni estado LISTO. Resultado positivo solo EVIDENCE_CONSISTENT_NOT_LIVE_ATTESTATION; evidencia insuficiente NO_GO.
- Archivos scripts/quality/g0-005-e-operational-readback.mjs y scripts/tests/g0-005-e-operational-readback.test.mjs. Rollback: revert selectivo del commit directo en dev sin force; sin cambio de runtime, DB, secretos ni QA/main/PROD.

## 2026-11-12 - G0-004.G - rechazo dirigido de E2E falso verde (Javier :40)
- Alcance exclusivo del Standard Comercial V3 G0-004.G: casos sintéticos de retry tras fallo, skip por configuración de secreto no disponible, navegador omitido y fallo global de worker. Se comprueba FLAKY_REVIEW/INCOMPLETE/INVALID_REPORT/FAIL; control positivo limpio PASS. No equivale a E2E browser real ni solicita leer secretos.
- Script dirigido `scripts/tests/g0-004-g-directed-negative.test.mjs`, compara clasificador existente sin cambiar API, JWT, empresa, MySQL ni frontend productivo. Reversión: revert protegido exclusivamente de esta prueba y nota de CHANGELOG en dev, nunca force-push.

## 2026-11-12 - G0-004.F - fixture medible de resultado E2E (Javier :40)
- Condicion seleccionada: Playwright JSON con 1 prueba pasada y 1 error global de setup; el verdict esperado es FAIL, la salida CLI es codigo 2 y no reproduce detalles del error. Control positivo: quitar el error global deja PASS. Son fixtures sinteticos y pruebas Node; NO equivalen a una ejecucion E2E real.
- Fuente Plan Comercial V3 G0-004.F; depende de G0-004.A LISTO, reusa clasificador `scripts/quality/playwright-outcome-classifier.mjs`. Regresion y fixture versionados en `scripts/tests/g0-004-f-global-setup-error.test.mjs` y `scripts/tests/fixtures/g0-004-f-global-setup-error.json`; rollback por revert protegido solo del commit directo en dev, sin force ni QA/main/PROD.

## 2026-11-12 - G0-004.A - cierre causal de clasificacion Playwright (Javier :40)
- Fuente Plan Comercial Inicial V3 G0-004.A; reutiliza G0-004.C/D/E LISTO. Corrige falso PASS si Playwright JSON contiene `errors` globales de setup/worker con tests positivos. Reportes con `errors` malformado son INVALID_REPORT. Errores globales solo cuentan numericamente, sin exponer titulos, PII, tokens ni contenido de errores.
- Regresion dirigida Node `scripts/tests/g0-004-a-playwright-global-errors.test.mjs`: positivos limpios, fallos globales, reporte sin pruebas, metadatos invalidos, skips, flakes, CLI no cero y no filtracion. Sin atribuir E2E navegador real, MySQL o auth a pruebas sinteticas.
- Archivos: `scripts/quality/playwright-outcome-classifier.mjs`, test nuevo y este CHANGELOG. Rollback: revert protegido del commit directo en dev, sin reescribir historia ni tocar main/qa/prod. A1-A4 y PLAN/COLA/BITACORA requieren readback exact SHA.

# SOLQARYN — Cambios operativos vigentes

## 2026-11-12 — Preparación documental de Alex 2/2 para eventual 6→8
- IDs Alex existentes preservados; horarios físicos registrados :10/:50 y nombres SECONDARY REVIEW 01/03 no alterados. Propuesta pendiente de decisión del titular: PRIMARY DEV 07 minuto :05, PRIMARY DEV 08 minuto :35, sin tocar las seis Tasks de Javier.
- Se deja handoff para sincronización física por Alex de prompts V13 direct-dev, readback real de identidad/horario/prompt y habilitación posterior únicamente por autorización expresa.
- Cuatro auditorías V13 sin cambios. Sin nuevas Tasks, ramas, PR, cambios funcionales o modificaciones de QA/PROD.


## 2026-11-12 — G0-006.E prueba negativa de fases retiradas (Javier :30)
- Cinco pruebas Node dirigidas al validador de trazabilidad de DEV directo: rechazan FASE8, M13, recibos de PR históricos, estados VALIDANDO disfrazados de LISTO, tests SKIPPED, SHA falso, auditoría A4 ausente y readback institucional no comprobado.
- Casos sintéticos, sin reabrir migraciones ni autorizar trabajo ajeno; el control del Plan Comercial sigue siendo externo al validador de forma intencional.

## 2026-11-12 — G0-004.E regresión de evidencia E2E fail-closed (Javier :30)
- Añadidas ocho pruebas Node sobre clasificador Playwright JSON vigente: reintentos fallidos/flaky, skip por secreto opcional, navegador no soportado, suite anidada, reporte vacío/inválido y CLI con salida distinta de cero; un PASS limpio es el control positivo.
- Pruebas sintéticas sin secretos o datos de empresas; solo cobertura de calidad, no autorización de despliegue ni certificación E2E contra navegador real.

## 2026-11-12 — Visibilidad exacta de VALIDANDO y paralelismo real por Scope
- Precisa la transición inmediata a EN_PROGRESO al reclamar microtarea; VALIDANDO sólo al disponer de candidato completo, ANTES de auditorías funcional, seguridad e integración; misma Task corrige sin abandonar la custodia; commit directo dev, cuarta auditoría posintegración y LISTO verificado. Otras Tasks trabajan en paralelo IDs libres. Meta 1/3 por corrida sin falsos cierres.


## 2026-11-12 — Limpieza definitiva de V13 directo en dev
- Reemplazados capítulos históricos contradictorios por un único contrato directo en docs/AUTOMATION_V13.md.
- Censo original y seis horarios Javier conservados, sin reactivación. Alex permanece fuera del control de la cuenta Javier.
- Cuatro auditorías distintas por microtarea, objetivo operativo mínimo 1 LISTO y meta 3 por corrida, commits fast-forward en dev, resolución causal y readback PLAN/COLA/BITACORA.
- Reglas antiguas de PR protegido, espera de approvals, checks bloqueantes, 52 PR FIFO, microfases y pilotos retiradas de las instrucciones activas.
- La historia anterior sigue disponible a través de commits anteriores de GitHub, pero no forma parte del contrato operativo actual.
- No hay cambios funcionales en frontend, backend, esquemas, QA/main/PROD, secretos, proveedores ni Tasks físicas.

## 2026-11-12 — G0-006.D: trazabilidad de cierre directo por cuatro auditorías
- Añadido verificador asesor y pruebas negativas del contrato requisito → código → pruebas → cuatro auditorías → SHA fast-forward dev → readback PLAN/COLA/BITACORA; un recibo PR-only o incompleto no acredita cierre directo.
- El contador histórico preserva sus eventos anteriores pero reconoce recibos DIRECT_DEV_FOUR_AUDITS únicamente cuando están completos y verificables. No concede LISTO, permisos, push ni modifica seguridad/tenants ni arquitectura.

## 2026-11-12 — G0-005.C: readback de DEV, rutas canonicas
- Delimitada la evidencia de equivalencia Vercel/DEV a scripts/vercel-dev-runtime-equivalence.mjs: las rutas comparadas no pueden tener segmentos vacios, separador Windows, caracteres de control ni terminacion de directorio.
- Anexadas cuatro negativas de la misma prueba offline, conservando positivos y el rechazo de cambios runtime/backend. No se infiere despliegue READY ni se toca QA/main/PROD.
- Rollback: revertir solo este bloque del normalizador y las cuatro pruebas, sin cambios en API, migraciones, datos ni credenciales.

## 2026-11-12 — G0-004.D: clasificar resultados Playwright E2E
- Nuevo CLI scripts/quality/playwright-outcome-classifier.mjs para resumir reportes JSON sin nombres ni datos de tests; PASS exige ejecuciones limpias, FAIL/FLAKY_REVIEW/INCOMPLETE/INVALID_REPORT no se cuentan verde.
- Cuatro tests Node con casos positivos, negativos, flaky, skip, reportes invalidos y exit codes 2/3; sin modificar configuracion de runner, secretos, rutas de tenants, frontend ni backend. Rollback: revertir exclusivamente script y test.

## 2026-11-12 — G0-005.D: validación canónica de manifiesto DEV/Vercel
- `scripts/vercel-dev-runtime-equivalence.mjs`: fail-closed de rutas del manifiesto `frontend_before/after` usando segmento top-level canónico, sin `.`/`..` ni separadores Windows/control. Agregadas 4 negativas al self-test offline, positivos preservados.
- Solo contrato de readback, no prueba de un despliegue READY real. Sin cambios de Vercel, Render, credenciales, datos, frontend/backend ni QA/PROD. Rollback: revertir esta validación y las cuatro negativas.

## 2026-11-12 — G0-004.H: regresión positiva del clasificador Playwright E2E
- Nuevo `scripts/tests/g0-004-h-positive-regression.test.mjs`: caso positivo de tres pruebas PASS anidadas y CLI exit 0, junto con negativos FAIL/FLAKY_REVIEW/INCOMPLETE/INVALID_REPORT y sanitización de títulos/errores sintéticos.
- A1 funcional y A2 seguridad fail-closed; A3 integración: Node 22 `node --test scripts/tests/g0-004-h-positive-regression.test.mjs` 4/4 PASS y `node --check` de test y módulo. Módulo original blob `c5085b044ffb0b4ffa8d971ab3280afb4a02c037` sin cambios; no significa ejecución real de navegador Playwright.
- Cambio limitado a test + CHANGELOG, sin runtime, dependencias, secretos, JWT/RBAC, tenant ni datos. Rollback: revertir sólo test G0-004.H y este bloque aditivo.

## 2026-11-12 — G0-004.B: matriz de recibos G0-004.F/G/H y regresión E2E sintética
- Matriz `docs/quality/g0-004-b-e2e-receipts.md` identifica 10 condiciones, rutas y negativos: setup global, worker error, retries false-green, skips por secret y navegador, éxito anidado y sanitización. Nuevo test `scripts/tests/g0-004-b-quality-receipts.test.mjs`: 4/4 Node22 PASS, source classifier exact blob c5085b; no se presenta como una ejecución real del navegador.
- A1 funcional, A2 seguridad fail-closed y A3 integración Node dirigida PASS; no altera runtime, tenants, JWT/RBAC, secretos o dependencias. Rollback selectivo: revertir este test, la matriz y el párrafo de changelog.
