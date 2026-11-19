# C05-04.M05.06 — Regresión proporcional de ligereza, accesibilidad y rendimiento

Autor de esta microtarea: DEV01 Javier, Task 6adbb9fff1548191a1bd5b30d9e9984a. Repositorio `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT. Riesgo R2 por superficie global de empresas y errores de autorización.

## A0: objetivo, impacto y rollback
PLAN_MAESTRO ID C05-04.M05.06. Consumidor Angular `/plataforma` con `PlataformaEmpresasService` y componente de ligereza existente; no cambia UI, DTO, rutas, API, JWT/RBAC ni MySQL. Dependencias C05-04.M04.01–06 y M05.01–05 con A5 real y DAG VALIDADO/G SI. El cambio es únicamente una suite Playwright/Chromium en GitHub-hosted, su workflow y trazabilidad documental. Rollback mediante git revert normal sobre dev, no force-push.

## A1–A3: matriz falsable de pruebas
- 25 empresas de fixture, 320/375/768/1280 viewport emulado, sin overflow horizontal >2px, control 44px, tabla semántica y un solo GET pese a cambios de densidad.
- Carga progresiva: fixture demora el GET, se ve `c0504-loading` y al liberar se presenta vacío, sin consultas duplicadas.
- HTTP 503: mensaje privado no se muestra, no hay éxito, toggle no reintenta; una acción explícita efectúa exactamente un reintento. Sin solicitudes de escritura.
- Anónimo y sesión 403 no exhiben el directorio ni consultan empresas. Son respuestas interceptadas; JWT y grants reales NO quedan certificados.
- Se reportan Navigation Timing y FCP local solo si Chromium los proporciona; una ausencia se consigna NO_MEDIDO. No equivalen a Core Web Vitals de usuarios reales: LCP/CLS/INP de campo NO_MEDIDO.

## A4 y A5
Ejecutar cuatro casos Playwright reales (Chromium GitHub-hosted), lint, build y checks pertinentes de Scope Lock/V5/frontend/seguridad sobre SHA causal. Un workflow QUEUED/IN_PROGRESS/SKIPPED/FAIL nunca es PASS. Validación de dispositivos físicos Android/iPhone/tabletas, desempeño bajo red real, Core Web Vitals de campo y accesibilidad integral quedan SIN_VERIFICAR; viewport emulado NO es dispositivo físico. Por ello este incremento puede permanecer VALIDANDO, nunca declarar LISTO global sólo por cuatro tests. A5 requiere distinta Task Javier y revisión propia. No QA/main/PROD/secretos. No modificar COLA/DAG ni BITACORA histórica.
