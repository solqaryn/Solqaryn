# C15-04.M05.04 — A0.90: V23 responsive storefront + foco real

Fecha: 2026-11-21. Repositorio: solqaryn/Solqaryn, rama dev. PROJECT_SCOPE_LOCK=STRICT.
TASK_ID autor: 6af16bb9492c8191b8e8c7d606f11507. Unidad existente en EN_PROGRESO, sin nuevo CLAIM; candado propio V17 SQV17_C15_04_M05_04 sobre PLAN H:I fila 2297.

## A0 — Cambio y motivo

Expande exclusivamente el caso Playwright de tres rutas publicas existentes (/tienda, /tienda/productos, /tienda/categorias) de 375/1280 a cinco anchuras: 320, 390, 768, 1280 y 1440. Agrega asercion de Tab REAL a la primera skip-link por ruta y tamaño, ademas del destino principal, unico h1, cero overflow horizontal del documento y cero solicitudes HTTP mutantes. 15 navegaciones reales previstas. No modifica runtime Angular, backend, datos ni endpoints. Riesgo del incremento R1, de la microtarea global R2.

## A1–A4 y limites

A1: 15 navegaciones, foco por teclado, h1, skip-link y overflow; la suite puede fallar ante regresion. A2: no mutacion HTTP y superficie publica; JWT, RBAC, tenant A/B y API real NO_APLICA al diff de esta prueba (la auditoria global aun exige evidencia independiente). A3: Playwright/Chromium localhost en GitHub-hosted pendiente hasta job exact-SHA. No simular PASS desde tests escritos; builds y Scope Lock deben finalizar SUCCESS. A4: verificar diff, job y check-runs exact-SHA, lectura posterior de PLAN/BITACORA, sin cambiar EN_PROGRESO ni owner hasta satisfacer alcance global. La documentacion A0.90 no acredita WCAG global ni toda la matriz de modulos.

## Referencias V23 condicionales

SKREF-03 Impeccable pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199; SKREF-05 Taste Leonxlnx/taste-skill@ccbc15639c97057cbfcf32ecebc38ef716e4bb37; SKREF-10 Angular angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9, todas desde SKILL.md original. Aplicadas: alcance de UI publica existente, teclado/foco real, responsive, no overflow. SKREF-04 motion, SKREF-06 copy, SKREF-11 threat model y SKREF-12 .NET performance: NO_APLICA, sin esos cambios. Capturas ANTES/DESPUES y zoom 200%: NO_APLICA a este test-only sin cambio visual productivo; no afirmar medicion que no existe.

## Handoff y rollback

Mantener PLAN H2297=EN_PROGRESO, I2297 autor DEV02 y lock V17 propio hasta cierre causal futuro. No nuevo CLAIM con WIP. Tras publicar, revisar check-runs y logs exact-SHA de Chromium, Scope Lock y V5; falla => corregir causa sin simular certificacion. Rollback por revert normal del commit, preservando otros commits. No QA, main, PROD ni secretos.
