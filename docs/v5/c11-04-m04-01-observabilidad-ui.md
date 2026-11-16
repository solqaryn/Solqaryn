# SOLQARYN V17 — C11-04.M04.01: superficie privada de observabilidad

Autor/owner: PRIMARY DEV 03 JAVIER, 6adc2e4bd8a48191ac7d8641ed95a1a3. Plan fila 1712; candado V17 SQV17_C11_04_M04_01 / ID 2000001712. Riesgo R2 por guía de métricas y autorización multiempresa.

## A0
Nueva ruta lazy protegida por plataformaGuard, /plataforma/observabilidad-v5. UI de solo lectura con cuatro superficies de revisión (logs seguros, métricas/costos, trazas/latencia, alertas/diagnóstico). El contenido se oculta con señal de sesión de plataforma revocada; no ejecuta consultas, workers, exporters o cambios de configuración. No certifica SLO, costos ni datos en vivo.

## A1–A3
Cinco Vitest de ruta y denegación, sesión plataforma, empresa forjada y revocación; tres Chromium reales con token y API sintéticos, 403, anónimo y prueba móvil de 375px con cero peticiones operativas. GitHub-hosted Node24/npm11 debe acreditar npm ci, lint, build, Vitest y Playwright. Mocks no acreditan JWT/RBAC HTTP, MySQL/EF, observabilidad viva ni proveedores. Para estos efectos corresponde NO_APLICA a diff frontend; SLO/RTO/latencia siguen NO_MEDIDO.

## A4–A5
CI dirigida, Scope Lock y procedencia V5 exigidos completed/success exact-SHA, con lectura del plan y BITACORA. En espera externa preservar VALIDANDO y owner, SHA, RUN y JOB; A5 sólo por revisor de Task Javier distinto. Rollback commit normal, no QA/main/PROD.
