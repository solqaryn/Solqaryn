# SOLQARYN V17 — C14-04.M04.05 accesibilidad de preparación de salida DEV

**Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Repo:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`; PLAN fila 2148, candado V17 2000002148.

## A0 / alcance y riesgo R2 conservador
La vista privada existente `/plataforma/flujo-v5` incluye un panel de preparación con cuatro condiciones declarativas `SIN_VERIFICAR`/`NO_MEDIDO`. Esta microtarea **solo** añade pruebas E2E reales de accesibilidad y amplía la suite Chromium SHA-aislada existente, sin activar release, DNS, respaldos, R2, proveedores, HTTP, JWT/RBAC, tenant, DB/EF o producción. No cambia código de la UI ni su guard; diseño de panel read-only sin acciones. Rollback por git revert normal.

## A1, A2 y A3
Cuatro casos Playwright: denegación anónima/403 sintética sin fuga; responsive 320/375/768/1280 con cuatro puertas sin overflow horizontal; semántica `aria-labelledby`, lista con nombre accesible, nota y estados `SIN_VERIFICAR`/`NO_MEDIDO`, sin botón, formulario, link ni foco atrapado dentro del panel; y cálculo real de contraste del estado `NO_MEDIDO` con umbral >=4.5:1. Los checks fallan ante exposición indebida, desbordamiento, ausencia de etiquetas, falsificación de PASS o bajo contraste. No hay elementos interactivos en el panel: foco visible y área táctil de 44px internos se registran NO_APLICA justificado, sin inventar control.

A3 relevante: Angular lint/build y Chromium GitHub-hosted, conservando la batería antigua de tests. A2 solo denegación UI con sesión sintética; **NO** certifica JWT HTTP, MFA, tenant A/B backend, privilegios productivos, MySQL, restore ni salud real de infraestructura.

## A4 y A5
Exigir checks `completed/success` del SHA pertinente, Scope Lock, procedencia V5, aceptación global cuando corresponda, y readback PLAN/COLA/DAG/BITACORA. `SKIPPED/QUEUED/IN_PROGRESS` no son PASS. Revisión A5 exclusivamente por otra Task Javier distinta, nunca autoaprobación. Sin QA/main/PROD ni cambios irreversibles.
