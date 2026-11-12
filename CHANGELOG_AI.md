## 2026-11-12 - G0-004.F - fixture medible de resultado E2E (Javier :40)
- Condicion seleccionada: Playwright JSON con 1 prueba pasada y 1 error global de setup; el verdict esperado es FAIL, la salida CLI es codigo 2 y no reproduce detalles del error. Control positivo: quitar el error global deja PASS. Son fixtures sinteticos y pruebas Node; NO equivalen a una ejecucion E2E real.
- Fuente Plan Comercial V3 G0-004.F; depende de G0-004.A LISTO, reusa clasificador `scripts/quality/playwright-outcome-classifier.mjs`. Regresion y fixture versionados en `scripts/tests/g0-004-f-global-setup-error.test.mjs` y `scripts/tests/fixtures/g0-004-f-global-setup-error.json`; rollback por revert protegido solo del commit directo en dev, sin force ni QA/main/PROD.

## 2026-11-12 - G0-004.A - cierre causal de clasificacion Playwright (Javier :40)
- Fuente Plan Comercial Inicial V3 G0-004.A; reutiliza G0-004.C/D/E LISTO. Corrige falso PASS si Playwright JSON contiene `errors` globales de setup/worker con tests positivos. Reportes con `errors` malformado son INVALID_REPORT. Errores globales solo cuentan numericamente, sin exponer titulos, PII, tokens ni contenido de errores.
- Regresion dirigida Node `scripts/tests/g0-004-a-playwright-global-errors.test.mjs`: positivos limpios, fallos globales, reporte sin pruebas, metadatos invalidos, skips, flakes, CLI no cero y no filtracion. Sin atribuir E2E navegador real, MySQL o auth a pruebas sinteticas.
- Archivos: `scripts/quality/playwright-outcome-classifier.mjs`, test nuevo y este CHANGELOG. Rollback: revert protegido del commit directo en dev, sin reescribir historia ni tocar main/qa/prod. A1-A4 y PLAN/COLA/BITACORA requieren readback exact SHA.

# SOLQARYN — Cambios operativos vigentes

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
