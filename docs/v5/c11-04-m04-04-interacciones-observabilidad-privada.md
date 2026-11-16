# C11-04.M04.04 — Navegación, mensajes y prevención de doble acción

Repositorio solqaryn/Solqaryn@dev · PROJECT_SCOPE_LOCK=STRICT. Autor DEV03 Javier, TASK_ID 6adc2e4bd8a48191ac7d8641ed95a1a3. Riesgo R1 (interfaz Angular).

## A0
La ruta privada /plataforma/observabilidad-v5 conserva plataformaGuard, estados y visibilidad dependiente de sesión backend. Se añade navegación semántica que contiene enlace de regreso y botón tipo button, con conexión accesible aria-describedby al estado aria-live y disposición flexible. No existen formularios de escritura, exporters, comandos de auditoría, métricas conectadas o autorización basada en branding.

## A1
Se añaden dos Vitest para comprobar navegación, enlace, ausencia de formularios, accesibilidad del mensaje, prevención de llamadas duplicadas, suspensión de la vista durante loading, 503 sin exposición de payload y restablecimiento del botón. Se añaden dos Playwright Chromium reales con sesión API sintética para nav/estado y una solicitud en red demorada: el botón se deshabilita y sólo se inicia un GET de revalidación adicional.

## A2/A3
Se preservan controles de sesión y revocación originales; ningún valor visual se convierte en JWT, permiso o tenant. Chromium se ejecuta con API sintética en localhost y no acredita autenticación HTTP/MySQL o pentest real. MySQL/EF, integración de proveedor y cambios API: NO_APLICA motivado. La CI dirigida ya existente ejecuta pruebas previas y nuevas, lint/build Angular en GitHub-hosted.

## A4/A5
Exigir checks completed/success en SHA exacto para Vitest/Chromium dirigido, frontend tipado/build, Scope Lock y procedencia V5; no contar queued/skipped/cancelled/in_progress como PASS. Conservar VALIDANDO con RUN/SHA si CI externa sigue en ejecución. A5 independiente de otra Task Javier, no autopase. Rollback con git revert normal sin force-push ni QA/main/PROD.
