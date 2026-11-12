# SOLQARYN — Cambios operativos vigentes

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
