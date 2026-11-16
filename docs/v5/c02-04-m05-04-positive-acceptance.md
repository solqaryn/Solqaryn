# C02-04.M05.04 — aceptación positiva de la consola global en DEV

**Gobierno:** SOLQARYN en dev, PROJECT_SCOPE_LOCK=STRICT, owner DEV03 TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3; PLAN_MAESTRO fila 425; candado V17 SQV17_C02_04_M05_04 / 2000000425. Riesgo R2 por interfaz protegida, autorización y CI nueva.

## A0: alcance, consumidores y rollback
Se prueba el catálogo contractual existente C02-04.M05.01 (13 rutas GV01–GV13) contra la aplicación Angular 22.2.1 real bajo Chromium GitHub-hosted. No se crea nueva autoridad: la ruta pasa por el guard existente y consulta `/plataforma/sesion`. El endpoint se intercepta con una **sesión sintética**; por tanto, no sustituye JWT/MFA/RBAC HTTP real, datos A/B reales, MySQL 8.4, auditoría durable, grants ni acceso productivo. No se toca API, EF, secretos, DB, entornos ni despliegue. Rollback: git revert fast-forward de este changeset y conservar BITACORA.

## A1–A3: casos causales
El suite verifica catálogo versionado de 13 IDs y rutas únicas; cada ruta positiva debe renderizar su encabezado privado esperado tras GET de sesión simulado 200, sin POST/PUT/PATCH/DELETE. La consola global muestra cuatro controles, revalida tras un segundo GET y no desborda a 320 px. La negativa simula 403 y exige redirección, ocultación de controles y diagnóstico privado. Cero mocks de navegador: Playwright corre en Chromium real; **solo el servidor de sesión está simulado**. Es integración Angular/Router/Guard/Component real, no autenticación HTTP ni integración MySQL.

## A4–A5 y no certificaciones
La CI dirigida usa npm ci, lint, build Angular productivo y Playwright, aislada por SHA con cancel-in-progress=false; comprobar SUCCESS exact-SHA, Scope Lock, V5, seguridad/frontend y aceptación integral si resulta requisito pertinente, sin reutilizar SKIPPED/CANCELLED como PASS. Dejar VALIDANDO mientras externa QUEUED/IN_PROGRESS y registrar run/owner/SHA, sin liberar DAG por anticipado. A5 únicamente otra Task Javier distinta de DEV03. Core Web Vitals reales, latencia de red y aislamiento tenant HTTP siguen NO_MEDIDO.
