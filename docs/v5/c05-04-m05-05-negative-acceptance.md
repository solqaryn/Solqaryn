# C05-04.M05.05 — Rechazos de seguridad para ligereza del directorio

Responsable: PRIMARY DEV 05, TASK_ID 6adc2e55f47c81919f6374e561687076. PLAN_MAESTRO fila 858. Rama dev, PROJECT_SCOPE_LOCK=STRICT.

## A0 — riesgo y límites
R2 por pruebas de seguridad UI y nuevo workflow CI. Se incorpora E2E independiente Playwright, sin cambios al componente o controles efectivos de backend. DAG819 F=VALIDADO/G=SI, COLA G/O=SI previo claim. Consumidor ruta /plataforma, directorio y controles cosméticos read-only. No nuevos API/DTO, datos, migraciones, secretos ni permisos. Rollback git revert normal.

## A1 y A2 — pruebas falsables
Cinco casos adversariales: anónimo no ve UI privada; sesión 403 niega lectura/render; directorio 403 no muestra diagnóstico ni éxito; tras respuesta 200, un 403 elimina del DOM datos previamente visibles y no emite operaciones HTTP de escritura; tras 200, error 503 elimina datos viejos y no revela error interno. La sesión y el proveedor HTTP se simulan mediante route.fulfill en el navegador. Estos escenarios demuestran UI fail-closed, NO aislamiento real de tenants ni revocación del servidor JWT/RBAC.

## A3, A4 y A5
A3 pertinente: Angular build/lint y cinco Playwright/Chromium reales GitHub-hosted, backend mock explícito. A4: Scope Lock, V5 y checks requeridos completed/success SHA exacto o descendiente con blobs idénticos, readback Sheets/Bitacora. QUEUED/IN_PROGRESS/SKIPPED/FAILED nunca PASS. JWT/MFA/RBAC server, tenant A/B, MySQL, proveedor real y Core Web Vitals NO_MEDIDO/NO_APLICA. A5 únicamente otra Task Javier, no autor. No QA/main/PROD.
