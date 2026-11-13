# G0-004.B — Matriz de evidencia dirigida del clasificador E2E

Fecha: 2026-11-12. Ambiente y alcance: sólo clasificación **sintética** de JSON Playwright en `dev`; esto no certifica ejecución real Chromium/Playwright, CI del proveedor ni cobertura HTTP/JWT/MySQL.

| Recibo | Requisito comprobado por prueba dirigida | Evidencia en código | Negativo requerido |
| --- | --- | --- | --- |
| G0-004.F | Setup global fallido domina suite con test limpio, control positivo | `scripts/tests/g0-004-f-global-setup-error.test.mjs` | Error global no se convierte en PASS ni filtra texto |
| G0-004.G | Retry fallido, skip por secret no disponible, navegador omitido y worker error | `scripts/tests/g0-004-g-directed-negative.test.mjs` | FLAKY_REVIEW / INCOMPLETE / FAIL, nunca PASS |
| G0-004.H | Tres tests positivos anidados, CLI exit 0 y redacción | `scripts/tests/g0-004-h-positive-regression.test.mjs` | FAIL, FLAKY, SKIPPED, INVALID no se aceptan como éxito |
| G0-004.B | Matriz completa de las 10 condiciones anteriores más regresión directa contra el módulo real | `scripts/tests/g0-004-b-quality-receipts.test.mjs` | 4 pruebas dirigidas con positivos, falsos verdes, errores y sanitización |

Fuente bajo prueba: `scripts/quality/playwright-outcome-classifier.mjs`; sin cambios al código productivo. Todo SKIPPED requiere motivo documentado y se clasifica `INCOMPLETE`, nunca como PASS, sin excepciones implícitas por secretos o navegadores. La matriz relaciona requisitos de clasificación y tests, **no** pretende demostrar que todos los escenarios funcionales E2E de SOLQARYN se ejecutaron. Para acreditar browser E2E usar un run real, sin sustituirlo por esta matriz.

Rollback: revertir exclusivamente el test G0-004.B, esta matriz y la entrada del changelog; no tocar F/G/H, el clasificador, reglas CI, datos o proveedores.
