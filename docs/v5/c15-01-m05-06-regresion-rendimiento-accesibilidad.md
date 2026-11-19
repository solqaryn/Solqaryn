# C15-01.M05.06 — Regresión, rendimiento y accesibilidad proporcionales (V22 DEV06)

PLATFORM=SOLQARYN; REPOSITORY=solqaryn/Solqaryn; BRANCH=dev; PROJECT_SCOPE_LOCK=STRICT. AUTHOR_TASK_ID=6af16bbad26881918c283abe06e1f32a. Riesgo R2 por superficie privada; cambio test-only + CI sin implementación del producto. PLAN_MAESTRO!D2191 pide ejecutar regresión, rendimiento y accesibilidad; PLAN!E2191 es meta acumulativa M05 y no autoriza certificar todos los 2340 requisitos con este solo test.

## A0 — Alcance e impacto
Superficie ya existente /plataforma/trazabilidad-cobertura-v5, Angular 22.2.1 con PlataformaSesionService y mensaje NO_MEDIDO. Este changeset crea test Playwright, workflow GitHub-hosted, este documento y entrada aditiva CHANGELOG. No cambia componente, rutas, RBAC, JWT, tenancy, backend .NET, EF/MySQL, DTO, datos, API ni infraestructura. Las fórmulas de COLA/DAG permanecen intactas. Rollback por git revert normal, sin fuerza ni borrar BITACORA.

## A1 — Tres pruebas Chromium nuevas y medidas delimitadas
1. Navegaciones reales de UI a 320, 768 y 1280 px, registra elapsedMs y domContentLoadedEventEnd, presupuesto de laboratorio 25 segundos por página, cero overflow y cero HTTP mutador. No certifica Core Web Vitals de usuarios, throughput, p95 o PROD.
2. Tab real desde inicio, foco visible >=3 px, blanco >=44x44 px, landmarks/ARIA y 4 dimensiones marcadas SIN_VERIFICAR; no proclama WCAG global.
3. Negativos 403 y 503 simulados de sesión, retorno a login y cero exposición de dimensiones/mensaje sensible; posterior sesión sana aislada muestra 4 dimensiones NO_MEDIDO.

## A2/A3 — Seguridad e integración real versus mocks
GitHub-hosted ejecuta Node 24, npm 11, Angular tipado/build y Playwright/Chromium real, con log de tiempos y artefactos. Sesión interceptada es SINTÉTICA, nunca JWT firmado ni autorización backend, tenant A/B, MySQL 8.4, IDOR/BOLA o entorno productivo; no equivale a esas pruebas. Como diff solo cambia test/workflow/documento, esas nuevas integraciones runtime son NO_APLICA para este ID y siguen pendientes en fases que las requieran. Ninguna credencial real, pago, UI de QA o cambio de permisos.

## A4/A5 — Requisitos para transición
Exigir GitHub Actions exact-SHA de workflow V22 C15-01.M05.06: 3/3 Chromium aprobadas más tipado/build, verify-solqaryn-project-scope y V5 provenance completed/success; checks obligatorios pertinentes SUCCESS. QUEUED, IN_PROGRESS, CANCELLED, SKIPPED y FAIL jamás PASS. Tras publicar conservar H=VALIDANDO mientras CI externa concluya, registrar SHA/run/check y readback PLAN/COLA/DAG/BITACORA bajo V17; al completar A0-A4 reales CAS a LISTO conservando owner. A5 revisión independiente de otra Task Javier diferente de DEV06, sin aprobarse a sí misma; no QA/main/PROD.