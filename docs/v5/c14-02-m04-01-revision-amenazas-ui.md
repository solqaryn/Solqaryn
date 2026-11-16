# SOLQARYN V17 — C14-02.M04.01: superficie privada de revisión de amenazas

**Task autora y owner:** 6adc2e4bd8a48191ac7d8641ed95a1a3 / JAVIER_CHATGPT. **Plan:** fila 2072. **Exclusión:** candado V17 SQV17_C14_02_M04_01 ID 2000002072. **Riesgo:** R2, autorización y tenancy UI.

## A0
Nueva ruta lazy privada /plataforma/revision-amenazas-v5, protegida por plataformaGuard. Una guía no operativa de cuatro frentes: BOLA/IDOR, sesión/suplantación, archivos/jobs/caché y host/backup. Solo muestra contenido cuando la señal PlataformaSesionService.sesion() confirme administrador de plataforma tras revalidación del backend. No ofrece endpoints, formularios, mutaciones, restauraciones, roles ni nuevas integraciones.

## A1 a A3
Cinco casos Vitest unitarios sobre ruta protegida, grant anónimo, empresa denegada, plataforma permitida y revocación; tres pruebas reales Playwright Chromium con token/API sintéticos en 390px para navegador. Requiere GitHub-hosted npm ci, lint, build. Los mocks no acreditan JWT HTTP, seguridad A/B real, MySQL EF, recuperación ni pentest. Esta UI no certifica el resultado de las pruebas de penetración: NO_MEDIDO. Ningún proveedor real aplica al diff.

## A4, A5 y rollback
Publicar individualmente en dev mediante fast-forward CAS; acreditar Scope Lock, procedencia CHANGELOG V5, CI dirigida del SHA exacto, PLAN/COLA/BITACORA con readback, y A5 por otra Task Javier. Si CI externa queued/in_progress, conservar VALIDANDO y SHA/run/job sin contabilizar PASS. Revert normal, nunca forzar history. QA/main/PROD no autorizados.
