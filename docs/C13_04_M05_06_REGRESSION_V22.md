# C13-04.M05.06 — V22 regression visual/responsive, teclado y rendimiento de pantallas publicas

PROJECT_SCOPE_LOCK=STRICT
BRANCH=dev
TASK_ID=6aeacb7820ac8191b7d768a6f33dfa09
RISK=R2_TEST_ONLY
A5=OTRA_TASK_JAVIER

## Salida atomica (PLAN_MAESTRO columna D)

Se agrega una matriz reproducible en GitHub-hosted real Chromium para cinco rutas publicas existentes (login, tienda, productos, categorias, ofertas), tres anchos (320/768/1280) y capturas reales de login/tienda en dos anchos. Se comprueban titulos, geometria sin overflow horizontal, teclado Tab y Enter nativos, guardias anonimas, no mutaciones, reduced-motion y tiempo DCL local medible. Se incluye un control adversarial que inyecta overflow y demuestra que la asercion detecta el defecto.

A0: diff limitado a Playwright/workflow/documentacion/changelog, sin API/DTO/DB/autorizacion/runtime alterados.
A1: cinco tests capaces de fallar ante ruta rota, encabezado falso, reflow deficiente, foco inaccesible, escape de guards y overflow; CI obligatoria en SHA.
A2: rutas privadas bloqueadas para anonimos en Chromium. No constituye certificacion JWT firmado ni RBAC real.
A3: Angular 22.2.1 en localhost + Chromium y pipeline npm lint/build produccion. No hay proveedor productivo ni migraciones MySQL.
A4: GitHub Actions exact SHA, Scope Lock/V5 y Sheets CAS/readback. Con CI QUEUED/IN_PROGRESS mantener VALIDANDO, sin PASS inventado.
A5: revision independiente posterior por otra Task Javier.

## Limites

Screenshots se adjuntan al reporte CI; no representan por si mismas baselines pixel-perfect aprobadas. DCL local no es Core Web Vitals de campo ni p95 productivo. La fase M05 acumulativa de PLAN E no esta certificada por un unico test y no se realizan pagos, operaciones fiscales, HTTP JWT reales, MySQL, PROD, qa ni main. Rollback por git revert normal y estado historico preservado.
